using CryptoGPT.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CryptoGPT.Application.Interfaces
{
    /// <summary>
    /// Interface for retrieving cryptocurrency news
    /// </summary>
    public interface INewsService
    {
        /// <summary>
        /// Get general market news
        /// </summary>
        Task<List<CryptoNewsItem>> GetMarketNewsAsync(int limit = 20);

        /// <summary>
        /// Get news for a specific coin
        /// </summary>
        Task<List<CryptoNewsItem>> GetCoinNewsAsync(string coinId, string symbol, int limit = 10);

        /// <summary>
        /// Get the latest news
        /// </summary>
        Task<List<CryptoNewsItem>> GetLatestNewsAsync(int limit = 20);
    }
}