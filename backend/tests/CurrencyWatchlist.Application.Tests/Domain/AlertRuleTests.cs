using CurrencyWatchlist.Domain.Entities;
using CurrencyWatchlist.Domain.Enums;
using FluentAssertions;

namespace CurrencyWatchlist.Application.Tests.Domain;

public class AlertRuleTests
{
    [Theory]
    [InlineData(1.65, 1.60, true)]
    [InlineData(1.60, 1.60, false)]
    [InlineData(1.55, 1.60, false)]
    public void IsTriggeredBy_Above_condition(decimal rate, decimal threshold, bool expected)
    {
        var rule = new AlertRule { Condition = AlertCondition.Above, Threshold = threshold };

        rule.IsTriggeredBy(rate).Should().Be(expected);
    }

    [Theory]
    [InlineData(1.55, 1.60, true)]
    [InlineData(1.60, 1.60, false)]
    [InlineData(1.65, 1.60, false)]
    public void IsTriggeredBy_Below_condition(decimal rate, decimal threshold, bool expected)
    {
        var rule = new AlertRule { Condition = AlertCondition.Below, Threshold = threshold };

        rule.IsTriggeredBy(rate).Should().Be(expected);
    }

    [Fact]
    public void IsTriggeredBy_throws_for_unknown_condition()
    {
        var rule = new AlertRule { Condition = (AlertCondition)99, Threshold = 1 };

        var act = () => rule.IsTriggeredBy(1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
