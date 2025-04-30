using CryptoGPT.Application.Interfaces;
using CryptoGPT.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace CryptoGPT.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Register caching service
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, HybridCacheService>();

            // Register HTTP clients
            services.AddHttpClient<ICoinGeckoService, CoinGeckoService>()
                .AddHttpMessageHandler(provider =>
                {
                    var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger<LoggingHttpMessageHandler>();
                    return new LoggingHttpMessageHandler(logger);
                });

            // Register cryptocurrency data services
            services.AddScoped<ICoinGeckoService, CoinGeckoService>();
            services.AddScoped<ICryptoDataService, MultiSourceCryptoService>();

            return services;
        }
    }
}