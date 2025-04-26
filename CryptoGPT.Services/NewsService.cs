using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CryptoGPT.Core.Interfaces;
using CryptoGPT.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CryptoGPT.Services
{
    /// <summary>
    /// Implementation of INewsService for cryptocurrency news
    /// </summary>
    public class NewsService : INewsService
    {
        private readonly HttpClient _httpClient;
        private readonly ICacheService _cacheService;
        private readonly ILogger<NewsService> _logger;
        private readonly JsonSerializerSettings _jsonSettings;

        // API endpoints and keys
        private const string CryptoCompareNewsUrl = "https://min-api.cryptocompare.com/data/v2/news/";

        // Cache keys
        private const string MarketNewsKey = "market_news_{0}";

        private const string CoinNewsKey = "coin_news_{0}_{1}";

        public NewsService(
            HttpClient httpClient,
            ICacheService cacheService,
            ILogger<NewsService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "CryptoGPT.Net");

            _jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
        }

        /// <inheritdoc/>
        public async Task<List<CryptoNewsItem>> GetMarketNewsAsync(int limit = 20)
        {
            try
            {
                // Try to get from cache first
                string cacheKey = string.Format(MarketNewsKey, limit);
                var cachedResult = await _cacheService.GetAsync<List<CryptoNewsItem>>(cacheKey);

                if (cachedResult != null)
                {
                    _logger.LogInformation("Retrieved {Count} market news items from cache", cachedResult.Count);
                    return cachedResult;
                }

                // Fetch from API (using CryptoCompare as primary source)
                string requestUrl = $"{CryptoCompareNewsUrl}?lang=EN&sortOrder=popular&limit={limit}";

                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var newsData = JsonConvert.DeserializeObject<CryptoCompareNewsResponse>(responseContent, _jsonSettings);
                if (newsData == null || newsData.Data == null)
                {
                    _logger.LogWarning("Failed to deserialize news response");
                    return new List<CryptoNewsItem>();
                }

                // Convert to our model
                var result = new List<CryptoNewsItem>();
                foreach (var item in newsData.Data)
                {
                    result.Add(new CryptoNewsItem
                    {
                        Title = item.Title,
                        Url = item.Url,
                        Source = item.Source,
                        PublishedAt = DateTimeOffset.FromUnixTimeSeconds(item.PublishedOn).DateTime,
                        ImageUrl = item.ImageUrl,
                        Description = item.Body?.Substring(0, Math.Min(item.Body.Length, 200)) + "..."
                    });
                }

                // Cache the result
                await _cacheService.SetAsync(cacheKey, result, 1800); // Cache for 30 minutes

                _logger.LogInformation("Retrieved {Count} market news items from API", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching market news: {Message}", ex.Message);
                return new List<CryptoNewsItem>();
            }
        }

        /// <inheritdoc/>
        public async Task<List<CryptoNewsItem>> GetCoinNewsAsync(string coinId, string symbol, int limit = 10)
        {
            try
            {
                // Try to get from cache first
                string cacheKey = string.Format(CoinNewsKey, coinId, limit);
                var cachedResult = await _cacheService.GetAsync<List<CryptoNewsItem>>(cacheKey);

                if (cachedResult != null)
                {
                    _logger.LogInformation("Retrieved {Count} news items for {CoinId} from cache", cachedResult.Count, coinId);
                    return cachedResult;
                }

                // Fetch from API (using CryptoCompare as primary source)
                string requestUrl = $"{CryptoCompareNewsUrl}?lang=EN&categories={symbol.ToUpper()}&sortOrder=popular&limit={limit}";

                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var newsData = JsonConvert.DeserializeObject<CryptoCompareNewsResponse>(responseContent, _jsonSettings);
                List<CryptoNewsItem> result = new List<CryptoNewsItem>();

                if (newsData?.Data != null && newsData.Data.Count > 0)
                {
                    // Convert to our model
                    foreach (var item in newsData.Data)
                    {
                        result.Add(new CryptoNewsItem
                        {
                            Title = item.Title,
                            Url = item.Url,
                            Source = item.Source,
                            PublishedAt = DateTimeOffset.FromUnixTimeSeconds(item.PublishedOn).DateTime,
                            ImageUrl = item.ImageUrl,
                            Description = item.Body?.Substring(0, Math.Min(item.Body?.Length ?? 0, 200)) + "..."
                        });
                    }
                }
                else
                {
                    // If no specific coin news found, fall back to market news and filter
                    _logger.LogInformation("No specific news found for {CoinId}, using fallback", coinId);
                    var marketNews = await GetMarketNewsAsync(30);

                    // Filter for mention of the coin in title
                    result = marketNews
                        .Where(n => n.Title.Contains(coinId, StringComparison.OrdinalIgnoreCase) ||
                                n.Title.Contains(symbol, StringComparison.OrdinalIgnoreCase))
                        .Take(limit)
                        .ToList();
                }

                // Cache the result
                await _cacheService.SetAsync(cacheKey, result, 1800); // Cache for 30 minutes

                _logger.LogInformation("Retrieved {Count} news items for {CoinId}", result.Count, coinId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching news for {CoinId}: {Message}", coinId, ex.Message);
                return new List<CryptoNewsItem>();
            }
        }

        #region DTO Models for News APIs

        private class CryptoCompareNewsResponse
        {
            public string Type { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public List<CryptoCompareNewsItem> Data { get; set; } = new List<CryptoCompareNewsItem>();
        }

        private class CryptoCompareNewsItem
        {
            public string Id { get; set; } = string.Empty;
            public string Guid { get; set; } = string.Empty;
            public long PublishedOn { get; set; }
            public string ImageUrl { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
            public string Source { get; set; } = string.Empty;
            public string Body { get; set; } = string.Empty;
            
            [JsonConverter(typeof(PipeSeparatedArrayConverter))]
            public string[] Categories { get; set; } = Array.Empty<string>();
            
            [JsonConverter(typeof(PipeSeparatedArrayConverter))]
            public string[] Tags { get; set; } = Array.Empty<string>();
        }

        #endregion DTO Models for News APIs

        #region JSON Converters

        private class PipeSeparatedArrayConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(string[]);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                    return Array.Empty<string>();

                var value = JToken.Load(reader);
                
                if (value.Type == JTokenType.Array)
                    return value.ToObject<string[]>();

                if (value.Type == JTokenType.String)
                {
                    var stringValue = value.ToString();
                    return string.IsNullOrEmpty(stringValue) 
                        ? Array.Empty<string>() 
                        : stringValue.Split('|', StringSplitOptions.RemoveEmptyEntries);
                }

                return Array.Empty<string>();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var array = value as string[];
                if (array == null || array.Length == 0)
                {
                    writer.WriteNull();
                    return;
                }

                writer.WriteValue(string.Join("|", array));
            }
        }

        #endregion JSON Converters
    }
}