using CurrencyWatchlist.Application.Interfaces;
using CurrencyWatchlist.Domain.Entities;
using CurrencyWatchlist.Infrastructure.Persistence;

namespace CurrencyWatchlist.Infrastructure.Repositories;

public class AlertEventRepository : IAlertEventRepository
{
    private readonly CurrencyWatchlistDbContext _context;

    public AlertEventRepository(CurrencyWatchlistDbContext context) => _context = context;

    public void Add(AlertEvent alertEvent) => _context.AlertEvents.Add(alertEvent);
}
