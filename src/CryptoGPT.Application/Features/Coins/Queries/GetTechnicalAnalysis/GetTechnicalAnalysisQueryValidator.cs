using FluentValidation;

namespace CryptoGPT.Application.Features.Coins.Queries.GetTechnicalAnalysis
{
    public class GetTechnicalAnalysisQueryValidator : AbstractValidator<GetTechnicalAnalysisQuery>
    {
        public GetTechnicalAnalysisQueryValidator()
        {
            RuleFor(x => x.CoinId)
                .NotEmpty().WithMessage("Coin ID is required")
                .MinimumLength(2).WithMessage("Coin ID must be at least 2 characters")
                .MaximumLength(50).WithMessage("Coin ID cannot exceed 50 characters");

            RuleFor(x => x.Days)
                .GreaterThan(0).WithMessage("Days must be greater than 0")
                .LessThanOrEqualTo(365).WithMessage("Days cannot exceed 365");
        }
    }
}