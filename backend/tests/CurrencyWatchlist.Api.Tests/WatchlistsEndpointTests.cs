using System.Net;
using System.Net.Http.Json;
using CurrencyWatchlist.Application.Dtos.Watchlists;
using FluentAssertions;

namespace CurrencyWatchlist.Api.Tests;

public class WatchlistsEndpointTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public WatchlistsEndpointTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Create_then_get_returns_the_watchlist()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/watchlists", new { name = "Test List" }, JsonTestOptions.Default);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<WatchlistResponse>(JsonTestOptions.Default);

        var getResponse = await _client.GetAsync($"/api/watchlists/{created!.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await getResponse.Content.ReadFromJsonAsync<WatchlistDetailResponse>(JsonTestOptions.Default);
        detail!.Name.Should().Be("Test List");
    }

    [Fact]
    public async Task Create_with_empty_name_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/api/watchlists", new { name = "" }, JsonTestOptions.Default);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_for_missing_watchlist_returns_404()
    {
        var response = await _client.GetAsync("/api/watchlists/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_lists_created_watchlists()
    {
        await _client.PostAsJsonAsync("/api/watchlists", new { name = "List A" }, JsonTestOptions.Default);
        await _client.PostAsJsonAsync("/api/watchlists", new { name = "List B" }, JsonTestOptions.Default);

        var response = await _client.GetAsync("/api/watchlists");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var all = await response.Content.ReadFromJsonAsync<List<WatchlistResponse>>(JsonTestOptions.Default);
        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task Delete_removes_the_watchlist()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/watchlists", new { name = "To Delete" }, JsonTestOptions.Default);
        var created = await createResponse.Content.ReadFromJsonAsync<WatchlistResponse>(JsonTestOptions.Default);

        var deleteResponse = await _client.DeleteAsync($"/api/watchlists/{created!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/watchlists/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_for_missing_watchlist_returns_404()
    {
        var response = await _client.DeleteAsync("/api/watchlists/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
