using CryptoGPT.Application.Common.Models;
using MediatR;

namespace CryptoGPT.Application.Features.Coins.Queries.GetMarketChart
{
    public class GetMarketChartQuery : IRequest<MarketHistoryDto>
    {
        public string CoinId { get; set; } = string.Empty;
        public int Days { get; set; } = 30;
    }
}