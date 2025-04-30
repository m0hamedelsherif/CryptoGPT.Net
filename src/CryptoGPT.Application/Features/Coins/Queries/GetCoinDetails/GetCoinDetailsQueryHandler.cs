using CryptoGPT.Application.Common.Models;
using CryptoGPT.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoGPT.Application.Features.Coins.Queries.GetCoinDetails
{
    public class GetCoinDetailsQueryHandler : IRequestHandler<GetCoinDetailsQuery, CryptoCurrencyDetailDto?>
    {
        private readonly ICryptoDataService _cryptoDataService;

        public GetCoinDetailsQueryHandler(ICryptoDataService cryptoDataService)
        {
            _cryptoDataService = cryptoDataService;
        }

        public async Task<CryptoCurrencyDetailDto?> Handle(GetCoinDetailsQuery request, CancellationToken cancellationToken)
        {
            var coinData = await _cryptoDataService.GetCoinDataAsync(request.CoinId);

            if (coinData == null)
                return null;

            // Map domain entity to DTO
            return new CryptoCurrencyDetailDto
            {
                Id = coinData.Id,
                Symbol = coinData.Symbol,
                Name = coinData.Name,
                Description = coinData.Description,
                ImageUrl = coinData.ImageUrl,
                Homepage = coinData.Homepage,
                Whitepaper = coinData.Whitepaper,
                BlockchainSite = coinData.BlockchainSite,
                Twitter = coinData.Twitter,
                Facebook = coinData.Facebook,
                Subreddit = coinData.Subreddit,
                Categories = coinData.Categories,
                HashingAlgorithm = coinData.HashingAlgorithm,
                GenesisDate = coinData.GenesisDate,
                SentimentVotesUpPercentage = coinData.SentimentVotesUpPercentage,
                SentimentVotesDownPercentage = coinData.SentimentVotesDownPercentage,
                CurrentPrice = coinData.CurrentPrice,
                MarketCap = coinData.MarketCap,
                PriceChangePercentage24h = coinData.PriceChangePercentage24h,
                MarketCapRank = coinData.MarketCapRank,
                Volume24h = coinData.Volume24h,
                CirculatingSupply = coinData.CirculatingSupply,
                TotalSupply = coinData.TotalSupply,
                MaxSupply = coinData.MaxSupply,
                AllTimeHigh = coinData.AllTimeHigh,
                AllTimeHighDate = coinData.AllTimeHighDate,
                AllTimeLow = coinData.AllTimeLow,
                AllTimeLowDate = coinData.AllTimeLowDate,
                High24h = coinData.High24h,
                Low24h = coinData.Low24h
            };
        }
    }
}