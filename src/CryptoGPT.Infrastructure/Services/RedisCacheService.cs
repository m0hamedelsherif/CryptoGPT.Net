using CryptoGPT.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace CryptoGPT.Infrastructure.Services
{
    /// <summary>
    /// Redis-based implementation of ICacheService
    /// </summary>
    public class RedisCacheService : ICacheService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private IDatabase Database => _redis.GetDatabase();

        public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Get cached item by key
        /// </summary>
        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                var value = await Database.StringGetAsync(key);
                if (value.IsNullOrEmpty)
                {
                    return null;
                }

                return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis cache get error: {Message}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Set item in cache with expiration time
        /// </summary>
        public async Task<bool> SetAsync<T>(string key, T value, TimeSpan expiry) where T : class
        {
            try
            {
                if (value == null)
                    return false;

                string serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
                return await Database.StringSetAsync(key, serializedValue, expiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis cache set error: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Remove item from cache by key
        /// </summary>
        public async Task<bool> RemoveAsync(string key)
        {
            try
            {
                return await Database.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis cache remove error: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Check if item exists in cache
        /// </summary>
        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                return await Database.KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis cache exists check error: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Set cache key to expire after a specified time
        /// </summary>
        public async Task<bool> SetExpiryAsync(string key, TimeSpan expiry)
        {
            try
            {
                return await Database.KeyExpireAsync(key, expiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis set expiry error: {Message}", ex.Message);
                return false;
            }
        }
    }
}