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
        /// Gets a cached item by key
        /// </summary>
        /// <typeparam name="T">Type of the cached item</typeparam>
        /// <param name="key">Cache key</param>
        /// <returns>Cached item or default value if not found</returns>
        Task<T?> GetAsync<T>(string key) where T : class;

        /// <summary>
        /// Sets an item in the cache with an expiration time
        /// </summary>
        /// <typeparam name="T">Type of the cached item</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="value">Item to cache</param>
        /// <param name="expirationTime">Expiration time</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task SetAsync<T>(string key, T value, TimeSpan expirationTime) where T : class;

        /// <summary>
        /// Removes an item from the cache
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task RemoveAsync(string key);

        /// <summary>
        /// Determines whether a key exists in the cache
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <returns>True if the key exists; otherwise, false</returns>
        Task<bool> ExistsAsync(string key);
    }
}