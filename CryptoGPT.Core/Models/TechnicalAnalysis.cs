using System.Collections.Generic;

namespace CryptoGPT.Core.Models
{
    /// <summary>
    /// Technical analysis results for a cryptocurrency
    /// </summary>
    public class TechnicalAnalysis
    {
        /// <summary>
        /// The cryptocurrency symbol
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// The last known price
        /// </summary>
        public decimal LastPrice { get; set; }

        /// <summary>
        /// Overall analysis result
        /// </summary>
        public Overall Overall { get; set; } = new Overall();

        /// <summary>
        /// List of signals from the analysis
        /// </summary>
        public List<Signal> Signals { get; set; } = new List<Signal>();

        /// <summary>
        /// Groups of technical indicators
        /// </summary>
        public List<TechnicalIndicatorGroup> IndicatorGroups { get; set; } = new List<TechnicalIndicatorGroup>();
        
        /// <summary>
        /// Time-series data for chart indicators
        /// </summary>
        public Dictionary<string, List<IndicatorTimePoint>> IndicatorSeries { get; set; } = new Dictionary<string, List<IndicatorTimePoint>>();
    }

    /// <summary>
    /// Overall analysis result
    /// </summary>
    public class Overall
    {
        /// <summary>
        /// The type of signal (bullish, bearish, neutral)
        /// </summary>
        public string Type { get; set; } = "neutral";

        /// <summary>
        /// The strength or confidence value of the signal
        /// </summary>
        public string Value { get; set; } = "0%";
    }

    /// <summary>
    /// A technical analysis signal
    /// </summary>
    public class Signal
    {
        /// <summary>
        /// The name of the signal
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The type of signal (buy, sell, hold)
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// A description of the signal
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Group of related technical indicators
    /// </summary>
    public class TechnicalIndicatorGroup
    {
        /// <summary>
        /// The type of indicator group
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Description of the indicator group
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Trading interpretation of the indicator
        /// </summary>
        public string Meaning { get; set; } = string.Empty;

        /// <summary>
        /// Weight of this indicator in the overall analysis
        /// </summary>
        public decimal? Weight { get; set; }

        /// <summary>
        /// List of indicator values
        /// </summary>
        public List<TechnicalIndicatorValue> Indicators { get; set; } = new List<TechnicalIndicatorValue>();
    }

    /// <summary>
    /// Technical indicator value
    /// </summary>
    public class TechnicalIndicatorValue
    {
        /// <summary>
        /// Name of the indicator
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Current value of the indicator
        /// </summary>
        public decimal? Value { get; set; }

        /// <summary>
        /// Signal from the indicator (bullish, bearish, neutral)
        /// </summary>
        public string Signal { get; set; } = "neutral";
    }
    
    /// <summary>
    /// Time point for indicator chart data
    /// </summary>
    public class IndicatorTimePoint
    {
        /// <summary>
        /// Timestamp in milliseconds
        /// </summary>
        public long Timestamp { get; set; }
        
        /// <summary>
        /// Value at this time point
        /// </summary>
        public decimal Value { get; set; }
    }
    
    /// <summary>
    /// Parameters for technical indicator calculation
    /// </summary>
    public class IndicatorParameters
    {
        /// <summary>
        /// Type of indicator (e.g., "rsi", "macd", "sma", "ema", "bollinger")
        /// </summary>
        public string Type { get; set; } = string.Empty;
        
        /// <summary>
        /// Primary period for the indicator
        /// </summary>
        public int Period { get; set; } = 14; // Default period
        
        /// <summary>
        /// Additional parameters as dictionary for complex indicators
        /// </summary>
        public Dictionary<string, object> AdditionalParameters { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Required data points for accurate calculation
        /// </summary>
        public int RequiredDataPoints { get; set; }
        
        /// <summary>
        /// Creates parameters for RSI indicator
        /// </summary>
        public static IndicatorParameters RSI(int period = 14)
        {
            return new IndicatorParameters 
            { 
                Type = "rsi", 
                Period = period,
                RequiredDataPoints = period + 35 // RSI needs more points for accuracy
            };
        }
        
        /// <summary>
        /// Creates parameters for MACD indicator
        /// </summary>
        public static IndicatorParameters MACD(int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9)
        {
            var parameters = new IndicatorParameters 
            { 
                Type = "macd", 
                Period = slowPeriod,
                RequiredDataPoints = slowPeriod + signalPeriod + 30 // MACD needs extended data
            };
            
            parameters.AdditionalParameters["fastPeriod"] = fastPeriod;
            parameters.AdditionalParameters["slowPeriod"] = slowPeriod;
            parameters.AdditionalParameters["signalPeriod"] = signalPeriod;
            
            return parameters;
        }
        
        /// <summary>
        /// Creates parameters for SMA indicator
        /// </summary>
        public static IndicatorParameters SMA(int period = 20)
        {
            return new IndicatorParameters 
            { 
                Type = "sma", 
                Period = period,
                RequiredDataPoints = period * 2 // Double the period for good accuracy
            };
        }
        
        /// <summary>
        /// Creates parameters for EMA indicator
        /// </summary>
        public static IndicatorParameters EMA(int period = 20)
        {
            return new IndicatorParameters 
            { 
                Type = "ema", 
                Period = period,
                RequiredDataPoints = period * 2 // Double the period for accuracy
            };
        }
        
        /// <summary>
        /// Creates parameters for Bollinger Bands indicator
        /// </summary>
        public static IndicatorParameters BollingerBands(int period = 20, double standardDeviations = 2.0)
        {
            var parameters = new IndicatorParameters 
            { 
                Type = "bollinger", 
                Period = period,
                RequiredDataPoints = period * 2 // Double the period for accuracy
            };
            
            parameters.AdditionalParameters["standardDeviations"] = standardDeviations;
            
            return parameters;
        }
        
        /// <summary>
        /// Calculate required data points based on indicator types
        /// </summary>
        public static int CalculateRequiredDataPoints(Dictionary<string, IndicatorParameters> indicators, int baseDays)
        {
            if (indicators == null || indicators.Count == 0)
                return baseDays;
                
            int maxRequired = baseDays;
            
            foreach (var indicator in indicators.Values)
            {
                maxRequired = Math.Max(maxRequired, indicator.RequiredDataPoints);
            }
            
            // Add 20% safety margin
            return (int)(maxRequired * 1.2);
        }
    }
}