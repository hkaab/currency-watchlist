using CurrencyWatchlist.Application.Dtos.Alerts;
using CurrencyWatchlist.Application.Validators;
using CurrencyWatchlist.Domain.Enums;
using FluentAssertions;

namespace CurrencyWatchlist.Application.Tests.Validators;

public class CreateAlertRuleRequestValidatorTests
{
    private readonly CreateAlertRuleRequestValidator _validator = new();

    [Fact]
    public void Valid_request_passes()
    {
        var result = _validator.Validate(new CreateAlertRuleRequest(1, AlertCondition.Above, 1.6m));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Zero_watchlist_item_id_fails()
    {
        var result = _validator.Validate(new CreateAlertRuleRequest(0, AlertCondition.Above, 1.6m));

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_threshold_fails(decimal threshold)
    {
        var result = _validator.Validate(new CreateAlertRuleRequest(1, AlertCondition.Below, threshold));

        result.IsValid.Should().BeFalse();
    }
}
