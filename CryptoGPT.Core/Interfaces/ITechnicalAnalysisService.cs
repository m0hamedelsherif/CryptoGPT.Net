using CryptoGPT.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CryptoGPT.Core.Interfaces
{
    /// <summary>
    /// Interface for technical analysis service
    /// Equivalent to the Python TechnicalAnalyzer class
    /// </summary>
    public interface ITechnicalAnalysisService
    {
        /// <summary>
        /// Perform technical analysis on a cryptocurrency
        /// </summary>
        /// <param name="symbol">Cryptocurrency symbol (e.g., "BTC")</param>
        /// <returns>Technical analysis results</returns>
        Task<TechnicalAnalysis> AnalyzeCryptoAsync(string symbol);
        
        /// <summary>
        /// Get available technical indicators
        /// </summary>
        /// <returns>List of available indicator names</returns>
        Task<List<string>> GetAvailableIndicatorsAsync();
    }
}