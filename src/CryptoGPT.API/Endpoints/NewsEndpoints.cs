using Asp.Versioning;
using Asp.Versioning.Builder;
using System;

namespace CryptoGPT.API.Endpoints
{
    public static class NewsEndpoints
    {
        public static WebApplication MapNewsEndpoints(this WebApplication app)
        {
            var versionSet = app.NewApiVersionSet()
                .HasApiVersion(new ApiVersion(1, 0))
                .ReportApiVersions()
                .Build();

            var group = app.MapGroup("/api/news")
                .WithTags("News")
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(new ApiVersion(1, 0))
                .WithOpenApi();

            // Get latest cryptocurrency news
            group.MapGet("/", () =>
            {
                // To be implemented with MediatR
                return Results.Ok(new[]
                {
                    new
                    {
                        id = "1",
                        title = "Cryptocurrency News Example",
                        url = "https://example.com/news/1",
                        source = "Example News",
                        publishedAt = DateTime.UtcNow.AddHours(-2),
                        summary = "This is a placeholder for news implementation."
                    }
                });
            })
            .WithName("GetLatestNews")
            .WithDescription("Get latest cryptocurrency news");

            // Get coin-specific news
            group.MapGet("/{coinId}", (string coinId) =>
            {
                // To be implemented with MediatR
                return Results.Ok(new[]
                {
                    new
                    {
                        id = "1",
                        coinId = coinId,
                        title = $"{coinId} News Example",
                        url = $"https://example.com/news/{coinId}/1",
                        source = "Example News",
                        publishedAt = DateTime.UtcNow.AddHours(-1),
                        summary = $"This is a placeholder for {coinId} specific news implementation."
                    }
                });
            })
            .WithName("GetCoinNews")
            .WithDescription("Get coin-specific news");

            return app;
        }
    }
}