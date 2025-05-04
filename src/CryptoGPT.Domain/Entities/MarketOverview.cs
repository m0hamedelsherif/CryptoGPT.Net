using System;
using System.Collections.Generic;

namespace CryptoGPT.Domain.Entities
{
    /// <summary>
    /// Represents a summary of the overall cryptocurrency market.
    /// </summary>
    public class MarketOverview
    {
        public DateTime GeneratedAt { get; set; }
        public string MarketSentiment { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<CryptoCurrency> TopPerformers { get; set; } = [];
        public List<CryptoCurrency> WorstPerformers { get; set; } = [];
        public List<CryptoCurrency> TopByVolume { get; set; } = [];
        public Dictionary<string, decimal?> MarketMetrics { get; set; } = [];
    }
}