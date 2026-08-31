"use client";

import Link from "next/link";
import { useState } from "react";
import type { WatchlistResponse } from "@/lib/types";

interface WatchlistListProps {
  watchlists: WatchlistResponse[];
  onDelete: (id: number) => Promise<unknown>;
}

export function WatchlistList({ watchlists, onDelete }: WatchlistListProps) {
  const [deletingId, setDeletingId] = useState<number | null>(null);

  if (watchlists.length === 0) {
    return <p className="empty-state">No watchlists yet. Create one above to get started.</p>;
  }

  async function handleDelete(id: number) {
    setDeletingId(id);
    try {
      await onDelete(id);
    } finally {
      setDeletingId(null);
    }
  }

  return (
    <ul className="list">
      {watchlists.map((watchlist) => (
        <li key={watchlist.id} className="list-row">
          <div className="list-row-main">
            <Link href={`/watchlists/${watchlist.id}`}>{watchlist.name}</Link>
            <span className="muted">
              {watchlist.itemCount} pair{watchlist.itemCount === 1 ? "" : "s"} · created{" "}
              {new Date(watchlist.createdAt).toLocaleDateString()}
            </span>
          </div>
          <button
            type="button"
            className="danger"
            onClick={() => handleDelete(watchlist.id)}
            disabled={deletingId === watchlist.id}
          >
            {deletingId === watchlist.id ? "Deleting..." : "Delete"}
          </button>
        </li>
      ))}
    </ul>
  );
}
