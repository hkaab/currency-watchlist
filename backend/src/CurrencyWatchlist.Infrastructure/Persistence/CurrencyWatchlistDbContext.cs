using CurrencyWatchlist.Application.Interfaces;
using CurrencyWatchlist.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CurrencyWatchlist.Infrastructure.Persistence;

public class CurrencyWatchlistDbContext : DbContext, IUnitOfWork
{
    public CurrencyWatchlistDbContext(DbContextOptions<CurrencyWatchlistDbContext> options) : base(options)
    {
    }

    public DbSet<Watchlist> Watchlists => Set<Watchlist>();
    public DbSet<WatchlistItem> WatchlistItems => Set<WatchlistItem>();
    public DbSet<RateSnapshot> RateSnapshots => Set<RateSnapshot>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<AlertEvent> AlertEvents => Set<AlertEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CurrencyWatchlistDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
