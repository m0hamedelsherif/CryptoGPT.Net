using CryptoGPT.Application.Interfaces;
using CryptoGPT.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CryptoGPT.Infrastructure.Services
{
    /// <summary>
    /// Technical analysis service for cryptocurrency data
    /// </summary>
    public class TechnicalAnalysisService : ITechnicalAnalysisService
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<TechnicalAnalysisService> _logger;
        private const string CacheKeyPrefix = "technical_analysis_";

        public TechnicalAnalysisService(ICacheService cacheService, ILogger<TechnicalAnalysisService> logger)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Calculate technical indicators for a price series
        /// </summary>
        public async Task<Dictionary<string, List<IndicatorTimePoint>>> CalculateIndicatorsAsync(
            List<PriceHistoryPoint> prices,
            Dictionary<string, IndicatorParameters> indicators)
        {
            if (prices == null || prices.Count == 0)
            {
                return new Dictionary<string, List<IndicatorTimePoint>>();
            }

            var result = new Dictionary<string, List<IndicatorTimePoint>>();

            try
            {
                // Convert price series to array for calculations
                var timestamp = prices.Select(p => p.Timestamp).ToArray();
                var priceValues = prices.Select(p => p.Price).ToArray();

                // Calculate each requested indicator
                foreach (var indicator in indicators)
                {
                    if (indicator.Key.StartsWith("SMA"))
                    {
                        result[indicator.Key] = CalculateSMA(timestamp, priceValues, indicator.Value);
                    }
                    else if (indicator.Key.StartsWith("EMA"))
                    {
                        result[indicator.Key] = CalculateEMA(timestamp, priceValues, indicator.Value);
                    }
                    else if (indicator.Key.StartsWith("RSI"))
                    {
                        result[indicator.Key] = CalculateRSI(timestamp, priceValues, indicator.Value);
                    }
                    else if (indicator.Key.StartsWith("MACD"))
                    {
                        result[indicator.Key] = CalculateMACD(timestamp, priceValues, indicator.Value);
                    }
                    else if (indicator.Key.StartsWith("BBANDS"))
                    {
                        var bbandsResult = CalculateBollingerBands(timestamp, priceValues, indicator.Value);
                        foreach (var band in bbandsResult)
                        {
                            result[band.Key] = band.Value;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Unsupported indicator: {Indicator}", indicator.Key);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating indicators: {Message}", ex.Message);
                return new Dictionary<string, List<IndicatorTimePoint>>();
            }
        }

        /// <summary>
        /// Generate comprehensive technical analysis for a cryptocurrency
        /// </summary>
        public async Task<TechnicalAnalysis> GetTechnicalAnalysisAsync(
            string coinId,
            List<PriceHistoryPoint> prices,
            int days = 30)
        {
            string cacheKey = $"{CacheKeyPrefix}{coinId}_{days}";

            try
            {
                if (prices == null || prices.Count == 0)
                {
                    throw new ArgumentException("Price series cannot be empty", nameof(prices));
                }

                // Create default indicators to calculate
                var indicators = new Dictionary<string, IndicatorParameters>
                {
                    { "SMA20", new IndicatorParameters { Period = 20 } },
                    { "SMA50", new IndicatorParameters { Period = 50 } },
                    { "SMA200", new IndicatorParameters { Period = 200 } },
                    { "EMA12", new IndicatorParameters { Period = 12 } },
                    { "EMA26", new IndicatorParameters { Period = 26 } },
                    { "RSI14", new IndicatorParameters { Period = 14 } },
                    { "MACD", new IndicatorParameters {
                        Period = 9, // Signal line
                        FastPeriod = 12,
                        SlowPeriod = 26
                    } },
                    { "BBANDS", new IndicatorParameters {
                        Period = 20,
                        Deviation = 2.0
                    } }
                };

                // Calculate all indicators
                var indicatorResults = await CalculateIndicatorsAsync(prices, indicators);

                // Extract the latest values for each indicator
                var latestValues = new Dictionary<string, decimal>();
                foreach (var indicator in indicatorResults)
                {
                    if (indicator.Value != null && indicator.Value.Count > 0)
                    {
                        latestValues[indicator.Key] = indicator.Value.Last().Value;
                    }
                }

                // Generate trend analysis
                string trendAnalysis = AnalyzeTrend(prices, indicatorResults);

                // Generate resistance and support levels
                var levels = CalculateSupportResistanceLevels(prices);

                // Generate signals
                var signals = GenerateSignals(prices, indicatorResults, latestValues);

                // Determine overall trend, signal and strength
                var overallAssessment = DetermineOverallAssessment(signals, indicatorResults, latestValues);

                // Create indicator groups for better organization
                var indicatorGroups = CreateIndicatorGroups(latestValues, signals);

                // Generate signal list for UI display
                var signalsList = GenerateSignalsList(signals);

                // Create the result
                var analysis = new TechnicalAnalysis
                {
                    CoinId = coinId,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    PeriodDays = days,
                    Indicators = indicatorResults,
                    LatestValues = latestValues,
                    TrendAnalysis = trendAnalysis,
                    SupportLevels = levels.SupportLevels,
                    ResistanceLevels = levels.ResistanceLevels,
                    Signals = signals,
                    Signal = overallAssessment.Signal,
                    Strength = overallAssessment.Strength,
                    Trend = overallAssessment.Trend,
                    Overall = new Overall
                    {
                        Type = overallAssessment.Trend,
                        Value = $"{overallAssessment.Strength * 10}%"
                    },
                    IndicatorGroups = indicatorGroups,
                    SignalsList = signalsList
                };

                // Cache the result
                await _cacheService.SetAsync(cacheKey, analysis, TimeSpan.FromHours(1));

                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating technical analysis: {Message}", ex.Message);

                return new TechnicalAnalysis
                {
                    CoinId = coinId,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    PeriodDays = days,
                    TrendAnalysis = "Error generating technical analysis",
                    Indicators = new Dictionary<string, List<IndicatorTimePoint>>(),
                    LatestValues = new Dictionary<string, decimal>(),
                    SupportLevels = new List<decimal>(),
                    ResistanceLevels = new List<decimal>(),
                    Signals = new Dictionary<string, string>(),
                    Signal = "hold",
                    Strength = 0,
                    Trend = "neutral"
                };
            }
        }

        /// <summary>
        /// Determine the overall assessment including trend, signal and strength
        /// </summary>
        private (string Trend, string Signal, int Strength) DetermineOverallAssessment(
            Dictionary<string, string> signals,
            Dictionary<string, List<IndicatorTimePoint>> indicators,
            Dictionary<string, decimal> latestValues)
        {
            // Default values
            string trend = "neutral";
            string signal = "hold";
            int strength = 5;

            // Count bullish and bearish signals
            int bullishCount = signals.Count(s => s.Value.StartsWith("BULLISH"));
            int bearishCount = signals.Count(s => s.Value.StartsWith("BEARISH"));
            int neutralCount = signals.Count(s => s.Value.StartsWith("NEUTRAL"));

            // Determine trend based on signal counts
            if (bullishCount > bearishCount)
            {
                trend = "bullish";
                // Determine strength based on proportion of bullish signals
                strength = Math.Min(10, 5 + (int)Math.Ceiling((double)bullishCount / (bullishCount + bearishCount + neutralCount) * 5));
            }
            else if (bearishCount > bullishCount)
            {
                trend = "bearish";
                // Determine strength based on proportion of bearish signals
                strength = Math.Min(10, 5 + (int)Math.Ceiling((double)bearishCount / (bullishCount + bearishCount + neutralCount) * 5));
            }
            else
            {
                // Equal signals or no signals
                trend = "neutral";
                strength = 5;
            }

            // RSI can modify strength
            if (latestValues.TryGetValue("RSI14", out decimal rsi))
            {
                if (rsi > 80) // Extremely overbought
                {
                    trend = "bearish";
                    strength = 9;
                }
                else if (rsi > 70) // Overbought
                {
                    if (trend == "bearish") strength = Math.Min(10, strength + 1);
                    if (trend == "bullish") strength = Math.Max(1, strength - 1);
                }
                else if (rsi < 20) // Extremely oversold
                {
                    trend = "bullish";
                    strength = 9;
                }
                else if (rsi < 30) // Oversold
                {
                    if (trend == "bullish") strength = Math.Min(10, strength + 1);
                    if (trend == "bearish") strength = Math.Max(1, strength - 1);
                }
            }

            // Bollinger Bands can modify strength
            if (latestValues.TryGetValue("BBANDS_UPPER", out decimal upperBand) &&
                latestValues.TryGetValue("BBANDS_MIDDLE", out decimal middleBand) &&
                latestValues.TryGetValue("BBANDS_LOWER", out decimal lowerBand))
            {
                if (latestValues.TryGetValue("Price", out decimal currentPrice))
                {
                    if (currentPrice > upperBand)
                    {
                        trend = "bearish";
                        strength = Math.Min(10, strength + 1);
                    }
                    else if (currentPrice < lowerBand)
                    {
                        trend = "bullish";
                        strength = Math.Min(10, strength + 1);
                    }
                }
            }

            // Determine buy/sell/hold signal
            if (trend == "bullish" && strength >= 7)
            {
                signal = "buy";
            }
            else if (trend == "bearish" && strength >= 7)
            {
                signal = "sell";
            }
            else
            {
                signal = "hold";
            }

            return (trend, signal, strength);
        }

        /// <summary>
        /// Create organized technical indicator groups
        /// </summary>
        private List<TechnicalIndicatorGroup> CreateIndicatorGroups(
            Dictionary<string, decimal> latestValues,
            Dictionary<string, string> signals)
        {
            var groups = new List<TechnicalIndicatorGroup>();

            // Create Moving Averages group
            var movingAverages = new TechnicalIndicatorGroup
            {
                Type = "Moving Averages",
                Description = "Trend following indicators that smooth price data",
                Meaning = "Moving averages help identify the direction of the trend",
                Weight = 0.4m,
                Indicators = new List<TechnicalIndicatorValue>()
            };

            // Add SMA indicators
            if (latestValues.TryGetValue("SMA20", out decimal sma20))
            {
                movingAverages.Indicators.Add(new TechnicalIndicatorValue
                {
                    Name = "SMA20",
                    Value = sma20,
                    Signal = DetermineMovingAverageSignal("SMA20", signals)
                });
            }

            if (latestValues.TryGetValue("SMA50", out decimal sma50))
            {
                movingAverages.Indicators.Add(new TechnicalIndicatorValue
                {
                    Name = "SMA50",
                    Value = sma50,
                    Signal = DetermineMovingAverageSignal("SMA50", signals)
                });
            }

            if (latestValues.TryGetValue("SMA200", out decimal sma200))
            {
                movingAverages.Indicators.Add(new TechnicalIndicatorValue
                {
                    Name = "SMA200",
                    Value = sma200,
                    Signal = DetermineMovingAverageSignal("SMA200", signals)
                });
            }

            // Add EMA indicators
            if (latestValues.TryGetValue("EMA12", out decimal ema12))
            {
                movingAverages.Indicators.Add(new TechnicalIndicatorValue
                {
                    Name = "EMA12",
                    Value = ema12,
                    Signal = DetermineMovingAverageSignal("EMA12", signals)
                });
            }

            if (latestValues.TryGetValue("EMA26", out decimal ema26))
            {
                movingAverages.Indicators.Add(new TechnicalIndicatorValue
                {
                    Name = "EMA26",
                    Value = ema26,
                    Signal = DetermineMovingAverageSignal("EMA26", signals)
                });
            }

            groups.Add(movingAverages);

            // Create Oscillators group
            var oscillators = new TechnicalIndicatorGroup
            {
                Type = "Oscillators",
                Description = "Momentum indicators that fluctuate between overbought and oversold levels",
                Meaning = "Oscillators help identify potential reversals in price",
                Weight = 0.3m,
                Indicators = new List<TechnicalIndicatorValue>()
            };

            // Add RSI
            if (latestValues.TryGetValue("RSI14", out decimal rsi14))
            {
                string rsiSignal = "neutral";
                if (rsi14 > 70) rsiSignal = "bearish";
                else if (rsi14 < 30) rsiSignal = "bullish";

                oscillators.Indicators.Add(new TechnicalIndicatorValue
                {
                    Name = "RSI14",
                    Value = rsi14,
                    Signal = rsiSignal
                });
            }

            // Add MACD
            if (latestValues.TryGetValue("MACD", out decimal macd))
            {
                string macdSignal = "neutral";
                if (signals.TryGetValue("MACD", out string macdSignalStr))
                {
                    if (macdSignalStr.Contains("BULLISH")) macdSignal = "bullish";
                    else if (macdSignalStr.Contains("BEARISH")) macdSignal = "bearish";
                }

                oscillators.Indicators.Add(new TechnicalIndicatorValue
                {
                    Name = "MACD",
                    Value = macd,
                    Signal = macdSignal
                });
            }

            groups.Add(oscillators);

            // Create Volatility Indicators group
            var volatilityIndicators = new TechnicalIndicatorGroup
            {
                Type = "Volatility Indicators",
                Description = "Measures of market volatility",
                Meaning = "Volatility indicators help identify potential breakouts",
                Weight = 0.3m,
                Indicators = new List<TechnicalIndicatorValue>()
            };

            // Add Bollinger Bands
            if (latestValues.TryGetValue("BBANDS_UPPER", out decimal upperBand) &&
                latestValues.TryGetValue("BBANDS_MIDDLE", out decimal middleBand) &&
                latestValues.TryGetValue("BBANDS_LOWER", out decimal lowerBand))
            {
                string bbandsSignal = "neutral";
                if (signals.TryGetValue("Bollinger Bands", out string bbandsSignalStr))
                {
                    if (bbandsSignalStr.Contains("BULLISH")) bbandsSignal = "bullish";
                    else if (bbandsSignalStr.Contains("BEARISH")) bbandsSignal = "bearish";
                }

                volatilityIndicators.Indicators.Add(new TechnicalIndicatorValue
                {
                    Name = "Bollinger Bands Upper",
                    Value = upperBand,
                    Signal = bbandsSignal
                });

                volatilityIndicators.Indicators.Add(new TechnicalIndicatorValue
                {
                    Name = "Bollinger Bands Middle",
                    Value = middleBand,
                    Signal = bbandsSignal
                });

                volatilityIndicators.Indicators.Add(new TechnicalIndicatorValue
                {
                    Name = "Bollinger Bands Lower",
                    Value = lowerBand,
                    Signal = bbandsSignal
                });
            }

            groups.Add(volatilityIndicators);

            return groups;
        }

        /// <summary>
        /// Determine the signal for a moving average
        /// </summary>
        private string DetermineMovingAverageSignal(string maName, Dictionary<string, string> signals)
        {
            // Check if we have a specific MA signal
            if (signals.TryGetValue(maName, out string specificSignal))
            {
                if (specificSignal.Contains("BULLISH")) return "bullish";
                if (specificSignal.Contains("BEARISH")) return "bearish";
            }

            // Check if we have a generic MA Crossover signal
            if (signals.TryGetValue("MA Crossover", out string crossoverSignal))
            {
                if (crossoverSignal.Contains("BULLISH") &&
                    (maName.Equals("SMA20") || maName.Equals("EMA12")))
                {
                    return "bullish";
                }

                if (crossoverSignal.Contains("BEARISH") &&
                    (maName.Equals("SMA20") || maName.Equals("EMA12")))
                {
                    return "bearish";
                }
            }

            return "neutral";
        }

        /// <summary>
        /// Generate list of signals for UI display
        /// </summary>
        private List<Signal> GenerateSignalsList(Dictionary<string, string> signals)
        {
            var signalsList = new List<Signal>();

            foreach (var signal in signals)
            {
                if (signal.Key == "Overall") continue; // Skip overall signal

                string type;
                if (signal.Value.StartsWith("BULLISH")) type = "buy";
                else if (signal.Value.StartsWith("BEARISH")) type = "sell";
                else type = "hold";

                signalsList.Add(new Signal
                {
                    Name = signal.Key,
                    Type = type,
                    Description = signal.Value.Replace("BULLISH: ", "").Replace("BEARISH: ", "").Replace("NEUTRAL: ", ""),
                    Value = signal.Value.Contains("BULLISH") ? "bullish" :
                           signal.Value.Contains("BEARISH") ? "bearish" : "neutral"
                });
            }

            return signalsList;
        }

        #region Technical Indicator Calculations

        /// <summary>
        /// Calculate Simple Moving Average (SMA)
        /// </summary>
        private List<IndicatorTimePoint> CalculateSMA(long[] timestamp, decimal[] prices, IndicatorParameters parameters)
        {
            int period = parameters.Period;
            if (period <= 0) period = 20; // Default period

            var result = new List<IndicatorTimePoint>();

            // Not enough data for calculation
            if (prices.Length < period)
            {
                return result;
            }

            // Initialize with nulls for periods where SMA can't be calculated
            for (int i = 0; i < period - 1; i++)
            {
                result.Add(new IndicatorTimePoint
                {
                    Timestamp = timestamp[i],
                    Value = 0 // Placeholder value
                });
            }

            // Calculate SMA for each point where we have enough data
            for (int i = period - 1; i < prices.Length; i++)
            {
                decimal sum = 0;
                for (int j = 0; j < period; j++)
                {
                    sum += prices[i - j];
                }

                decimal sma = sum / period;

                result.Add(new IndicatorTimePoint
                {
                    Timestamp = timestamp[i],
                    Value = sma
                });
            }

            return result;
        }

        /// <summary>
        /// Calculate Exponential Moving Average (EMA)
        /// </summary>
        private List<IndicatorTimePoint> CalculateEMA(long[] timestamp, decimal[] prices, IndicatorParameters parameters)
        {
            int period = parameters.Period;
            if (period <= 0) period = 20; // Default period

            var result = new List<IndicatorTimePoint>();

            // Not enough data for calculation
            if (prices.Length < period)
            {
                return result;
            }

            // Initialize with nulls for periods where EMA can't be calculated
            for (int i = 0; i < period - 1; i++)
            {
                result.Add(new IndicatorTimePoint
                {
                    Timestamp = timestamp[i],
                    Value = 0 // Placeholder value
                });
            }

            // Calculate first EMA as SMA
            decimal sum = 0;
            for (int i = 0; i < period; i++)
            {
                sum += prices[i];
            }
            decimal ema = sum / period;

            result.Add(new IndicatorTimePoint
            {
                Timestamp = timestamp[period - 1],
                Value = ema
            });

            // Calculate multiplier
            decimal multiplier = 2m / (period + 1m);

            // Calculate EMA for remaining points
            for (int i = period; i < prices.Length; i++)
            {
                ema = (prices[i] * multiplier) + (ema * (1 - multiplier));

                result.Add(new IndicatorTimePoint
                {
                    Timestamp = timestamp[i],
                    Value = ema
                });
            }

            return result;
        }

        /// <summary>
        /// Calculate Relative Strength Index (RSI)
        /// </summary>
        private List<IndicatorTimePoint> CalculateRSI(long[] timestamp, decimal[] prices, IndicatorParameters parameters)
        {
            int period = parameters.Period;
            if (period <= 0) period = 14; // Default period

            var result = new List<IndicatorTimePoint>();

            // Not enough data for calculation
            if (prices.Length <= period)
            {
                return result;
            }

            // Calculate price changes
            var changes = new decimal[prices.Length - 1];
            for (int i = 1; i < prices.Length; i++)
            {
                changes[i - 1] = prices[i] - prices[i - 1];
            }

            // Initialize with nulls for periods where RSI can't be calculated
            for (int i = 0; i < period; i++)
            {
                result.Add(new IndicatorTimePoint
                {
                    Timestamp = timestamp[i],
                    Value = 0 // Placeholder value
                });
            }

            // Calculate first RS
            decimal sumGain = 0;
            decimal sumLoss = 0;

            for (int i = 0; i < period; i++)
            {
                if (changes[i] > 0)
                    sumGain += changes[i];
                else
                    sumLoss += Math.Abs(changes[i]);
            }

            decimal avgGain = sumGain / period;
            decimal avgLoss = sumLoss / period;

            // Avoid division by zero
            if (avgLoss == 0)
            {
                result.Add(new IndicatorTimePoint
                {
                    Timestamp = timestamp[period],
                    Value = 100
                });
            }
            else
            {
                decimal rs = avgGain / avgLoss;
                decimal rsi = 100 - (100 / (1 + rs));

                result.Add(new IndicatorTimePoint
                {
                    Timestamp = timestamp[period],
                    Value = rsi
                });
            }

            // Calculate smoothed RSI for remaining points
            for (int i = period + 1; i < prices.Length; i++)
            {
                decimal change = prices[i] - prices[i - 1];
                decimal gain = change > 0 ? change : 0;
                decimal loss = change < 0 ? Math.Abs(change) : 0;

                // Smooth the averages
                avgGain = ((avgGain * (period - 1)) + gain) / period;
                avgLoss = ((avgLoss * (period - 1)) + loss) / period;

                // Avoid division by zero
                if (avgLoss == 0)
                {
                    result.Add(new IndicatorTimePoint
                    {
                        Timestamp = timestamp[i],
                        Value = 100
                    });
                }
                else
                {
                    decimal rs = avgGain / avgLoss;
                    decimal rsi = 100 - (100 / (1 + rs));

                    result.Add(new IndicatorTimePoint
                    {
                        Timestamp = timestamp[i],
                        Value = rsi
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Calculate MACD (Moving Average Convergence Divergence)
        /// </summary>
        private List<IndicatorTimePoint> CalculateMACD(long[] timestamp, decimal[] prices, IndicatorParameters parameters)
        {
            int fastPeriod = parameters.FastPeriod;
            int slowPeriod = parameters.SlowPeriod;
            int signalPeriod = parameters.Period;

            if (fastPeriod <= 0) fastPeriod = 12; // Default fast period
            if (slowPeriod <= 0) slowPeriod = 26; // Default slow period
            if (signalPeriod <= 0) signalPeriod = 9; // Default signal period

            var result = new List<IndicatorTimePoint>();

            // Not enough data for calculation
            if (prices.Length <= slowPeriod + signalPeriod)
            {
                return result;
            }

            // Calculate fast and slow EMAs
            var emaFastParams = new IndicatorParameters { Period = fastPeriod };
            var emaSlowParams = new IndicatorParameters { Period = slowPeriod };

            var emaFast = CalculateEMA(timestamp, prices, emaFastParams);
            var emaSlow = CalculateEMA(timestamp, prices, emaSlowParams);

            // Calculate MACD line (fast EMA - slow EMA)
            var macdLine = new decimal[prices.Length];
            for (int i = 0; i < slowPeriod - 1; i++)
            {
                // Not enough data for EMA calculation yet
                macdLine[i] = 0;
            }

            for (int i = slowPeriod - 1; i < prices.Length; i++)
            {
                macdLine[i] = emaFast[i].Value - emaSlow[i].Value;
            }

            // Calculate signal line (EMA of MACD line)
            // First, we need enough MACD values to calculate signal EMA
            var signalLine = new decimal[prices.Length];
            for (int i = 0; i < slowPeriod + signalPeriod - 2; i++)
            {
                signalLine[i] = 0;
            }

            // Calculate first signal as SMA of MACD
            decimal sum = 0;
            for (int i = slowPeriod - 1; i < slowPeriod + signalPeriod - 1; i++)
            {
                sum += macdLine[i];
            }
            decimal signal = sum / signalPeriod;
            signalLine[slowPeriod + signalPeriod - 2] = signal;

            // Calculate multiplier for signal EMA
            decimal multiplier = 2m / (signalPeriod + 1m);

            // Calculate signal EMA for remaining points
            for (int i = slowPeriod + signalPeriod - 1; i < prices.Length; i++)
            {
                signal = (macdLine[i] * multiplier) + (signal * (1 - multiplier));
                signalLine[i] = signal;
            }

            // Generate result as MACD histogram (MACD line - signal line)
            for (int i = 0; i < slowPeriod - 1; i++)
            {
                result.Add(new IndicatorTimePoint
                {
                    Timestamp = timestamp[i],
                    Value = 0 // Placeholder value
                });
            }

            for (int i = slowPeriod - 1; i < prices.Length; i++)
            {
                if (i >= slowPeriod + signalPeriod - 2)
                {
                    // We have both MACD and signal line
                    decimal histogram = macdLine[i] - signalLine[i];
                    result.Add(new IndicatorTimePoint
                    {
                        Timestamp = timestamp[i],
                        Value = histogram
                    });
                }
                else
                {
                    // We only have MACD line, no signal line yet
                    result.Add(new IndicatorTimePoint
                    {
                        Timestamp = timestamp[i],
                        Value = macdLine[i]
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Calculate Bollinger Bands
        /// </summary>
        private Dictionary<string, List<IndicatorTimePoint>> CalculateBollingerBands(
            long[] timestamp, decimal[] prices, IndicatorParameters parameters)
        {
            int period = parameters.Period;
            double deviation = (double)parameters.Deviation;

            if (period <= 0) period = 20; // Default period
            if (deviation <= 0) deviation = 2.0; // Default deviation

            var result = new Dictionary<string, List<IndicatorTimePoint>>();
            var upperBand = new List<IndicatorTimePoint>();
            var middleBand = new List<IndicatorTimePoint>();
            var lowerBand = new List<IndicatorTimePoint>();

            result["BBANDS_UPPER"] = upperBand;
            result["BBANDS_MIDDLE"] = middleBand;
            result["BBANDS_LOWER"] = lowerBand;

            if (prices.Length < period)
            {
                return result;
            }

            var smaParams = new IndicatorParameters { Period = period };
            var sma = CalculateSMA(timestamp, prices, smaParams);

            for (int i = 0; i < period - 1; i++)
            {
                upperBand.Add(new IndicatorTimePoint { Timestamp = timestamp[i], Value = 0 });
                middleBand.Add(new IndicatorTimePoint { Timestamp = timestamp[i], Value = 0 });
                lowerBand.Add(new IndicatorTimePoint { Timestamp = timestamp[i], Value = 0 });
            }

            for (int i = period - 1; i < prices.Length; i++)
            {
                double sum = 0;
                for (int j = 0; j < period; j++)
                {
                    double diff = (double)(prices[i - j] - sma[i].Value);
                    sum += diff * diff;
                }

                double stdDev = Math.Sqrt(sum / period);

                decimal midValue = sma[i].Value;
                decimal upperValue = midValue + (decimal)(stdDev * deviation);
                decimal lowerValue = midValue - (decimal)(stdDev * deviation);

                upperBand.Add(new IndicatorTimePoint { Timestamp = timestamp[i], Value = upperValue });
                middleBand.Add(new IndicatorTimePoint { Timestamp = timestamp[i], Value = midValue });
                lowerBand.Add(new IndicatorTimePoint { Timestamp = timestamp[i], Value = lowerValue });
            }

            return result;
        }

        #endregion Technical Indicator Calculations

        #region Analysis Methods

        /// <summary>
        /// Analyze price trend based on indicators
        /// </summary>
        private string AnalyzeTrend(List<PriceHistoryPoint> prices, Dictionary<string, List<IndicatorTimePoint>> indicators)
        {
            if (prices.Count < 200 ||
                !indicators.ContainsKey("SMA20") ||
                !indicators.ContainsKey("SMA50") ||
                !indicators.ContainsKey("SMA200"))
            {
                return "Insufficient data for trend analysis";
            }

            var sma20 = indicators["SMA20"];
            var sma50 = indicators["SMA50"];
            var sma200 = indicators["SMA200"];

            // Get latest values
            decimal latestPrice = prices.Last().Price;
            decimal latestSMA20 = sma20.Last().Value;
            decimal latestSMA50 = sma50.Last().Value;
            decimal latestSMA200 = sma200.Last().Value;

            string trendAnalysis;

            // Strong uptrend
            if (latestPrice > latestSMA20 && latestSMA20 > latestSMA50 && latestSMA50 > latestSMA200)
            {
                trendAnalysis = "Strong uptrend detected. Price is above all major moving averages with positive alignment.";
            }
            // Strong downtrend
            else if (latestPrice < latestSMA20 && latestSMA20 < latestSMA50 && latestSMA50 < latestSMA200)
            {
                trendAnalysis = "Strong downtrend detected. Price is below all major moving averages with negative alignment.";
            }
            // Weak uptrend or recovery
            else if (latestPrice > latestSMA20 && latestPrice > latestSMA50 && latestPrice < latestSMA200)
            {
                trendAnalysis = "Potential recovery or weak uptrend. Price is above short-term moving averages but remains below long-term moving average.";
            }
            // Weak downtrend or pullback
            else if (latestPrice < latestSMA20 && latestPrice < latestSMA50 && latestPrice > latestSMA200)
            {
                trendAnalysis = "Potential pullback or weak downtrend. Price is below short-term moving averages but remains above long-term moving average.";
            }
            // Consolidation or sideways
            else if (Math.Abs(latestSMA20 - latestSMA50) / latestSMA50 < 0.02m)
            {
                trendAnalysis = "Market appears to be in consolidation. Short-term moving averages are converging.";
            }
            else
            {
                trendAnalysis = "Mixed signals in the trend. No clear directional bias detected.";
            }

            // Add RSI analysis if available
            if (indicators.ContainsKey("RSI14"))
            {
                decimal latestRSI = indicators["RSI14"].Last().Value;

                if (latestRSI > 70)
                {
                    trendAnalysis += " RSI indicates overbought conditions, suggesting caution for buyers.";
                }
                else if (latestRSI < 30)
                {
                    trendAnalysis += " RSI indicates oversold conditions, suggesting potential for a bounce.";
                }
                else if (latestRSI >= 55 && latestRSI <= 70)
                {
                    trendAnalysis += " RSI shows strong bullish momentum but not yet overbought.";
                }
                else if (latestRSI >= 30 && latestRSI <= 45)
                {
                    trendAnalysis += " RSI shows bearish momentum but not yet oversold.";
                }
            }

            // Add MACD analysis if available
            if (indicators.ContainsKey("MACD"))
            {
                var macd = indicators["MACD"];
                if (macd.Count >= 2)
                {
                    decimal currentMACD = macd[macd.Count - 1].Value;
                    decimal previousMACD = macd[macd.Count - 2].Value;

                    if (currentMACD > 0 && previousMACD <= 0)
                    {
                        trendAnalysis += " MACD has recently crossed above zero, indicating bullish momentum shift.";
                    }
                    else if (currentMACD < 0 && previousMACD >= 0)
                    {
                        trendAnalysis += " MACD has recently crossed below zero, indicating bearish momentum shift.";
                    }
                    else if (currentMACD > previousMACD)
                    {
                        trendAnalysis += " MACD histogram is increasing, suggesting growing bullish momentum.";
                    }
                    else if (currentMACD < previousMACD)
                    {
                        trendAnalysis += " MACD histogram is decreasing, suggesting growing bearish momentum.";
                    }
                }
            }

            return trendAnalysis;
        }

        /// <summary>
        /// Calculate support and resistance levels
        /// </summary>
        private (List<decimal> SupportLevels, List<decimal> ResistanceLevels) CalculateSupportResistanceLevels(List<PriceHistoryPoint> prices)
        {
            if (prices.Count < 50)
            {
                return (new List<decimal>(), new List<decimal>());
            }

            decimal currentPrice = prices.Last().Price;
            var orderedPrices = prices.OrderBy(p => p.Price).Select(p => p.Price).ToList();

            // Calculate first quartile and third quartile
            int n = orderedPrices.Count;
            int q1Index = n / 4;
            int q3Index = (3 * n) / 4;

            decimal q1 = orderedPrices[q1Index];
            decimal q3 = orderedPrices[q3Index];

            // Find price clusters
            var clusters = FindPriceClusters(orderedPrices);

            // Separate into support and resistance levels based on current price
            var supportLevels = clusters.Where(c => c < currentPrice).OrderByDescending(c => c).Take(3).ToList();
            var resistanceLevels = clusters.Where(c => c > currentPrice).OrderBy(c => c).Take(3).ToList();

            // Ensure we have at least one support and resistance level
            if (supportLevels.Count == 0)
            {
                supportLevels.Add(q1); // Add first quartile as support if no clusters are found
            }

            if (resistanceLevels.Count == 0)
            {
                resistanceLevels.Add(q3); // Add third quartile as resistance if no clusters are found
            }

            return (supportLevels, resistanceLevels);
        }

        /// <summary>
        /// Find price clusters for support/resistance
        /// </summary>
        private List<decimal> FindPriceClusters(List<decimal> prices)
        {
            var clusters = new List<decimal>();
            decimal minPrice = prices.Min();
            decimal maxPrice = prices.Max();
            decimal priceRange = maxPrice - minPrice;

            // Define bin width based on price range (adjust divisor based on desired granularity)
            decimal binWidth = priceRange / 20;
            if (binWidth == 0) binWidth = 0.01m; // Handle case where all prices are the same

            // Count occurrences in each bin
            var bins = new Dictionary<int, int>();
            foreach (decimal price in prices)
            {
                int binIndex = (int)((price - minPrice) / binWidth);
                if (bins.ContainsKey(binIndex))
                {
                    bins[binIndex]++;
                }
                else
                {
                    bins[binIndex] = 1;
                }
            }

            // Find bins with high frequencies (local maxima)
            var sortedBins = bins.OrderByDescending(b => b.Value).Take(10);

            foreach (var bin in sortedBins)
            {
                decimal centerPrice = minPrice + (bin.Key * binWidth) + (binWidth / 2);
                clusters.Add(centerPrice);
            }

            return clusters;
        }

        /// <summary>
        /// Generate trading signals based on indicators
        /// </summary>
        private Dictionary<string, string> GenerateSignals(
            List<PriceHistoryPoint> prices,
            Dictionary<string, List<IndicatorTimePoint>> indicators,
            Dictionary<string, decimal> latestValues)
        {
            var signals = new Dictionary<string, string>();

            if (prices.Count < 30)
            {
                signals["Error"] = "Insufficient data for signal generation";
                return signals;
            }

            decimal currentPrice = prices.Last().Price;

            // Moving Average Crossover Signals
            if (indicators.ContainsKey("SMA20") && indicators.ContainsKey("SMA50"))
            {
                var sma20 = indicators["SMA20"];
                var sma50 = indicators["SMA50"];

                if (sma20.Count > 1 && sma50.Count > 1)
                {
                    bool currentCrossover = sma20.Last().Value > sma50.Last().Value;
                    bool previousCrossover = sma20[sma20.Count - 2].Value > sma50[sma50.Count - 2].Value;

                    if (currentCrossover && !previousCrossover)
                    {
                        signals["MA Crossover"] = "BULLISH: Short-term MA crossed above medium-term MA";
                    }
                    else if (!currentCrossover && previousCrossover)
                    {
                        signals["MA Crossover"] = "BEARISH: Short-term MA crossed below medium-term MA";
                    }
                }
            }

            // RSI Signals
            if (indicators.ContainsKey("RSI14"))
            {
                decimal rsi = latestValues["RSI14"];

                if (rsi > 70)
                {
                    signals["RSI"] = "BEARISH: Overbought (RSI > 70)";
                }
                else if (rsi < 30)
                {
                    signals["RSI"] = "BULLISH: Oversold (RSI < 30)";
                }
            }

            // MACD Signals
            if (indicators.ContainsKey("MACD"))
            {
                var macd = indicators["MACD"];

                if (macd.Count >= 2)
                {
                    decimal currentMACD = macd[macd.Count - 1].Value;
                    decimal previousMACD = macd[macd.Count - 2].Value;

                    if (currentMACD > 0 && previousMACD <= 0)
                    {
                        signals["MACD"] = "BULLISH: MACD crossed above zero line";
                    }
                    else if (currentMACD < 0 && previousMACD >= 0)
                    {
                        signals["MACD"] = "BEARISH: MACD crossed below zero line";
                    }
                    else if (currentMACD > previousMACD && currentMACD < 0)
                    {
                        signals["MACD"] = "NEUTRAL/BULLISH: MACD histogram increasing but below zero";
                    }
                    else if (currentMACD < previousMACD && currentMACD > 0)
                    {
                        signals["MACD"] = "NEUTRAL/BEARISH: MACD histogram decreasing but above zero";
                    }
                }
            }

            // Bollinger Band Signals
            if (indicators.ContainsKey("BBANDS_UPPER") &&
                indicators.ContainsKey("BBANDS_MIDDLE") &&
                indicators.ContainsKey("BBANDS_LOWER"))
            {
                decimal upperBand = latestValues["BBANDS_UPPER"];
                decimal middleBand = latestValues["BBANDS_MIDDLE"];
                decimal lowerBand = latestValues["BBANDS_LOWER"];

                if (currentPrice > upperBand)
                {
                    signals["Bollinger Bands"] = "BEARISH: Price above upper Bollinger Band";
                }
                else if (currentPrice < lowerBand)
                {
                    signals["Bollinger Bands"] = "BULLISH: Price below lower Bollinger Band";
                }
                else
                {
                    double bandWidth = (double)((upperBand - lowerBand) / middleBand);
                    if (bandWidth < 0.1) // Tight bands (adjust threshold as needed)
                    {
                        signals["Bollinger Bands"] = "NEUTRAL: Tight Bollinger Bands suggest potential breakout";
                    }
                }
            }

            // Generate overall signal
            int bullishSignals = signals.Count(s => s.Value.StartsWith("BULLISH"));
            int bearishSignals = signals.Count(s => s.Value.StartsWith("BEARISH"));

            if (bullishSignals > bearishSignals)
            {
                signals["Overall"] = "BULLISH: More bullish signals than bearish";
            }
            else if (bearishSignals > bullishSignals)
            {
                signals["Overall"] = "BEARISH: More bearish signals than bullish";
            }
            else
            {
                signals["Overall"] = "NEUTRAL: Mixed signals or consolidation";
            }

            return signals;
        }

        #endregion Analysis Methods
    }
}