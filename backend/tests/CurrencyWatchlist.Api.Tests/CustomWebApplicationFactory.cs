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
/// Boots the real Api pipeline against an isolated in-memory SQLite database and a mocked
/// <see cref="IRateProvider"/>, so tests never hit the real Frankfurter API.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public IRateProvider RateProviderFake { get; } = Substitute.For<IRateProvider>();

    public CustomWebApplicationFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<CurrencyWatchlistDbContext>>();
            services.AddDbContext<CurrencyWatchlistDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<IRateProvider>();
            services.AddSingleton(RateProviderFake);
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
