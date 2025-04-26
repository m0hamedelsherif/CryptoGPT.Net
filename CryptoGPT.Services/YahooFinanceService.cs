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
    public class YahooFinanceService : IYahooFinanceService
    {
        private readonly HttpClient _httpClient;
        private readonly ICacheService _cacheService;
        private readonly ILogger<YahooFinanceService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        // We'll use the Yahoo Finance unofficial API endpoints (community proxies)
        private readonly string _baseUrl = "https://query1.finance.yahoo.com";

        private const string QuoteEndpoint = "/v7/finance/quote?symbols={0}";
        private const string ChartEndpoint = "/v8/finance/chart/{0}?interval=1d&range={1}d";

        private const string TopCoinsKey = "yahoo_top_coins_{0}";
        private const string CoinDataKey = "yahoo_coin_data_{0}";
        private const string MarketChartKey = "yahoo_market_chart_{0}_{1}";
        private const string TechnicalAnalysisPriceDataKey = "yahoo_tech_analysis_prices_{0}_{1}";

        // A static list of popular crypto symbols for fallback top coins
        private static readonly string[] DefaultTopSymbols = new[] { "BTC-USD", "ETH-USD", "BNB-USD", "SOL-USD", "XRP-USD", "ADA-USD", "DOGE-USD", "AVAX-USD", "DOT-USD", "TRX-USD" };

        public YahooFinanceService(HttpClient httpClient, ICacheService cacheService, ILogger<YahooFinanceService> logger)
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
                    _logger.LogInformation("[Yahoo] Retrieved top {Count} coins from cache", cachedResult.Count);
                    return cachedResult;
                }
                var symbols = DefaultTopSymbols.Take(limit).ToArray();
                string requestUrl = string.Format(QuoteEndpoint, string.Join(",", symbols));
                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();
                var apiResult = await response.Content.ReadFromJsonAsync<YahooQuoteResponse>(_jsonOptions);
                if (apiResult?.QuoteResponse?.Result == null)
                {
                    _logger.LogWarning("[Yahoo] Failed to deserialize quote response");
                    return new List<CryptoCurrency>();
                }
                var result = apiResult.QuoteResponse.Result.Select(MapToCryptoCurrency).ToList();
                await _cacheService.SetAsync(cacheKey, result, 300);
                _logger.LogInformation("[Yahoo] Retrieved top {Count} coins from API", result.Count);
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[Yahoo] Error fetching top coins: {Message}", ex.Message);
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
                    _logger.LogInformation("[Yahoo] Retrieved coin data for {CoinId} from cache", coinId);
                    return cachedResult;
                }
                // Yahoo uses symbols like BTC-USD, ETH-USD, etc.
                string symbol = ToYahooSymbol(coinId);
                string requestUrl = string.Format(QuoteEndpoint, symbol);
                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();
                var apiResult = await response.Content.ReadFromJsonAsync<YahooQuoteResponse>(_jsonOptions);
                var quote = apiResult?.QuoteResponse?.Result?.FirstOrDefault();
                if (quote == null)
                {
                    _logger.LogWarning("[Yahoo] No data found for {CoinId}", coinId);
                    return null;
                }
                var result = MapToCryptoCurrencyDetail(quote);
                await _cacheService.SetAsync(cacheKey, result, 900);
                _logger.LogInformation("[Yahoo] Retrieved coin data for {CoinId} from API", coinId);
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[Yahoo] Error fetching coin data: {Message}", ex.Message);
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
                    _logger.LogInformation("[Yahoo] Retrieved market chart for {CoinId} ({Days} days) from cache", coinId, days);
                    return cachedResult;
                }
                string symbol = ToYahooSymbol(coinId);
                string requestUrl = string.Format(ChartEndpoint, symbol, days);
                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();
                var apiResult = await response.Content.ReadFromJsonAsync<YahooChartResponse>(_jsonOptions);
                var chart = apiResult?.Chart?.Result?.FirstOrDefault();
                if (chart == null || chart.Timestamp == null || chart.Indicators?.Quote == null)
                {
                    _logger.LogWarning("[Yahoo] No chart data for {CoinId}", coinId);
                    return null;
                }
                var prices = new List<PriceHistoryPoint>();
                for (int i = 0; i < chart.Timestamp.Count && i < chart.Indicators.Quote[0].Close.Count; i++)
                {
                    prices.Add(new PriceHistoryPoint
                    {
                        Timestamp = chart.Timestamp[i],
                        Price = chart.Indicators.Quote[0].Close[i] ?? 0
                    });
                }
                var result = new MarketHistory
                {
                    CoinId = coinId,
                    Symbol = symbol,
                    Prices = prices,
                    MarketCaps = new List<PriceHistoryPoint>(),
                    Volumes = new List<PriceHistoryPoint>()
                };
                await _cacheService.SetAsync(cacheKey, result, days <= 1 ? 300 : 3600);
                _logger.LogInformation("[Yahoo] Retrieved market chart for {CoinId} ({Days} days) from API", coinId, days);
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[Yahoo] Error fetching market chart: {Message}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Gets price data specifically for technical analysis with proper caching
        /// </summary>
        /// <param name="symbol">The cryptocurrency symbol (e.g. BTC, ETH)</param>
        /// <param name="range">The time range to fetch data for (in days)</param>
        /// <returns>List of closing prices for the specified crypto</returns>
        public async Task<List<decimal>> GetPriceDataForTechnicalAnalysisAsync(string symbol, int range = 90)
        {
            try
            {
                string cacheKey = string.Format(TechnicalAnalysisPriceDataKey, symbol.ToLowerInvariant(), range);
                var cachedResult = await _cacheService.GetAsync<List<decimal>>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogInformation("[Yahoo] Retrieved technical analysis price data for {Symbol} from cache", symbol);
                    return cachedResult;
                }

                // Yahoo uses symbols like BTC-USD, ETH-USD, etc.
                string yahooSymbol = ToYahooSymbol(symbol);
                string requestUrl = string.Format(ChartEndpoint, yahooSymbol, range);
                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                var responseData = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                var prices = ExtractPricesFromYahooResponse(responseData);

                if (prices.Count == 0)
                {
                    _logger.LogWarning("[Yahoo] No price data found for {Symbol} in the requested range", symbol);
                    return new List<decimal>();
                }

                // Cache the results - for technical analysis, we can cache longer as historical data doesn't change
                // Cache for 1 hour (3600 seconds)
                await _cacheService.SetAsync(cacheKey, prices, 3600);
                _logger.LogInformation("[Yahoo] Retrieved technical analysis price data for {Symbol} from API", symbol);
                return prices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Yahoo] Error fetching technical analysis price data for {Symbol}: {Message}", symbol, ex.Message);
                return new List<decimal>();
            }
        }

        /// <summary>
        /// Extract closing prices from Yahoo Finance API response
        /// </summary>
        private List<decimal> ExtractPricesFromYahooResponse(JsonDocument response)
        {
            var prices = new List<decimal>();
            try
            {
                var closePrices = response.RootElement
                    .GetProperty("chart")
                    .GetProperty("result")[0]
                    .GetProperty("indicators")
                    .GetProperty("quote")[0]
                    .GetProperty("close");

                foreach (var price in closePrices.EnumerateArray())
                {
                    if (price.ValueKind != JsonValueKind.Null)
                        prices.Add(price.GetDecimal());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Yahoo] Failed to extract prices from Yahoo response: {Message}", ex.Message);
            }
            return prices;
        }

        #region Mapping & DTOs

        private CryptoCurrency MapToCryptoCurrency(YahooQuote quote)
        {
            return new CryptoCurrency
            {
                Id = quote.Symbol?.Split('-')[0].ToLower() ?? string.Empty,
                Symbol = quote.Symbol?.Split('-')[0].ToUpper() ?? string.Empty,
                Name = quote.ShortName ?? quote.Symbol ?? string.Empty,
                CurrentPrice = quote.RegularMarketPrice ?? 0,
                MarketCap = quote.MarketCap ?? 0,
                PriceChangePercentage24h = quote.RegularMarketChangePercent ?? 0,
                MarketCapRank = 0, // Not available
                Volume24h = quote.RegularMarketVolume ?? 0,
                ImageUrl = $"https://assets.coincap.io/assets/icons/{quote.Symbol?.Split('-')[0].ToLower()}@2x.png"
            };
        }

        private CryptoCurrencyDetail MapToCryptoCurrencyDetail(YahooQuote quote)
        {
            return new CryptoCurrencyDetail
            {
                Id = quote.Symbol?.Split('-')[0].ToLower() ?? string.Empty,
                Symbol = quote.Symbol?.Split('-')[0].ToUpper() ?? string.Empty,
                Name = quote.ShortName ?? quote.Symbol ?? string.Empty,
                Description = string.Empty,
                ImageUrl = $"https://assets.coincap.io/assets/icons/{quote.Symbol?.Split('-')[0].ToLower()}@2x.png",
                CurrentPrice = quote.RegularMarketPrice ?? 0,
                MarketCap = quote.MarketCap ?? 0,
                PriceChangePercentage24h = quote.RegularMarketChangePercent ?? 0,
                MarketCapRank = 0,
                Volume24h = quote.RegularMarketVolume ?? 0,
                CirculatingSupply = 0,
                TotalSupply = null,
                MaxSupply = null,
                AllTimeHigh = 0,
                AllTimeHighDate = DateTime.MinValue
            };
        }

        private string ToYahooSymbol(string coinId)
        {
            // Map common coin IDs to Yahoo symbols
            return coinId.ToUpper() + "-USD";
        }

        private class YahooQuoteResponse
        {
            public YahooQuoteResult? QuoteResponse { get; set; }
        }

        private class YahooQuoteResult
        {
            public List<YahooQuote>? Result { get; set; }
        }

        private class YahooQuote
        {
            public string? Symbol { get; set; }
            public string? ShortName { get; set; }
            public decimal? RegularMarketPrice { get; set; }
            public decimal? MarketCap { get; set; }
            public decimal? RegularMarketChangePercent { get; set; }
            public decimal? RegularMarketVolume { get; set; }
        }

        private class YahooChartResponse
        {
            public YahooChartResult? Chart { get; set; }
        }

        private class YahooChartResult
        {
            public List<YahooChartData>? Result { get; set; }
        }

        private class YahooChartData
        {
            public List<long>? Timestamp { get; set; }
            public YahooChartIndicators? Indicators { get; set; }
        }

        private class YahooChartIndicators
        {
            public List<YahooChartQuote> Quote { get; set; } = new List<YahooChartQuote>();
        }

        private class YahooChartQuote
        {
            public List<decimal?> Close { get; set; } = new List<decimal?>();
        }

        #endregion Mapping & DTOs
    }
}