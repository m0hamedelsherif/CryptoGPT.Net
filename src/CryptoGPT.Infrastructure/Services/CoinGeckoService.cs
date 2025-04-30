using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CryptoGPT.Application.Interfaces;
using CryptoGPT.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CryptoGPT.Infrastructure.Services
{
    /// <summary>
    /// Implementation of ICoinGeckoService using CoinGecko API
    /// </summary>
    public class CoinGeckoService : ICoinGeckoService
    {
        private readonly HttpClient _httpClient;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CoinGeckoService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        // Base API URL
        private readonly string _baseUrl = "https://api.coingecko.com/api/v3/";

        // API endpoints
        private const string CoinsMarketsEndpoint = "coins/markets";
        private const string CoinEndpoint = "coins/{0}";
        private const string MarketChartEndpoint = "coins/{0}/market_chart";

        // Cache keys
        private const string TopCoinsKey = "top_coins_{0}";
        private const string CoinDataKey = "coin_data_{0}";
        private const string MarketChartKey = "market_chart_{0}_{1}";

        // Rate limiting
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
                
                // Try to get from cache first
                var cachedResult = await _cacheService.GetAsync<List<CryptoCurrency>>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogInformation("Retrieved top {Count} coins from cache", cachedResult.Count);
                    return cachedResult;
                }

                // If not in cache, fetch from API
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
                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5)); // 5 min cache
                
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
                
                // Try to get from cache first
                var cachedResult = await _cacheService.GetAsync<CryptoCurrencyDetail>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogInformation("Retrieved coin data for {CoinId} from cache", coinId);
                    return cachedResult;
                }

                // If not in cache, fetch from API
                string requestUrl = string.Format(CoinEndpoint, coinId) + 
                    "?localization=false&tickers=false&market_data=true&community_data=true&developer_data=false&sparkline=false";
                
                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();
                
                var coinData = await response.Content.ReadFromJsonAsync<CoinGeckoAssetDetails>(_jsonOptions);
                if (coinData == null)
                {
                    _logger.LogWarning("Failed to deserialize CoinGecko detail response");
                    return null;
                }
                
                var result = MapToCryptoCurrencyDetail(coinData);
                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15)); // 15 min cache
                
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
                
                // Try to get from cache first
                var cachedResult = await _cacheService.GetAsync<MarketHistory>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogInformation("Retrieved market chart for {CoinId} ({Days} days) from cache", coinId, days);
                    return cachedResult;
                }

                // If not in cache, fetch from API
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
                    Symbol = coinId,
                    Prices = ConvertToPoints(marketData.Prices),
                    MarketCaps = ConvertToPoints(marketData.MarketCaps),
                    Volumes = ConvertToPoints(marketData.TotalVolumes)
                };
                
                // Cache time depends on the timeframe
                TimeSpan cacheTime = days <= 1 ? 
                    TimeSpan.FromMinutes(5) : 
                    TimeSpan.FromHours(1);
                
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

        #region Helper Methods and Model Classes

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
                Description = detail.Description?.GetValueOrDefault("en") ?? string.Empty,
                ImageUrl = detail.Image?.Large ?? string.Empty,
                Homepage = detail.Links?.Homepage?.FirstOrDefault() ?? string.Empty,
                Whitepaper = detail.Links?.WhitePaper ?? string.Empty,
                BlockchainSite = detail.Links?.BlockchainSites?.FirstOrDefault() ?? string.Empty,
                Twitter = detail.Links?.TwitterScreenName ?? string.Empty,
                Facebook = detail.Links?.FacebookName ?? string.Empty,
                Subreddit = detail.Links?.SubredditUrl ?? string.Empty,
                Categories = detail.Categories?.ToList() ?? new List<string>(),
                HashingAlgorithm = detail.HashingAlgorithm,
                GenesisDate = detail.GenesisDate?.ToString(),
                SentimentVotesUpPercentage = detail.SentimentVotesUpPercentage,
                SentimentVotesDownPercentage = detail.SentimentVotesDownPercentage,
                CurrentPrice = detail.MarketData?.CurrentPrice?.GetValueOrDefault("usd") ?? 0,
                MarketCap = detail.MarketData?.MarketCaps?.GetValueOrDefault("usd") ?? 0,
                PriceChangePercentage24h = detail.MarketData?.PriceChangePercentage24h,
                MarketCapRank = detail.MarketCapRank.HasValue ? (int?)Convert.ToInt32(detail.MarketCapRank) : null,
                Volume24h = detail.MarketData?.TotalVolumes?.GetValueOrDefault("usd") ?? 0,
                CirculatingSupply = detail.MarketData?.CirculatingSupply ?? 0,
                TotalSupply = detail.MarketData?.TotalSupply,
                MaxSupply = detail.MarketData?.MaxSupply,
                AllTimeHigh = detail.MarketData?.AllTimeHighs?.GetValueOrDefault("usd") ?? 0,
                AllTimeHighDate = detail.MarketData?.AllTimeHighDates?.GetValueOrDefault("usd") ?? DateTime.MinValue,
                AllTimeLow = detail.MarketData?.AllTimeLows?.GetValueOrDefault("usd") ?? 0,
                AllTimeLowDate = detail.MarketData?.AllTimeLowDates?.GetValueOrDefault("usd") ?? DateTime.MinValue,
                High24h = detail.MarketData?.High24h?.GetValueOrDefault("usd"),
                Low24h = detail.MarketData?.Low24h?.GetValueOrDefault("usd")
            };
        }

        private List<PriceHistoryPoint> ConvertToPoints(List<List<decimal>>? dataPoints)
        {
            if (dataPoints == null)
                return new List<PriceHistoryPoint>();

            return dataPoints.Select(point => new PriceHistoryPoint
            {
                Timestamp = Convert.ToInt64(point[0]),
                Price = point[1]
            }).ToList();
        }

        // Model classes for CoinGecko API responses
        
        public class CoinGeckoMarket
        {
            public string? Id { get; set; }
            public string? Symbol { get; set; }
            public string? Name { get; set; }
            public string? Image { get; set; }
            public decimal CurrentPrice { get; set; }
            public decimal MarketCap { get; set; }
            public decimal? MarketCapRank { get; set; }
            public decimal? PriceChangePercentage24h { get; set; }
            public decimal TotalVolume { get; set; }
        }
        
        public class CoinGeckoAssetDetails
        {
            public string Id { get; set; } = string.Empty;
            public string? Symbol { get; set; }
            public string? Name { get; set; }
            public Dictionary<string, string>? Description { get; set; }
            public ImageData? Image { get; set; }
            public decimal? MarketCapRank { get; set; }
            public string? HashingAlgorithm { get; set; }
            public DateTime? GenesisDate { get; set; }
            public decimal? SentimentVotesUpPercentage { get; set; }
            public decimal? SentimentVotesDownPercentage { get; set; }
            public MarketDataInfo? MarketData { get; set; }
            public LinksData? Links { get; set; }
            public List<string>? Categories { get; set; }
            
            public class ImageData
            {
                public string? Thumb { get; set; }
                public string? Small { get; set; }
                public string? Large { get; set; }
            }
            
            public class LinksData
            {
                public List<string>? Homepage { get; set; }
                public string? WhitePaper { get; set; }
                public List<string>? BlockchainSites { get; set; }
                public string? TwitterScreenName { get; set; }
                public string? FacebookName { get; set; }
                public string? SubredditUrl { get; set; }
            }
            
            public class MarketDataInfo
            {
                public Dictionary<string, decimal>? CurrentPrice { get; set; }
                public Dictionary<string, decimal>? MarketCaps { get; set; }
                public Dictionary<string, decimal>? TotalVolumes { get; set; }
                public Dictionary<string, decimal>? AllTimeHighs { get; set; }
                public Dictionary<string, DateTime>? AllTimeHighDates { get; set; }
                public Dictionary<string, decimal>? AllTimeLows { get; set; }
                public Dictionary<string, DateTime>? AllTimeLowDates { get; set; }
                public Dictionary<string, decimal>? High24h { get; set; }
                public Dictionary<string, decimal>? Low24h { get; set; }
                public decimal? CirculatingSupply { get; set; }
                public decimal? TotalSupply { get; set; }
                public decimal? MaxSupply { get; set; }
                public decimal? PriceChangePercentage24h { get; set; }
            }
        }
        
        public class CoinGeckoMarketChart
        {
            public List<List<decimal>>? Prices { get; set; }
            public List<List<decimal>>? MarketCaps { get; set; }
            public List<List<decimal>>? TotalVolumes { get; set; }
        }

        #endregion
    }
}