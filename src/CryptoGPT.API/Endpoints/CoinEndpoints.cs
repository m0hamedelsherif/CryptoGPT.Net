using Asp.Versioning;
using Asp.Versioning.Builder;
using CryptoGPT.Application.Features.Coins.Queries.GetCoinDetails;
using CryptoGPT.Application.Features.Coins.Queries.GetMarketChart;
using CryptoGPT.Application.Features.Coins.Queries.GetTopCoins;
using FluentValidation;
using MediatR;
using System.Net;

namespace CryptoGPT.API.Endpoints
{
    public static class CoinEndpoints
    {
        public static WebApplication MapCoinEndpoints(this WebApplication app)
        {
            var versionSet = app.NewApiVersionSet()
                .HasApiVersion(new ApiVersion(1, 0))
                .HasApiVersion(new ApiVersion(2, 0))
                .ReportApiVersions()
                .Build();

            var group = app.MapGroup("/api/coin")
                .WithTags("Coins")
                .WithApiVersionSet(versionSet)
                .WithOpenApi();

            // Get top cryptocurrencies by market cap
            group.MapGet("/", async (IMediator mediator, int? limit) =>
            {
                try
                {
                    var result = await mediator.Send(new GetTopCoinsQuery { Limit = limit ?? 10 });
                    return Results.Ok(result);
                }
                catch (ValidationException ex)
                {
                    return Results.ValidationProblem(
                        ex.Errors.ToDictionary(
                            error => error.PropertyName,
                            error => new[] { error.ErrorMessage }
                        ),
                        statusCode: (int)HttpStatusCode.BadRequest
                    );
                }
            })
            .WithName("GetTopCoins")
            .WithDescription("Get top cryptocurrencies by market cap");

            // Get detailed information for a specific cryptocurrency
            group.MapGet("/{coinId}", async (IMediator mediator, string coinId) =>
            {
                try
                {
                    var result = await mediator.Send(new GetCoinDetailsQuery { CoinId = coinId });

                    if (result == null)
                        return Results.NotFound();

                    return Results.Ok(result);
                }
                catch (ValidationException ex)
                {
                    return Results.ValidationProblem(
                        ex.Errors.ToDictionary(
                            error => error.PropertyName,
                            error => new[] { error.ErrorMessage }
                        ),
                        statusCode: (int)HttpStatusCode.BadRequest
                    );
                }
            })
            .WithName("GetCoinDetails")
            .WithDescription("Get detailed information for a specific cryptocurrency");

            // Get historical market data for a cryptocurrency
            group.MapGet("/{coinId}/chart", async (IMediator mediator, string coinId, int? days) =>
            {
                try
                {
                    var result = await mediator.Send(new GetMarketChartQuery
                    {
                        CoinId = coinId,
                        Days = days ?? 30
                    });

                    return Results.Ok(result);
                }
                catch (ValidationException ex)
                {
                    return Results.ValidationProblem(
                        ex.Errors.ToDictionary(
                            error => error.PropertyName,
                            error => new[] { error.ErrorMessage }
                        ),
                        statusCode: (int)HttpStatusCode.BadRequest
                    );
                }
            })
            .WithName("GetMarketChart")
            .WithDescription("Get historical market data for a cryptocurrency");

            return app;
        }
    }
}