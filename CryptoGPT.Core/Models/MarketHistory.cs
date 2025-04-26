namespace CryptoGPT.Core.Models
{
    /// <summary>
    /// Represents historical market data for a cryptocurrency
    /// </summary>
    public class MarketHistory
    {
        public string CoinId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public List<PriceHistoryPoint> Prices { get; set; } = new List<PriceHistoryPoint>();
        public List<PriceHistoryPoint> MarketCaps { get; set; } = new List<PriceHistoryPoint>();
        public List<PriceHistoryPoint> Volumes { get; set; } = new List<PriceHistoryPoint>();
        public Dictionary<string, List<IndicatorTimePoint>> IndicatorSeries { get; set; }
    }
}