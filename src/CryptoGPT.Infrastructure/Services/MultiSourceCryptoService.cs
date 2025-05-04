using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoGPT.Application.Interfaces;
using CryptoGPT.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CryptoGPT.Infrastructure.Services
{
    /// <summary>
    /// Implementation of ICryptoDataService that combines multiple data sources
    /// with fallback mechanisms for better reliability
    /// </summary>
    public class MultiSourceCryptoService : ICryptoDataService
    {
        private readonly ICoinGeckoService _coinGeckoService;
        private readonly ICoinCapService _coinCapService;
        private readonly IYahooFinanceService _yahooFinanceService;
        private readonly ITechnicalAnalysisService _technicalAnalysisService;
        private readonly ILogger<MultiSourceCryptoService> _logger;

        public MultiSourceCryptoService(
            ICoinGeckoService coinGeckoService,
            ICoinCapService coinCapService,
            IYahooFinanceService yahooFinanceService,
            ITechnicalAnalysisService technicalAnalysisService,
            ILogger<MultiSourceCryptoService> logger)
        {
            _coinGeckoService = coinGeckoService ?? throw new ArgumentNullException(nameof(coinGeckoService));
            _coinCapService = coinCapService ?? throw new ArgumentNullException(nameof(coinCapService));
            _yahooFinanceService = yahooFinanceService ?? throw new ArgumentNullException(nameof(yahooFinanceService));
            _technicalAnalysisService = technicalAnalysisService ?? throw new ArgumentNullException(nameof(technicalAnalysisService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get top cryptocurrencies by market cap from multiple sources
        /// </summary>
        public async Task<List<CryptoCurrency>> GetTopCoinsAsync(int limit = 10)
        {
            _logger.LogInformation("Getting top {Limit} coins", limit);
            
            // Try CoinGecko first (most complete data)
            try
            {
                var result = await _coinGeckoService.GetTopCoinsAsync(limit);
                if (result != null && result.Any())
                {
                    _logger.LogInformation("Retrieved {Count} coins from CoinGecko", result.Count);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get top coins from CoinGecko: {Message}", ex.Message);
            }
            
            // Try CoinCap as fallback
            try
            {
                var result = await _coinCapService.GetTopCoinsAsync(limit);
                if (result != null && result.Any())
                {
                    _logger.LogInformation("Retrieved {Count} coins from CoinCap", result.Count);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get top coins from CoinCap: {Message}", ex.Message);
            }
            
            // Try Yahoo Finance as last resort
            try
            {
                var result = await _yahooFinanceService.GetTopCoinsAsync(limit);
                if (result != null && result.Any())
                {
                    _logger.LogInformation("Retrieved {Count} coins from Yahoo Finance", result.Count);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get top coins from Yahoo Finance: {Message}", ex.Message);
            }
            
            _logger.LogWarning("Failed to retrieve top coins from any source");
            return new List<CryptoCurrency>();
        }

        /// <summary>
        /// Get detailed information for a specific cryptocurrency from multiple sources
        /// </summary>
        public async Task<CryptoCurrencyDetail?> GetCoinDataAsync(string coinId)
        {
            if (string.IsNullOrEmpty(coinId))
            {
                _logger.LogWarning("Empty coin ID provided");
                return null;
            }
            
            _logger.LogInformation("Getting coin data for {CoinId}", coinId);
            
            // Try CoinGecko first (most complete data)
            try
            {
                var result = await _coinGeckoService.GetCoinDataAsync(coinId);
                if (result != null)
                {
                    _logger.LogInformation("Retrieved coin data for {CoinId} from CoinGecko", coinId);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get coin data from CoinGecko: {Message}", ex.Message);
            }
            
            // Try CoinCap as fallback
            try
            {
                var result = await _coinCapService.GetCoinDataAsync(coinId);
                if (result != null)
                {
                    _logger.LogInformation("Retrieved coin data for {CoinId} from CoinCap", coinId);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get coin data from CoinCap: {Message}", ex.Message);
            }
            
            // Try Yahoo Finance as last resort
            try
            {
                string yahooSymbol = _yahooFinanceService.ConvertToYahooSymbol(coinId);
                var result = await _yahooFinanceService.GetCoinDataAsync(yahooSymbol);
                if (result != null)
                {
                    _logger.LogInformation("Retrieved coin data for {CoinId} from Yahoo Finance", coinId);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get coin data from Yahoo Finance: {Message}", ex.Message);
            }
            
            _logger.LogWarning("Failed to retrieve coin data for {CoinId} from any source", coinId);
            return null;
        }

        /// <summary>
        /// Get detailed information for a specific cryptocurrency from multiple sources
        /// </summary>
        public async Task<CryptoCurrencyDetail?> GetCoinDetailsAsync(string coinId)
        {
            // Reuse the existing implementation of GetCoinDataAsync
            return await GetCoinDataAsync(coinId);
        }

        /// <summary>
        /// Get historical market data for a cryptocurrency from multiple sources
        /// </summary>
        public async Task<MarketHistory?> GetMarketChartAsync(string coinId, int days = 30, Dictionary<string, IndicatorParameters>? indicators = null)
        {
            if (string.IsNullOrEmpty(coinId))
            {
                _logger.LogWarning("Empty coin ID provided");
                return null;
            }
            
            _logger.LogInformation("Getting market chart for {CoinId} ({Days} days)", coinId, days);
            
            MarketHistory? marketData = null;
            
            // Calculate actual days needed if indicators are requested
            int requiredDays = days;
            if (indicators != null && indicators.Count > 0)
            {
                requiredDays = IndicatorParameters.CalculateRequiredDataPoints(indicators, days);
                _logger.LogDebug("Calculated {RequiredDays} days needed for indicators ({DisplayDays} days visible)", requiredDays, days);
            }
            
            // Try CoinGecko first (most complete data)
            try
            {
                marketData = await _coinGeckoService.GetMarketChartAsync(coinId, requiredDays);
                if (marketData != null && marketData.Prices.Any())
                {
                    _logger.LogInformation("Retrieved market chart for {CoinId} from CoinGecko", coinId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get market chart from CoinGecko: {Message}", ex.Message);
            }
            
            // Try CoinCap if CoinGecko fails
            if (marketData == null || !marketData.Prices.Any())
            {
                try
                {
                    marketData = await _coinCapService.GetMarketChartAsync(coinId, requiredDays);
                    if (marketData != null && marketData.Prices.Any())
                    {
                        _logger.LogInformation("Retrieved market chart for {CoinId} from CoinCap", coinId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get market chart from CoinCap: {Message}", ex.Message);
                }
            }
            
            // Try Yahoo Finance as last resort
            if (marketData == null || !marketData.Prices.Any())
            {
                try
                {
                    string yahooSymbol = _yahooFinanceService.ConvertToYahooSymbol(coinId);
                    marketData = await _yahooFinanceService.GetMarketChartAsync(yahooSymbol, requiredDays);
                    if (marketData != null && marketData.Prices.Any())
                    {
                        _logger.LogInformation("Retrieved market chart for {CoinId} from Yahoo Finance", coinId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get market chart from Yahoo Finance: {Message}", ex.Message);
                }
            }
            
            // If we have market data and indicators requested, calculate them
            if (marketData != null && marketData.Prices.Any() && indicators != null && indicators.Count > 0)
            {
                try
                {
                    _logger.LogDebug("Calculating {Count} indicators for {CoinId}", indicators.Count, coinId);
                    var indicatorSeries = await _technicalAnalysisService.CalculateIndicatorsAsync(marketData.Prices, indicators);
                    marketData.IndicatorSeries = indicatorSeries;
                    
                    // Trim to requested days if we fetched extra for indicator calculation
                    if (requiredDays > days)
                    {
                        TrimMarketHistory(marketData, days);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating indicators: {Message}", ex.Message);
                }
            }
            
            if (marketData == null || !marketData.Prices.Any())
            {
                _logger.LogWarning("Failed to retrieve market chart for {CoinId} from any source", coinId);
            }
            
            return marketData;
        }

        /// <summary>
        /// Get historical market data for a cryptocurrency
        /// </summary>
        public async Task<MarketHistory?> GetHistoricalDataAsync(string coinId, int days = 30)
        {
            // Reuse the existing implementation of GetMarketChartAsync without indicators
            return await GetMarketChartAsync(coinId, days);
        }

        /// <summary>
        /// Get overall cryptocurrency market overview
        /// </summary>
        public async Task<MarketOverview> GetMarketOverviewAsync()
        {
            _logger.LogInformation("Getting market overview");
            
            // Try CoinGecko first (most complete data)
            try
            {
                var result = await _coinGeckoService.GetMarketOverviewAsync();
                if (result != null)
                {
                    _logger.LogInformation("Retrieved market overview from CoinGecko");
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get market overview from CoinGecko: {Message}", ex.Message);
            }
            
            // Fallback: Create a basic market overview from top coins
            try
            {
                var topCoins = await GetTopCoinsAsync(10);
                if (topCoins != null && topCoins.Any())
                {
                    var overview = new MarketOverview
                    {
                        GeneratedAt = DateTime.UtcNow,
                        MarketSentiment = "neutral",
                        Summary = $"Basic market overview based on top {topCoins.Count} coins.",
                        TopPerformers = topCoins.OrderByDescending(c => c.PriceChangePercentage24h).Take(3).ToList(),
                        WorstPerformers = topCoins.OrderBy(c => c.PriceChangePercentage24h).Take(3).ToList()
                    };
                    
                    _logger.LogInformation("Generated basic market overview from top coins");
                    return overview;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate basic market overview: {Message}", ex.Message);
            }
            
            // Last resort: Return an empty market overview
            _logger.LogWarning("Failed to retrieve market overview from any source");
            return new MarketOverview
            {
                GeneratedAt = DateTime.UtcNow,
                MarketSentiment = "unknown",
                Summary = "No market data available.",
                TopPerformers = new List<CryptoCurrency>(),
                WorstPerformers = new List<CryptoCurrency>()
            };
        }

        /// <summary>
        /// Get technical analysis for a cryptocurrency
        /// </summary>
        public async Task<TechnicalAnalysis> GetTechnicalAnalysisAsync(string coinId, int days = 30)
        {
            if (string.IsNullOrEmpty(coinId))
            {
                _logger.LogWarning("Empty coin ID provided");
                return new TechnicalAnalysis { CoinId = coinId };
            }
            
            _logger.LogInformation("Getting technical analysis for {CoinId} ({Days} days)", coinId, days);
            
            // Get market data first
            var marketData = await GetMarketChartAsync(coinId, days);
            if (marketData == null || !marketData.Prices.Any())
            {
                _logger.LogWarning("Failed to retrieve market data for technical analysis of {CoinId}", coinId);
                return new TechnicalAnalysis { CoinId = coinId };
            }
            
            // Calculate technical analysis
            try
            {
                var result = await _technicalAnalysisService.GetTechnicalAnalysisAsync(coinId, marketData.Prices, days);
                _logger.LogInformation("Completed technical analysis for {CoinId}", coinId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating technical analysis: {Message}", ex.Message);
                return new TechnicalAnalysis { CoinId = coinId };
            }
        }

        /// <summary>
        /// Trim market history data to the specified number of days
        /// </summary>
        private void TrimMarketHistory(MarketHistory history, int days)
        {
            if (history == null || !history.Prices.Any())
                return;
                
            var earliestTimestamp = DateTimeOffset.UtcNow.AddDays(-days).ToUnixTimeMilliseconds();
            
            history.Prices = history.Prices
                .Where(p => p.Timestamp >= earliestTimestamp)
                .ToList();
                
            history.MarketCaps = history.MarketCaps
                .Where(p => p.Timestamp >= earliestTimestamp)
                .ToList();
                
            history.Volumes = history.Volumes
                .Where(p => p.Timestamp >= earliestTimestamp)
                .ToList();
                
            if (history.IndicatorSeries != null)
            {
                foreach (var key in history.IndicatorSeries.Keys.ToList())
                {
                    history.IndicatorSeries[key] = history.IndicatorSeries[key]
                        .Where(p => p.Timestamp >= earliestTimestamp)
                        .ToList();
                }
            }
        }
    }
}