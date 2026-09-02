using System.Net;
using CurrencyWatchlist.Infrastructure.ExternalServices;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CurrencyWatchlist.Api.Tests;

public class FrankfurterCurrencyCatalogTests
{
    private static FrankfurterCurrencyCatalog CreateSut(CountingHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.frankfurter.app/") };
        return new FrankfurterCurrencyCatalog(httpClient, new MemoryCache(new MemoryCacheOptions()), Substitute.For<ILogger<FrankfurterCurrencyCatalog>>());
    }

    [Fact]
    public async Task Recognizes_currencies_returned_by_the_provider()
    {
        var json = """{"AUD":"Australian Dollar","USD":"United States Dollar"}""";
        var handler = new CountingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });
        var sut = CreateSut(handler);

        (await sut.IsSupportedAsync("AUD", CancellationToken.None)).Should().BeTrue();
        (await sut.IsSupportedAsync("aud", CancellationToken.None)).Should().BeTrue(); // case-insensitive
        (await sut.IsSupportedAsync("ZZZ", CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task Fetches_the_list_only_once_and_caches_it()
    {
        var json = """{"AUD":"Australian Dollar"}""";
        var handler = new CountingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });
        var sut = CreateSut(handler);

        await sut.IsSupportedAsync("AUD", CancellationToken.None);
        await sut.IsSupportedAsync("USD", CancellationToken.None);
        await sut.IsSupportedAsync("EUR", CancellationToken.None);

        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Fails_open_when_the_provider_is_unreachable()
    {
        var handler = new CountingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var sut = CreateSut(handler);

        var result = await sut.IsSupportedAsync("ANYTHING", CancellationToken.None);

        result.Should().BeTrue();
    }

    private sealed class CountingHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public CountingHttpMessageHandler(HttpResponseMessage response) => _response = response;

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(_response);
        }
    }
}
