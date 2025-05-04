using CryptoGPT.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CryptoGPT.Application.Interfaces
{
    /// <summary>
    /// Interface for interacting with the CoinGecko API
    /// </summary>
    public interface ICoinGeckoService
    {
        /// <summary>
        /// Get top cryptocurrencies by market cap from CoinGecko
        /// </summary>
        /// <param name="limit">Maximum number of coins to return</param>
        /// <returns>List of cryptocurrency information</returns>
        Task<List<CryptoCurrency>> GetTopCoinsAsync(int limit = 10);

        /// <summary>
        /// Get detailed information for a specific cryptocurrency from CoinGecko
        /// </summary>
        /// <param name="coinId">Coin identifier (e.g. "bitcoin")</param>
        /// <returns>Detailed cryptocurrency information</returns>
        Task<CryptoCurrencyDetail?> GetCoinDataAsync(string coinId);

        /// <summary>
        /// Get historical market data for a cryptocurrency from CoinGecko
        /// </summary>
        /// <param name="coinId">Coin identifier (e.g. "bitcoin")</param>
        /// <param name="days">Number of days of historical data to retrieve</param>
        /// <returns>Market history data</returns>
        Task<MarketHistory?> GetMarketChartAsync(string coinId, int days = 30);

        /// <summary>
        /// Get market overview data from CoinGecko
        /// </summary>
        /// <returns>Market overview data</returns>
        Task<MarketOverview> GetMarketOverviewAsync();
    }
}