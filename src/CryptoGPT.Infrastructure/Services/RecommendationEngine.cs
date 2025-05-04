using CryptoGPT.Application.Interfaces;
using CryptoGPT.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoGPT.Infrastructure.Services
{
    /// <summary>
    /// Service that provides cryptocurrency investment recommendations based on market data and news
    /// </summary>
    public class RecommendationEngine : IRecommendationEngine
    {
        private readonly ICryptoDataService _cryptoDataService;
        private readonly ITechnicalAnalysisService _technicalAnalysisService;
        private readonly INewsService _newsService;
        private readonly ILlmService _llmService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<RecommendationEngine> _logger;
        private readonly int _recommendationCacheDuration;

        // Cache keys
        private const string AssetRecommendationKey = "recommendation_{0}";
        private const string MarketOverviewKey = "market_overview";

        public RecommendationEngine(
            ICryptoDataService cryptoDataService,
            ITechnicalAnalysisService technicalAnalysisService,
            INewsService newsService,
            ILlmService llmService,
            ICacheService cacheService,
            ILogger<RecommendationEngine> logger,
            IConfiguration configuration)
        {
            _cryptoDataService = cryptoDataService ?? throw new ArgumentNullException(nameof(cryptoDataService));
            _technicalAnalysisService = technicalAnalysisService ?? throw new ArgumentNullException(nameof(technicalAnalysisService));
            _newsService = newsService ?? throw new ArgumentNullException(nameof(newsService));
            _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _recommendationCacheDuration = configuration.GetValue<int>("CacheDurations:Recommendations") == 0 
                ? 60 
                : configuration.GetValue<int>("CacheDurations:Recommendations");
        }

        /// <summary>
        /// Get market overview with key trends and recommendations
        /// </summary>
        public async Task<MarketOverview> GetMarketOverviewAsync()
        {
            try
            {
                var cachedResult = await _cacheService.GetAsync<MarketOverview>(MarketOverviewKey);
                if (cachedResult != null)
                {
                    _logger.LogInformation("[RECOMMENDATION] Retrieved market overview from cache");
                    return cachedResult;
                }

                // Get data for top 10 coins by market cap
                var topCoins = await _cryptoDataService.GetTopCoinsAsync(10);
                if (topCoins == null || !topCoins.Any())
                {
                    _logger.LogWarning("[RECOMMENDATION] Failed to retrieve top coins for market overview");
                    return new MarketOverview
                    {
                        GeneratedAt = DateTime.UtcNow,
                        MarketSentiment = "neutral",
                        Summary = "Unable to generate market overview due to data unavailability."
                    };
                }

                // Calculate market sentiment based on price changes
                var averageDailyChange = topCoins.Average(c => c.PriceChangePercentage24h ?? 0);
                var marketSentiment = DetermineMarketSentiment((double)averageDailyChange);

                // Get latest market news
                var latestNews = await _newsService.GetMarketNewsAsync(5);
                
                // Prepare context for LLM
                var promptBuilder = new StringBuilder();
                promptBuilder.AppendLine("Generate a cryptocurrency market overview based on the following data:");
                promptBuilder.AppendLine("\nTop cryptocurrencies by market cap:");
                
                foreach (var coin in topCoins)
                {
                    promptBuilder.AppendLine($"- {coin.Name} ({coin.Symbol}): ${coin.CurrentPrice:F2}, 24h change: {coin.PriceChangePercentage24h:F2}%");
                }
                
                promptBuilder.AppendLine("\nLatest cryptocurrency news headlines:");
                foreach (var news in latestNews)
                {
                    promptBuilder.AppendLine($"- {news.Title} ({news.Source})");
                }
                
                promptBuilder.AppendLine("\nBased on this data, provide:");
                promptBuilder.AppendLine("1. A concise market overview (3-4 sentences)");
                promptBuilder.AppendLine("2. Key trends to watch (2-3 bullet points)");
                promptBuilder.AppendLine("3. General investment recommendation (conservative, moderate, or aggressive)");

                // Generate the market overview using LLM
                var llmResponse = await _llmService.GetCompletionAsync(promptBuilder.ToString());
                
                if (string.IsNullOrEmpty(llmResponse))
                {
                    _logger.LogWarning("[RECOMMENDATION] Failed to generate market overview with LLM");
                    return new MarketOverview
                    {
                        GeneratedAt = DateTime.UtcNow,
                        MarketSentiment = marketSentiment,
                        Summary = $"Market sentiment: {marketSentiment}. Average 24h change for top 10 coins: {averageDailyChange:F2}%."
                    };
                }

                var overview = new MarketOverview
                {
                    GeneratedAt = DateTime.UtcNow,
                    MarketSentiment = marketSentiment,
                    Summary = llmResponse,
                    TopPerformers = topCoins.OrderByDescending(c => c.PriceChangePercentage24h).Take(3).ToList(),
                    WorstPerformers = topCoins.OrderBy(c => c.PriceChangePercentage24h).Take(3).ToList()
                };

                // Cache the result
                await _cacheService.SetAsync(MarketOverviewKey, overview, TimeSpan.FromMinutes(_recommendationCacheDuration));
                _logger.LogInformation("[RECOMMENDATION] Generated new market overview");
                
                return overview;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RECOMMENDATION] Error generating market overview: {Message}", ex.Message);
                return new MarketOverview
                {
                    GeneratedAt = DateTime.UtcNow,
                    MarketSentiment = "neutral",
                    Summary = "Unable to generate market overview due to an error."
                };
            }
        }

        /// <summary>
        /// Get investment recommendation for a specific asset
        /// </summary>
        public async Task<AssetRecommendation> GetAssetRecommendationAsync(string coinId)
        {
            if (string.IsNullOrEmpty(coinId))
            {
                return new AssetRecommendation
                {
                    CoinId = coinId,
                    GeneratedAt = DateTime.UtcNow,
                    Recommendation = "hold",
                    Confidence = 0,
                    Summary = "Invalid coin identifier provided."
                };
            }

            try
            {
                var cacheKey = string.Format(AssetRecommendationKey, coinId);
                var cachedResult = await _cacheService.GetAsync<AssetRecommendation>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogInformation("[RECOMMENDATION] Retrieved recommendation for {CoinId} from cache", coinId);
                    return cachedResult;
                }

                // Get asset data
                var coinData = await _cryptoDataService.GetCoinDataAsync(coinId);
                if (coinData == null)
                {
                    _logger.LogWarning("[RECOMMENDATION] Failed to retrieve coin data for {CoinId}", coinId);
                    return new AssetRecommendation
                    {
                        CoinId = coinId,
                        GeneratedAt = DateTime.UtcNow,
                        Recommendation = "hold",
                        Confidence = 0,
                        Summary = $"Could not retrieve data for coin {coinId}."
                    };
                }

                // Get historical price data
                var historicalData = await _cryptoDataService.GetMarketChartAsync(coinId, 30);
                var pricePoints = historicalData?.Prices?.Select(p => new PriceHistoryPoint 
                {
                    Timestamp = p.Timestamp,
                    Price = p.Price  // Using Price instead of Value
                }).ToList() ?? new List<PriceHistoryPoint>();
                
                // Get technical analysis indicators
                var technicalAnalysis = await _technicalAnalysisService.GetTechnicalAnalysisAsync(coinId, pricePoints, 30);
                
                // Get recent news for the coin
                var coinNews = await _newsService.GetCoinNewsAsync(coinId, coinData.Symbol, 3);
                // Convert CryptoNewsItem to NewsArticle for compatibility
                var newsArticles = coinNews.Select(item => new NewsArticle
                {
                    Id = item.Id,
                    Title = item.Title,
                    Summary = item.Summary,
                    Url = item.Url,
                    Source = item.Source,
                    PublishedAt = item.PublishedAt,
                    Sentiment = "neutral" // Default sentiment since CryptoNewsItem may not have this property
                }).ToList();

                // Calculate sentiment score from news
                var newsSentiment = CalculateNewsSentiment(newsArticles);

                // Determine initial recommendation based on technical indicators
                var recommendation = "hold";
                var confidence = 0.5;

                if (technicalAnalysis != null)
                {
                    // Use 'Signal' property instead of non-existent 'SignalStrength' property
                    var strength = 0.5; // Default value
                    
                    // Extract signal strength from the analysis or use a default
                    if (technicalAnalysis.Signals.TryGetValue("Overall", out var overallSignal))
                    {
                        if (overallSignal.Contains("BULLISH"))
                        {
                            recommendation = "buy";
                            strength = 0.7;
                        }
                        else if (overallSignal.Contains("BEARISH"))
                        {
                            recommendation = "sell";
                            strength = 0.7;
                        }
                    }
                    
                    if (strength > 0.7)
                    {
                        confidence = strength;
                    }
                    else
                    {
                        // Adjust with news sentiment
                        if (newsSentiment > 0.6 && recommendation != "sell")
                        {
                            recommendation = "buy";
                            confidence = 0.5 + (newsSentiment - 0.5) / 2;
                        }
                        else if (newsSentiment < 0.4 && recommendation != "buy")
                        {
                            recommendation = "sell";
                            confidence = 0.5 + (0.5 - newsSentiment) / 2;
                        }
                        else
                        {
                            confidence = strength;
                        }
                    }
                }

                // Prepare context for LLM
                var promptBuilder = new StringBuilder();
                promptBuilder.AppendLine($"Generate an investment recommendation for {coinData.Name} ({coinData.Symbol}) based on the following data:");
                promptBuilder.AppendLine($"\nCurrent price: ${coinData.CurrentPrice:F2}");
                promptBuilder.AppendLine($"24h price change: {coinData.PriceChangePercentage24h:F2}%");
                promptBuilder.AppendLine($"7d price change: {coinData.PriceChangePercentage7d:F2}%");
                
                // Use market cap and volume from the data
                promptBuilder.AppendLine($"Market cap: ${coinData.MarketCap:N0}");
                promptBuilder.AppendLine($"Volume: ${coinData.Volume24h:N0}");

                if (technicalAnalysis != null)
                {
                    promptBuilder.AppendLine("\nTechnical indicators:");
                    
                    // Extract RSI from latest values or provide a default
                    decimal rsiValue = 50;
                    if (technicalAnalysis.LatestValues.TryGetValue("RSI14", out var rsi))
                    {
                        rsiValue = rsi;
                    }
                    
                    promptBuilder.AppendLine($"- RSI: {rsiValue:F2} ({InterpretRsi((double)rsiValue)})");
                    
                    // Get MACD signal based on the technical analysis output
                    var macdSignal = "neutral";
                    if (technicalAnalysis.Signals.TryGetValue("MACD", out var macdSignalValue))
                    {
                        macdSignal = macdSignalValue.Contains("BULLISH") ? "bullish" : macdSignalValue.Contains("BEARISH") ? "bearish" : "neutral";
                    }
                    
                    promptBuilder.AppendLine($"- MACD: {(macdSignal == "bullish" ? "Bullish" : "Bearish")} signal");
                    promptBuilder.AppendLine($"- Overall signal: {recommendation.ToUpperInvariant()} with {confidence:P0} confidence");
                }

                if (coinNews.Any())
                {
                    promptBuilder.AppendLine("\nRecent news:");
                    foreach (var news in coinNews)
                    {
                        promptBuilder.AppendLine($"- {news.Title} ({news.Source})");
                    }
                }

                promptBuilder.AppendLine("\nBased on this data:");
                promptBuilder.AppendLine("1. Provide a concise investment analysis for this asset (3-4 sentences)");
                promptBuilder.AppendLine("2. Explain key factors affecting its price");
                promptBuilder.AppendLine("3. Provide a clear recommendation: BUY, HOLD, or SELL");
                promptBuilder.AppendLine("4. Risk assessment (low, medium, high)");

                // Generate the recommendation using LLM
                var llmResponse = await _llmService.GenerateContentAsync(promptBuilder.ToString());
                
                var assetRecommendation = new AssetRecommendation
                {
                    CoinId = coinId,
                    CoinName = coinData.Name,
                    CoinSymbol = coinData.Symbol,
                    GeneratedAt = DateTime.UtcNow,
                    Recommendation = recommendation,
                    Confidence = confidence,
                    Summary = !string.IsNullOrEmpty(llmResponse) 
                        ? llmResponse 
                        : $"Technical analysis suggests a {recommendation} recommendation with {confidence:P0} confidence."
                };

                // Cache the result
                await _cacheService.SetAsync(cacheKey, assetRecommendation, TimeSpan.FromMinutes(_recommendationCacheDuration));
                _logger.LogInformation("[RECOMMENDATION] Generated new recommendation for {CoinId}", coinId);
                
                return assetRecommendation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RECOMMENDATION] Error generating recommendation for {CoinId}: {Message}", coinId, ex.Message);
                return new AssetRecommendation
                {
                    CoinId = coinId,
                    GeneratedAt = DateTime.UtcNow,
                    Recommendation = "hold",
                    Confidence = 0,
                    Summary = "Unable to generate recommendation due to an error."
                };
            }
        }

        #region Helper methods

        private string DetermineMarketSentiment(double averageChange)
        {
            if (averageChange > 5)
                return "very bullish";
            else if (averageChange > 2)
                return "bullish";
            else if (averageChange > 0.5)
                return "slightly bullish";
            else if (averageChange > -0.5)
                return "neutral";
            else if (averageChange > -2)
                return "slightly bearish";
            else if (averageChange > -5)
                return "bearish";
            else
                return "very bearish";
        }

        private double CalculateNewsSentiment(List<NewsArticle> news)
        {
            if (news == null || !news.Any())
                return 0.5; // Neutral

            int positive = 0, negative = 0, neutral = 0;

            foreach (var article in news)
            {
                if (article.Sentiment == null)
                {
                    neutral++;
                    continue;
                }
                
                switch (article.Sentiment.ToLower())
                {
                    case "positive":
                        positive++;
                        break;
                    case "negative":
                        negative++;
                        break;
                    default:
                        neutral++;
                        break;
                }
            }

            double total = positive + negative + neutral;
            return (positive + (neutral * 0.5)) / total;
        }

        private string InterpretRsi(double rsi)
        {
            if (rsi > 70)
                return "Overbought";
            else if (rsi < 30)
                return "Oversold";
            else
                return "Neutral";
        }

        #endregion
    }
}