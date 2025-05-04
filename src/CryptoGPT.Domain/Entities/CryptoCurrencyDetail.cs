using System;

namespace CryptoGPT.Domain.Entities
{
    /// <summary>
    /// Represents detailed cryptocurrency information
    /// </summary>
    public class CryptoCurrencyDetail : CryptoCurrency
    {
        /// <summary>
        /// Description of the cryptocurrency
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Official homepage URL
        /// </summary>
        public string Homepage { get; set; } = string.Empty;

        /// <summary>
        /// Highest price in the last 24 hours
        /// </summary>
        public decimal? High24h { get; set; }

        /// <summary>
        /// Lowest price in the last 24 hours
        /// </summary>
        public decimal? Low24h { get; set; }

        /// <summary>
        /// All-time high price
        /// </summary>
        public decimal AllTimeHigh { get; set; }

        /// <summary>
        /// Total supply of the cryptocurrency (null if unlimited)
        /// </summary>
        public decimal? TotalSupply { get; set; }

        /// <summary>
        /// All-time low price
        /// </summary>
        public decimal AllTimeLow { get; set; }

        /// <summary>
        /// Date when all-time low was reached
        /// </summary>
        public DateTime AllTimeLowDate { get; set; }

        /// <summary>
        /// Whitepaper URL
        /// </summary>
        public string? Whitepaper { get; set; }

        /// <summary>
        /// Blockchain site URL
        /// </summary>
        public string? BlockchainSite { get; set; }

        /// <summary>
        /// Twitter handle
        /// </summary>
        public string? Twitter { get; set; }

        /// <summary>
        /// Facebook page URL
        /// </summary>
        public string? Facebook { get; set; }

        /// <summary>
        /// Subreddit URL
        /// </summary>
        public string? Subreddit { get; set; }

        /// <summary>
        /// Hashing algorithm used
        /// </summary>
        public string? HashingAlgorithm { get; set; }

        /// <summary>
        /// Genesis date of the cryptocurrency
        /// </summary>
        public DateTime? GenesisDate { get; set; }

        /// <summary>
        /// Percentage of sentiment votes up
        /// </summary>
        public decimal? SentimentVotesUpPercentage { get; set; }

        /// <summary>
        /// Percentage of sentiment votes down
        /// </summary>
        public decimal? SentimentVotesDownPercentage { get; set; }

        public List<string>? Categories { get; set; }
    }
}