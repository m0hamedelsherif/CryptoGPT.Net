using System;
using System.Collections.Generic;

namespace CryptoGPT.Domain.Entities
{
    /// <summary>
    /// Represents a cryptocurrency with market data
    /// </summary>
    public class CryptoCurrency
    {
        /// <summary>
        /// Unique identifier for the cryptocurrency
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Name of the cryptocurrency
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Symbol/ticker of the cryptocurrency
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Current price in USD
        /// </summary>
        public decimal PriceUsd { get; set; }

        /// <summary>
        /// Market capitalization in USD
        /// </summary>
        public decimal MarketCapUsd { get; set; }

        /// <summary>
        /// 24-hour trading volume in USD
        /// </summary>
        public decimal Volume24hUsd { get; set; }

        /// <summary>
        /// Price change percentage in the last 24 hours
        /// </summary>
        public decimal? PriceChangePercentage24h { get; set; }

        /// <summary>
        /// Price change percentage in the last 7 days
        /// </summary>
        public decimal PriceChangePercentage7d { get; set; }

        /// <summary>
        /// Available supply of the cryptocurrency
        /// </summary>
        public decimal CirculatingSupply { get; set; }

        /// <summary>
        /// Maximum supply of the cryptocurrency (if applicable)
        /// </summary>
        public decimal? MaxSupply { get; set; }

        /// <summary>
        /// Market rank by market capitalization
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// All-time high price in USD
        /// </summary>
        public decimal AllTimeHighUsd { get; set; }

        /// <summary>
        /// Date of the all-time high price
        /// </summary>
        public DateTime? AllTimeHighDate { get; set; }

        /// <summary>
        /// URL to the cryptocurrency's image/logo
        /// </summary>
        public string ImageUrl { get; set; } = string.Empty;

        /// <summary>
        /// Last updated timestamp
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Price history data points
        /// </summary>
        public List<PriceDataPoint>? PriceHistory { get; set; }

        // Aliases for compatibility with Application layer
        public decimal CurrentPrice { get => PriceUsd; set => PriceUsd = value; }

        public decimal MarketCap { get => MarketCapUsd; set => MarketCapUsd = value; }
        public decimal Volume24h { get => Volume24hUsd; set => Volume24hUsd = value; }
        public decimal? MarketCapRank { get; set; }
    }

    /// <summary>
    /// Represents a price data point for historical data
    /// </summary>
    public class PriceDataPoint
    {
        /// <summary>
        /// Timestamp of the price data point
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Price in USD at the given timestamp
        /// </summary>
        public decimal PriceUsd { get; set; }

        /// <summary>
        /// Trading volume in USD at the given timestamp
        /// </summary>
        public decimal? VolumeUsd { get; set; }

        /// <summary>
        /// Market capitalization in USD at the given timestamp
        /// </summary>
        public decimal? MarketCapUsd { get; set; }
    }
}