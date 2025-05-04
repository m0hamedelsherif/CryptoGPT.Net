using System;
using System.Collections.Generic;

namespace CryptoGPT.Application.Common.Models
{
    /// <summary>
    /// DTO for detailed cryptocurrency information
    /// </summary>
    public class CryptoCurrencyDetailDto
    {
        public string Id { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; } // Ensure nullable
        public string ImageUrl { get; set; } = string.Empty;
        public string? Homepage { get; set; } // Ensure nullable
        public string? Whitepaper { get; set; } // Ensure nullable
        public string? BlockchainSite { get; set; } // Ensure nullable
        public string? Twitter { get; set; } // Ensure nullable
        public string? Facebook { get; set; } // Ensure nullable
        public string? Subreddit { get; set; } // Ensure nullable
        public List<string>? Categories { get; set; } = new(); // Ensure nullable
        public string? HashingAlgorithm { get; set; }
        public string? GenesisDate { get; set; }
        public decimal? SentimentVotesUpPercentage { get; set; }
        public decimal? SentimentVotesDownPercentage { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal MarketCap { get; set; }
        public decimal? PriceChangePercentage24h { get; set; }
        public decimal? MarketCapRank { get; set; }
        public decimal Volume24h { get; set; }
        public decimal? CirculatingSupply { get; set; } // Ensure nullable
        public decimal? TotalSupply { get; set; }
        public decimal? MaxSupply { get; set; }
        public decimal? AllTimeHigh { get; set; } // Ensure nullable
        public DateTime? AllTimeHighDate { get; set; } // Ensure nullable string
        public decimal? AllTimeLow { get; set; } // Ensure nullable
        public DateTime? AllTimeLowDate { get; set; } // Ensure nullable string
        public decimal? High24h { get; set; }
        public decimal? Low24h { get; set; }
    }
}