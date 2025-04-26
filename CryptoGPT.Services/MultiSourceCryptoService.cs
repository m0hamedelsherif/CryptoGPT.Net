using Microsoft.Extensions.Logging;
using CryptoGPT.Core.Models; // Assuming your core models are in this namespace
using CryptoGPT.Core.Interfaces; // Assuming your service interfaces are here

namespace CryptoGPT.Services
{
    /// <summary>
    /// Class for fetching and processing cryptocurrency market data with multiple fallbacks:
    /// 1. CoinGecko API (primary source)
    /// 2. CoinCap API (first fallback when CoinGecko rate limits are reached or fails)
    /// 3. Yahoo Finance (second fallback when both CoinGecko and CoinCap fail)
    /// </summary>
    public class MultiSourceCryptoService : ICryptoDataService
    {
        private readonly ICoinGeckoService _coinGeckoService;
        private readonly ICoinCapService _coinCapService; // Hypothetical CoinCap service
        private readonly IYahooFinanceService _yahooFinanceService; // Hypothetical Yahoo Finance service
        private readonly ILogger<MultiSourceCryptoService> _logger;

        // CoinGecko Rate Limiting State
        private bool _coinGeckoRateLimited = false;

        private DateTime _coinGeckoRateLimitUntil = DateTime.MinValue;
        private const int CoinGeckoRateLimitResetSeconds = 60;

        // CoinCap Availability State
        private bool _coinCapUnavailable = false;

        private DateTime _coinCapRetryAfter = DateTime.MinValue;
        private const int CoinCapRetrySeconds = 300;

        // Current Active Data Source for Monitoring
        private string _currentSource = "coingecko";

