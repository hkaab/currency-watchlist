using CurrencyWatchlist.Domain.Entities;

namespace CurrencyWatchlist.Application.Interfaces;

public interface IAlertEventRepository
{
    void Add(AlertEvent alertEvent);
}
