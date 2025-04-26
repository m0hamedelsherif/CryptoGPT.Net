using System;
using System.Text.Json;
using System.Threading.Tasks;
using CryptoGPT.Core.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace CryptoGPT.Services
{
    /// <summary>
    /// Redis cache implementation of ICacheService
    /// </summary>
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisCacheService(
            IDistributedCache cache,
            ILogger<RedisCacheService> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        /// <inheritdoc/>
        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                var data = await _cache.GetStringAsync(key);
                
                if (string.IsNullOrEmpty(data))
                {
                    return null;
                }

                return JsonSerializer.Deserialize<T>(data, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving {Key} from cache: {Message}", key, ex.Message);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task SetAsync<T>(string key, T value, int expirySeconds = 300)
        {
            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(expirySeconds)
                };

                var data = JsonSerializer.Serialize(value, _jsonOptions);
                await _cache.SetStringAsync(key, data, options);
                
                _logger.LogDebug("Cached {Type} data for key {Key} with {Expiry}s expiry", 
                    typeof(T).Name, key, expirySeconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error caching {Key}: {Message}", key, ex.Message);
            }
        }

        /// <inheritdoc/>
        public async Task RemoveAsync(string key)
        {
            try
            {
                await _cache.RemoveAsync(key);
                _logger.LogDebug("Removed {Key} from cache", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing {Key} from cache: {Message}", key, ex.Message);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ContainsKeyAsync(string key)
        {
            try
            {
                var data = await _cache.GetStringAsync(key);
                return !string.IsNullOrEmpty(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if cache contains {Key}: {Message}", key, ex.Message);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, int expirySeconds = 300) where T : class
        {
            try
            {
                // Try to get from cache first
                var data = await GetAsync<T>(key);
                
                // If found in cache, return it
                if (data != null)
                {
                    return data;
                }
                
                // Not found, create new data
                var newData = await factory();
                
                // Cache the new data
                if (newData != null)
                {
                    await SetAsync(key, newData, expirySeconds);
                }
                
                return newData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetOrCreate for {Key}: {Message}", key, ex.Message);
                
                // If cache fails, fallback to direct execution
                return await factory();
            }
        }

        /// <inheritdoc/>
        public bool IsConnected()
        {
            try
            {
                // We'll do a simple ping-like operation by setting and immediately getting a value
                var key = $"ping_{Guid.NewGuid():N}";
                _cache.SetString(key, "ping", new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5)
                });
                
                var result = _cache.GetString(key);
                return result == "ping";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis connectivity check failed: {Message}", ex.Message);
                return false;
            }
        }
    }
}