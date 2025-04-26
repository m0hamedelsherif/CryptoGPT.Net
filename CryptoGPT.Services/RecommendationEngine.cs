using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using CryptoGPT.Core.Interfaces;
using CryptoGPT.Core.Models;
using Microsoft.Extensions.Logging;

namespace CryptoGPT.Services
{
    /// <summary>
    /// Implementation of IRecommendationEngine that uses LLM for generating cryptocurrency recommendations
    /// </summary>
    public class RecommendationEngine : IRecommendationEngine
    {
        private readonly ICryptoDataService _cryptoDataService;
        private readonly ILlmService _llmService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<RecommendationEngine> _logger;

        // Cache keys
        private const string RecommendationKey = "recommendation_{0}_{1}";
        private const string MarketSnapshotKey = "market_snapshot";

        public RecommendationEngine(
            ICryptoDataService cryptoDataService,
            ILlmService llmService,
            ICacheService cacheService,
            ILogger<RecommendationEngine> logger)
        {
            _cryptoDataService = cryptoDataService ?? throw new ArgumentNullException(nameof(cryptoDataService));
            _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, object>> GenerateRecommendationsAsync(string userQuery, RiskProfile riskProfile = RiskProfile.Moderate)
        {
            try
            {
                // Create a simplified hash of the query to use in cache key
                string queryHash = Convert.ToBase64String(
                    System.Security.Cryptography.MD5.HashData(
                        System.Text.Encoding.UTF8.GetBytes(userQuery.ToLowerInvariant())
                    )
                ).Substring(0, 10);

                // Try to get from cache first
                string cacheKey = string.Format(RecommendationKey, queryHash, riskProfile.ToString());
                var cachedResult = await _cacheService.GetAsync<Dictionary<string, object>>(cacheKey);
                
                if (cachedResult != null)
                {
                    _logger.LogInformation("Retrieved recommendations from cache");
                    return cachedResult;
                }

                // Get crypto market data
                var topCoins = await _cryptoDataService.GetTopCoinsAsync(50);
                var marketOverview = await _cryptoDataService.GetMarketOverviewAsync();
                
                // Prepare context for the LLM
                var context = new Dictionary<string, object>
                {
                    { "user_query", userQuery },
                    { "risk_profile", riskProfile.ToString() },
                    { "top_coins", FormatCoinsForPrompt(topCoins) },
                    { "top_gainers", FormatCoinsForPrompt(marketOverview.TopGainers) },
                    { "top_losers", FormatCoinsForPrompt(marketOverview.TopLosers) },
                    { "market_metrics", marketOverview.MarketMetrics }
                };

                // Build the prompt for the LLM
                string prompt = GenerateRecommendationPrompt(context);
                
                // Get LLM response
                var structuredResponse = await _llmService.GenerateStructuredResponseAsync<Dictionary<string, object>>(
                    prompt, 
                    "llama2", 
                    GetSystemPrompt(), 
                    0.7f
                );
                
                // Add metadata to response
                structuredResponse["timestamp"] = DateTime.UtcNow.ToString("o");
                structuredResponse["risk_profile"] = riskProfile.ToString();
                
                // Cache the result (for 30 minutes)
                await _cacheService.SetAsync(cacheKey, structuredResponse, 1800);
                
                _logger.LogInformation("Generated recommendations for query: {Query}", userQuery);
                return structuredResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating recommendations: {Message}", ex.Message);
                
                // Return a basic error response
                return new Dictionary<string, object>
                {
                    { "error", "Failed to generate recommendations" },
                    { "message", ex.Message },
                    { "timestamp", DateTime.UtcNow.ToString("o") }
                };
            }
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, object>> GetMarketSnapshotAsync()
        {
            try
            {
                // Try to get from cache first
                var cachedResult = await _cacheService.GetAsync<Dictionary<string, object>>(MarketSnapshotKey);
                
                if (cachedResult != null)
                {
                    return cachedResult;
                }

                // Get market data
                var marketOverview = await _cryptoDataService.GetMarketOverviewAsync();
                var result = new Dictionary<string, object>
                {
                    { "market_metrics", marketOverview.MarketMetrics },
                    { "top_gainers", marketOverview.TopGainers },
                    { "top_losers", marketOverview.TopLosers },
                    { "top_by_volume", marketOverview.TopByVolume },
                    { "timestamp", DateTime.UtcNow.ToString("o") },
                    { "data_source", _cryptoDataService.GetCurrentDataSource() }
                };
                
                // Cache the result (for 5 minutes)
                await _cacheService.SetAsync(MarketSnapshotKey, result, 300);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting market snapshot: {Message}", ex.Message);
                
                return new Dictionary<string, object>
                {
                    { "error", "Failed to get market snapshot" },
                    { "message", ex.Message },
                    { "timestamp", DateTime.UtcNow.ToString("o") }
                };
            }
        }

        #region Helper Methods

        private static string GenerateRecommendationPrompt(Dictionary<string, object> context)
        {
            return $@"
I need crypto investment recommendations based on the following:

USER QUERY: {context["user_query"]}

RISK PROFILE: {context["risk_profile"]}

CURRENT MARKET DATA:
Top 50 Cryptocurrencies: 
{context["top_coins"]}

Top Gainers (24h):
{context["top_gainers"]}

Top Losers (24h):
{context["top_losers"]}

Market Metrics:
- Total Market Cap: ${FormatAmount(GetMetricValue(context["market_metrics"], "total_market_cap"))}
- 24h Trading Volume: ${FormatAmount(GetMetricValue(context["market_metrics"], "total_24h_volume"))}
- BTC Dominance: {GetMetricValue(context["market_metrics"], "btc_dominance"):0.00}%

Provide a detailed investment recommendation including:
1. Market analysis summary
2. Top 3-5 recommended cryptocurrencies with rationale
3. Investment strategy (entry points, allocation percentages, etc.)
4. Risk assessment
5. Timeframe recommendations

Respond in a structured format as a JSON object with the following keys:
- market_analysis: a summary of current market conditions
- recommendations: an array of recommended cryptocurrencies, each with symbol, name, rationale, and suggested allocation percentage
- strategy: investment strategy details
- risk_assessment: analysis of risks involved
- timeframe: suggested investment timeframe
";
        }

        private static string GetSystemPrompt()
        {
            return @"
You are a crypto investment advisor with expertise in cryptocurrency markets. 
Your goal is to provide personalized investment recommendations based on the user's query and risk profile.

Adjust your recommendations based on the risk profile:
- Conservative: Focus on established cryptocurrencies (BTC, ETH) and stablecoins, emphasize capital preservation
- Moderate: Balanced mix of established and mid-cap cryptocurrencies, some growth potential with managed risk
- Aggressive: Include smaller cap cryptocurrencies with higher growth potential, accept higher volatility

Base your recommendations on:
1. Current market data provided in the prompt
2. Technical factors like price trends and trading volume
3. User's specific investment goals and constraints
4. Risk profile specified

Your response must be a valid JSON object with the following structure:
{
  ""market_analysis"": ""string"",
  ""recommendations"": [
    {
      ""symbol"": ""string"",
      ""name"": ""string"",
      ""rationale"": ""string"",
      ""allocation"": number
    }
  ],
  ""strategy"": ""string"",
  ""risk_assessment"": ""string"",
  ""timeframe"": ""string""
}

Ensure your recommendations are specific, actionable, and tailored to the user's needs.
";
        }

        private static string FormatCoinsForPrompt(List<CryptoCurrency> coins)
        {
            var result = new System.Text.StringBuilder();
            foreach (var coin in coins)
            {
                result.AppendLine($"- {coin.Name} ({coin.Symbol}): ${coin.CurrentPrice:0.00}, " +
                                 $"24h Change: {coin.PriceChangePercentage24h:0.00}%, " +
                                 $"Market Cap: ${FormatAmount(coin.MarketCap)}");
            }
            return result.ToString();
        }

        private static string FormatAmount(decimal amount)
        {
            if (amount >= 1_000_000_000_000)
                return $"{amount / 1_000_000_000_000:0.00}T";
            if (amount >= 1_000_000_000)
                return $"{amount / 1_000_000_000:0.00}B";
            if (amount >= 1_000_000)
                return $"{amount / 1_000_000:0.00}M";
            if (amount >= 1_000)
                return $"{amount / 1_000:0.00}K";
            
            return $"{amount:0.00}";
        }

        private static decimal GetMetricValue(object metrics, string key)
        {
            if (metrics is Dictionary<string, decimal> metricsDict && metricsDict.TryGetValue(key, out decimal value))
            {
                return value;
            }
            return 0;
        }

        #endregion
    }
}