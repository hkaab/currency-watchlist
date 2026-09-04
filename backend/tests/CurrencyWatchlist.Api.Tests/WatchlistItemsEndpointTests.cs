using System.Net;
using System.Net.Http.Json;
using CurrencyWatchlist.Application.Dtos.Items;
using CurrencyWatchlist.Application.Dtos.Watchlists;
using FluentAssertions;
using NSubstitute;

namespace CurrencyWatchlist.Api.Tests;

public class WatchlistItemsEndpointTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public WatchlistItemsEndpointTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Add_item_normalizes_and_returns_it()
    {
        var watchlist = await CreateWatchlistAsync("Items List");

        var response = await _client.PostAsJsonAsync(
            $"/api/watchlists/{watchlist.Id}/items", new { baseCurrency = "usd", quoteCurrency = "aud" }, JsonTestOptions.Default);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var item = await response.Content.ReadFromJsonAsync<WatchlistItemResponse>(JsonTestOptions.Default);
        item!.BaseCurrency.Should().Be("USD");
        item.QuoteCurrency.Should().Be("AUD");
    }

    [Fact]
    public async Task Add_item_with_invalid_currency_code_returns_400()
    {
        var watchlist = await CreateWatchlistAsync("Items List");

        var response = await _client.PostAsJsonAsync(
            $"/api/watchlists/{watchlist.Id}/items", new { baseCurrency = "US", quoteCurrency = "AUD" }, JsonTestOptions.Default);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Add_item_with_a_currency_the_provider_does_not_support_returns_400()
    {
        _factory.CurrencyCatalogFake.IsSupportedAsync("ZZZ", Arg.Any<CancellationToken>()).Returns(false);
        var watchlist = await CreateWatchlistAsync("Items List");

        var response = await _client.PostAsJsonAsync(
            $"/api/watchlists/{watchlist.Id}/items", new { baseCurrency = "ZZZ", quoteCurrency = "AUD" }, JsonTestOptions.Default);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Add_item_duplicating_an_existing_pair_on_the_watchlist_returns_409()
    {
        var watchlist = await CreateWatchlistAsync("Items List");
        await _client.PostAsJsonAsync(
            $"/api/watchlists/{watchlist.Id}/items", new { baseCurrency = "USD", quoteCurrency = "AUD" }, JsonTestOptions.Default);

        var response = await _client.PostAsJsonAsync(
            $"/api/watchlists/{watchlist.Id}/items", new { baseCurrency = "usd", quoteCurrency = "aud" }, JsonTestOptions.Default);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Add_item_to_missing_watchlist_returns_404()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/watchlists/999/items", new { baseCurrency = "USD", quoteCurrency = "AUD" }, JsonTestOptions.Default);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Remove_item_returns_204_then_watchlist_no_longer_lists_it()
    {
        var watchlist = await CreateWatchlistAsync("Items List");
        var addResponse = await _client.PostAsJsonAsync(
            $"/api/watchlists/{watchlist.Id}/items", new { baseCurrency = "USD", quoteCurrency = "AUD" }, JsonTestOptions.Default);
        var item = await addResponse.Content.ReadFromJsonAsync<WatchlistItemResponse>(JsonTestOptions.Default);

        var deleteResponse = await _client.DeleteAsync($"/api/watchlists/{watchlist.Id}/items/{item!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await (await _client.GetAsync($"/api/watchlists/{watchlist.Id}"))
            .Content.ReadFromJsonAsync<WatchlistDetailResponse>(JsonTestOptions.Default);
        detail!.Items.Should().BeEmpty();
    }

    private async Task<WatchlistResponse> CreateWatchlistAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/watchlists", new { name }, JsonTestOptions.Default);
        return (await response.Content.ReadFromJsonAsync<WatchlistResponse>(JsonTestOptions.Default))!;
    }
}
