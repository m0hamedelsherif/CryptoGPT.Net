using FluentValidation;

namespace CryptoGPT.Application.Features.Coins.Queries.GetTopCoins
{
    public class GetTopCoinsQueryValidator : AbstractValidator<GetTopCoinsQuery>
    {
        public GetTopCoinsQueryValidator()
        {
            RuleFor(x => x.Limit)
                .GreaterThan(0).WithMessage("Limit must be greater than 0")
                .LessThanOrEqualTo(250).WithMessage("Limit cannot exceed 250 coins");
        }
    }
}