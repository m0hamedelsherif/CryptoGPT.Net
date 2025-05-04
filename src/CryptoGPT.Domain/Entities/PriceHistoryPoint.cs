using System.Text.Json.Serialization;

namespace CryptoGPT.Domain.Entities
{
    /// <summary>
    /// Represents a single point of price history data
    /// </summary>
    public class PriceHistoryPoint
    {
        /// <summary>
        /// Unix timestamp in milliseconds
        /// </summary>
        public long Timestamp { get; set; }
        
        /// <summary>
        /// Price value at the given timestamp
        /// </summary>
        public decimal Price { get; set; }
    }
}