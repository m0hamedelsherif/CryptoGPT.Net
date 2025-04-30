using CryptoGPT.Application.Common.Models;
using MediatR;
using System.Collections.Generic;

namespace CryptoGPT.Application.Features.Coins.Queries.GetTopCoins
{
    public class GetTopCoinsQuery : IRequest<List<CryptoCurrencyDto>>
    {
        public int Limit { get; set; } = 10;
    }
}