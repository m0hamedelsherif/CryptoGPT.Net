using System.Collections.Generic;

namespace CryptoGPT.Application.Common.Models
{
    public class PriceHistoryPointDto
    {
        public long Timestamp { get; set; }
        public decimal Price { get; set; }
        public decimal? Volume { get; set; } // Add nullable Volume
    }
}