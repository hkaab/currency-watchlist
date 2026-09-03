using CurrencyWatchlist.Api.BackgroundServices;
using CurrencyWatchlist.Application.Dtos.Rates;
using CurrencyWatchlist.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CurrencyWatchlist.Api.Tests;

public class RateRefreshBackgroundServiceTests
{
    private static (RateRefreshBackgroundService Service, IRateService RateService) CreateSut(int intervalMinutes = 60)
    {
        var rateService = Substitute.For<IRateService>();

        var services = new ServiceCollection();
        services.AddScoped(_ => rateService);
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateRefresh:IntervalMinutes"] = intervalMinutes.ToString(),
            })
            .Build();

        var service = new RateRefreshBackgroundService(
            scopeFactory, Substitute.For<ILogger<RateRefreshBackgroundService>>(), configuration);

        return (service, rateService);
    }

    [Fact]
    public async Task Refreshes_immediately_on_startup_without_waiting_for_the_interval()
    {
        // A long interval isolates the assertion to the immediate startup refresh only.
        var (service, rateService) = CreateSut(intervalMinutes: 60);
        var called = new TaskCompletionSource();
        rateService.RefreshAsync(null, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                called.TrySetResult();
                return new RefreshRatesResponse(0, []);
            });

        await service.StartAsync(CancellationToken.None);
        await called.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await service.StopAsync(CancellationToken.None);

        await rateService.Received(1).RefreshAsync(null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Shuts_down_cleanly_while_waiting_between_ticks()
    {
        // A long interval ensures the service is sitting inside PeriodicTimer.WaitForNextTickAsync
        // (not mid-refresh) when StopAsync cancels it - the exact path that must not throw.
        var (service, rateService) = CreateSut(intervalMinutes: 60);
        rateService.RefreshAsync(null, Arg.Any<CancellationToken>()).Returns(new RefreshRatesResponse(0, []));

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(100);

        var stop = async () => await service.StopAsync(CancellationToken.None);
        await stop.Should().NotThrowAsync();
    }

    [Fact]
    public async Task IntervalSeconds_overrides_IntervalMinutes_when_both_are_set()
    {
        var rateService = Substitute.For<IRateService>();
        var services = new ServiceCollection();
        services.AddScoped(_ => rateService);
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateRefresh:IntervalMinutes"] = "60",
                ["RateRefresh:IntervalSeconds"] = "1",
            })
            .Build();

        var service = new RateRefreshBackgroundService(
            scopeFactory, Substitute.For<ILogger<RateRefreshBackgroundService>>(), configuration);

        var callCount = 0;
        var secondCall = new TaskCompletionSource();
        rateService.RefreshAsync(null, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (Interlocked.Increment(ref callCount) == 2)
                {
                    secondCall.TrySetResult();
                }
                return new RefreshRatesResponse(0, []);
            });

        await service.StartAsync(CancellationToken.None);
        // A second call inside 3s is only possible if the 1-second override won, not the 60-minute value.
        await secondCall.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task A_failed_refresh_does_not_fault_the_service()
    {
        var (service, rateService) = CreateSut(intervalMinutes: 60);
        var called = new TaskCompletionSource();
        rateService.RefreshAsync(null, Arg.Any<CancellationToken>())
            .Returns<RefreshRatesResponse>(_ =>
            {
                called.TrySetResult();
                throw new InvalidOperationException("Frankfurter is down");
            });

        await service.StartAsync(CancellationToken.None);
        await called.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Would throw if the exception escaped ExecuteAsync and faulted the hosted service.
        var stop = async () => await service.StopAsync(CancellationToken.None);
        await stop.Should().NotThrowAsync();
    }
}
