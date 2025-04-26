using System.Text.Json;
using CryptoGPT.Core.Interfaces;
using CryptoGPT.Core.Models;
using Microsoft.Extensions.Logging;

namespace CryptoGPT.Services
{
    /// <summary>
    /// Provides technical analysis on crypto assets.
    /// </summary>
    public class TechnicalAnalysisService : ITechnicalAnalysisService
    {
        private readonly ICacheService _cacheService;
        private readonly IYahooFinanceService _yahooFinanceService;
        private readonly ILogger<TechnicalAnalysisService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        private const string TechnicalAnalysisKey = "tech_analysis_{0}";

        private readonly Dictionary<string, (Func<List<decimal>, Dictionary<string, object>> Calculate, decimal Weight, string Description, string Meaning)> _indicators;

        public TechnicalAnalysisService(IYahooFinanceService yahooFinanceService, ICacheService cacheService, ILogger<TechnicalAnalysisService> logger)
        {
            _yahooFinanceService = yahooFinanceService ?? throw new ArgumentNullException(nameof(yahooFinanceService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            _indicators = new Dictionary<string, (Func<List<decimal>, Dictionary<string, object>>, decimal, string, string)>
            {
                { "sma", (CalculateSimpleMovingAverage, 0.25m, "Simple Moving Average (SMA) shows average price over a period.", "Above SMA = bullish; below = bearish.") },
                { "ema", (CalculateExponentialMovingAverage, 0.20m, "Exponential Moving Average (EMA) gives more weight to recent prices.", "Above EMA = bullish; below = bearish.") },
                { "rsi", (CalculateRelativeStrengthIndex, 0.30m, "Relative Strength Index (RSI) measures speed of price changes.", "RSI >70 bearish; RSI <30 bullish.") },
                { "macd", (CalculateMACD, 0.15m, "MACD shows momentum changes.", "MACD line above signal line = bullish.") },
                { "bollinger", (CalculateBollingerBands, 0.10m, "Bollinger Bands measure price volatility.", "Price near lower band = bullish; near upper band = bearish.") }
            };
        }

        public async Task<TechnicalAnalysis> AnalyzeCryptoAsync(string symbol)
        {
            try
            {
                string cacheKey = string.Format(TechnicalAnalysisKey, symbol.ToLowerInvariant());
                var cachedResult = await _cacheService.GetAsync<TechnicalAnalysis>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogInformation("Retrieved technical analysis for {Symbol} from cache.", symbol);
                    return cachedResult;
                }
                
                // Use YahooFinanceService to get price data for technical analysis
                var prices = await _yahooFinanceService.GetPriceDataForTechnicalAnalysisAsync(symbol, 90);
                var historicalData = await _yahooFinanceService.GetMarketChartAsync(symbol, 30);

                if (prices.Count < 30)
                {
                    return new TechnicalAnalysis
                    {
                        Symbol = symbol,
                        Overall = new() { Type = "error", Value = "Not enough data for analysis" },
                        IndicatorGroups = new(),
                        Signals = new()
                    };
                }

                var analysis = new TechnicalAnalysis
                {
                    Symbol = symbol,
                    LastPrice = prices[^1],
                    IndicatorGroups = CalculateAllIndicatorGroups(prices),
                    Signals = new List<Signal>()
                };

                SetSignalsAndOverall(analysis);
                
                // Generate time series data for chart indicators
                if (historicalData?.Prices != null && historicalData.Prices.Count > 0)
                {
                    analysis.IndicatorSeries = GenerateIndicatorTimeSeriesData(historicalData.Prices, prices);
                }

                await _cacheService.SetAsync(cacheKey, analysis, 3600); // Cache for 1 hour
                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing {Symbol}: {Message}", symbol, ex.Message);
                return new TechnicalAnalysis
                {
                    Symbol = symbol,
                    Overall = new() { Type = "error", Value = ex.Message },
                    IndicatorGroups = new(),
                    Signals = new List<Signal>
                    {
                        new Signal { Name = "error", Type = "error", Description = ex.Message }
                    }
                };
            }
        }

        public Task<List<string>> GetAvailableIndicatorsAsync()
        {
            var indicators = _indicators.Select(x => $"{x.Key}: {x.Value.Description} ({x.Value.Meaning})").ToList();
            return Task.FromResult(indicators);
        }

        private List<TechnicalIndicatorGroup> CalculateAllIndicatorGroups(List<decimal> prices)
        {
            var groups = new List<TechnicalIndicatorGroup>();
            foreach (var (name, (calculateFunc, weight, description, meaning)) in _indicators)
            {
                var group = new TechnicalIndicatorGroup
                {
                    Type = name,
                    Weight = weight,
                    Description = description,
                    Meaning = meaning
                };

                var calculatedIndicators = calculateFunc(prices);
                foreach (var kv in calculatedIndicators)
                {
                    group.Indicators.Add(new TechnicalIndicatorValue
                    {
                        Name = kv.Key,
                        Value = kv.Value as decimal?
                    });
                }

                groups.Add(group);
            }
            return groups;
        }
        
        private Dictionary<string, List<IndicatorTimePoint>> GenerateIndicatorTimeSeriesData(List<PriceHistoryPoint> historicalPrices, List<decimal> prices)
        {
            var result = new Dictionary<string, List<IndicatorTimePoint>>();
            
            // Use only as many price points as we have historical timestamps
            var priceSeries = prices.Skip(Math.Max(0, prices.Count - historicalPrices.Count)).ToList();
            if (priceSeries.Count < 20) // Ensure minimum data points for indicators
                return result;
            
            // Get timestamps from historical data to align indicators with price chart
            var timestamps = historicalPrices.Select(p => p.Timestamp).ToList();
            
            // Calculate SMA series
            if (priceSeries.Count >= 14)
            {
                var sma14Series = new List<IndicatorTimePoint>();
                for (int i = 13; i < priceSeries.Count; i++)
                {
                    var sma = priceSeries.Skip(i - 13).Take(14).Average();
                    sma14Series.Add(new IndicatorTimePoint
                    {
                        Timestamp = timestamps[i],
                        Value = Math.Round(sma, 2)
                    });
                }
                result["sma_14"] = sma14Series;
            }
            
            // Calculate EMA series
            if (priceSeries.Count >= 14)
            {
                var ema14Series = new List<IndicatorTimePoint>();
                decimal ema = priceSeries.Take(14).Average();
                decimal multiplier = 2m / (14 + 1);
                
                for (int i = 13; i < priceSeries.Count; i++)
                {
                    ema = ((priceSeries[i] - ema) * multiplier) + ema;
                    ema14Series.Add(new IndicatorTimePoint
                    {
                        Timestamp = timestamps[i],
                        Value = Math.Round(ema, 2)
                    });
                }
                result["ema_14"] = ema14Series;
            }
            
            // Calculate Bollinger Bands series
            if (priceSeries.Count >= 20)
            {
                var upperBandSeries = new List<IndicatorTimePoint>();
                var middleBandSeries = new List<IndicatorTimePoint>();
                var lowerBandSeries = new List<IndicatorTimePoint>();
                
                for (int i = 19; i < priceSeries.Count; i++)
                {
                    var segment = priceSeries.Skip(i - 19).Take(20);
                    decimal sma = segment.Average();
                    decimal stdDev = (decimal)Math.Sqrt(segment.Average(v => Math.Pow((double)(v - sma), 2)));
                    
                    var upperBand = sma + (2 * stdDev);
                    var lowerBand = sma - (2 * stdDev);
                    
                    upperBandSeries.Add(new IndicatorTimePoint
                    {
                        Timestamp = timestamps[i],
                        Value = Math.Round(upperBand, 2)
                    });
                    
                    middleBandSeries.Add(new IndicatorTimePoint
                    {
                        Timestamp = timestamps[i],
                        Value = Math.Round(sma, 2)
                    });
                    
                    lowerBandSeries.Add(new IndicatorTimePoint
                    {
                        Timestamp = timestamps[i],
                        Value = Math.Round(lowerBand, 2)
                    });
                }
                
                result["bollinger_upper"] = upperBandSeries;
                result["bollinger_middle"] = middleBandSeries;
                result["bollinger_lower"] = lowerBandSeries;
            }
            
            // Calculate RSI series
            if (priceSeries.Count >= 15) // Need at least 1 day for diff + 14 for RSI
            {
                var rsiSeries = new List<IndicatorTimePoint>();
                var gains = new List<decimal>();
                var losses = new List<decimal>();
                
                for (int i = 1; i < priceSeries.Count; i++)
                {
                    decimal diff = priceSeries[i] - priceSeries[i - 1];
                    gains.Add(Math.Max(0, diff));
                    losses.Add(Math.Max(0, -diff));
                }
                
                decimal avgGain = gains.Take(14).Sum() / 14;
                decimal avgLoss = losses.Take(14).Sum() / 14;
                
                // First RSI point
                decimal rs = avgLoss == 0 ? avgGain : avgGain / avgLoss;
                decimal rsi = 100 - (100 / (1 + rs));
                
                rsiSeries.Add(new IndicatorTimePoint
                {
                    Timestamp = timestamps[14], // 14 days after start
                    Value = Math.Round(rsi, 2)
                });
                
                // Rest of RSI points
                for (int i = 14; i < gains.Count; i++)
                {
                    avgGain = ((avgGain * 13) + gains[i]) / 14;
                    avgLoss = ((avgLoss * 13) + losses[i]) / 14;
                    
                    rs = avgLoss == 0 ? avgGain : avgGain / avgLoss;
                    rsi = 100 - (100 / (1 + rs));
                    
                    rsiSeries.Add(new IndicatorTimePoint
                    {
                        Timestamp = timestamps[i + 1], // +1 because gains/losses are offset by 1
                        Value = Math.Round(rsi, 2)
                    });
                }
                
                result["rsi"] = rsiSeries;
            }
            
            // Calculate MACD series
            if (priceSeries.Count >= 26)
            {
                var macdSeries = new List<IndicatorTimePoint>();
                var signalSeries = new List<IndicatorTimePoint>();
                var histogramSeries = new List<IndicatorTimePoint>();
                
                // Calculate initial EMAs
                decimal ema12 = priceSeries.Take(12).Average();
                decimal ema26 = priceSeries.Take(26).Average();
                
                // Calculate multipliers
                decimal k12 = 2m / (12m + 1m);
                decimal k26 = 2m / (26m + 1m);
                decimal k9 = 2m / (9m + 1m);
                
                // Update EMAs for the rest of the period
                for (int i = 11; i < priceSeries.Count; i++)
                {
                    if (i >= 11) // Calculate EMA 12
                        ema12 = priceSeries[i] * k12 + ema12 * (1 - k12);
                    
                    if (i >= 25) // Calculate EMA 26
                    {
                        ema26 = priceSeries[i] * k26 + ema26 * (1 - k26);
                        
                        // Calculate MACD Line
                        decimal macdLine = ema12 - ema26;
                        
                        // For the first point, signal line equals MACD line
                        decimal signalLine = i == 25 ? macdLine : 
                            macdSeries[^1].Value * k9 + 
                            signalSeries[^1].Value * (1 - k9);
                            
                        // Calculate histogram
                        decimal histogram = macdLine - signalLine;
                        
                        // Add data points
                        macdSeries.Add(new IndicatorTimePoint
                        {
                            Timestamp = timestamps[i],
                            Value = Math.Round(macdLine, 4)
                        });
                        
                        signalSeries.Add(new IndicatorTimePoint
                        {
                            Timestamp = timestamps[i],
                            Value = Math.Round(signalLine, 4)
                        });
                        
                        histogramSeries.Add(new IndicatorTimePoint
                        {
                            Timestamp = timestamps[i],
                            Value = Math.Round(histogram, 4)
                        });
                    }
                }
                
                result["macd_line"] = macdSeries;
                result["signal_line"] = signalSeries;
                result["histogram"] = histogramSeries;
            }
            
            return result;
        }

        private void SetSignalsAndOverall(TechnicalAnalysis analysis)
        {
            decimal bullish = 0;
            decimal bearish = 0;
            var signals = new List<Signal>();

            foreach (var group in analysis.IndicatorGroups)
            {
                foreach (var indicator in group.Indicators)
                {
                    string signal = "neutral";

                    switch (group.Type)
                    {
                        case "sma":
                        case "ema":
                            if (analysis.LastPrice > (indicator.Value ?? 0))
                                signal = "bullish";
                            else
                                signal = "bearish";
                            break;

                        case "rsi":
                            if (indicator.Value > 70)
                                signal = "bearish";
                            else if (indicator.Value < 30)
                                signal = "bullish";
                            break;

                        case "macd":
                            if (indicator.Name == "histogram")
                            {
                                signal = (indicator.Value > 0) ? "bullish" : "bearish";
                            }
                            break;

                        case "bollinger":
                            if (indicator.Name == "upper_band" && analysis.LastPrice >= (indicator.Value ?? 0))
                                signal = "bearish";
                            else if (indicator.Name == "lower_band" && analysis.LastPrice <= (indicator.Value ?? 0))
                                signal = "bullish";
                            break;
                    }

                    if (signal == "bullish")
                        bullish += group.Weight ?? 0;
                    else if (signal == "bearish")
                        bearish += group.Weight ?? 0;

                    indicator.Signal = signal;
                    signals.Add(new Signal { Name = indicator.Name, Type = signal, Description = $"{indicator.Name} indicates {signal}" });
                }
            }

            decimal score = bullish - bearish;
            int scorePct = (int)(score * 100);

            if (scorePct > 10)
            {
                analysis.Overall.Type = "bullish";
                analysis.Overall.Value = $"{scorePct}%";
                signals.Add(new Signal { Name = "buy", Type = "buy", Description = "Buy signal based on aggregated indicators." });
            }
            else if (scorePct < -10)
            {
                analysis.Overall.Type = "bearish";
                analysis.Overall.Value = $"{Math.Abs(scorePct)}%";
                signals.Add(new Signal { Name = "sell", Type = "sell", Description = "Sell signal based on aggregated indicators." });
            }
            else
            {
                analysis.Overall.Type = "neutral";
                analysis.Overall.Value = $"{scorePct}%";
                signals.Add(new Signal { Name = "hold", Type = "hold", Description = "Hold signal based on aggregated indicators." });
            }

            analysis.Signals = signals;
        }

        #region Indicator Calculation Functions

        private Dictionary<string, object> CalculateSimpleMovingAverage(List<decimal> prices)
        {
            var result = new Dictionary<string, object>();
            int[] periods = { 7, 14, 30, 50, 200 };

            foreach (var period in periods)
            {
                if (prices.Count >= period)
                {
                    var avg = prices.Skip(prices.Count - period).Take(period).Average();
                    result[$"sma_{period}"] = Math.Round(avg, 2);
                }
            }
            return result;
        }

        private Dictionary<string, object> CalculateExponentialMovingAverage(List<decimal> prices)
        {
            var result = new Dictionary<string, object>();
            int[] periods = { 7, 14, 30, 50, 200 };

            foreach (var period in periods)
            {
                if (prices.Count >= period)
                {
                    decimal ema = prices.Take(period).Average();
                    decimal multiplier = 2m / (period + 1);

                    for (int i = period; i < prices.Count; i++)
                        ema = ((prices[i] - ema) * multiplier) + ema;

                    result[$"ema_{period}"] = Math.Round(ema, 2);
                }
            }
            return result;
        }

        private Dictionary<string, object> CalculateRelativeStrengthIndex(List<decimal> prices)
        {
            var result = new Dictionary<string, object>();
            int period = 14;

            if (prices.Count <= period) return result;

            var gains = new List<decimal>();
            var losses = new List<decimal>();

            for (int i = 1; i < prices.Count; i++)
            {
                var diff = prices[i] - prices[i - 1];
                gains.Add(Math.Max(0, diff));
                losses.Add(Math.Max(0, -diff));
            }

            decimal avgGain = gains.Take(period).Sum() / period;
            decimal avgLoss = losses.Take(period).Sum() / period;

            for (int i = period; i < gains.Count; i++)
            {
                avgGain = (avgGain * (period - 1) + gains[i]) / period;
                avgLoss = (avgLoss * (period - 1) + losses[i]) / period;
            }

            decimal rs = avgLoss == 0 ? avgGain : avgGain / avgLoss;
            decimal rsi = 100 - (100 / (1 + rs));

            result["rsi"] = Math.Round(rsi, 2);
            return result;
        }

        private Dictionary<string, object> CalculateMACD(List<decimal> prices)
        {
            var result = new Dictionary<string, object>();
            if (prices.Count < 26) return result;

            decimal ema12 = prices.Take(12).Average();
            decimal ema26 = prices.Take(26).Average();
            
            // Calculate multipliers
            decimal k12 = 2m / (12m + 1m);
            decimal k26 = 2m / (26m + 1m);
            
            // Update EMAs for the period
            for (int i = 12; i < prices.Count; i++)
            {
                if (i >= 12)
                    ema12 = prices[i] * k12 + ema12 * (1 - k12);
                
                if (i >= 26)
                    ema26 = prices[i] * k26 + ema26 * (1 - k26);
            }

            decimal macdLine = ema12 - ema26;
            decimal signalLine = macdLine * 0.9m; // Simple approximation
            decimal histogram = macdLine - signalLine;

            result["macd_line"] = Math.Round(macdLine, 4);
            result["signal_line"] = Math.Round(signalLine, 4);
            result["histogram"] = Math.Round(histogram, 4);

            return result;
        }

        private Dictionary<string, object> CalculateBollingerBands(List<decimal> prices)
        {
            var result = new Dictionary<string, object>();
            int period = 20;

            if (prices.Count < period) return result;

            var recent = prices.Skip(prices.Count - period).Take(period);
            decimal sma = recent.Average();
            decimal stdDev = (decimal)Math.Sqrt(recent.Average(v => Math.Pow((double)(v - sma), 2)));

            result["middle_band"] = Math.Round(sma, 2);
            result["upper_band"] = Math.Round(sma + (2 * stdDev), 2);
            result["lower_band"] = Math.Round(sma - (2 * stdDev), 2);

            return result;
        }

        #endregion Indicator Calculation Functions
    }
}