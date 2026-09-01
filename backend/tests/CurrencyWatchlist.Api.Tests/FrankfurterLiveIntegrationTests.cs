using CurrencyWatchlist.Application.Common.Exceptions;
using CurrencyWatchlist.Application.Interfaces;
using CurrencyWatchlist.Infrastructure.ExternalServices;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyWatchlist.Api.Tests;

/// <summary>
/// Calls the real api.frankfurter.app - no mocking - to prove the HTTP client, JSON mapping,
/// and resilience wiring genuinely work against the live service, not just our assumptions
/// about its response shape. Everywhere else in this suite, IRateProvider is faked.
///
/// Deliberately excluded from the default `dotnet test` run (see ci.yml and the "Tests"
/// section of the root README) for the same reason the Playwright e2e suite is kept out of
/// CI: it depends on a real third-party network call, which is a source of flakiness CI
/// should not carry. Run explicitly with:
///   dotnet test --filter "Category=Live"
/// </summary>
[Trait("Category", "Live")]
public class FrankfurterLiveIntegrationTests
{
    private static IRateProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient<IRateProvider, FrankfurterRateProvider>(client =>
            {
                client.BaseAddress = new Uri("https://api.frankfurter.app/");
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddFrankfurterResilience();

        return services.BuildServiceProvider().GetRequiredService<IRateProvider>();
    }

    [Fact]
    public async Task Fetches_a_real_rate_for_a_known_pair()
    {
        var provider = CreateProvider();

        var result = await provider.GetLatestRatesAsync("USD", ["AUD"], CancellationToken.None);

        result.Should().ContainSingle();
        result[0].BaseCurrency.Should().Be("USD");
        result[0].QuoteCurrency.Should().Be("AUD");
        result[0].Rate.Should().BeGreaterThan(0);
        result[0].SourceTimestamp.Should().BeAfter(DateTime.UtcNow.AddDays(-14));
    }

    [Fact]
    public async Task Fetches_multiple_quote_currencies_for_one_base_in_a_single_call()
    {
        var provider = CreateProvider();

        var result = await provider.GetLatestRatesAsync("USD", ["AUD", "EUR"], CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(q => q.QuoteCurrency == "AUD" && q.Rate > 0);
        result.Should().Contain(q => q.QuoteCurrency == "EUR" && q.Rate > 0);
    }

    [Fact]
    public async Task Unknown_currency_against_the_real_api_throws_UnknownCurrencyException()
    {
        var provider = CreateProvider();

        var act = () => provider.GetLatestRatesAsync("USD", ["ZZZ"], CancellationToken.None);

        await act.Should().ThrowAsync<UnknownCurrencyException>();
    }
}
