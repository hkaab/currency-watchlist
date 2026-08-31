using CurrencyWatchlist.Application.Dtos.Watchlists;
using CurrencyWatchlist.Application.Validators;
using FluentAssertions;

namespace CurrencyWatchlist.Application.Tests.Validators;

public class CreateWatchlistRequestValidatorTests
{
    private readonly CreateWatchlistRequestValidator _validator = new();

    [Fact]
    public void Valid_request_passes()
    {
        var result = _validator.Validate(new CreateWatchlistRequest("My Watchlist"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_name_fails(string name)
    {
        var result = _validator.Validate(new CreateWatchlistRequest(name));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Name_over_max_length_fails()
    {
        var result = _validator.Validate(new CreateWatchlistRequest(new string('a', 101)));

        result.IsValid.Should().BeFalse();
    }
}
