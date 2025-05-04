using System;
using System.Collections.Generic;

namespace CryptoGPT.Domain.Entities
{
    /// <summary>
    /// Represents a technical analysis of a cryptocurrency
    /// </summary>
    public class TechnicalAnalysis
    {
        /// <summary>
        /// The cryptocurrency ID
        /// </summary>
        public string CoinId { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp of the analysis
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// Period in days
        /// </summary>
        public int PeriodDays { get; set; }

        /// <summary>
        /// Overall analysis result
        /// </summary>
        public Overall Overall { get; set; } = new Overall();

        /// <summary>
        /// Current trend (up, down, sideways)
        /// </summary>
        public string Trend { get; set; } = "neutral";

        /// <summary>
        /// Strength of the signal (1-10)
        /// </summary>
        public int Strength { get; set; } = 5;

        /// <summary>
        /// Trading signal (buy, sell, hold)
        /// </summary>
        public string Signal { get; set; } = "hold";

        /// <summary>
        /// List of detected technical signals (for UI display)
        /// </summary>
        public List<Signal> SignalsList { get; set; } = new List<Signal>();

        /// <summary>
        /// Dictionary of signals returned by analysis
        /// </summary>
        public Dictionary<string, string> Signals { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Text analysis of trend
        /// </summary>
        public string TrendAnalysis { get; set; } = string.Empty;

        /// <summary>
        /// Technical indicators with their time series
        /// </summary>
        public Dictionary<string, List<IndicatorTimePoint>> Indicators { get; set; } = new Dictionary<string, List<IndicatorTimePoint>>();

        /// <summary>
        /// Latest values for each indicator
        /// </summary>
        public Dictionary<string, decimal> LatestValues { get; set; } = new Dictionary<string, decimal>();

        /// <summary>
        /// Groups of technical indicators organized by type
        /// </summary>
        public List<TechnicalIndicatorGroup> IndicatorGroups { get; set; } = new List<TechnicalIndicatorGroup>();

        /// <summary>
        /// Support levels
        /// </summary>
        public List<decimal> SupportLevels { get; set; } = new List<decimal>();

        /// <summary>
        /// Resistance levels
        /// </summary>
        public List<decimal> ResistanceLevels { get; set; } = new List<decimal>();

        /// <summary>
        /// Indicator time series data for charting
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

        /// <summary>
        /// The value of the signal
        /// </summary>
        public string Value { get; set; } = string.Empty;
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
    /// Class to hold Bollinger Bands values
    /// </summary>
    public class BollingerBandsPoint
    {
        public long Timestamp { get; set; }
        public decimal UpperBand { get; set; }
        public decimal MiddleBand { get; set; }
        public decimal LowerBand { get; set; }
    }
}