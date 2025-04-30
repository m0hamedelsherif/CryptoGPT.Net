using CryptoGPT.Application.Common.Models;
using MediatR;

namespace CryptoGPT.Application.Features.Coins.Queries.GetCoinDetails
{
    public class GetCoinDetailsQuery : IRequest<CryptoCurrencyDetailDto?>
    {
        public string CoinId { get; set; } = string.Empty;
    }
}