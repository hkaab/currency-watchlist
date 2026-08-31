using CurrencyWatchlist.Application.Dtos.Items;
using CurrencyWatchlist.Application.Validators;
using FluentAssertions;

namespace CurrencyWatchlist.Application.Tests.Validators;

public class CreateWatchlistItemRequestValidatorTests
{
    private readonly CreateWatchlistItemRequestValidator _validator = new();

    [Fact]
    public void Valid_request_passes()
    {
        var result = _validator.Validate(new CreateWatchlistItemRequest("USD", "AUD"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("US", "AUD")]
    [InlineData("USDD", "AUD")]
    [InlineData("US1", "AUD")]
    [InlineData("", "AUD")]
    public void Invalid_base_currency_fails(string baseCurrency, string quoteCurrency)
    {
        var result = _validator.Validate(new CreateWatchlistItemRequest(baseCurrency, quoteCurrency));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Same_base_and_quote_currency_fails()
    {
        var result = _validator.Validate(new CreateWatchlistItemRequest("USD", "usd"));

        result.IsValid.Should().BeFalse();
    }
}
