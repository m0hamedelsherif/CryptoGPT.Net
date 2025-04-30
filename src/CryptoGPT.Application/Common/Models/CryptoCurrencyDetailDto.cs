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
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Homepage { get; set; } = string.Empty;
        public string Whitepaper { get; set; } = string.Empty;
        public string BlockchainSite { get; set; } = string.Empty;
        public string Twitter { get; set; } = string.Empty;
        public string Facebook { get; set; } = string.Empty;
        public string Subreddit { get; set; } = string.Empty;
        public List<string> Categories { get; set; } = new();
        public string? HashingAlgorithm { get; set; }
        public string? GenesisDate { get; set; }
        public decimal? SentimentVotesUpPercentage { get; set; }
        public decimal? SentimentVotesDownPercentage { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal MarketCap { get; set; }
        public decimal? PriceChangePercentage24h { get; set; }
        public int? MarketCapRank { get; set; }
        public decimal Volume24h { get; set; }
        public decimal CirculatingSupply { get; set; }
        public decimal? TotalSupply { get; set; }
        public decimal? MaxSupply { get; set; }
        public decimal AllTimeHigh { get; set; }
        public DateTime AllTimeHighDate { get; set; }
        public decimal AllTimeLow { get; set; }
        public DateTime AllTimeLowDate { get; set; }
        public decimal? High24h { get; set; }
        public decimal? Low24h { get; set; }
    }
}