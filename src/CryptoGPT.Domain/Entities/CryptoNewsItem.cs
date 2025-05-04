using System;

namespace CryptoGPT.Domain.Entities
{
    /// <summary>
    /// Represents a cryptocurrency news article
    /// </summary>
    public class CryptoNewsItem
    {
        /// <summary>
        /// Unique identifier for the news item
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// The title of the news article
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Summary or brief description of the article
        /// </summary>
        public string Summary { get; set; } = string.Empty;
        
        /// <summary>
        /// Detailed description of the article
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// URL to the full news article
        /// </summary>
        public string Url { get; set; } = string.Empty;
        
        /// <summary>
        /// URL to the news article image
        /// </summary>
        public string ImageUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Source of the news (publication or website)
        /// </summary>
        public string Source { get; set; } = string.Empty;
        
        /// <summary>
        /// Publication date and time
        /// </summary>
        public DateTime PublishedAt { get; set; }
        
        /// <summary>
        /// Optional sentiment analysis result (positive, negative, neutral)
        /// </summary>
        public string Sentiment { get; set; } = "neutral";
        
        /// <summary>
        /// Optional list of cryptocurrencies mentioned in the article
        /// </summary>
        public string[] RelatedCoins { get; set; } = Array.Empty<string>();
    }
}