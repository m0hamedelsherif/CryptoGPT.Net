namespace CryptoGPT.Domain.Entities
{
    /// <summary>
    /// Represents a single point of technical indicator data
    /// </summary>
    public class IndicatorTimePoint
    {
        /// <summary>
        /// Unix timestamp in milliseconds
        /// </summary>
        public long Timestamp { get; set; }
        
        /// <summary>
        /// Value of the indicator at the given timestamp
        /// </summary>
        public decimal Value { get; set; }
    }
}