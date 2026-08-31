using CurrencyWatchlist.Application.Dtos.Alerts;
using FluentValidation;

namespace CurrencyWatchlist.Application.Validators;

public sealed class CreateAlertRuleRequestValidator : AbstractValidator<CreateAlertRuleRequest>
{
    public CreateAlertRuleRequestValidator()
    {
        RuleFor(x => x.WatchlistItemId).GreaterThan(0);
        RuleFor(x => x.Condition).IsInEnum();
        RuleFor(x => x.Threshold).GreaterThan(0);
    }
}
