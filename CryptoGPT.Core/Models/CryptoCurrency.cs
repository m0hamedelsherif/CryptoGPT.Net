using System;
using System.Collections.Generic;

namespace CryptoGPT.Core.Models
{
    /// <summary>
    /// Represents basic cryptocurrency information
    /// </summary>
    public class CryptoCurrency
    {
        public string Id { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public decimal MarketCap { get; set; }
        public decimal? PriceChangePercentage24h { get; set; }
        public decimal? MarketCapRank { get; set; }
        public decimal Volume24h { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }
}