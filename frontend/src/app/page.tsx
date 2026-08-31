"use client";

import { CreateWatchlistForm } from "@/components/watchlists/CreateWatchlistForm";
import { WatchlistList } from "@/components/watchlists/WatchlistList";
import { ErrorBanner } from "@/components/common/ErrorBanner";
import { LoadingSpinner } from "@/components/common/LoadingSpinner";
import { useWatchlists } from "@/hooks/useWatchlists";

export default function WatchlistsPage() {
  const { watchlists, isLoading, error, create, remove } = useWatchlists();

  return (
    <main className="page">
      <div className="page-header">
        <h1>Currency Watchlists</h1>
      </div>

      {error && <ErrorBanner message={error} />}

      <section className="card">
        <h2>New watchlist</h2>
        <CreateWatchlistForm onCreate={create} />
      </section>

      <section className="card">
        <h2>Your watchlists</h2>
        {isLoading ? <LoadingSpinner label="Loading watchlists..." /> : (
          <WatchlistList watchlists={watchlists} onDelete={remove} />
        )}
      </section>
    </main>
  );
}
