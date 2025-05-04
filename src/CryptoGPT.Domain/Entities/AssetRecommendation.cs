using System;

namespace CryptoGPT.Domain.Entities
{
    /// <summary>
    /// Represents an investment recommendation for a specific cryptocurrency asset.
    /// </summary>
    public class AssetRecommendation
    {
        public string CoinId { get; set; } = string.Empty;
        public string CoinName { get; set; } = string.Empty;
        public string CoinSymbol { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public string Recommendation { get; set; } = string.Empty; // buy, hold, sell
        public double Confidence { get; set; } // 0.0 to 1.0
        public string Summary { get; set; } = string.Empty;
    }
}
