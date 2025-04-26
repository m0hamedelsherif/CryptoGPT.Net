using CryptoGPT.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CryptoGPT.Core.Interfaces
{
    /// <summary>
    /// Interface for the cryptocurrency recommendation engine
    /// Equivalent to the Python RecommendationEngine class
    /// </summary>
    public interface IRecommendationEngine
    {
        /// <summary>
        /// Generate personalized cryptocurrency recommendations based on user query and risk profile
        /// </summary>
        /// <param name="userQuery">User's query or investment goals</param>
        /// <param name="riskProfile">Risk tolerance profile</param>
        /// <returns>Dictionary containing recommendations and analysis</returns>
        Task<Dictionary<string, object>> GenerateRecommendationsAsync(string userQuery, RiskProfile riskProfile = RiskProfile.Moderate);
        
        /// <summary>
        /// Get a snapshot of the current cryptocurrency market
        /// </summary>
        /// <returns>Market data snapshot</returns>
        Task<Dictionary<string, object>> GetMarketSnapshotAsync();
    }
}