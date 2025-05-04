using CryptoGPT.Application.Interfaces;
using CryptoGPT.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoGPT.Application.Features.Coins.Queries.GetMarketOverview;

public class GetMarketOverviewQueryHandler : IRequestHandler<GetMarketOverviewQuery, MarketOverview>
{
    private readonly ICryptoDataService _cryptoDataService;

    public GetMarketOverviewQueryHandler(ICryptoDataService cryptoDataService)
    {
        _cryptoDataService = cryptoDataService;
    }

    public async Task<MarketOverview> Handle(GetMarketOverviewQuery request, CancellationToken cancellationToken)
    {
        // Assuming GetMarketOverviewAsync returns the Domain.Entities.MarketOverview type
        // Removed cancellationToken as the service method doesn't accept it
        return await _cryptoDataService.GetMarketOverviewAsync(); 
    }
}