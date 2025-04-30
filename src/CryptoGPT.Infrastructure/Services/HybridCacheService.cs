using System;
using System.Threading.Tasks;
using CryptoGPT.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CryptoGPT.Infrastructure.Services
{
    /// <summary>
    /// Hybrid cache implementation that uses memory cache
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

        public Task SetAsync<T>(string key, T value, TimeSpan expirationTime) where T : class
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expirationTime
            };
            _memoryCache.Set(key, value, options);
            _logger.LogDebug("[HybridCache] Cached {Type} data for key {Key} with {Expiry}s expiry", 
                typeof(T).Name, key, expirationTime.TotalSeconds);
            
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            _memoryCache.Remove(key);
            _logger.LogDebug("[HybridCache] Removed {Key} from cache", key);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key)
        {
            return Task.FromResult(_memoryCache.TryGetValue(key, out _));
        }
    }
}