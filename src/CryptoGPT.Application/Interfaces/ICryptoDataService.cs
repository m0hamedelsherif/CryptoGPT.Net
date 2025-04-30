using CryptoGPT.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CryptoGPT.Application.Interfaces
{
    /// <summary>
    /// Interface for cryptocurrency data services
    /// </summary>
    public interface ICryptoDataService
    {
        /// <summary>
        /// Get a list of top cryptocurrencies by market cap
        /// </summary>
        /// <param name="limit">Maximum number of coins to return</param>
        /// <returns>List of cryptocurrency information</returns>
        Task<List<CryptoCurrency>> GetTopCoinsAsync(int limit = 10);

        /// <summary>
        /// Get detailed information for a specific cryptocurrency
        /// </summary>
        /// <param name="coinId">Coin identifier (e.g. "bitcoin")</param>
        /// <returns>Detailed cryptocurrency information or null if not found</returns>
        Task<CryptoCurrencyDetail?> GetCoinDataAsync(string coinId);

        /// <summary>
        /// Get historical market data for a cryptocurrency
        /// </summary>
        /// <param name="coinId">Coin identifier (e.g. "bitcoin")</param>
        /// <param name="days">Number of days of historical data to retrieve</param>
        /// <returns>Market history data</returns>
        Task<MarketHistory> GetMarketChartAsync(string coinId, int days = 30);
        
        /// <summary>
        /// Get historical market data for a cryptocurrency with extended data for technical analysis
        /// </summary>
        /// <param name="coinId">Coin identifier (e.g. "bitcoin")</param>
        /// <param name="days">Base number of days of historical data to retrieve</param>
        /// <param name="indicators">Dictionary of indicators and their parameters</param>
        /// <returns>Market history data with sufficient historical points for indicator calculations</returns>
        Task<MarketHistory> GetExtendedMarketChartAsync(string coinId, int days = 30, 
            Dictionary<string, IndicatorParameters>? indicators = null);

        /// <summary>
        /// Get market overview with top gainers, losers, and by volume
        /// </summary>
        /// <returns>Market overview information</returns>
        Task<MarketOverview> GetMarketOverviewAsync();

        /// <summary>
        /// Gets the name of the currently active data source
        /// </summary>
        /// <returns>Name of active data source (e.g. "coingecko", "coincap", "yahoo")</returns>
        string GetCurrentDataSource();
    }
}