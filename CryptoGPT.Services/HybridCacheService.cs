using System;
using System.Threading.Tasks;
using CryptoGPT.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CryptoGPT.Services
{
    /// <summary>
    /// Hybrid cache implementation (currently in-memory only, Redis disabled)
    /// </summary>
    public class HybridCacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<HybridCacheService> _logger;

        public HybridCacheService(IMemoryCache memoryCache, ILogger<HybridCacheService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public Task<T?> GetAsync<T>(string key) where T : class
        {
            _memoryCache.TryGetValue(key, out T? value);
            return Task.FromResult(value);
        }

        public Task SetAsync<T>(string key, T value, int expirySeconds = 300)
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(expirySeconds)
            };
            _memoryCache.Set(key, value, options);
            _logger.LogDebug("[HybridCache] Cached {Type} data for key {Key} with {Expiry}s expiry", typeof(T).Name, key, expirySeconds);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            _memoryCache.Remove(key);
            _logger.LogDebug("[HybridCache] Removed {Key} from cache", key);
            return Task.CompletedTask;
        }

        public Task<bool> ContainsKeyAsync(string key)
        {
            return Task.FromResult(_memoryCache.TryGetValue(key, out _));
        }

        public Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, int expirySeconds = 300) where T : class
        {
            if (_memoryCache.TryGetValue(key, out T? value) && value != null)
            {
                return Task.FromResult(value);
            }
            return GetOrCreateInternalAsync(key, factory, expirySeconds);
        }

        private async Task<T> GetOrCreateInternalAsync<T>(string key, Func<Task<T>> factory, int expirySeconds) where T : class
        {
            var value = await factory();
            if (value != null)
            {
                await SetAsync(key, value, expirySeconds);
            }
            return value;
        }

        public bool IsConnected() => true;
    }
}
