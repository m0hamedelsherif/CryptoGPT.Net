using System;

namespace CryptoGPT.Domain.Entities
{
    /// <summary>
    /// Represents a news article with sentiment analysis
    /// </summary>
    public class NewsArticle
    {
        /// <summary>
        /// Unique identifier for the news article
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// The title of the article
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Summary or description of the article
        /// </summary>
        public string Summary { get; set; } = string.Empty;
        
        /// <summary>
        /// URL to the full article
        /// </summary>
        public string Url { get; set; } = string.Empty;
        
        /// <summary>
        /// Source of the article (publication or website)
        /// </summary>
        public string Source { get; set; } = string.Empty;
        
        /// <summary>
        /// Publication date and time
        /// </summary>
        public DateTime PublishedAt { get; set; }
        
        /// <summary>
        /// Sentiment analysis result (positive, negative, neutral)
        /// </summary>
        public string Sentiment { get; set; } = "neutral";
    }
}