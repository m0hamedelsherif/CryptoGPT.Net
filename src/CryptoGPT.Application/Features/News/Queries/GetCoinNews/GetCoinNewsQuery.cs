using CryptoGPT.Domain.Entities;
using MediatR;
using System.Collections.Generic;

namespace CryptoGPT.Application.Features.News.Queries.GetCoinNews
{
    /// <summary>
    /// Query to get news for a specific cryptocurrency
    /// </summary>
    public class GetCoinNewsQuery : IRequest<List<CryptoNewsItem>>
    {
        /// <summary>
        /// The coin ID to get news for
        /// </summary>
        public string CoinId { get; set; } = string.Empty;
        
        /// <summary>
        /// The coin symbol (used for better news matching)
        /// </summary>
        public string Symbol { get; set; } = string.Empty;
        
        /// <summary>
        /// Maximum number of news items to return
        /// </summary>
        public int Limit { get; set; } = 10;
    }
}