using CryptoGPT.Application.Interfaces;
using CryptoGPT.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;

namespace CryptoGPT.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register HTTP clients
            services.AddHttpClient<CoinGeckoService>();
            services.AddHttpClient<CoinCapService>();
            services.AddHttpClient<YahooFinanceService>();

            // Register Redis if configured
            var redisConnection = configuration.GetConnectionString("Redis");
            if (!string.IsNullOrEmpty(redisConnection))
            {
                services.AddSingleton<IConnectionMultiplexer>(_ =>
                    ConnectionMultiplexer.Connect(redisConnection));
                services.AddSingleton<RedisCacheService>();
                services.AddSingleton<ICacheService>(provider =>
                {
                    var redis = provider.GetService<RedisCacheService>();
                    var logger = provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<HybridCacheService>>();
                    return new HybridCacheService(redis, logger);
                });
            }
            else
            {
                services.AddSingleton<ICacheService, HybridCacheService>(provider =>
                    new HybridCacheService(null, provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<HybridCacheService>>()));
            }

            // Register cryptocurrency data services
            services.AddScoped<ICoinGeckoService, CoinGeckoService>();
            services.AddScoped<ICoinCapService, CoinCapService>();
            services.AddScoped<IYahooFinanceService, YahooFinanceService>();
            services.AddScoped<ITechnicalAnalysisService, TechnicalAnalysisService>();
            services.AddScoped<ICryptoDataService, MultiSourceCryptoService>();

            // Register LLM services if configured
            var ollamaBaseUrl = configuration["LlmServices:Ollama:BaseUrl"];
            if (!string.IsNullOrEmpty(ollamaBaseUrl))
            {
                services.AddHttpClient("OllamaApi", client =>
                {
                    client.BaseAddress = new Uri(ollamaBaseUrl);
                });
                services.AddScoped<ILlmService, OllamaLlmService>();
            }

            // Register other services
            services.AddScoped<INewsService, NewsService>();
            services.AddScoped<IRecommendationEngine, RecommendationEngine>();
            services.AddScoped<ILlmService, OllamaLlmService>();

            return services;
        }
    }
}