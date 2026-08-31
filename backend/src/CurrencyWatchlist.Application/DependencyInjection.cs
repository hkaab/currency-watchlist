using CurrencyWatchlist.Application.EventHandlers;
using CurrencyWatchlist.Application.Events;
using CurrencyWatchlist.Application.Services;
using CurrencyWatchlist.Domain.Events;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyWatchlist.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IWatchlistService, WatchlistService>();
        services.AddScoped<IWatchlistItemService, WatchlistItemService>();
        services.AddScoped<IRateService, RateService>();
        services.AddScoped<IAlertService, AlertService>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IDomainEventHandler<RatesRefreshedEvent>, EvaluateAlertsOnRateRefreshHandler>();
        services.AddScoped<IDomainEventHandler<RatesRefreshedEvent>, PushRateUpdateHandler>();
        services.AddScoped<IDomainEventHandler<AlertTriggeredEvent>, PushAlertNotificationHandler>();

        return services;
    }
}
