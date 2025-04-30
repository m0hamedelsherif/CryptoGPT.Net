using CryptoGPT.Application.Common.Models;
using CryptoGPT.Application.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoGPT.Application.Features.Coins.Queries.GetMarketChart
{
    public class GetMarketChartQueryHandler : IRequestHandler<GetMarketChartQuery, MarketHistoryDto>
    {
        private readonly ICryptoDataService _cryptoDataService;

        public GetMarketChartQueryHandler(ICryptoDataService cryptoDataService)
        {
            _cryptoDataService = cryptoDataService;
        }

        public async Task<MarketHistoryDto> Handle(GetMarketChartQuery request, CancellationToken cancellationToken)
        {
            var marketHistory = await _cryptoDataService.GetMarketChartAsync(request.CoinId, request.Days);

            // Map domain entity to DTO
            var result = new MarketHistoryDto
            {
                CoinId = marketHistory.CoinId,
                Symbol = marketHistory.Symbol,
                Prices = marketHistory.Prices.Select(p => new PriceHistoryPointDto
                {
                    Timestamp = p.Timestamp,
                    Price = p.Price
                }).ToList(),
                MarketCaps = marketHistory.MarketCaps.Select(p => new PriceHistoryPointDto
                {
                    Timestamp = p.Timestamp,
                    Price = p.Price
                }).ToList(),
                Volumes = marketHistory.Volumes.Select(p => new PriceHistoryPointDto
                {
                    Timestamp = p.Timestamp,
                    Price = p.Price
                }).ToList()
            };

            // Map indicator series if any
            foreach (var indicatorSeries in marketHistory.IndicatorSeries)
            {
                result.IndicatorSeries[indicatorSeries.Key] = indicatorSeries.Value
                    .Select(p => new IndicatorTimePointDto
                    {
                        Timestamp = p.Timestamp,
                        Value = p.Value
                    }).ToList();
            }

            return result;
        }
    }
}