using System.Net.Http.Json;
using System.Text.Json;
using CryptoGPT.Core.Interfaces;
using CryptoGPT.Core.Models;
using Microsoft.Extensions.Logging;
using CoinGecko.Net.Objects.Models;
using System.Text.Json.Serialization;

namespace CryptoGPT.Services
{
    /// <summary>
    /// Implementation of ICoinGeckoService using CoinGecko API, aligning with Python service URLs
    /// </summary>
    public class CoinGeckoService : ICoinGeckoService
    {
        private readonly HttpClient _httpClient;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CoinGeckoService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        // Base API URL
        private readonly string _baseUrl = "https://api.coingecko.com/api/v3/";

        // API endpoints (aligned with Python and CoinGecko docs)
        // https://www.coingecko.com/api/documentation
        private const string CoinsMarketsEndpoint = "coins/markets"; // GET /coins/markets

        private const string CoinEndpoint = "coins/{0}"; // GET /coins/{id}
        private const string MarketChartEndpoint = "coins/{0}/market_chart"; // GET /coins/{id}/market_chart

        // Cache keys
        private const string TopCoinsKey = "top_coins_{0}";

        private const string CoinDataKey = "coin_data_{0}";
        private const string MarketChartKey = "market_chart_{0}_{1}";

        private DateTime _lastRequestTime = DateTime.MinValue;
        private readonly TimeSpan _minRequestInterval = TimeSpan.FromSeconds(1.5);
        private bool _rateLimited = false;
        private DateTime _rateLimitUntil = DateTime.MinValue;
        private const int RateLimitResetSeconds = 60;

        public CoinGeckoService(
            HttpClient httpClient,
            ICacheService cacheService,
            ILogger<CoinGeckoService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Configure HttpClient
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "CryptoGPT.Net");

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        private async Task RateLimitAsync()
        {
            var now = DateTime.UtcNow;
            var timeSinceLast = now - _lastRequestTime;
            if (timeSinceLast < _minRequestInterval)
            {
                await Task.Delay(_minRequestInterval - timeSinceLast);
            }
            _lastRequestTime = DateTime.UtcNow;
        }

        private bool HandleRateLimitError(HttpRequestException ex)
        {
            if (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                (ex.Message != null && (ex.Message.Contains("429") || ex.Message.ToLower().Contains("too many requests"))))
            {
                _rateLimited = true;
                _rateLimitUntil = DateTime.UtcNow.AddSeconds(RateLimitResetSeconds);
                _logger.LogWarning($"CoinGecko rate limit detected. Will not retry until {_rateLimitUntil:O}");
                return true;
            }
            return false;
        }

        private bool IsRateLimited()
        {
            if (_rateLimited && DateTime.UtcNow > _rateLimitUntil)
            {
                _rateLimited = false;
            }
            return _rateLimited;
        }

