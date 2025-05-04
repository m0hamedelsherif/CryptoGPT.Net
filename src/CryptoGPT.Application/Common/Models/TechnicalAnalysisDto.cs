using CryptoGPT.Domain.Entities;
using System;
using System.Collections.Generic;

namespace CryptoGPT.Application.Common.Models
{
    public class TechnicalAnalysisDto
    {
        public string CoinId { get; set; } = string.Empty;
        public string Signal { get; set; } = string.Empty;  // buy, sell, hold
        public int Strength { get; set; } = 0;  // 1-10
        public string Trend { get; set; } = string.Empty;  // bullish, bearish, neutral
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        public Dictionary<string, List<IndicatorTimePoint>> Indicators { get; set; } = new();
        public Dictionary<string, object> MetaData { get; set; } = new();

        // Indicator groups
        public Dictionary<string, object> MovingAverages { get; set; } = new();

        public Dictionary<string, object> Oscillators { get; set; } = new();
        public Dictionary<string, object> VolatilityIndicators { get; set; } = new();
        public Dictionary<string, object> VolumeIndicators { get; set; } = new();

        // Summary text
        public string Summary { get; set; } = string.Empty;

        public Dictionary<string, string> IndicatorSignals { get; set; } = new();
    }
}