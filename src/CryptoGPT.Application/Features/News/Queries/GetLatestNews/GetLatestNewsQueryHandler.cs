using CryptoGPT.Application.Interfaces;
using CryptoGPT.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CryptoGPT.Application.Features.News.Queries.GetLatestNews
{
    public class GetLatestNewsQueryHandler : IRequestHandler<GetLatestNewsQuery, List<CryptoNewsItem>>
    {
        private readonly INewsService _newsService;
        private readonly ILogger<GetLatestNewsQueryHandler> _logger;

        public GetLatestNewsQueryHandler(
            INewsService newsService,
            ILogger<GetLatestNewsQueryHandler> logger)
        {
            _newsService = newsService ?? throw new ArgumentNullException(nameof(newsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<CryptoNewsItem>> Handle(GetLatestNewsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting latest cryptocurrency news, limit: {Limit}", request.Limit);
            return await _newsService.GetMarketNewsAsync(request.Limit);
        }
    }
}