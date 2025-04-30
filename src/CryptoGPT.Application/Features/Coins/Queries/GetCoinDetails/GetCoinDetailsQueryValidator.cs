using FluentValidation;

namespace CryptoGPT.Application.Features.Coins.Queries.GetCoinDetails
{
    public class GetCoinDetailsQueryValidator : AbstractValidator<GetCoinDetailsQuery>
    {
        public GetCoinDetailsQueryValidator()
        {
            RuleFor(x => x.CoinId)
                .NotEmpty().WithMessage("Coin ID is required")
                .MinimumLength(2).WithMessage("Coin ID must be at least 2 characters")
                .MaximumLength(50).WithMessage("Coin ID cannot exceed 50 characters");
        }
    }
}