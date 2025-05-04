using CryptoGPT.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace CryptoGPT.Infrastructure.Services
{
    /// <summary>
    /// Hybrid cache implementation that combines in-memory cache with Redis
    /// Falls back to in-memory only if Redis is not available
    /// </summary>
    public class HybridCacheService : ICacheService
    {
        private readonly RedisCacheService? _redisCache;
        private readonly ILogger<HybridCacheService> _logger;
        private readonly ConcurrentDictionary<string, CacheItem> _memoryCache = new();
        
        private class CacheItem
        {
            public object Value { get; set; } = null!;
            public DateTime ExpirationTime { get; set; }
        }

        public HybridCacheService(RedisCacheService? redisCache, ILogger<HybridCacheService> logger)
        {
            _redisCache = redisCache;
            _logger = logger;
        }

        /// <summary>
        /// Get cached item by key
        /// </summary>
        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            // Try Redis first if available
            if (_redisCache != null)
            {
                try
                {
                    var redisResult = await _redisCache.GetAsync<T>(key);
                    if (redisResult != null)
                    {
                        _logger.LogDebug("Redis cache hit for key: {Key}", key);
                        return redisResult;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Redis cache error, falling back to memory cache: {Message}", ex.Message);
                }
            }
            
            // Try memory cache as fallback
            if (_memoryCache.TryGetValue(key, out var item))
            {
                if (DateTime.UtcNow <= item.ExpirationTime)
                {
                    _logger.LogDebug("Memory cache hit for key: {Key}", key);
                    return (T)item.Value;
                }
                
                // Remove expired item
                _memoryCache.TryRemove(key, out _);
            }
            
            _logger.LogDebug("Cache miss for key: {Key}", key);
            return null;
        }

        /// <summary>
        /// Set item in cache with expiration time
        /// </summary>
        public async Task<bool> SetAsync<T>(string key, T value, TimeSpan expiry) where T : class
        {
            if (value == null)
                return false;
                
            bool redisSuccess = false;
            
            // Try Redis first if available
            if (_redisCache != null)
            {
                try
                {
                    redisSuccess = await _redisCache.SetAsync(key, value, expiry);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Redis cache set error: {Message}", ex.Message);
                }
            }
            
            // Always update memory cache as well
            _memoryCache[key] = new CacheItem
            {
                Value = value,
                ExpirationTime = DateTime.UtcNow.Add(expiry)
            };
            
            return _redisCache == null || redisSuccess;
        }

        /// <summary>
        /// Remove item from cache by key
        /// </summary>
        public async Task<bool> RemoveAsync(string key)
        {
            bool redisSuccess = true;
            
            // Try Redis first if available
            if (_redisCache != null)
            {
                try
                {
                    redisSuccess = await _redisCache.RemoveAsync(key);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Redis cache remove error: {Message}", ex.Message);
                    redisSuccess = false;
                }
            }
            
            // Always remove from memory cache as well
            _memoryCache.TryRemove(key, out _);
            
            return redisSuccess;
        }

        /// <summary>
        /// Check if item exists in cache
        /// </summary>
        public async Task<bool> ExistsAsync(string key)
        {
            // Check Redis first if available
            if (_redisCache != null)
            {
                try
                {
                    if (await _redisCache.ExistsAsync(key))
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Redis cache exists check error: {Message}", ex.Message);
                }
            }
            
            // Check memory cache as fallback
            if (_memoryCache.TryGetValue(key, out var item))
            {
                if (DateTime.UtcNow <= item.ExpirationTime)
                {
                    return true;
                }
                
                // Remove expired item
                _memoryCache.TryRemove(key, out _);
            }
            
            return false;
        }
        
        /// <summary>
        /// Perform cache cleanup of expired items
        /// </summary>
        public void CleanupExpiredItems()
        {
            var now = DateTime.UtcNow;
            foreach (var key in _memoryCache.Keys)
            {
                if (_memoryCache.TryGetValue(key, out var item) && now > item.ExpirationTime)
                {
                    _memoryCache.TryRemove(key, out _);
                }
            }
        }
    }
}