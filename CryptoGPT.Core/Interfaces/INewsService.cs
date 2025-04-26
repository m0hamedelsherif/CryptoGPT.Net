using CryptoGPT.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CryptoGPT.Core.Interfaces
{
    /// <summary>
    /// Interface for cryptocurrency news service
    /// Equivalent to the Python CryptoNewsService class
    /// </summary>
    public interface INewsService
    {
        /// <summary>
        /// Get latest cryptocurrency market news
        /// </summary>
        /// <param name="limit">Maximum number of news items to return</param>
        /// <returns>List of news items</returns>
        Task<List<CryptoNewsItem>> GetMarketNewsAsync(int limit = 20);

        /// <summary>
        /// Get news specific to a cryptocurrency
        /// </summary>
        /// <param name="coinId">Coin identifier (e.g. "bitcoin")</param>
        /// <param name="symbol">Coin symbol (e.g. "btc")</param>
        /// <param name="limit">Maximum number of news items to return</param>
        /// <returns>List of news items specific to the coin</returns>
        Task<List<CryptoNewsItem>> GetCoinNewsAsync(string coinId, string symbol, int limit = 10);
    }
}