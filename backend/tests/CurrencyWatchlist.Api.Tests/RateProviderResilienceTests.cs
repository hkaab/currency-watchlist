using System.Net;
using CurrencyWatchlist.Application.Common.Exceptions;
using CurrencyWatchlist.Application.Interfaces;
using CurrencyWatchlist.Infrastructure.ExternalServices;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyWatchlist.Api.Tests;

/// <summary>
/// Exercises the real Polly-based retry/circuit-breaker pipeline (AddFrankfurterResilience),
/// not just FrankfurterRateProvider in isolation - built through the same DI wiring as
/// production, with only the transport swapped for a scripted stub.
/// </summary>
public class RateProviderResilienceTests
{
    private static (IRateProvider Provider, SequenceHttpMessageHandler Handler) CreateResilientProvider(
        params HttpResponseMessage[] responses)
    {
        var handler = new SequenceHttpMessageHandler(responses);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient<IRateProvider, FrankfurterRateProvider>(client =>
            {
                client.BaseAddress = new Uri("https://api.frankfurter.app/");
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .AddFrankfurterResilience();

        var provider = services.BuildServiceProvider().GetRequiredService<IRateProvider>();
        return (provider, handler);
    }

    [Fact]
    public async Task Retries_a_transient_failure_then_succeeds()
    {
        var json = """{"amount":1.0,"base":"USD","date":"2026-02-23","rates":{"AUD":1.52}}""";
        var (provider, handler) = CreateResilientProvider(
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent(json) });

        var result = await provider.GetLatestRatesAsync("USD", ["AUD"], CancellationToken.None);

        result.Should().ContainSingle(q => q.Rate == 1.52m);
        handler.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task Exhausts_retries_and_throws_when_the_provider_stays_down()
    {
        var (provider, handler) = CreateResilientProvider(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var act = () => provider.GetLatestRatesAsync("USD", ["AUD"], CancellationToken.None);

        await act.Should().ThrowAsync<RateProviderUnavailableException>();
        handler.CallCount.Should().Be(4); // 1 initial attempt + 3 retries
    }

    [Fact]
    public async Task Does_not_retry_a_non_transient_unknown_currency_response()
    {
        var (provider, handler) = CreateResilientProvider(new HttpResponseMessage(HttpStatusCode.NotFound));

        var act = () => provider.GetLatestRatesAsync("USD", ["ZZZ"], CancellationToken.None);

        await act.Should().ThrowAsync<UnknownCurrencyException>();
        handler.CallCount.Should().Be(1);
    }

    private static StringContent JsonContent(string json) =>
        new(json, System.Text.Encoding.UTF8, "application/json");

    private sealed class SequenceHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public SequenceHttpMessageHandler(IEnumerable<HttpResponseMessage> responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            var response = _responses.Count > 1 ? _responses.Dequeue() : _responses.Peek();
            return Task.FromResult(response);
        }
    }
}
