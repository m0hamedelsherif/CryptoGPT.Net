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
    /// Implementation of ICoinCapService for accessing CoinCap API data
    /// </summary>
    public class CoinCapService : ICoinCapService
    {
        private readonly HttpClient _httpClient;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CoinCapService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        // API endpoints
        private const string BaseUrl = "https://api.coincap.io/v2";
        private const string AssetsEndpoint = "/assets";
        private const string AssetEndpoint = "/assets/{0}";
        private const string HistoryEndpoint = "/assets/{0}/history";

        // Cache keys
        private const string TopCoinsKey = "coincap_top_coins_{0}";
        private const string CoinDataKey = "coincap_coin_data_{0}";
        private const string MarketChartKey = "coincap_market_chart_{0}_{1}";

        public CoinCapService(HttpClient httpClient, ICacheService cacheService, ILogger<CoinCapService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Set base URL and default headers
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "CryptoGPT.Net");

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Get top cryptocurrencies by market cap
        /// </summary>
        public async Task<List<CryptoCurrency>> GetTopCoinsAsync(int limit = 10)
        {
            try
            {
                string cacheKey = string.Format(TopCoinsKey, limit);
                var cachedResult = await _cacheService.GetAsync<List<CryptoCurrency>>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogInformation("[CoinCap] Retrieved top {Count} coins from cache", cachedResult.Count);
                    return cachedResult;
                }

                string requestUrl = $"{AssetsEndpoint}?limit={limit}";
                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                var apiResponse = await response.Content.ReadFromJsonAsync<CoinCapResponse<List<CoinCapAsset>>>(_jsonOptions);
                if (apiResponse?.Data == null)
                {
                    _logger.LogWarning("[CoinCap] Failed to deserialize assets response");
                    return new List<CryptoCurrency>();
                }

                var result = apiResponse.Data.Select(MapToCryptoCurrency).ToList();
                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
                _logger.LogInformation("[CoinCap] Retrieved top {Count} coins from API", result.Count);
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[CoinCap] Error fetching top coins: {Message}", ex.Message);
                return new List<CryptoCurrency>();
            }
        }

        /// <summary>
        /// Get detailed information for a specific cryptocurrency
        /// </summary>
        public async Task<CryptoCurrencyDetail?> GetCoinDataAsync(string coinId)
        {
            try
            {
                if (string.IsNullOrEmpty(coinId))
                    return null;

                string cacheKey = string.Format(CoinDataKey, coinId);
                var cachedResult = await _cacheService.GetAsync<CryptoCurrencyDetail>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogInformation("[CoinCap] Retrieved coin data for {CoinId} from cache", coinId);
                    return cachedResult;
                }

                string requestUrl = string.Format(AssetEndpoint, coinId);
                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                var apiResponse = await response.Content.ReadFromJsonAsync<CoinCapResponse<CoinCapAsset>>(_jsonOptions);
                if (apiResponse?.Data == null)
                {
                    _logger.LogWarning("[CoinCap] Failed to deserialize asset response for {CoinId}", coinId);
                    return null;
                }

                var result = MapToCryptoCurrencyDetail(apiResponse.Data);
                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15));
                _logger.LogInformation("[CoinCap] Retrieved coin data for {CoinId} from API", coinId);
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[CoinCap] Error fetching coin data: {Message}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Get historical market data for a cryptocurrency
        /// </summary>
        public async Task<MarketHistory?> GetMarketChartAsync(string coinId, int days = 30)
        {
            try
            {
                if (string.IsNullOrEmpty(coinId))
                    return null;

                string cacheKey = string.Format(MarketChartKey, coinId, days);
                var cachedResult = await _cacheService.GetAsync<MarketHistory>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogInformation("[CoinCap] Retrieved market chart for {CoinId} ({Days} days) from cache", coinId, days);
                    return cachedResult;
                }

                // Calculate interval based on days
                string interval = days <= 1 ? "m5" : days <= 7 ? "h1" : days <= 30 ? "h6" : "d1";
                
                // Calculate start and end timestamps
                long endTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long startTime = DateTimeOffset.UtcNow.AddDays(-days).ToUnixTimeMilliseconds();
                
                string requestUrl = string.Format(HistoryEndpoint, coinId) + $"?interval={interval}&start={startTime}&end={endTime}";
                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                var apiResponse = await response.Content.ReadFromJsonAsync<CoinCapResponse<List<CoinCapHistoryPoint>>>(_jsonOptions);
                if (apiResponse?.Data == null)
                {
                    _logger.LogWarning("[CoinCap] Failed to deserialize history response for {CoinId}", coinId);
                    return null;
                }

                // Get the asset details for additional info
                var assetDetail = await GetCoinDataAsync(coinId);
                
                var result = new MarketHistory
                {
                    CoinId = coinId,
                    Symbol = assetDetail?.Symbol ?? coinId.ToUpperInvariant(),
                    Prices = apiResponse.Data.Select(h => new PriceHistoryPoint
                    {
                        Timestamp = h.Time,
                        Price = h.PriceUsd
                    }).ToList(),
                    
                    // CoinCap doesn't provide separate market cap and volume history endpoints
                    // Will have to estimate based on current circulating supply
                    MarketCaps = apiResponse.Data.Select(h => new PriceHistoryPoint
                    {
                        Timestamp = h.Time,
                        Price = h.PriceUsd * (assetDetail?.CirculatingSupply ?? 0)
                    }).ToList(),
                    
                    Volumes = new List<PriceHistoryPoint>() // Not provided by CoinCap in the history endpoint
                };

                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(days <= 1 ? 5 : 60));
                _logger.LogInformation("[CoinCap] Retrieved market chart for {CoinId} ({Days} days) from API", coinId, days);
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[CoinCap] Error fetching market chart: {Message}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Get market overview data
        /// </summary>
        public async Task<MarketOverview> GetMarketOverviewAsync()
        {
            // Basic implementation using top coins data
            var topCoins = await GetTopCoinsAsync(10);
            return new MarketOverview
            {
                GeneratedAt = DateTime.UtcNow,
                MarketSentiment = "neutral",
                Summary = $"Top {topCoins.Count} coins overview from CoinCap.",
                TopPerformers = topCoins.OrderByDescending(c => c.PriceChangePercentage24h).Take(3).ToList(),
                WorstPerformers = topCoins.OrderBy(c => c.PriceChangePercentage24h).Take(3).ToList()
            };
        }

        #region Mapping & DTOs

        private CryptoCurrency MapToCryptoCurrency(CoinCapAsset asset)
        {
            return new CryptoCurrency
            {
                Id = asset.Id ?? string.Empty,
                Symbol = asset.Symbol?.ToUpperInvariant() ?? string.Empty,
                Name = asset.Name ?? string.Empty,
                CurrentPrice = asset.PriceUsd,
                MarketCap = asset.MarketCapUsd,
                PriceChangePercentage24h = asset.ChangePercent24Hr,
                MarketCapRank = asset.Rank,
                Volume24h = asset.VolumeUsd24Hr ?? 0,
                ImageUrl = $"https://assets.coincap.io/assets/icons/{asset.Symbol?.ToLowerInvariant() ?? string.Empty}@2x.png"
            };
        }

        private CryptoCurrencyDetail MapToCryptoCurrencyDetail(CoinCapAsset asset)
        {
            return new CryptoCurrencyDetail
            {
                Id = asset.Id ?? string.Empty,
                Symbol = asset.Symbol?.ToUpperInvariant() ?? string.Empty,
                Name = asset.Name ?? string.Empty,
                Description = string.Empty, // Not provided by CoinCap API
                ImageUrl = $"https://assets.coincap.io/assets/icons/{asset.Symbol?.ToLowerInvariant() ?? string.Empty}@2x.png",
                Homepage = $"https://coincap.io/assets/{asset.Id}",
                CurrentPrice = asset.PriceUsd,
                MarketCap = asset.MarketCapUsd,
                PriceChangePercentage24h = asset.ChangePercent24Hr,
                MarketCapRank = asset.Rank,
                Volume24h = asset.VolumeUsd24Hr ?? 0,
                CirculatingSupply = asset.Supply ?? 0,
                TotalSupply = asset.MaxSupply,
                MaxSupply = asset.MaxSupply,
                High24h = 0, // Not directly provided
                Low24h = 0, // Not directly provided
                AllTimeHigh = 0, // Not provided
                AllTimeHighDate = DateTime.MinValue // Not provided
            };
        }

        #endregion

        #region DTO Classes

        private class CoinCapResponse<T>
        {
            [JsonPropertyName("data")]
            public T? Data { get; set; }

            [JsonPropertyName("timestamp")]
            public long Timestamp { get; set; }
        }

        private class CoinCapAsset
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("rank")]
            public int Rank { get; set; }

            [JsonPropertyName("symbol")]
            public string? Symbol { get; set; }

            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("supply")]
            public decimal? Supply { get; set; }

            [JsonPropertyName("maxSupply")]
            public decimal? MaxSupply { get; set; }

            [JsonPropertyName("marketCapUsd")]
            public decimal MarketCapUsd { get; set; }

            [JsonPropertyName("volumeUsd24Hr")]
            public decimal? VolumeUsd24Hr { get; set; }

            [JsonPropertyName("priceUsd")]
            public decimal PriceUsd { get; set; }

            [JsonPropertyName("changePercent24Hr")]
            public decimal ChangePercent24Hr { get; set; }

            [JsonPropertyName("explorer")]
            public string? Explorer { get; set; }
        }

        private class CoinCapHistoryPoint
        {
            [JsonPropertyName("priceUsd")]
            public decimal PriceUsd { get; set; }

            [JsonPropertyName("time")]
            public long Time { get; set; }

            [JsonPropertyName("date")]
            public string? Date { get; set; }
        }

        #endregion
    }
}