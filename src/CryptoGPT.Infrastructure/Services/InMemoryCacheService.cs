using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using CryptoGPT.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CryptoGPT.Infrastructure.Services
{
    /// <summary>
    /// Simple in-memory implementation of ICacheService
    /// </summary>
    public class InMemoryCacheService : ICacheService
    {
        private readonly ILogger<InMemoryCacheService> _logger;
        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
        
        private class CacheEntry
        {
            public object Value { get; set; } = null!;
            public DateTime ExpiresAt { get; set; }
        }

        public InMemoryCacheService(ILogger<InMemoryCacheService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get an item from the cache
        /// </summary>
        public Task<T?> GetAsync<T>(string key) where T : class
        {
            CleanupExpiredEntries();
            
            if (_cache.TryGetValue(key, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
            {
                _logger.LogDebug("Cache hit: {Key}", key);
                return Task.FromResult((T)entry.Value);
            }
            
            _logger.LogDebug("Cache miss: {Key}", key);
            return Task.FromResult<T?>(null);
        }

        /// <summary>
        /// Set an item in the cache
        /// </summary>
        public Task<bool> SetAsync<T>(string key, T value, TimeSpan expiry) where T : class
        {
            _cache[key] = new CacheEntry
            {
                Value = value,
                ExpiresAt = DateTime.UtcNow.Add(expiry)
            };
            _logger.LogDebug("Cache set: {Key} with expiry {Expiry}s", key, expiry.TotalSeconds);
            return Task.FromResult(true);
        }

        /// <summary>
        /// Remove an item from the cache
        /// </summary>
        public Task<bool> RemoveAsync(string key)
        {
            var removed = _cache.TryRemove(key, out _);
            _logger.LogDebug("Cache removed: {Key}", key);
            return Task.FromResult(removed);
        }

        /// <summary>
        /// Check if item exists in cache
        /// </summary>
        public Task<bool> ExistsAsync(string key)
        {
            CleanupExpiredEntries();
            var exists = _cache.TryGetValue(key, out var entry) && entry.ExpiresAt > DateTime.UtcNow;
            return Task.FromResult(exists);
        }

        /// <summary>
        /// Cleanup expired entries periodically
        /// </summary>
        private void CleanupExpiredEntries()
        {
            // Only cleanup occasionally to avoid excessive overhead
            if (Random.Shared.Next(100) < 5) // ~5% chance of cleanup on each access
            {
                var now = DateTime.UtcNow;
                foreach (var key in _cache.Keys)
                {
                    if (_cache.TryGetValue(key, out var entry) && entry.ExpiresAt <= now)
                    {
                        _cache.TryRemove(key, out _);
                    }
                }
            }
        }
    }
}