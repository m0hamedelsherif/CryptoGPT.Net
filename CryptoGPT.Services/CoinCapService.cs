using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using CryptoGPT.Core.Interfaces;
using CryptoGPT.Core.Models;
using Microsoft.Extensions.Logging;

namespace CryptoGPT.Services
{
    public class CoinCapService : ICoinCapService
    {
        private readonly HttpClient _httpClient;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CoinCapService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        private readonly string _baseUrl = "https://api.coincap.io/v2";
        private const string AssetsEndpoint = "/assets";
        private const string AssetEndpoint = "/assets/{0}";
        private const string AssetHistoryEndpoint = "/assets/{0}/history";

        private const string TopCoinsKey = "coincap_top_coins_{0}";
        private const string CoinDataKey = "coincap_coin_data_{0}";
        private const string MarketChartKey = "coincap_market_chart_{0}_{1}";

        public CoinCapService(HttpClient httpClient, ICacheService cacheService, ILogger<CoinCapService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "CryptoGPT.Net");

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

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
                var apiResult = await response.Content.ReadFromJsonAsync<CoinCapAssetsResponse>(_jsonOptions);
                if (apiResult?.Data == null)
                {
                    _logger.LogWarning("[CoinCap] Failed to deserialize assets response");
                    return new List<CryptoCurrency>();
                }
                var result = apiResult.Data.Select(MapToCryptoCurrency).ToList();
                await _cacheService.SetAsync(cacheKey, result, 300);
                _logger.LogInformation("[CoinCap] Retrieved top {Count} coins from API", result.Count);
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[CoinCap] Error fetching top coins: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<CryptoCurrencyDetail?> GetCoinDataAsync(string coinId)
        {
            try
            {
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
                var apiResult = await response.Content.ReadFromJsonAsync<CoinCapAssetResponse>(_jsonOptions);
                if (apiResult?.Data == null)
                {
                    _logger.LogWarning("[CoinCap] Failed to deserialize asset response");
                    return null;
                }
                var result = MapToCryptoCurrencyDetail(apiResult.Data);
                await _cacheService.SetAsync(cacheKey, result, 900);
                _logger.LogInformation("[CoinCap] Retrieved coin data for {CoinId} from API", coinId);
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[CoinCap] Error fetching coin data: {Message}", ex.Message);
                return null;
            }
        }

        public async Task<MarketHistory?> GetMarketChartAsync(string coinId, int days = 30)
        {
            try
            {
                string cacheKey = string.Format(MarketChartKey, coinId, days);
                var cachedResult = await _cacheService.GetAsync<MarketHistory>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogInformation("[CoinCap] Retrieved market chart for {CoinId} ({Days} days) from cache", coinId, days);
                    return cachedResult;
                }
                // CoinCap supports intervals: m1, m5, m15, m30, h1, h2, h6, h12, d1
                string interval = days <= 1 ? "m15" : days <= 7 ? "h2" : "d1";
                long end = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long start = DateTimeOffset.UtcNow.AddDays(-days).ToUnixTimeMilliseconds();
                string requestUrl = string.Format(AssetHistoryEndpoint, coinId) + $"?interval={interval}&start={start}&end={end}";
                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();
                var apiResult = await response.Content.ReadFromJsonAsync<CoinCapHistoryResponse>(_jsonOptions);
                if (apiResult?.Data == null)
                {
                    _logger.LogWarning("[CoinCap] Failed to deserialize history response");
                    return null;
                }
                var result = new MarketHistory
                {
                    CoinId = coinId,
                    Symbol = coinId,
                    Prices = apiResult.Data.Select(MapToPriceHistoryPoint).ToList(),
                    MarketCaps = new List<PriceHistoryPoint>(), // CoinCap does not provide market cap history in this endpoint
                    Volumes = new List<PriceHistoryPoint>() // CoinCap does not provide volume history in this endpoint
                };
                await _cacheService.SetAsync(cacheKey, result, days <= 1 ? 300 : 3600);
                _logger.LogInformation("[CoinCap] Retrieved market chart for {CoinId} ({Days} days) from API", coinId, days);
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[CoinCap] Error fetching market chart: {Message}", ex.Message);
                return null;
            }
        }

