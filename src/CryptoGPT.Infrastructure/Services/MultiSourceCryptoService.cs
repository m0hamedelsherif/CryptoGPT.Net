using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoGPT.Application.Interfaces;
using CryptoGPT.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CryptoGPT.Infrastructure.Services
{
    /// <summary>
    /// Service that combines multiple cryptocurrency data sources
    /// </summary>
    public class MultiSourceCryptoService : ICryptoDataService
    {
        private readonly ICoinGeckoService _coinGeckoService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<MultiSourceCryptoService> _logger;
        private readonly string _activeDataSource;

        // Constants
        private const string CoinGeckoSource = "coingecko";

        public MultiSourceCryptoService(
            ICoinGeckoService coinGeckoService,
            ICacheService cacheService,
            ILogger<MultiSourceCryptoService> logger)
        {
            _coinGeckoService = coinGeckoService ?? throw new ArgumentNullException(nameof(coinGeckoService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // For now, we're only using CoinGecko
            _activeDataSource = CoinGeckoSource;
        }

        public async Task<List<CryptoCurrency>> GetTopCoinsAsync(int limit = 10)
        {
            try
            {
                // Currently only using CoinGecko data source
                return await _coinGeckoService.GetTopCoinsAsync(limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top coins: {Message}", ex.Message);
                return new List<CryptoCurrency>();
            }
        }

        public async Task<CryptoCurrencyDetail?> GetCoinDataAsync(string coinId)
        {
            try
            {
                // Currently only using CoinGecko data source
                return await _coinGeckoService.GetCoinDataAsync(coinId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting coin data for {CoinId}: {Message}", coinId, ex.Message);
                return null;
            }
        }

        public async Task<MarketHistory> GetMarketChartAsync(string coinId, int days = 30)
        {
            try
            {
                // Currently only using CoinGecko data source
                return await _coinGeckoService.GetMarketChartAsync(coinId, days);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting market chart for {CoinId}: {Message}", coinId, ex.Message);
                return new MarketHistory { CoinId = coinId };
            }
        }

        public async Task<MarketHistory> GetExtendedMarketChartAsync(string coinId, int days = 30, Dictionary<string, IndicatorParameters>? indicators = null)
        {
            try
            {
                // Get base market history data
                var marketHistory = await GetMarketChartAsync(coinId, days);
                
                // If indicators calculation is implemented in the future, it would be done here
                
                return marketHistory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting extended market chart for {CoinId}: {Message}", coinId, ex.Message);
                return new MarketHistory { CoinId = coinId };
            }
        }

        public async Task<MarketOverview> GetMarketOverviewAsync()
        {
            // Sample implementation - would be expanded with actual API calls
            try
            {
                var coins = await GetTopCoinsAsync(100);
                
                // Top gainers (by 24h % change)
                var topGainers = coins.FindAll(c => c.PriceChangePercentage24h > 0)
                    .OrderByDescending(c => c.PriceChangePercentage24h)
                    .Take(10)
                    .ToList();
                
                // Top losers (by 24h % change)
                var topLosers = coins.FindAll(c => c.PriceChangePercentage24h < 0)
                    .OrderBy(c => c.PriceChangePercentage24h)
                    .Take(10)
                    .ToList();
                
                // Top by volume
                var topByVolume = coins
                    .OrderByDescending(c => c.Volume24h)
                    .Take(10)
                    .ToList();
                
                return new MarketOverview
                {
                    TopGainers = topGainers,
                    TopLosers = topLosers,
                    TopByVolume = topByVolume,
                    MarketMetrics = new Dictionary<string, decimal?>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting market overview: {Message}", ex.Message);
                return new MarketOverview();
            }
        }

        public string GetCurrentDataSource()
        {
            return _activeDataSource;
        }
    }
}