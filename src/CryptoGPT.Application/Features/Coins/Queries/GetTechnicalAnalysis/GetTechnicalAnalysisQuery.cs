using CryptoGPT.Application.Common.Models;
using MediatR;

namespace CryptoGPT.Application.Features.Coins.Queries.GetTechnicalAnalysis
{
    public class GetTechnicalAnalysisQuery : IRequest<TechnicalAnalysisDto>
    {
        public string CoinId { get; set; } = string.Empty;
        public int Days { get; set; } = 30;
    }
}