        public async Task<List<CryptoCurrency>> GetTopCoinsAsync(int limit = 10)
        {
            if (IsRateLimited())
                throw new HttpRequestException("CoinGecko rate limited", null, System.Net.HttpStatusCode.TooManyRequests);
            try
            {
                await RateLimitAsync();
                string cacheKey = string.Format(TopCoinsKey, limit);
                var cachedResult = await _cacheService.GetAsync<List<CryptoCurrency>>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogInformation("Retrieved top {Count} coins from cache", cachedResult.Count);
                    return cachedResult;
                }

                string requestUrl = $"{CoinsMarketsEndpoint}?vs_currency=usd&order=market_cap_desc&per_page={limit}&page=1&sparkline=false";
                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();
                var coinDataList = await response.Content.ReadFromJsonAsync<List<CoinGeckoMarket>>(_jsonOptions);
                if (coinDataList == null)
                {
                    _logger.LogWarning("Failed to deserialize CoinGecko response");
                    return new List<CryptoCurrency>();
                }
                var result = coinDataList.Select(MapToCryptoCurrency).ToList();
                await _cacheService.SetAsync(cacheKey, result, 300); // 5 min
                _logger.LogInformation("Retrieved top {Count} coins from CoinGecko API", result.Count);
                return result;
            }
            catch (HttpRequestException ex)
            {
                if (HandleRateLimitError(ex))
                    throw;
                _logger.LogError(ex, "Error fetching top coins from CoinGecko: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<CryptoCurrencyDetail?> GetCoinDataAsync(string coinId)
        {
            if (IsRateLimited())
                throw new HttpRequestException("CoinGecko rate limited", null, System.Net.HttpStatusCode.TooManyRequests);
            try
            {
                await RateLimitAsync();
                string cacheKey = string.Format(CoinDataKey, coinId);
                var cachedResult = await _cacheService.GetAsync<CryptoCurrencyDetail>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogInformation("Retrieved coin data for {CoinId} from cache", coinId);
                    return cachedResult;
                }
                string requestUrl = string.Format(CoinEndpoint, coinId) + "?localization=false&tickers=false&market_data=true&community_data=true&developer_data=false&sparkline=false";
                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();
                var coinData = await response.Content.ReadFromJsonAsync<CoinGeckoAssetDetails>(_jsonOptions);
                if (coinData == null)
                {
                    _logger.LogWarning("Failed to deserialize CoinGecko detail response");
                    return null;
                }
                var result = MapToCryptoCurrencyDetail(coinData);
                await _cacheService.SetAsync(cacheKey, result, 900); // 15 min
                _logger.LogInformation("Retrieved coin data for {CoinId} from CoinGecko API", coinId);
                return result;
            }
            catch (HttpRequestException ex)
            {
                if (HandleRateLimitError(ex))
                    throw;
                _logger.LogError(ex, "Error fetching coin data from CoinGecko: {Message}", ex.Message);
                return null;
            }
        }

        public async Task<MarketHistory> GetMarketChartAsync(string coinId, int days = 30)
        {
            if (IsRateLimited())
                throw new HttpRequestException("CoinGecko rate limited", null, System.Net.HttpStatusCode.TooManyRequests);
            try
            {
                await RateLimitAsync();
                string cacheKey = string.Format(MarketChartKey, coinId, days);
                var cachedResult = await _cacheService.GetAsync<MarketHistory>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogInformation("Retrieved market chart for {CoinId} ({Days} days) from cache", coinId, days);
                    return cachedResult;
                }
                string requestUrl = string.Format(MarketChartEndpoint, coinId) + $"?vs_currency=usd&days={days}";
                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();
                var marketData = await response.Content.ReadFromJsonAsync<CoinGeckoMarketChart>(_jsonOptions);
                if (marketData == null)
                {
                    _logger.LogWarning("Failed to deserialize CoinGecko market chart response");
                    return new MarketHistory { CoinId = coinId };
                }
                var result = new MarketHistory
                {
                    CoinId = coinId,
                    Symbol = coinId, // Could be improved by fetching symbol
                    Prices = ConvertToPoints(marketData.Prices),
                    MarketCaps = ConvertToPoints(marketData.MarketCaps),
                    Volumes = ConvertToPoints(marketData.TotalVolumes)
                };
                int cacheTime = days <= 1 ? 300 : 3600;
                await _cacheService.SetAsync(cacheKey, result, cacheTime);
                _logger.LogInformation("Retrieved market chart for {CoinId} ({Days} days) from CoinGecko API", coinId, days);
                return result;
            }
            catch (HttpRequestException ex)
            {
                if (HandleRateLimitError(ex))
                    throw;
                _logger.LogError(ex, "Error fetching market chart from CoinGecko: {Message}", ex.Message);
                return new MarketHistory { CoinId = coinId };
            }
        }

        public async Task<MarketOverview> GetMarketOverviewAsync()
        {
            var requestUrl = $"{CoinsMarketsEndpoint}?vs_currency=usd&order=market_cap_desc&per_page=100&page=1&sparkline=false";

            var response = await _httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();
            var coinDataList = await response.Content.ReadFromJsonAsync<List<CoinGeckoMarket>>(_jsonOptions) ?? new List<CoinGeckoMarket>();
            var coins = coinDataList.Select(MapToCryptoCurrency).ToList();

            // Top gainers (by 24h % change)
            var topGainers = coins.OrderByDescending(c => c.PriceChangePercentage24h).Take(10).ToList();
            // Top losers (by 24h % change)
            var topLosers = coins.OrderBy(c => c.PriceChangePercentage24h).Take(10).ToList();
            // Top by volume
            var topByVolume = coins.OrderByDescending(c => c.Volume24h).Take(10).ToList();

            // Fetch global market metrics
            var globalUrl = "global";
            var globalResponse = await _httpClient.GetAsync(globalUrl);
            globalResponse.EnsureSuccessStatusCode();
            var globalData = await globalResponse.Content.ReadFromJsonAsync<CoinGeckoGlobalDataResponse>(_jsonOptions);
            var metrics = new Dictionary<string, decimal?>();
            if (globalData?.Data != null)
            {
                if (globalData.Data.TotalMarketCap != null && globalData.Data.TotalMarketCap.TryGetValue("usd", out var totalCap))
                    metrics["total_market_cap_usd"] = totalCap;
                if (globalData.Data.TotalVolume != null && globalData.Data.TotalVolume.TryGetValue("usd", out var totalVol))
                    metrics["total_volume_usd"] = totalVol;
                if (globalData.Data.MarketCapPercentage != null && globalData.Data.MarketCapPercentage.TryGetValue("btc", out var btcDom))
                    metrics["btc_dominance"] = btcDom;
                if (globalData.Data.MarketCapPercentage != null && globalData.Data.MarketCapPercentage.TryGetValue("eth", out var ethDom))
                    metrics["eth_dominance"] = ethDom;
            }

            return new MarketOverview
            {
                TopGainers = topGainers,
                TopLosers = topLosers,
                TopByVolume = topByVolume,
                MarketMetrics = metrics
            };
        }

