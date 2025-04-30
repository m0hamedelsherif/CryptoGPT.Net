using CryptoGPT.Application.Common.Models;
using CryptoGPT.Application.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoGPT.Application.Features.Coins.Queries.GetTopCoins
{
    public class GetTopCoinsQueryHandler : IRequestHandler<GetTopCoinsQuery, List<CryptoCurrencyDto>>
    {
        private readonly ICryptoDataService _cryptoDataService;

        public GetTopCoinsQueryHandler(ICryptoDataService cryptoDataService)
        {
            _cryptoDataService = cryptoDataService;
        }

        public async Task<List<CryptoCurrencyDto>> Handle(GetTopCoinsQuery request, CancellationToken cancellationToken)
        {
            var coins = await _cryptoDataService.GetTopCoinsAsync(request.Limit);

            // Map domain entities to DTOs
            return coins.Select(c => new CryptoCurrencyDto
            {
                Id = c.Id,
                Symbol = c.Symbol,
                Name = c.Name,
                CurrentPrice = c.CurrentPrice,
                MarketCap = c.MarketCap,
                PriceChangePercentage24h = c.PriceChangePercentage24h,
                MarketCapRank = c.MarketCapRank,
                Volume24h = c.Volume24h,
                ImageUrl = c.ImageUrl
            }).ToList();
        }
    }
}