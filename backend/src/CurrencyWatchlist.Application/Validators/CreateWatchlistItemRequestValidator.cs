using CurrencyWatchlist.Application.Dtos.Items;
using CurrencyWatchlist.Application.Interfaces;
using FluentValidation;

namespace CurrencyWatchlist.Application.Validators;

public sealed class CreateWatchlistItemRequestValidator : AbstractValidator<CreateWatchlistItemRequest>
{
    private const string CurrencyCodePattern = "^[A-Za-z]{3}$";

    public CreateWatchlistItemRequestValidator(ICurrencyCatalog currencyCatalog)
    {
        RuleFor(x => x.BaseCurrency)
            .NotEmpty()
            .Matches(CurrencyCodePattern)
            .WithMessage("BaseCurrency must be a 3-letter ISO currency code (e.g. USD).")
            .DependentRules(() =>
            {
                RuleFor(x => x.BaseCurrency)
                    .MustAsync((code, ct) => currencyCatalog.IsSupportedAsync(code, ct))
                    .WithMessage(x => $"'{x.BaseCurrency.ToUpperInvariant()}' is not a currency the rate provider supports.");
            });

        RuleFor(x => x.QuoteCurrency)
            .NotEmpty()
            .Matches(CurrencyCodePattern)
            .WithMessage("QuoteCurrency must be a 3-letter ISO currency code (e.g. AUD).")
            .DependentRules(() =>
            {
                RuleFor(x => x.QuoteCurrency)
                    .MustAsync((code, ct) => currencyCatalog.IsSupportedAsync(code, ct))
                    .WithMessage(x => $"'{x.QuoteCurrency.ToUpperInvariant()}' is not a currency the rate provider supports.");
            });

        RuleFor(x => x)
            .Must(x => !string.Equals(x.BaseCurrency, x.QuoteCurrency, StringComparison.OrdinalIgnoreCase))
            .WithMessage("BaseCurrency and QuoteCurrency must be different.")
            .OverridePropertyName("QuoteCurrency");
    }
}
