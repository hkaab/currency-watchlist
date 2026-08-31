"use client";

import { useCallback, useEffect, useState } from "react";
import { ApiError } from "@/lib/api/client";
import { watchlistsApi } from "@/lib/api/watchlists";
import type { WatchlistResponse } from "@/lib/types";

export function useWatchlists() {
  const [watchlists, setWatchlists] = useState<WatchlistResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    (async () => {
      try {
        const data = await watchlistsApi.list();
        if (!cancelled) {
          setWatchlists(data);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof ApiError ? err.message : "Failed to load watchlists.");
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  const create = useCallback(async (name: string) => {
    const created = await watchlistsApi.create(name);
    setWatchlists((prev) => [created, ...prev]);
    return created;
  }, []);

  const remove = useCallback(async (id: number) => {
    await watchlistsApi.remove(id);
    setWatchlists((prev) => prev.filter((w) => w.id !== id));
  }, []);

  return { watchlists, isLoading, error, create, remove };
}
