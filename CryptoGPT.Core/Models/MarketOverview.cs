namespace CryptoGPT.Core.Models
{
    /// <summary>
    /// Represents a market overview with top gainers, losers, and volume data
    /// </summary>
    public class MarketOverview
    {
        public List<CryptoCurrency> TopGainers { get; set; } = new List<CryptoCurrency>();
        public List<CryptoCurrency> TopLosers { get; set; } = new List<CryptoCurrency>();
        public List<CryptoCurrency> TopByVolume { get; set; } = new List<CryptoCurrency>();
        public Dictionary<string, decimal?> MarketMetrics { get; set; } = new Dictionary<string, decimal?>();
    }
}