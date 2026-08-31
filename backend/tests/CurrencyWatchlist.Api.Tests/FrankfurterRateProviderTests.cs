using System.Net;
using CurrencyWatchlist.Application.Common.Exceptions;
using CurrencyWatchlist.Infrastructure.ExternalServices;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CurrencyWatchlist.Api.Tests;

public class FrankfurterRateProviderTests
{
    private static FrankfurterRateProvider CreateSut(HttpResponseMessage response)
    {
        var handler = new StubHttpMessageHandler(response);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.frankfurter.app/") };
        return new FrankfurterRateProvider(httpClient, Substitute.For<ILogger<FrankfurterRateProvider>>());
    }

    [Fact]
    public async Task Successful_response_maps_to_domain_rate_quotes()
    {
        var json = """{"amount":1.0,"base":"USD","date":"2026-02-23","rates":{"AUD":1.52}}""";
        var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });

        var result = await sut.GetLatestRatesAsync("USD", ["AUD"], CancellationToken.None);

        result.Should().ContainSingle();
        result[0].BaseCurrency.Should().Be("USD");
        result[0].QuoteCurrency.Should().Be("AUD");
        result[0].Rate.Should().Be(1.52m);
    }

    [Fact]
    public async Task NotFound_response_throws_UnknownCurrencyException()
    {
        var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.NotFound));

        var act = () => sut.GetLatestRatesAsync("USD", ["ZZZ"], CancellationToken.None);

        await act.Should().ThrowAsync<UnknownCurrencyException>();
    }

    [Fact]
    public async Task Server_error_response_throws_RateProviderUnavailableException()
    {
        var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var act = () => sut.GetLatestRatesAsync("USD", ["AUD"], CancellationToken.None);

        await act.Should().ThrowAsync<RateProviderUnavailableException>();
    }

    [Fact]
    public async Task Missing_quote_currency_in_response_throws_UnknownCurrencyException()
    {
        var json = """{"amount":1.0,"base":"USD","date":"2026-02-23","rates":{}}""";
        var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });

        var act = () => sut.GetLatestRatesAsync("USD", ["ZZZ"], CancellationToken.None);

        await act.Should().ThrowAsync<UnknownCurrencyException>();
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public StubHttpMessageHandler(HttpResponseMessage response) => _response = response;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(_response);
    }
}
