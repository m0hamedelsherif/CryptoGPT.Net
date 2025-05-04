using CryptoGPT.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CryptoGPT.Application.Interfaces
{
    /// <summary>
    /// Interface for technical analysis services
    /// </summary>
    public interface ITechnicalAnalysisService
    {
        /// <summary>
        /// Calculate technical indicators for a price series
        /// </summary>
        /// <param name="prices">Historical price points</param>
        /// <param name="indicators">Indicators to calculate with their parameters</param>
        /// <returns>Dictionary of indicator series by name</returns>
        Task<Dictionary<string, List<IndicatorTimePoint>>> CalculateIndicatorsAsync(List<PriceHistoryPoint> prices, Dictionary<string, IndicatorParameters> indicators);

        /// <summary>
        /// Generate comprehensive technical analysis for a cryptocurrency
        /// </summary>
        /// <param name="coinId">Coin identifier</param>
        /// <param name="prices">Historical price points</param>
        /// <param name="days">Number of days to analyze</param>
        /// <returns>Technical analysis results</returns>
        Task<TechnicalAnalysis> GetTechnicalAnalysisAsync(string coinId, List<PriceHistoryPoint> prices, int days = 30);
    }
}