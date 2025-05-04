using CryptoGPT.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CryptoGPT.Application.Interfaces
{
    /// <summary>
    /// Interface for interacting with Yahoo Finance API
    /// </summary>
    public interface IYahooFinanceService
    {
        /// <summary>
        /// Get top cryptocurrencies by market cap from Yahoo Finance
        /// </summary>
        /// <param name="limit">Maximum number of coins to return</param>
        /// <returns>List of cryptocurrency information</returns>
        Task<List<CryptoCurrency>> GetTopCoinsAsync(int limit = 10);

        /// <summary>
        /// Get detailed information for a specific cryptocurrency from Yahoo Finance
        /// </summary>
        /// <param name="symbol">Yahoo Finance symbol (e.g. "BTC-USD")</param>
        /// <returns>Detailed cryptocurrency information</returns>
        Task<CryptoCurrencyDetail?> GetCoinDataAsync(string symbol);

        /// <summary>
        /// Get historical market data for a cryptocurrency from Yahoo Finance
        /// </summary>
        /// <param name="symbol">Yahoo Finance symbol (e.g. "BTC-USD")</param>
        /// <param name="days">Number of days of historical data to retrieve</param>
        /// <returns>Market history data</returns>
        Task<MarketHistory?> GetMarketChartAsync(string symbol, int days = 30);

        /// <summary>
        /// Convert standard coin ID to Yahoo Finance symbol format
        /// </summary>
        /// <param name="coinId">Coin identifier (e.g. "bitcoin")</param>
        /// <returns>Yahoo Finance symbol (e.g. "BTC-USD")</returns>
        string ConvertToYahooSymbol(string coinId);
    }
}