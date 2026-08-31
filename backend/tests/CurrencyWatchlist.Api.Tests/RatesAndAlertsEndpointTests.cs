using System.Net;
using System.Net.Http.Json;
using CurrencyWatchlist.Application.Common.Exceptions;
using CurrencyWatchlist.Application.Dtos.Alerts;
using CurrencyWatchlist.Application.Dtos.Items;
using CurrencyWatchlist.Application.Dtos.Rates;
using CurrencyWatchlist.Application.Dtos.Watchlists;
using CurrencyWatchlist.Application.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace CurrencyWatchlist.Api.Tests;

public class RatesAndAlertsEndpointTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public RatesAndAlertsEndpointTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Golden_path_refresh_create_alert_and_evaluate_triggers()
    {
        _factory.RateProviderFake
            .GetLatestRatesAsync("USD", Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<RateQuote> { new("USD", "AUD", 1.65m, DateTime.UtcNow) });

        var watchlist = await CreateWatchlistAsync("Golden Path");
        var item = await AddItemAsync(watchlist.Id, "USD", "AUD");

        var refreshResponse = await _client.PostAsync($"/api/rates/refresh?watchlistId={watchlist.Id}", null);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refresh = await refreshResponse.Content.ReadFromJsonAsync<RefreshRatesResponse>(JsonTestOptions.Default);
        refresh!.RefreshedPairCount.Should().Be(1);

        var latestResponse = await _client.GetAsync("/api/rates/latest?base=USD&quote=AUD");
        latestResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var alertResponse = await _client.PostAsJsonAsync(
            "/api/alerts", new { watchlistItemId = item.Id, condition = "Above", threshold = 1.0m }, JsonTestOptions.Default);
        alertResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var alert = await alertResponse.Content.ReadFromJsonAsync<AlertRuleResponse>(JsonTestOptions.Default);

        var evaluateResponse = await _client.PostAsync($"/api/alerts/{alert!.Id}/evaluate", null);
        evaluateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var evaluation = await evaluateResponse.Content.ReadFromJsonAsync<AlertEvaluationResult>(JsonTestOptions.Default);
        evaluation!.IsTriggered.Should().BeTrue();
        evaluation.AlertEventId.Should().NotBeNull();
    }

    [Fact]
    public async Task Evaluate_not_triggered_returns_false_result()
    {
        _factory.RateProviderFake
            .GetLatestRatesAsync("USD", Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<RateQuote> { new("USD", "AUD", 1.2m, DateTime.UtcNow) });

        var watchlist = await CreateWatchlistAsync("Not Triggered");
        var item = await AddItemAsync(watchlist.Id, "USD", "AUD");
        var alert = await CreateAlertAsync(item.Id, "Above", 5.0m);

        var response = await _client.PostAsync($"/api/alerts/{alert.Id}/evaluate", null);

        var evaluation = await response.Content.ReadFromJsonAsync<AlertEvaluationResult>(JsonTestOptions.Default);
        evaluation!.IsTriggered.Should().BeFalse();
        evaluation.AlertEventId.Should().BeNull();
    }

    [Fact]
    public async Task Refresh_with_unknown_currency_returns_400()
    {
        _factory.RateProviderFake
            .GetLatestRatesAsync(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<RateQuote>>(_ => throw new UnknownCurrencyException("ZZZ"));

        var watchlist = await CreateWatchlistAsync("Bad Currency");
        await AddItemAsync(watchlist.Id, "USD", "ZZZ");

        var response = await _client.PostAsync($"/api/rates/refresh?watchlistId={watchlist.Id}", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_when_provider_unavailable_returns_502()
    {
        _factory.RateProviderFake
            .GetLatestRatesAsync(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<RateQuote>>(_ => throw new RateProviderUnavailableException("provider down"));

        var watchlist = await CreateWatchlistAsync("Provider Down");
        await AddItemAsync(watchlist.Id, "USD", "AUD");

        var response = await _client.PostAsync($"/api/rates/refresh?watchlistId={watchlist.Id}", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task GetLatest_for_pair_with_no_snapshot_returns_404()
    {
        var response = await _client.GetAsync("/api/rates/latest?base=USD&quote=AUD");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_alert_for_missing_item_returns_404()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/alerts", new { watchlistItemId = 999, condition = "Above", threshold = 1.0m }, JsonTestOptions.Default);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Evaluate_missing_alert_returns_404()
    {
        var response = await _client.PostAsync("/api/alerts/999/evaluate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<WatchlistResponse> CreateWatchlistAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/watchlists", new { name }, JsonTestOptions.Default);
        return (await response.Content.ReadFromJsonAsync<WatchlistResponse>(JsonTestOptions.Default))!;
    }

    private async Task<WatchlistItemResponse> AddItemAsync(int watchlistId, string baseCurrency, string quoteCurrency)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/watchlists/{watchlistId}/items", new { baseCurrency, quoteCurrency }, JsonTestOptions.Default);
        return (await response.Content.ReadFromJsonAsync<WatchlistItemResponse>(JsonTestOptions.Default))!;
    }

    private async Task<AlertRuleResponse> CreateAlertAsync(int watchlistItemId, string condition, decimal threshold)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/alerts", new { watchlistItemId, condition, threshold }, JsonTestOptions.Default);
        return (await response.Content.ReadFromJsonAsync<AlertRuleResponse>(JsonTestOptions.Default))!;
    }
}
