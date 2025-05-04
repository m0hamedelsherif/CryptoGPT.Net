using CryptoGPT.Domain.Entities;
using MediatR;
using System.Collections.Generic;

namespace CryptoGPT.Application.Features.News.Queries.GetLatestNews
{
    /// <summary>
    /// Query to get latest cryptocurrency news
    /// </summary>
    public class GetLatestNewsQuery : IRequest<List<CryptoNewsItem>>
    {
        /// <summary>
        /// Maximum number of news items to return
        /// </summary>
        public int Limit { get; set; } = 20;
    }
}