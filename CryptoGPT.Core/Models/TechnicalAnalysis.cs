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
}