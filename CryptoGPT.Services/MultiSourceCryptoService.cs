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
        public async Task<MarketHistory> GetExtendedMarketChartAsync(string coinId, int days = 30, 
            Dictionary<string, IndicatorParameters>? indicators = null)
        {
            // Calculate required data points based on indicator requirements
            int requiredDays = IndicatorParameters.CalculateRequiredDataPoints(indicators, days);
            
            _logger.LogInformation("Getting extended market chart for {CoinId} with {BaseDays} base days, " +
                "extended to {RequiredDays} days for indicator calculations", 
                coinId, days, requiredDays);
            
            // Get extended chart data
            MarketHistory chart = null;
            string source = await CheckSourceAvailabilityAsync();

            try
            {
                // Try to get data from the current source
                switch (source)
                {
                    case "coingecko":
                        try
                        {
                            chart = await _coinGeckoService.GetMarketChartAsync(coinId, requiredDays);
                        }
                        catch (HttpRequestException ex)
                        {
                            if (HandleCoinGeckoRateLimitError(ex))
                            {
                                // Recursive call with fallback
                                _logger.LogWarning("CoinGecko rate limited, trying fallback for {CoinId}", coinId);
                                return await GetExtendedMarketChartAsync(coinId, days, indicators);
                            }
                            throw; // Rethrow if not rate limit error
                        }
                        break;
                    
                    case "coincap":
                        try
                        {
                            chart = await _coinCapService.GetMarketChartAsync(coinId, requiredDays);
                            if (chart == null)
                            {
                                HandleCoinCapError(new Exception($"No market chart data for {coinId} in CoinCap"));
                                // Try with Yahoo Finance
                                chart = await _yahooFinanceService.GetMarketChartAsync(coinId, requiredDays);
                            }
                        }
                        catch (Exception ex)
                        {
                            HandleCoinCapError(ex);
                            // Try with Yahoo Finance
                            chart = await _yahooFinanceService.GetMarketChartAsync(coinId, requiredDays);
                        }
                        break;
                    
                    case "yahoo":
                    default:
                        chart = await _yahooFinanceService.GetMarketChartAsync(coinId, requiredDays);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get extended market chart for {CoinId} from all sources: {Error}", 
                    coinId, ex.Message);
                
                // Return a default object with error information
                chart = new MarketHistory { 
                    CoinId = coinId,
                    Symbol = coinId.ToUpperInvariant()
                };
                
                // Add an error indicator to communicate failure
                var errorPoint = new IndicatorTimePoint { 
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Value = 0
                };
                
                chart.IndicatorSeries = new Dictionary<string, List<IndicatorTimePoint>> {
                    { "error", new List<IndicatorTimePoint> { errorPoint } }
                };
                
                return chart;
            }

            // Process chart data
            if (chart != null)
            {
                // Downsample price data for display but keep full dataset for indicators
                var fullPrices = new List<PriceHistoryPoint>(chart.Prices);
                chart.Prices = Downsample(chart.Prices);
                
                // Try to get technical indicators if we have a symbol
                try 
                {
                    // Extract symbol from coinId or use the one in the chart
                    string symbol = chart.Symbol;
                    
                    // If symbol is empty, try common mappings or extract from coinId
                    if (string.IsNullOrEmpty(symbol))
                    {
                        symbol = coinId.ToUpperInvariant() switch
                        {
                            "BITCOIN" => "BTC",
                            "ETHEREUM" => "ETH",
                            "LITECOIN" => "LTC",
                            "RIPPLE" => "XRP",
                            "DOGECOIN" => "DOGE",
                            _ => coinId.ToUpperInvariant() // Use coinId as fallback
                        };
                    }
                    
                    if (!string.IsNullOrEmpty(symbol))
                    {
                        // Generate indicator time series based on full dataset
                        var indicatorSeries = await GenerateIndicatorsWithParametersAsync(
                            fullPrices, 
                            symbol,
                            indicators);
                            
                        if (indicatorSeries != null && indicatorSeries.Count > 0)
                        {
                            chart.IndicatorSeries = indicatorSeries;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate technical indicators for {CoinId}: {Error}", 
                        coinId, ex.Message);
                    // Continue without indicators
                    chart.IndicatorSeries = new Dictionary<string, List<IndicatorTimePoint>>();
                }
                
                return chart;
            }

            // Return empty chart with error info if all attempts failed
            _logger.LogError("Failed to retrieve extended market chart for {CoinId} from all sources", coinId);
            return new MarketHistory { 
                CoinId = coinId,
                IndicatorSeries = new Dictionary<string, List<IndicatorTimePoint>> {
                    { "error", new List<IndicatorTimePoint> { 
                        new() { 
                            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            Value = 0
                        } 
                    }}
                }
            };
        }

        /// <summary>
        /// Generates indicator time series data based on price history and provided parameters
        /// </summary>
        /// <param name="prices">Historical price data points</param>
        /// <param name="symbol">Symbol of the cryptocurrency</param>
        /// <param name="indicators">Optional dictionary of indicator parameters</param>
        /// <returns>Dictionary of indicator time series</returns>
        private async Task<Dictionary<string, List<IndicatorTimePoint>>> GenerateIndicatorsWithParametersAsync(
            List<PriceHistoryPoint> prices,
            string symbol,
            Dictionary<string, IndicatorParameters>? indicators = null)
        {
            var result = new Dictionary<string, List<IndicatorTimePoint>>();
            
            if (prices == null || prices.Count == 0)
            {
                return result;
            }
            
            // If no indicators specified, use default set
            if (indicators == null || indicators.Count == 0)
            {
                indicators = new Dictionary<string, IndicatorParameters>
                {
                    { "sma_20", IndicatorParameters.SMA(20) },
                    { "sma_50", IndicatorParameters.SMA(50) },
                    { "sma_200", IndicatorParameters.SMA(200) },
                    { "ema_20", IndicatorParameters.EMA(20) },
                    { "rsi_14", IndicatorParameters.RSI(14) },
                    { "macd", IndicatorParameters.MACD() },
                    { "bollinger_bands", IndicatorParameters.BollingerBands() }
                };
            }
            
            // Process each requested indicator
            foreach (var kvp in indicators)
            {
                string name = kvp.Key;
                var parameters = kvp.Value;
                
                try
                {
                    switch (parameters.Type.ToLowerInvariant())
                    {
                        case "sma":
                            result[name] = CalculateSMA(prices, parameters.Period);
                            break;
                            
                        case "ema":
                            result[name] = CalculateEMA(prices, parameters.Period);
                            break;
                            
                        case "rsi":
                            result[name] = CalculateRSI(prices, parameters.Period);
                            break;
                            
                        case "macd":
                            int fastPeriod = 12;
                            int slowPeriod = 26;
                            int signalPeriod = 9;
                            
                            if (parameters.AdditionalParameters.ContainsKey("fastPeriod"))
                                fastPeriod = Convert.ToInt32(parameters.AdditionalParameters["fastPeriod"]);
                                
                            if (parameters.AdditionalParameters.ContainsKey("slowPeriod"))
                                slowPeriod = Convert.ToInt32(parameters.AdditionalParameters["slowPeriod"]);
                                
                            if (parameters.AdditionalParameters.ContainsKey("signalPeriod"))
                                signalPeriod = Convert.ToInt32(parameters.AdditionalParameters["signalPeriod"]);
                            
                            var macdResult = CalculateMACD(prices, fastPeriod, slowPeriod, signalPeriod);
                            result[$"{name}_line"] = macdResult.Item1;
                            result[$"{name}_signal"] = macdResult.Item2;
                            result[$"{name}_histogram"] = macdResult.Item3;
                            break;
                            
                        case "bollinger":
                            double stdDevs = 2.0;
                            if (parameters.AdditionalParameters.ContainsKey("standardDeviations"))
                                stdDevs = Convert.ToDouble(parameters.AdditionalParameters["standardDeviations"]);
                                
                            var bbResult = CalculateBollingerBands(prices, parameters.Period, (decimal)stdDevs);
                            result[$"{name}_upper"] = bbResult.Item1;
                            result[$"{name}_middle"] = bbResult.Item2;
                            result[$"{name}_lower"] = bbResult.Item3;
                            break;
                            
                        default:
                            _logger.LogWarning("Unknown indicator type: {IndicatorType}", parameters.Type);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating indicator {IndicatorName} for {Symbol}: {Error}", 
                        name, symbol, ex.Message);
                }
            }
            
            return result;
        }

        /// <summary>
        /// Calculates Simple Moving Average for price data
        /// </summary>
        private List<IndicatorTimePoint> CalculateSMA(List<PriceHistoryPoint> prices, int period)
        {
            var result = new List<IndicatorTimePoint>();
            
            if (prices.Count < period)
                return result;
                
            for (int i = period - 1; i < prices.Count; i++)
            {
                decimal sum = 0;
                for (int j = 0; j < period; j++)
                {
                    sum += prices[i - j].Price;
                }
                
                var sma = sum / period;
                result.Add(new IndicatorTimePoint
                {
                    Timestamp = prices[i].Timestamp,
                    Value = Math.Round(sma, 2)
                });
            }
            
            return result;
        }
        
        /// <summary>
        /// Calculates Exponential Moving Average for price data
        /// </summary>
        private List<IndicatorTimePoint> CalculateEMA(List<PriceHistoryPoint> prices, int period)
        {
            var result = new List<IndicatorTimePoint>();
            
            if (prices.Count < period)
                return result;
                
            // Calculate first EMA as SMA
            decimal sum = 0;
            for (int i = 0; i < period; i++)
            {
                sum += prices[i].Price;
            }
            
            decimal ema = sum / period;
            result.Add(new IndicatorTimePoint
            {
                Timestamp = prices[period - 1].Timestamp,
                Value = Math.Round(ema, 2)
            });
            
            // Calculate multiplier
            decimal k = 2m / (period + 1);
            
            // Calculate EMA for remaining periods
            for (int i = period; i < prices.Count; i++)
            {
                ema = (prices[i].Price - ema) * k + ema;
                result.Add(new IndicatorTimePoint
                {
                    Timestamp = prices[i].Timestamp,
                    Value = Math.Round(ema, 2)
                });
            }
            
            return result;
        }
        
        /// <summary>
        /// Calculates Bollinger Bands (Upper, Middle, Lower)
        /// </summary>
        private (List<IndicatorTimePoint>, List<IndicatorTimePoint>, List<IndicatorTimePoint>) 
            CalculateBollingerBands(List<PriceHistoryPoint> prices, int period, decimal standardDeviations)
        {
            var upper = new List<IndicatorTimePoint>();
            var middle = new List<IndicatorTimePoint>();
            var lower = new List<IndicatorTimePoint>();
            
            if (prices.Count < period)
                return (upper, middle, lower);
                
            // Calculate SMA first (middle band)
            middle = CalculateSMA(prices, period);
            
            // Calculate upper and lower bands with standard deviation
            for (int i = period - 1; i < prices.Count; i++)
            {
                // Calculate standard deviation
                decimal sum = 0;
                for (int j = 0; j < period; j++)
                {
                    decimal diff = prices[i - j].Price - middle[i - (period - 1)].Value;
                    sum += diff * diff;
                }
                
                decimal stdDev = (decimal)Math.Sqrt((double)(sum / period));
                
                // Calculate bands
                decimal upperBand = middle[i - (period - 1)].Value + (stdDev * standardDeviations);
                decimal lowerBand = middle[i - (period - 1)].Value - (stdDev * standardDeviations);
                
                upper.Add(new IndicatorTimePoint
                {
                    Timestamp = prices[i].Timestamp,
                    Value = Math.Round(upperBand, 2)
                });
                
                lower.Add(new IndicatorTimePoint
                {
                    Timestamp = prices[i].Timestamp,
                    Value = Math.Round(lowerBand, 2)
                });
            }
            
            return (upper, middle, lower);
        }
        
        /// <summary>
        /// Calculates Relative Strength Index (RSI)
        /// </summary>
        private List<IndicatorTimePoint> CalculateRSI(List<PriceHistoryPoint> prices, int period)
        {
            var result = new List<IndicatorTimePoint>();
            
            if (prices.Count <= period)
                return result;
                
            List<decimal> gains = new List<decimal>();
            List<decimal> losses = new List<decimal>();
            
            // Calculate price changes
            for (int i = 1; i < prices.Count; i++)
            {
                decimal change = prices[i].Price - prices[i-1].Price;
                gains.Add(change > 0 ? change : 0);
                losses.Add(change < 0 ? -change : 0);
            }
            
            // Calculate initial average gain and loss
            decimal avgGain = gains.Take(period).Average();
            decimal avgLoss = losses.Take(period).Average();
            
            // Calculate first RSI
            decimal rs = avgLoss == 0 ? 100 : avgGain / avgLoss;
            decimal rsi = 100 - (100 / (1 + rs));
            
            result.Add(new IndicatorTimePoint
            {
                Timestamp = prices[period].Timestamp,
                Value = Math.Round(rsi, 2)
            });
            
            // Calculate remaining RSIs
            for (int i = period; i < gains.Count; i++)
            {
                // Use Wilder's smoothing method
                avgGain = ((avgGain * (period - 1)) + gains[i]) / period;
                avgLoss = ((avgLoss * (period - 1)) + losses[i]) / period;
                
                rs = avgLoss == 0 ? 100 : avgGain / avgLoss;
                rsi = 100 - (100 / (1 + rs));
                
                result.Add(new IndicatorTimePoint
                {
                    Timestamp = prices[i+1].Timestamp,
                    Value = Math.Round(rsi, 2)
                });
            }
            
            return result;
        }
        
        /// <summary>
        /// Calculates Moving Average Convergence Divergence (MACD)
        /// </summary>
        private (List<IndicatorTimePoint>, List<IndicatorTimePoint>, List<IndicatorTimePoint>) 
            CalculateMACD(List<PriceHistoryPoint> prices, int fastPeriod, int slowPeriod, int signalPeriod)
        {
            var macdLine = new List<IndicatorTimePoint>();
            var signalLine = new List<IndicatorTimePoint>();
            var histogram = new List<IndicatorTimePoint>();
            
            if (prices.Count < slowPeriod + signalPeriod)
                return (macdLine, signalLine, histogram);
                
            // Calculate fast and slow EMAs
            var fastEMA = CalculateEMA(prices, fastPeriod);
            var slowEMA = CalculateEMA(prices, slowPeriod);
            
            // Create the MACD line (fast EMA - slow EMA)
            for (int i = slowPeriod - fastPeriod; i < slowEMA.Count; i++)
            {
                var timestamp = slowEMA[i].Timestamp;
                int fastIndex = i + (slowPeriod - fastPeriod);
                
                if (fastIndex < fastEMA.Count)
                {
                    decimal macdValue = fastEMA[fastIndex].Value - slowEMA[i].Value;
                    
                    macdLine.Add(new IndicatorTimePoint
                    {
                        Timestamp = timestamp,
                        Value = Math.Round(macdValue, 4)
                    });
                }
            }
            
            // Calculate the signal line (9-day EMA of MACD line)
            if (macdLine.Count < signalPeriod)
                return (macdLine, signalLine, histogram);
                
            // Calculate first signal as SMA
            decimal sum = 0;
            for (int i = 0; i < signalPeriod; i++)
            {
                sum += macdLine[i].Value;
            }
            
            decimal signal = sum / signalPeriod;
            signalLine.Add(new IndicatorTimePoint
            {
                Timestamp = macdLine[signalPeriod - 1].Timestamp,
                Value = Math.Round(signal, 4)
            });
            
            // Calculate signal line for remaining periods
            decimal k = 2m / (signalPeriod + 1);
            for (int i = signalPeriod; i < macdLine.Count; i++)
            {
                signal = (macdLine[i].Value - signal) * k + signal;
                signalLine.Add(new IndicatorTimePoint
                {
                    Timestamp = macdLine[i].Timestamp,
                    Value = Math.Round(signal, 4)
                });
            }
            
            // Calculate histogram (MACD line - Signal line)
            for (int i = 0; i < signalLine.Count; i++)
            {
                int macdIndex = i + (signalPeriod - 1);
                histogram.Add(new IndicatorTimePoint
                {
                    Timestamp = signalLine[i].Timestamp,
                    Value = Math.Round(macdLine[macdIndex].Value - signalLine[i].Value, 4)
                });
            }
            
            return (macdLine, signalLine, histogram);
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