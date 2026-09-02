using CurrencyWatchlist.Application.Dtos.Items;
using CurrencyWatchlist.Application.Interfaces;
using CurrencyWatchlist.Application.Validators;
using FluentAssertions;
using NSubstitute;

namespace CurrencyWatchlist.Application.Tests.Validators;

public class CreateWatchlistItemRequestValidatorTests
{
    private readonly ICurrencyCatalog _currencyCatalog = Substitute.For<ICurrencyCatalog>();
    private readonly CreateWatchlistItemRequestValidator _validator;

    public CreateWatchlistItemRequestValidatorTests()
    {
        _currencyCatalog.IsSupportedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _validator = new CreateWatchlistItemRequestValidator(_currencyCatalog);
    }

    [Fact]
    public async Task Valid_request_passes()
    {
        var result = await _validator.ValidateAsync(new CreateWatchlistItemRequest("USD", "AUD"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("US", "AUD")]
    [InlineData("USDD", "AUD")]
    [InlineData("US1", "AUD")]
    [InlineData("", "AUD")]
    public async Task Invalid_base_currency_format_fails(string baseCurrency, string quoteCurrency)
    {
        var result = await _validator.ValidateAsync(new CreateWatchlistItemRequest(baseCurrency, quoteCurrency));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Same_base_and_quote_currency_fails()
    {
        var result = await _validator.ValidateAsync(new CreateWatchlistItemRequest("USD", "usd"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Unsupported_currency_fails_even_with_a_valid_3_letter_format()
    {
        _currencyCatalog.IsSupportedAsync("ZZZ", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _validator.ValidateAsync(new CreateWatchlistItemRequest("ZZZ", "AUD"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("not a currency"));
    }

    [Fact]
    public async Task Skips_the_catalog_check_when_the_format_is_already_invalid()
    {
        var result = await _validator.ValidateAsync(new CreateWatchlistItemRequest("US", "AUD"));

        result.IsValid.Should().BeFalse();
        await _currencyCatalog.DidNotReceive().IsSupportedAsync("US", Arg.Any<CancellationToken>());
    }
}
