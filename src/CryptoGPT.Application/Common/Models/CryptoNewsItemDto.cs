// filepath: d:/PersonalWork/CryptoGPT.Net/src/CryptoGPT.Application/Common/Models/CryptoNewsItemDto.cs
using System;
using System.Collections.Generic;

namespace CryptoGPT.Application.Common.Models
{
    /// <summary>
    /// DTO for cryptocurrency news item, aligned with frontend model.
    /// </summary>
    public class CryptoNewsItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
        public string? Sentiment { get; set; }
        public List<string>? RelatedCoins { get; set; }
    }
}
