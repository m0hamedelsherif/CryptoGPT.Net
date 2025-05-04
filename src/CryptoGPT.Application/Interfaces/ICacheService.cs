using System;
using System.Threading.Tasks;

namespace CryptoGPT.Application.Interfaces
{
    /// <summary>
    /// Interface for caching services
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Get cached item by key
        /// </summary>
        /// <typeparam name="T">Type of the cached item</typeparam>
        /// <param name="key">Cache key</param>
        /// <returns>Cached item or null if not found</returns>
        Task<T?> GetAsync<T>(string key) where T : class;

        /// <summary>
        /// Set item in cache with expiration time
        /// </summary>
        /// <typeparam name="T">Type of the item to cache</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="value">Value to cache</param>
        /// <param name="expiry">Cache expiration timespan</param>
        /// <returns>True if successful</returns>
        Task<bool> SetAsync<T>(string key, T value, TimeSpan expiry) where T : class;

        /// <summary>
        /// Remove item from cache by key
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <returns>True if successful</returns>
        Task<bool> RemoveAsync(string key);

        /// <summary>
        /// Check if item exists in cache
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <returns>True if exists</returns>
        Task<bool> ExistsAsync(string key);
    }
}