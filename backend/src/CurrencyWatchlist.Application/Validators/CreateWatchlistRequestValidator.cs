using CurrencyWatchlist.Application.Dtos.Watchlists;
using FluentValidation;

namespace CurrencyWatchlist.Application.Validators;

public sealed class CreateWatchlistRequestValidator : AbstractValidator<CreateWatchlistRequest>
{
    public CreateWatchlistRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}
