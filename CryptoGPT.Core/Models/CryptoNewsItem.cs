using System;
using System.Collections.Generic;

namespace CryptoGPT.Core.Models
{
    /// <summary>
    /// Represents news item related to cryptocurrency
    /// </summary>
    public class CryptoNewsItem
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
    }
}