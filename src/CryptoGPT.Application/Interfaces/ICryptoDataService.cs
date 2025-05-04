using CryptoGPT.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CryptoGPT.Application.Interfaces
{
    /// <summary>
    /// Interface for a multi-source cryptocurrency data service
    /// </summary>
    public interface ICryptoDataService
    {
        /// <summary>
        /// Get top cryptocurrencies by market cap
        /// </summary>
        /// <param name="limit">Maximum number of coins to return</param>
        /// <returns>List of cryptocurrency information</returns>
        Task<List<CryptoCurrency>> GetTopCoinsAsync(int limit = 10);

        /// <summary>
        /// Get detailed information for a specific cryptocurrency
        /// </summary>
        /// <param name="coinId">Coin identifier (e.g. "bitcoin")</param>
        /// <returns>Detailed cryptocurrency information</returns>
        Task<CryptoCurrencyDetail?> GetCoinDataAsync(string coinId);

        /// <summary>
        /// Get detailed information for a specific cryptocurrency
        /// </summary>
        /// <param name="coinId">Coin identifier (e.g. "bitcoin")</param>
        /// <returns>Detailed cryptocurrency information</returns>
        Task<CryptoCurrencyDetail?> GetCoinDetailsAsync(string coinId);

        /// <summary>
        /// Get historical market data for a cryptocurrency
        /// </summary>
        /// <param name="coinId">Coin identifier (e.g. "bitcoin")</param>
        /// <param name="days">Number of days of historical data to retrieve</param>
        /// <param name="indicators">Optional technical indicators to calculate</param>
        /// <returns>Market history data</returns>
        Task<MarketHistory?> GetMarketChartAsync(string coinId, int days = 30, Dictionary<string, IndicatorParameters>? indicators = null);

        /// <summary>
        /// Get historical market data for a cryptocurrency
        /// </summary>
        /// <param name="coinId">Coin identifier (e.g. "bitcoin")</param>
        /// <param name="days">Number of days of historical data to retrieve</param>
        /// <returns>Market history data</returns>
        Task<MarketHistory?> GetHistoricalDataAsync(string coinId, int days = 30);

        /// <summary>
        /// Get technical analysis for a cryptocurrency
        /// </summary>
        /// <param name="coinId">Coin identifier (e.g. "bitcoin")</param>
        /// <param name="days">Number of days to analyze</param>
        /// <returns>Technical analysis results</returns>
        Task<TechnicalAnalysis> GetTechnicalAnalysisAsync(string coinId, int days = 30);

        /// <summary>
        /// Get overall cryptocurrency market overview
        /// </summary>
        /// <returns>Market overview data</returns>
        Task<MarketOverview> GetMarketOverviewAsync();
    }
}