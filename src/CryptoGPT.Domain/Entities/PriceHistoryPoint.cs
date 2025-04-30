using System.Text.Json.Serialization;

namespace CryptoGPT.Domain.Entities
{
    /// <summary>
    /// Represents historical price data point for a cryptocurrency
    /// </summary>
    public class PriceHistoryPoint
    {
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }
    }
}