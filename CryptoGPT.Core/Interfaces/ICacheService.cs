using System;
using System.Threading.Tasks;

namespace CryptoGPT.Core.Interfaces
{
    /// <summary>
    /// Interface for caching service
    /// Equivalent to the Python RedisCache class
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Get a value from cache
        /// </summary>
        /// <typeparam name="T">Type to deserialize to</typeparam>
        /// <param name="key">Cache key</param>
        /// <returns>Cached value or default if not found</returns>
        Task<T?> GetAsync<T>(string key) where T : class;

        /// <summary>
        /// Set a value in cache
        /// </summary>
        /// <typeparam name="T">Type to serialize</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="value">Value to cache</param>
        /// <param name="expirySeconds">Seconds until cache expiry</param>
        Task SetAsync<T>(string key, T value, int expirySeconds = 300);

        /// <summary>
        /// Remove a value from cache
        /// </summary>
        /// <param name="key">Cache key to remove</param>
        Task RemoveAsync(string key);

        /// <summary>
        /// Check if cache contains a key
        /// </summary>
        /// <param name="key">Key to check</param>
        /// <returns>True if key exists in cache</returns>
        Task<bool> ContainsKeyAsync(string key);

        /// <summary>
        /// Get or create cache item
        /// </summary>
        /// <typeparam name="T">Type to cache</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="factory">Function to create value if not in cache</param>
        /// <param name="expirySeconds">Seconds until cache expiry</param>
        /// <returns>Cached value or newly created value</returns>
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, int expirySeconds = 300) where T : class;
        
        /// <summary>
        /// Check if the cache service is connected and functioning
        /// </summary>
        /// <returns>True if cache service is available</returns>
        bool IsConnected();
    }
}