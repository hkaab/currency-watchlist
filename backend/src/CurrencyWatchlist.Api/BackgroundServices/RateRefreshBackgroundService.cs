using CurrencyWatchlist.Application.Services;

namespace CurrencyWatchlist.Api.BackgroundServices;

/// <summary>
/// Keeps rates fresh without any client ever having to ask: refreshes every distinct
/// currency pair on a fixed interval, reusing the exact same <see cref="IRateService.RefreshAsync"/>
/// call the "Refresh Rates" button makes. That means a scheduled tick drives the same
/// RatesRefreshedEvent -&gt; alert evaluation -&gt; SignalR push pipeline as a manual refresh -
/// connected clients receive it as a live push, no frontend polling involved.
/// </summary>
public class RateRefreshBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RateRefreshBackgroundService> _logger;
    private readonly TimeSpan _interval;

    public RateRefreshBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<RateRefreshBackgroundService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        // IntervalSeconds is a finer-grained override for demo/testing use (e.g. watching it
        // react on screen); IntervalMinutes is the normal, documented way to configure this.
        var seconds = configuration.GetValue<int?>("RateRefresh:IntervalSeconds");
        _interval = seconds.HasValue
            ? TimeSpan.FromSeconds(seconds.Value)
            : TimeSpan.FromMinutes(configuration.GetValue<int?>("RateRefresh:IntervalMinutes") ?? 5);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Rate refresh background service starting, interval {Interval}", _interval);

        // Populate rates immediately on startup rather than leaving the app empty for a full interval.
        await RefreshAllAsync(stoppingToken);

        try
        {
            using var timer = new PeriodicTimer(_interval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RefreshAllAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected on graceful shutdown - PeriodicTimer.WaitForNextTickAsync throws this when
            // stoppingToken is cancelled. Left uncaught, BackgroundServiceExceptionBehavior's
            // default (StopHost) would treat a normal shutdown as a fatal, host-stopping error.
        }
    }

    private async Task RefreshAllAsync(CancellationToken cancellationToken)
    {
        // IRateService is scoped; this service is a singleton, so a fresh scope is required per tick.
        using var scope = _scopeFactory.CreateScope();
        var rateService = scope.ServiceProvider.GetRequiredService<IRateService>();

        try
        {
            var result = await rateService.RefreshAsync(watchlistId: null, cancellationToken);
            _logger.LogInformation("Scheduled rate refresh completed: {Count} pair(s) updated", result.RefreshedPairCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during shutdown.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Scheduled rate refresh failed; will retry on the next tick");
        }
    }
}
