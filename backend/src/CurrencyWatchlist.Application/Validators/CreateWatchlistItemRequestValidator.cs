using CurrencyWatchlist.Application.Dtos.Items;
using FluentValidation;

namespace CurrencyWatchlist.Application.Validators;

public sealed class CreateWatchlistItemRequestValidator : AbstractValidator<CreateWatchlistItemRequest>
{
    private const string CurrencyCodePattern = "^[A-Za-z]{3}$";

    public CreateWatchlistItemRequestValidator()
    {
        RuleFor(x => x.BaseCurrency)
            .NotEmpty()
            .Matches(CurrencyCodePattern)
            .WithMessage("BaseCurrency must be a 3-letter ISO currency code (e.g. USD).");

        RuleFor(x => x.QuoteCurrency)
            .NotEmpty()
            .Matches(CurrencyCodePattern)
            .WithMessage("QuoteCurrency must be a 3-letter ISO currency code (e.g. AUD).");

        RuleFor(x => x)
            .Must(x => !string.Equals(x.BaseCurrency, x.QuoteCurrency, StringComparison.OrdinalIgnoreCase))
            .WithMessage("BaseCurrency and QuoteCurrency must be different.")
            .OverridePropertyName("QuoteCurrency");
    }
}
