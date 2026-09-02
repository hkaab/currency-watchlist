using CurrencyWatchlist.Application.Events;
using CurrencyWatchlist.Application.Interfaces;
using CurrencyWatchlist.Infrastructure.ExternalServices;
using CurrencyWatchlist.Infrastructure.Events;
using CurrencyWatchlist.Infrastructure.Persistence;
using CurrencyWatchlist.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyWatchlist.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") ?? "Data Source=currencywatchlist.db";
        services.AddDbContext<CurrencyWatchlistDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CurrencyWatchlistDbContext>());

        services.AddScoped<IWatchlistRepository, WatchlistRepository>();
        services.AddScoped<IWatchlistItemRepository, WatchlistItemRepository>();
        services.AddScoped<IRateSnapshotRepository, RateSnapshotRepository>();
        services.AddScoped<IAlertRuleRepository, AlertRuleRepository>();
        services.AddScoped<IAlertEventRepository, AlertEventRepository>();

        services.AddScoped<IEventPublisher, InMemoryEventPublisher>();

        var frankfurterBaseUrl = configuration["RateProvider:BaseUrl"] ?? "https://api.frankfurter.app/";
        services.AddHttpClient<IRateProvider, FrankfurterRateProvider>(client =>
        {
            client.BaseAddress = new Uri(frankfurterBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddFrankfurterResilience();

        services.AddMemoryCache();
        services.AddHttpClient<ICurrencyCatalog, FrankfurterCurrencyCatalog>(client =>
        {
            client.BaseAddress = new Uri(frankfurterBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddFrankfurterResilience();

        return services;
    }
}