        #region Mapping & DTOs

        private CryptoCurrency MapToCryptoCurrency(CoinCapAsset asset)
        {
            return new CryptoCurrency
            {
                Id = asset.Id ?? string.Empty,
                Symbol = asset.Symbol ?? string.Empty,
                Name = asset.Name ?? string.Empty,
                CurrentPrice = TryParseDecimal(asset.PriceUsd),
                MarketCap = TryParseDecimal(asset.MarketCapUsd),
                PriceChangePercentage24h = TryParseDecimal(asset.ChangePercent24Hr),
                MarketCapRank = int.TryParse(asset.Rank, out var rank) ? rank : 0,
                Volume24h = TryParseDecimal(asset.VolumeUsd24Hr),
                ImageUrl = $"https://assets.coincap.io/assets/icons/{asset.Symbol?.ToLower()}@2x.png"
            };
        }

        private CryptoCurrencyDetail MapToCryptoCurrencyDetail(CoinCapAsset asset)
        {
            return new CryptoCurrencyDetail
            {
                Id = asset.Id ?? string.Empty,
                Symbol = asset.Symbol ?? string.Empty,
                Name = asset.Name ?? string.Empty,
                Description = string.Empty, // CoinCap does not provide description
                ImageUrl = $"https://assets.coincap.io/assets/icons/{asset.Symbol?.ToLower()}@2x.png",
                CurrentPrice = TryParseDecimal(asset.PriceUsd),
                MarketCap = TryParseDecimal(asset.MarketCapUsd),
                PriceChangePercentage24h = TryParseDecimal(asset.ChangePercent24Hr),
                MarketCapRank = int.TryParse(asset.Rank, out var rank) ? rank : 0,
                Volume24h = TryParseDecimal(asset.VolumeUsd24Hr),
                CirculatingSupply = TryParseDecimal(asset.Supply),
                TotalSupply = TryParseNullableDecimal(asset.MaxSupply),
                MaxSupply = TryParseNullableDecimal(asset.MaxSupply),
                AllTimeHigh = 0, // Not available
                AllTimeHighDate = DateTime.MinValue // Not available
            };
        }

        private PriceHistoryPoint MapToPriceHistoryPoint(CoinCapHistoryPoint point)
        {
            return new PriceHistoryPoint
            {
                Timestamp = point.Time,
                Price = TryParseDecimal(point.PriceUsd)
            };
        }

        private decimal TryParseDecimal(string? value)
        {
            return decimal.TryParse(value, out var d) ? d : 0;
        }

        private double? TryParseDouble(string? value)
        {
            return double.TryParse(value, out var d) ? d : (double?)null;
        }

        private decimal? TryParseNullableDecimal(string? value)
        {
            return decimal.TryParse(value, out var d) ? d : (decimal?)null;
        }

        private class CoinCapAssetsResponse
        { public List<CoinCapAsset>? Data { get; set; } }

        private class CoinCapAssetResponse
        { public CoinCapAsset? Data { get; set; } }

        private class CoinCapHistoryResponse
        { public List<CoinCapHistoryPoint>? Data { get; set; } }

        private class CoinCapAsset
        {
            public string? Id { get; set; }
            public string? Rank { get; set; }
            public string? Symbol { get; set; }
            public string? Name { get; set; }
            public string? Supply { get; set; }
            public string? MaxSupply { get; set; }
            public string? MarketCapUsd { get; set; }
            public string? VolumeUsd24Hr { get; set; }
            public string? PriceUsd { get; set; }
            public string? ChangePercent24Hr { get; set; }
        }

        private class CoinCapHistoryPoint
        {
            public long Time { get; set; }
            public string? PriceUsd { get; set; }
        }

        #endregion Mapping & DTOs
    }
}