        #region Helper Methods

        private CryptoCurrency MapToCryptoCurrency(CoinGeckoMarket coin)
        {
            return new CryptoCurrency
            {
                Id = coin.Id ?? string.Empty,
                Symbol = coin.Symbol?.ToUpper() ?? string.Empty,
                Name = coin.Name ?? string.Empty,
                CurrentPrice = coin.CurrentPrice,
                MarketCap = coin.MarketCap,
                PriceChangePercentage24h = Convert.ToDecimal(coin.PriceChangePercentage24h ?? 0),
                MarketCapRank = coin.MarketCapRank,
                Volume24h = coin.TotalVolume,
                ImageUrl = coin.Image ?? string.Empty
            };
        }

        private CryptoCurrencyDetail MapToCryptoCurrencyDetail(CoinGeckoAssetDetails detail)
        {
            return new CryptoCurrencyDetail
            {
                Id = detail.Id,
                Symbol = detail.Symbol?.ToUpper() ?? string.Empty,
                Name = detail.Name ?? string.Empty,
                Description = detail.Description.GetValueOrDefault("en"),
                ImageUrl = detail.Image?.Large ?? string.Empty,
                Homepage = detail.Links.Homepage.FirstOrDefault(),
                Whitepaper = detail.Links.WhitePaper,
                BlockchainSite = detail.Links.BlockchainSites.FirstOrDefault(),
                Twitter = detail.Links.TwitterScreenName,
                Facebook = detail.Links.FacebookName,
                Subreddit = detail.Links.SubredditUrl,
                Categories = detail.Categories?.ToList(),
                HashingAlgorithm = detail.HashingAlgorithm,
                GenesisDate = detail.GenesisDate?.ToString(),
                SentimentVotesUpPercentage = detail.SentimentVotesUpPercentage,
                SentimentVotesDownPercentage = detail.SentimentVotesDownPercentage,
                CurrentPrice = detail.MarketData.CurrentPrice.GetValueOrDefault("usd"),
                MarketCap = detail.MarketData.MarketCaps.GetValueOrDefault("usd"),
                PriceChangePercentage24h = detail.MarketData.PriceChangePercentage24h,
                MarketCapRank = detail.MarketCapRank,
                Volume24h = detail.MarketData.TotalVolumes.GetValueOrDefault("usd"),
                CirculatingSupply = detail.MarketData.CirculatingSupply ?? 0,
                TotalSupply = detail.MarketData.TotalSupply,
                MaxSupply = detail.MarketData.MaxSupply,
                AllTimeHigh = detail.MarketData.AllTimeHighs.GetValueOrDefault("usd"),
                AllTimeHighDate = detail.MarketData.AllTimeHighDates.GetValueOrDefault("usd"),
                AllTimeLow = detail.MarketData.AllTimeLows.GetValueOrDefault("usd"),
                AllTimeLowDate = detail.MarketData.AllTimeLowDates.GetValueOrDefault("usd"),
                High24h = detail.MarketData.High24h.GetValueOrDefault("usd"),
                Low24h = detail.MarketData.Low24h.GetValueOrDefault("usd")
            };
        }

        private List<PriceHistoryPoint> ConvertToPoints(IEnumerable<CoinGeckoMarketChartValue> dataPoints)
        {
            if (dataPoints == null)
                return new List<PriceHistoryPoint>();

            return dataPoints.Select(point => new PriceHistoryPoint
            {
                Timestamp = new DateTimeOffset(point.Timestamp).ToUnixTimeMilliseconds(),
                Price = point.Value
            }).ToList();
        }

        #endregion Helper Methods

        public class CoinGeckoGlobalDataResponse
        {
            [JsonPropertyName("data")]
            public CoinGeckoGlobalData? Data { get; set; }
        }
    }
}