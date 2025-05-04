using CryptoGPT.Application.Interfaces;
using CryptoGPT.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoGPT.Application.Features.News.Queries.GetCoinNews
{
    public class GetCoinNewsQueryHandler : IRequestHandler<GetCoinNewsQuery, List<CryptoNewsItem>>
    {
        private readonly INewsService _newsService;
        private readonly ILogger<GetCoinNewsQueryHandler> _logger;

        public GetCoinNewsQueryHandler(
            INewsService newsService,
            ILogger<GetCoinNewsQueryHandler> logger)
        {
            _newsService = newsService ?? throw new ArgumentNullException(nameof(newsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<CryptoNewsItem>> Handle(GetCoinNewsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting news for coin {CoinId} ({Symbol}), limit: {Limit}",
                request.CoinId, request.Symbol, request.Limit);

            return await _newsService.GetCoinNewsAsync(request.CoinId, request.Symbol, request.Limit);
        }
    }
}