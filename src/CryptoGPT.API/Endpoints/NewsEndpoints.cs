using Asp.Versioning;
using CryptoGPT.Application.Features.News.Queries.GetCoinNews;
using CryptoGPT.Application.Features.News.Queries.GetLatestNews;
using MediatR;

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
            group.MapGet("/", async (IMediator mediator, int limit = 20) =>
            {
                var query = new GetLatestNewsQuery { Limit = limit };
                var result = await mediator.Send(query);
                return Results.Ok(result);
            })
            .WithName("GetLatestNews")
            .WithDescription("Get latest cryptocurrency news");

            // Get coin-specific news
            group.MapGet("/{coinId}", async (IMediator mediator, string coinId, string symbol, int limit) =>
            {
                var query = new GetCoinNewsQuery
                {
                    CoinId = coinId,
                    Symbol = symbol,
                    Limit = limit <= 0 ? 10 : limit
                };
                var result = await mediator.Send(query);
                return Results.Ok(result);
            })
            .WithName("GetCoinNews")
            .WithDescription("Get coin-specific news");

            return app;
        }
    }
}