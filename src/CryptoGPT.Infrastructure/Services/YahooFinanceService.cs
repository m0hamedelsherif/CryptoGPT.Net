using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using CryptoGPT.Application.Interfaces;
using CryptoGPT.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CryptoGPT.Infrastructure.Services
{
    /// <summary>
    /// Service for interacting with Yahoo Finance API for cryptocurrency data
    /// </summary>
    public class YahooFinanceService : IYahooFinanceService
    {
        private readonly HttpClient _httpClient;
        private readonly ICacheService _cacheService;
        private readonly ILogger<YahooFinanceService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        // Lists of common crypto symbols on Yahoo Finance
        private readonly List<string> _topCryptoSymbols = new()
        {
            "BTC-USD", "ETH-USD", "USDT-USD", "BNB-USD", "SOL-USD",
            "XRP-USD", "USDC-USD", "STETH-USD", "ADA-USD", "AVAX-USD",
            "DOGE-USD", "DOT-USD", "MATIC-USD", "TON-USD", "LINK-USD", 
            "SHIB-USD", "TRX-USD", "UNI-USD", "BCH-USD", "LTC-USD"
        };

        // Cache keys
        private const string TopCoinsKey = "yahoo_top_coins_{0}";
        private const string CoinDataKey = "yahoo_coin_data_{0}";
        private const string MarketChartKey = "yahoo_market_chart_{0}_{1}";

        public YahooFinanceService(
            HttpClient httpClient,
            ICacheService cacheService,
            ILogger<YahooFinanceService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Get top cryptocurrencies by market cap from Yahoo Finance
        /// </summary>
        public async Task<List<CryptoCurrency>> GetTopCoinsAsync(int limit = 10)
        {
            try
            {
                string cacheKey = string.Format(TopCoinsKey, limit);
                var cachedResult = await _cacheService.GetAsync<List<CryptoCurrency>>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogInformation("[Yahoo Finance] Retrieved top {Count} coins from cache", cachedResult.Count);
                    return cachedResult;
                }

                // Use our predefined list and fetch them using the multi-quote endpoint
                var symbolsToFetch = _topCryptoSymbols.Take(Math.Min(limit, _topCryptoSymbols.Count)).ToList();
                string symbolsStr = string.Join(",", symbolsToFetch);
                
                var requestUrl = $"https://query1.finance.yahoo.com/v7/finance/quote?symbols={symbolsStr}";
                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();
                
                var yahooResponse = await response.Content.ReadFromJsonAsync<YahooFinanceResponse>(_jsonOptions);
                if (yahooResponse?.QuoteResponse?.Result == null)
                {
                    _logger.LogWarning("[Yahoo Finance] Failed to deserialize quotes response");
                    return new List<CryptoCurrency>();
                }

                var quotes = yahooResponse.QuoteResponse.Result;
                
                var result = quotes.Select((q, i) => new CryptoCurrency
                {
                    Id = q.Symbol?.Replace("-USD", "").ToLowerInvariant() ?? string.Empty,
                    Symbol = q.Symbol?.Replace("-USD", "") ?? string.Empty,
                    Name = q.ShortName ?? q.LongName ?? q.Symbol ?? string.Empty,
                    CurrentPrice = q.RegularMarketPrice ?? 0,
                    MarketCap = q.MarketCap ?? 0,
                    PriceChangePercentage24h = q.RegularMarketChangePercent,
                    MarketCapRank = i + 1, // Yahoo doesn't provide rank directly
                    Volume24h = q.RegularMarketVolume ?? 0,
                    ImageUrl = string.Empty // Yahoo doesn't provide image URLs
                }).ToList();

                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
                _logger.LogInformation("[Yahoo Finance] Retrieved top {Count} coins from API", result.Count);
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[Yahoo Finance] Error fetching top coins: {Message}", ex.Message);
                return new List<CryptoCurrency>();
            }
        }

        /// <summary>
        /// Get detailed information for a specific cryptocurrency from Yahoo Finance
        /// </summary>
        public async Task<CryptoCurrencyDetail?> GetCoinDataAsync(string coinId)
        {
            try
            {
                string cacheKey = string.Format(CoinDataKey, coinId);
                var cachedResult = await _cacheService.GetAsync<CryptoCurrencyDetail>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogInformation("[Yahoo Finance] Retrieved coin data for {CoinId} from cache", coinId);
                    return cachedResult;
                }

                // Convert coinId to Yahoo Finance symbol format
                string symbol = ConvertToYahooSymbol(coinId);
                
                // Fetch quote data
                var requestUrl = $"https://query1.finance.yahoo.com/v7/finance/quote?symbols={symbol}";
                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();
                
                var yahooResponse = await response.Content.ReadFromJsonAsync<YahooFinanceResponse>(_jsonOptions);
                if (yahooResponse?.QuoteResponse?.Result == null || !yahooResponse.QuoteResponse.Result.Any())
                {
                    _logger.LogWarning("[Yahoo Finance] Failed to deserialize quote data for {CoinId}", coinId);
                    return null;
                }

                var quote = yahooResponse.QuoteResponse.Result.First();
                
                var result = new CryptoCurrencyDetail
                {
                    Id = coinId.ToLowerInvariant(),
                    Symbol = quote.Symbol?.Replace("-USD", "") ?? coinId.ToUpperInvariant(),
                    Name = quote.ShortName ?? quote.LongName ?? quote.Symbol ?? string.Empty,
                    Description = string.Empty, // Yahoo doesn't provide descriptions in this API
                    ImageUrl = string.Empty, // Yahoo doesn't provide image URLs
                    Homepage = string.Empty, // Yahoo doesn't provide homepages
                    CurrentPrice = quote.RegularMarketPrice ?? 0,
                    MarketCap = quote.MarketCap ?? 0,
                    PriceChangePercentage24h = quote.RegularMarketChangePercent,
                    MarketCapRank = 0, // Not provided by Yahoo Finance
                    Volume24h = quote.RegularMarketVolume ?? 0,
                    High24h = quote.RegularMarketDayHigh ?? 0,
                    Low24h = quote.RegularMarketDayLow ?? 0,
                    CirculatingSupply = 0, // Not provided in this API response
                    TotalSupply = null, // Not provided in this API response
                    MaxSupply = null, // Not provided in this API response
                    AllTimeHigh = 0, // Not provided in this API response
                    AllTimeHighDate = DateTime.MinValue // Not provided in this API response
                };

                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15));
                _logger.LogInformation("[Yahoo Finance] Retrieved coin data for {CoinId} from API", coinId);
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[Yahoo Finance] Error fetching coin data for {CoinId}: {Message}", coinId, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Get historical market data for a cryptocurrency from Yahoo Finance
        /// </summary>
        public async Task<MarketHistory?> GetMarketChartAsync(string coinId, int days = 30)
        {
            try
            {
                string cacheKey = string.Format(MarketChartKey, coinId, days);
                var cachedResult = await _cacheService.GetAsync<MarketHistory>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogInformation("[Yahoo Finance] Retrieved market chart for {CoinId} ({Days} days) from cache", coinId, days);
                    return cachedResult;
                }

                // Convert coinId to Yahoo Finance symbol format
                string symbol = ConvertToYahooSymbol(coinId);
                
                // Calculate interval based on days
                string interval = days switch
                {
                    <= 7 => "1h", // 1-hour intervals for shorter timeframes
                    <= 30 => "1d", // 1-day intervals for medium timeframes
                    <= 90 => "1d", // 1-day intervals for medium-long timeframes
                    _ => "1wk" // 1-week intervals for longer timeframes
                };
                
                // Calculate range based on days
                string range = days switch
                {
                    <= 1 => "1d",
                    <= 5 => "5d",
                    <= 7 => "7d",
                    <= 30 => "1mo",
                    <= 90 => "3mo",
                    <= 180 => "6mo",
                    <= 365 => "1y",
                    _ => "max"
                };

                var requestUrl = $"https://query1.finance.yahoo.com/v8/finance/chart/{symbol}?range={range}&interval={interval}";
                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();
                
                var chartResponse = await response.Content.ReadFromJsonAsync<YahooChartResponse>(_jsonOptions);
                if (chartResponse?.Chart?.Result == null || !chartResponse.Chart.Result.Any())
                {
                    _logger.LogWarning("[Yahoo Finance] Failed to deserialize chart data for {CoinId}", coinId);
                    return null;
                }

                var chartResult = chartResponse.Chart.Result.First();
                var timestamps = chartResult.Timestamp;
                var quote = chartResult.Indicators?.Quote?.FirstOrDefault();

                if (timestamps == null || quote?.Close == null || timestamps.Count != quote.Close.Count)
                {
                    _logger.LogWarning("[Yahoo Finance] Invalid chart data structure for {CoinId}", coinId);
                    return null;
                }

                var pricePoints = new List<PriceHistoryPoint>();
                var volumePoints = new List<PriceHistoryPoint>();

                for (int i = 0; i < timestamps.Count; i++)
                {
                    if (quote.Close[i].HasValue) // Skip null values
                    {
                        // Convert Unix seconds to milliseconds
                        long timestampMs = timestamps[i] * 1000;
                        
                        pricePoints.Add(new PriceHistoryPoint 
                        { 
                            Timestamp = timestampMs, 
                            Price = quote.Close[i].Value 
                        });
                        
                        if (quote.Volume != null && i < quote.Volume.Count && quote.Volume[i].HasValue)
                        {
                            volumePoints.Add(new PriceHistoryPoint 
                            { 
                                Timestamp = timestampMs, 
                                Price = quote.Volume[i].Value 
                            });
                        }
                    }
                }

                var result = new MarketHistory
                {
                    CoinId = coinId,
                    Symbol = symbol.Replace("-USD", ""),
                    Prices = pricePoints,
                    MarketCaps = new List<PriceHistoryPoint>(), // Yahoo doesn't provide market cap history
                    Volumes = volumePoints
                };

                // Cache longer for larger time frames
                var cacheTime = days <= 1 ? TimeSpan.FromMinutes(5) : 
                               days <= 7 ? TimeSpan.FromMinutes(30) : 
                               TimeSpan.FromHours(6);
                               
                await _cacheService.SetAsync(cacheKey, result, cacheTime);
                _logger.LogInformation("[Yahoo Finance] Retrieved market chart for {CoinId} ({Days} days) from API", coinId, days);
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[Yahoo Finance] Error fetching market chart for {CoinId}: {Message}", coinId, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Convert coinId to Yahoo Finance symbol format
        /// </summary>
        public string ConvertToYahooSymbol(string coinId)
        {
            if (string.IsNullOrWhiteSpace(coinId))
                return string.Empty;
            return coinId.ToUpperInvariant() + "-USD";
        }

        #region DTO Classes

        private class YahooFinanceResponse
        {
            public YahooQuoteResponse? QuoteResponse { get; set; }
        }

        private class YahooQuoteResponse
        {
            public List<YahooQuote>? Result { get; set; }
            public YahooError? Error { get; set; }
        }

        private class YahooQuote
        {
            public string? Symbol { get; set; }
            public string? ShortName { get; set; }
            public string? LongName { get; set; }
            public decimal? RegularMarketPrice { get; set; }
            public decimal? RegularMarketDayHigh { get; set; }
            public decimal? RegularMarketDayLow { get; set; }
            public decimal? RegularMarketVolume { get; set; }
            public decimal? RegularMarketChange { get; set; }
            public decimal? RegularMarketChangePercent { get; set; }
            public decimal? MarketCap { get; set; }
        }

        private class YahooChartResponse
        {
            public YahooChart? Chart { get; set; }
        }

        private class YahooChart
        {
            public List<YahooChartResult>? Result { get; set; }
            public YahooError? Error { get; set; }
        }

        private class YahooChartResult
        {
            public List<long>? Timestamp { get; set; }
            public YahooIndicators? Indicators { get; set; }
        }

        private class YahooIndicators
        {
            public List<YahooQuoteIndicator>? Quote { get; set; }
        }

        private class YahooQuoteIndicator
        {
            public List<decimal?>? Close { get; set; }
            public List<decimal?>? Open { get; set; }
            public List<decimal?>? High { get; set; }
            public List<decimal?>? Low { get; set; }
            public List<decimal?>? Volume { get; set; }
        }

        private class YahooError
        {
            public string? Code { get; set; }
            public string? Description { get; set; }
        }

        #endregion
    }
}