        public MultiSourceCryptoService(
            ICoinGeckoService coinGeckoService,
            ICoinCapService coinCapService, // Inject the hypothetical CoinCap service
            IYahooFinanceService yahooFinanceService, // Inject the hypothetical Yahoo Finance service
            ILogger<MultiSourceCryptoService> logger)
        {
            _coinGeckoService = coinGeckoService ?? throw new ArgumentNullException(nameof(coinGeckoService));
            _coinCapService = coinCapService ?? throw new ArgumentNullException(nameof(coinCapService));
            _yahooFinanceService = yahooFinanceService ?? throw new ArgumentNullException(nameof(yahooFinanceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.LogInformation("MultiSourceCryptoService initialized with CoinGecko (primary), CoinCap (fallback 1), and Yahoo Finance (fallback 2)");
        }

        private async Task<string> CheckSourceAvailabilityAsync()
        {
            DateTime currentTime = DateTime.UtcNow;

            // Check if CoinGecko is available
            if (_coinGeckoRateLimited && currentTime > _coinGeckoRateLimitUntil)
            {
                _logger.LogInformation("CoinGecko rate limit cooldown expired, attempting to use CoinGecko again");
                _coinGeckoRateLimited = false;
                _currentSource = "coingecko";
                return "coingecko";
            }

            // If CoinGecko is rate limited, check CoinCap availability
            if (_coinGeckoRateLimited)
            {
                // Check if CoinCap is available
                if (_coinCapUnavailable && currentTime > _coinCapRetryAfter)
                {
                    _logger.LogInformation("CoinCap retry period expired, attempting to use CoinCap again");
                    _coinCapUnavailable = false;
                    _currentSource = "coincap";
                    return "coincap";
                }

                // If CoinCap is unavailable, use Yahoo Finance
                if (_coinCapUnavailable)
                {
                    _logger.LogDebug($"Using Yahoo Finance fallback. CoinCap unavailable for {(_coinCapRetryAfter - currentTime).TotalSeconds:F0} more seconds");
                    _currentSource = "yahoo";
                    return "yahoo";
                }

                // If CoinGecko is limited but CoinCap is available, use CoinCap
                _logger.LogDebug($"Using CoinCap fallback. CoinGecko rate limited for {(_coinGeckoRateLimitUntil - currentTime).TotalSeconds:F0} more seconds");
                _currentSource = "coincap";
                return "coincap";
            }

            // If CoinGecko is available, use it
            _currentSource = "coingecko";
            return "coingecko";
        }

        private bool HandleCoinGeckoRateLimitError(HttpRequestException ex)
        {
            if (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                DateTime currentTime = DateTime.UtcNow;
                _coinGeckoRateLimited = true;
                _coinGeckoRateLimitUntil = currentTime.AddSeconds(CoinGeckoRateLimitResetSeconds);
                _logger.LogWarning($"CoinGecko rate limit detected. Using fallback for {CoinGeckoRateLimitResetSeconds} seconds");
                return true;
            }
            return false;
        }

        private bool HandleCoinCapError(Exception ex)
        {
            DateTime currentTime = DateTime.UtcNow;
            _coinCapUnavailable = true;
            _coinCapRetryAfter = currentTime.AddSeconds(CoinCapRetrySeconds);
            _logger.LogWarning($"CoinCap API error detected. Using Yahoo Finance fallback for {CoinCapRetrySeconds} seconds: {ex.Message}");
            return true;
        }

        /// <summary>
        /// Downsamples a list of PriceHistoryPoint to a maximum number of points.
        /// </summary>
        private List<PriceHistoryPoint> Downsample(List<PriceHistoryPoint> points, int maxPoints = 200)
        {
            if (points == null || points.Count <= maxPoints)
                return points ?? new List<PriceHistoryPoint>();
            var result = new List<PriceHistoryPoint>();
            int step = (int)Math.Ceiling(points.Count / (double)maxPoints);
            for (int i = 0; i < points.Count; i += step)
            {
                result.Add(points[i]);
            }
            // Ensure last point is included
            if (result.Count == 0 || result[result.Count - 1].Timestamp != points[points.Count - 1].Timestamp)
            {
                result.Add(points[points.Count - 1]);
            }
            return result;
        }

        /// <inheritdoc/>
        public async Task<List<CryptoCurrency>> GetTopCoinsAsync(int limit = 10)
        {
            string source = await CheckSourceAvailabilityAsync();

            if (source == "coingecko")
            {
                try
                {
                    return await _coinGeckoService.GetTopCoinsAsync(limit);
                }
                catch (HttpRequestException ex)
                {
                    if (HandleCoinGeckoRateLimitError(ex))
                    {
                        return await GetTopCoinsAsync(limit); // Retry with fallback
                    }
                    _logger.LogError(ex, "Error fetching top coins from CoinGecko");
                    // Fall through to CoinCap
                    _coinGeckoRateLimited = true;
                    _coinGeckoRateLimitUntil = DateTime.UtcNow.AddSeconds(CoinGeckoRateLimitResetSeconds);
                }
            }

            if (source == "coincap")
            {
                try
                {
                    var coins = await _coinCapService.GetTopCoinsAsync(limit);
                    if (coins != null && coins.Count > 0)
                    {
                        return coins;
                    }
                    HandleCoinCapError(new Exception("Empty or null response from CoinCap"));
                }
                catch (Exception ex)
                {
                    HandleCoinCapError(ex);
                }
            }

            if (source == "yahoo")
            {
                try
                {
                    return await _yahooFinanceService.GetTopCoinsAsync(limit);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching top coins from Yahoo Finance");
                }
            }

            _logger.LogError($"Failed to retrieve top coins from all sources (last attempted: {source})");
            return new List<CryptoCurrency>(); // Return empty list as a last resort
        }

        /// <inheritdoc/>
        public async Task<CryptoCurrencyDetail?> GetCoinDataAsync(string coinId)
        {
            string source = await CheckSourceAvailabilityAsync();

            if (source == "coingecko")
            {
                try
                {
                    return await _coinGeckoService.GetCoinDataAsync(coinId);
                }
                catch (HttpRequestException ex)
                {
                    if (HandleCoinGeckoRateLimitError(ex))
                    {
                        return await GetCoinDataAsync(coinId); // Retry with fallback
                    }
                    _logger.LogError(ex, $"Error fetching data for {coinId} from CoinGecko");
                    _coinGeckoRateLimited = true;
                    _coinGeckoRateLimitUntil = DateTime.UtcNow.AddSeconds(CoinGeckoRateLimitResetSeconds);
                }
            }

            if (source == "coincap")
            {
                try
                {
                    var data = await _coinCapService.GetCoinDataAsync(coinId);
                    if (data != null)
                    {
                        return data;
                    }
                    HandleCoinCapError(new Exception($"No data found for {coinId} in CoinCap"));
                }
                catch (Exception ex)
                {
                    HandleCoinCapError(ex);
                }
            }

            if (source == "yahoo")
            {
                try
                {
                    return await _yahooFinanceService.GetCoinDataAsync(coinId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error fetching data for {coinId} from Yahoo Finance");
                }
            }

            _logger.LogError($"Failed to retrieve data for {coinId} from all sources (last attempted: {source})");
            return null;
        }

        /// <inheritdoc/>
        public async Task<MarketHistory> GetMarketChartAsync(string coinId, int days = 30)
        {
            string source = await CheckSourceAvailabilityAsync();

            if (source == "coingecko")
            {
                try
                {
                    var chart = await _coinGeckoService.GetMarketChartAsync(coinId, days);
                    chart.Prices = Downsample(chart.Prices);
                    return chart;
                }
                catch (HttpRequestException ex)
                {
                    if (HandleCoinGeckoRateLimitError(ex))
                    {
                        return await GetMarketChartAsync(coinId, days); // Retry with fallback
                    }
                    _logger.LogError(ex, $"Error fetching market chart for {coinId} from CoinGecko");
                    _coinGeckoRateLimited = true;
                    _coinGeckoRateLimitUntil = DateTime.UtcNow.AddSeconds(CoinGeckoRateLimitResetSeconds);
                }
            }

            if (source == "coincap")
            {
                try
                {
                    var chartData = await _coinCapService.GetMarketChartAsync(coinId, days);
                    if (chartData != null)
                    {
                        chartData.Prices = Downsample(chartData.Prices);
                        return chartData;
                    }
                    HandleCoinCapError(new Exception($"No market chart data for {coinId} in CoinCap"));
                }
                catch (Exception ex)
                {
                    HandleCoinCapError(ex);
                }
            }

            if (source == "yahoo")
            {
                try
                {
                    var chart = await _yahooFinanceService.GetMarketChartAsync(coinId, days);
                    if (chart != null)
                    {
                        chart.Prices = Downsample(chart.Prices);
                        return chart;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error fetching market chart for {coinId} from Yahoo Finance");
                }
            }

            _logger.LogError($"Failed to retrieve market chart for {coinId} from all sources (last attempted: {source})");
            return new MarketHistory { CoinId = coinId }; // Return a default object
        }

        /// <inheritdoc/>
        public Task<MarketOverview> GetMarketOverviewAsync()
        {
            // Implement fallback logic here if needed for MarketOverview
            // For now, we'll just call the CoinGecko service
            return _coinGeckoService.GetMarketOverviewAsync();
        }

        /// <inheritdoc/>
        public string GetCurrentDataSource()
        {
            return _currentSource;
        }
    }

    public interface ICoinGeckoService
    {
        Task<List<CryptoCurrency>> GetTopCoinsAsync(int limit = 10);

        Task<CryptoCurrencyDetail?> GetCoinDataAsync(string coinId);

        Task<MarketHistory> GetMarketChartAsync(string coinId, int days = 30);

        Task<MarketOverview> GetMarketOverviewAsync();
    }

    // Define interfaces for the fallback services (if they don't exist)
    public interface ICoinCapService
    {
        Task<List<CryptoCurrency>> GetTopCoinsAsync(int limit = 10);

        Task<CryptoCurrencyDetail?> GetCoinDataAsync(string coinId);

        Task<MarketHistory?> GetMarketChartAsync(string coinId, int days = 30);
    }

    public interface IYahooFinanceService
    {
        Task<List<CryptoCurrency>> GetTopCoinsAsync(int limit = 10);

        Task<CryptoCurrencyDetail?> GetCoinDataAsync(string coinId);

        Task<MarketHistory?> GetMarketChartAsync(string coinId, int days = 30);
        
        /// <summary>
        /// Gets price data specifically for technical analysis with proper caching
        /// </summary>
        /// <param name="symbol">The cryptocurrency symbol (e.g. BTC, ETH)</param>
        /// <param name="range">The time range to fetch data for (in days)</param>
        /// <returns>List of closing prices for the specified crypto</returns>
        Task<List<decimal>> GetPriceDataForTechnicalAnalysisAsync(string symbol, int range = 90);
    }
}