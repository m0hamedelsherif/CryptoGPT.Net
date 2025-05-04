// filepath: d:/PersonalWork/CryptoGPT.Net/src/CryptoGPT.Application/Common/Models/MarketOverviewDto.cs
using System;
using System.Collections.Generic;

namespace CryptoGPT.Application.Common.Models
{
    /// <summary>
    /// DTO for market overview data, aligned with frontend model.
    /// </summary>
    public class MarketOverviewDto
    {
        public decimal TotalMarketCap { get; set; }
        public decimal TotalVolume24h { get; set; }
        public decimal BtcDominance { get; set; }
        public string MarketSentiment { get; set; } = string.Empty;
        public List<CryptoCurrencyDto> TopGainers { get; set; } = new();
        public List<CryptoCurrencyDto> TopLosers { get; set; } = new();
        public List<CryptoCurrencyDto> TopByVolume { get; set; } = new();
        public Dictionary<string, decimal>? MarketMetrics { get; set; } // Align with frontend Record<string, number>
    }
}
