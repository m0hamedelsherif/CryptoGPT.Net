// filepath: d:/PersonalWork/CryptoGPT.Net/src/CryptoGPT.Application/Common/Models/RiskProfileDto.cs
using System.Collections.Generic;

namespace CryptoGPT.Application.Common.Models
{
    /// <summary>
    /// DTO for user risk profile, aligned with frontend model.
    /// </summary>
    public class RiskProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string RiskTolerance { get; set; } = string.Empty; // Using string for 'low' | 'medium' | 'high'
        public string InvestmentTimeframe { get; set; } = string.Empty; // Using string for 'short' | 'medium' | 'long'
        public List<string> InvestmentGoals { get; set; } = new();
        public List<string>? PreferredCryptocurrencies { get; set; }
    }
}
