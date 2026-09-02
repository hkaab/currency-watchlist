using CurrencyWatchlist.Application.Interfaces;
using CurrencyWatchlist.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace CurrencyWatchlist.Api.Tests;

/// <summary>
/// Boots the real Api pipeline against an isolated in-memory SQLite database and mocked
/// <see cref="IRateProvider"/>/<see cref="ICurrencyCatalog"/>, so tests never hit the real
/// Frankfurter API.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public IRateProvider RateProviderFake { get; } = Substitute.For<IRateProvider>();

    /// <summary>Permissive by default (every code is "supported") so existing tests don't need to configure it explicitly.</summary>
    public ICurrencyCatalog CurrencyCatalogFake { get; } = Substitute.For<ICurrencyCatalog>();

    public CustomWebApplicationFactory()
    {
        _connection.Open();
        CurrencyCatalogFake.IsSupportedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<CurrencyWatchlistDbContext>>();
            services.AddDbContext<CurrencyWatchlistDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<IRateProvider>();
            services.AddSingleton(RateProviderFake);

            services.RemoveAll<ICurrencyCatalog>();
            services.AddSingleton(CurrencyCatalogFake);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
