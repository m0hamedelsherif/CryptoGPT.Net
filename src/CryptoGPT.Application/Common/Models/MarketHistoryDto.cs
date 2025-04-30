using System.Collections.Generic;

namespace CryptoGPT.Application.Common.Models
{
    public class PriceHistoryPointDto
    {
        public long Timestamp { get; set; }
        public decimal Price { get; set; }
    }

    public class IndicatorTimePointDto
    {
        public long Timestamp { get; set; }
        public decimal Value { get; set; }
    }

    public class MarketHistoryDto
    {
        public string CoinId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public List<PriceHistoryPointDto> Prices { get; set; } = new List<PriceHistoryPointDto>();
        public List<PriceHistoryPointDto> MarketCaps { get; set; } = new List<PriceHistoryPointDto>();
        public List<PriceHistoryPointDto> Volumes { get; set; } = new List<PriceHistoryPointDto>();
        public Dictionary<string, List<IndicatorTimePointDto>> IndicatorSeries { get; set; } = new Dictionary<string, List<IndicatorTimePointDto>>();
    }
}