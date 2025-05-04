using CryptoGPT.Domain.Entities; // Assuming MarketOverview is in Domain.Entities
using MediatR;

namespace CryptoGPT.Application.Features.Coins.Queries.GetMarketOverview;

public record GetMarketOverviewQuery : IRequest<MarketOverview>;