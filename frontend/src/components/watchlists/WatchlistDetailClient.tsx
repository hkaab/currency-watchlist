"use client";

import Link from "next/link";
import { useState } from "react";
import { ApiError } from "@/lib/api/client";
import { AddItemForm } from "@/components/items/AddItemForm";
import { ItemList } from "@/components/items/ItemList";
import { CreateAlertForm } from "@/components/alerts/CreateAlertForm";
import { AlertList } from "@/components/alerts/AlertList";
import { RecentAlertEvents } from "@/components/alerts/RecentAlertEvents";
import { RateHistoryChart } from "@/components/charts/RateHistoryChart";
import { ErrorBanner } from "@/components/common/ErrorBanner";
import { LoadingSpinner } from "@/components/common/LoadingSpinner";
import { useWatchlistDetail } from "@/hooks/useWatchlistDetail";
import { useAlerts } from "@/hooks/useAlerts";

export function WatchlistDetailClient({ watchlistId }: { watchlistId: number }) {
  const {
    watchlist,
    isLoading,
    isRefreshing,
    error: watchlistError,
    addItem,
    removeItem,
    refresh,
  } = useWatchlistDetail(watchlistId);
  const { alerts, error: alertsError, recentEvents, create: createAlert, evaluate } = useAlerts(watchlistId);

  const [selectedItemId, setSelectedItemId] = useState<number | null>(null);
  const [refreshMessage, setRefreshMessage] = useState<string | null>(null);
  const [chartRefreshKey, setChartRefreshKey] = useState(0);

  if (isLoading) {
    return (
      <main className="page">
        <LoadingSpinner label="Loading watchlist..." />
      </main>
    );
  }

  if (!watchlist) {
    return (
      <main className="page">
        {watchlistError && <ErrorBanner message={watchlistError} />}
      </main>
    );
  }

  const selectedItem = watchlist.items.find((i) => i.id === selectedItemId) ?? watchlist.items[0] ?? null;

  async function handleRefresh() {
    setRefreshMessage(null);
    try {
      const result = await refresh();
      setRefreshMessage(`Refreshed ${result.refreshedPairCount} pair${result.refreshedPairCount === 1 ? "" : "s"}.`);
      setChartRefreshKey((k) => k + 1);
    } catch (err) {
      setRefreshMessage(err instanceof ApiError ? err.message : "Refresh failed.");
    }
  }

  return (
    <main className="page">
      <div className="page-header">
        <div>
          <Link href="/" className="back-link">
            ← All watchlists
          </Link>
          <h1>{watchlist.name}</h1>
        </div>
        <button type="button" onClick={handleRefresh} disabled={isRefreshing}>
          {isRefreshing ? "Refreshing..." : "Refresh Rates"}
        </button>
      </div>

      {watchlistError && <ErrorBanner message={watchlistError} />}
      {refreshMessage && <p className="muted">{refreshMessage}</p>}

      <section className="card">
        <h2>Add currency pair</h2>
        <AddItemForm onAdd={addItem} />
      </section>

      <section className="card">
        <h2>Currency pairs</h2>
        <ItemList
          items={watchlist.items}
          selectedItemId={selectedItem?.id ?? null}
          onSelect={setSelectedItemId}
          onRemove={removeItem}
        />
      </section>

      {selectedItem && (
        <section className="card">
          <h2>
            Rate history: {selectedItem.baseCurrency} → {selectedItem.quoteCurrency}
          </h2>
          <RateHistoryChart
            baseCurrency={selectedItem.baseCurrency}
            quoteCurrency={selectedItem.quoteCurrency}
            refreshKey={chartRefreshKey}
          />
        </section>
      )}

      <section className="card">
        <h2>Create alert</h2>
        {alertsError && <ErrorBanner message={alertsError} />}
        <CreateAlertForm items={watchlist.items} onCreate={createAlert} />
      </section>

      <section className="card">
        <h2>Alert rules</h2>
        <AlertList alerts={alerts} onEvaluate={evaluate} />
      </section>

      <section className="card">
        <h2>Recent alert activity</h2>
        <RecentAlertEvents events={recentEvents} />
      </section>
    </main>
  );
}
