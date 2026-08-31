"use client";

import { useCallback, useEffect, useState } from "react";
import { ApiError } from "@/lib/api/client";
import { itemsApi } from "@/lib/api/items";
import { ratesApi } from "@/lib/api/rates";
import { watchlistsApi } from "@/lib/api/watchlists";
import { useLiveUpdates } from "@/hooks/useLiveUpdates";
import type { RateSnapshotResponse, WatchlistDetailResponse } from "@/lib/types";

export function useWatchlistDetail(watchlistId: number) {
  const [watchlist, setWatchlist] = useState<WatchlistDetailResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    (async () => {
      try {
        const data = await watchlistsApi.getById(watchlistId);
        if (!cancelled) {
          setWatchlist(data);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof ApiError ? err.message : "Failed to load watchlist.");
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
  }, [watchlistId]);

  const applyRateSnapshots = useCallback((snapshots: RateSnapshotResponse[]) => {
    setWatchlist((prev) => {
      if (!prev) {
        return prev;
      }
      return {
        ...prev,
        items: prev.items.map((item) => {
          const match = snapshots.find(
            (s) => s.baseCurrency === item.baseCurrency && s.quoteCurrency === item.quoteCurrency,
          );
          return match ? { ...item, latestRate: match } : item;
        }),
      };
    });
  }, []);

  useLiveUpdates(watchlistId, { onRatesUpdated: applyRateSnapshots });

  const addItem = useCallback(
    async (baseCurrency: string, quoteCurrency: string) => {
      const item = await itemsApi.add(watchlistId, baseCurrency, quoteCurrency);
      setWatchlist((prev) => (prev ? { ...prev, items: [...prev.items, item] } : prev));
      return item;
    },
    [watchlistId],
  );

  const removeItem = useCallback(
    async (itemId: number) => {
      await itemsApi.remove(watchlistId, itemId);
      setWatchlist((prev) =>
        prev ? { ...prev, items: prev.items.filter((i) => i.id !== itemId) } : prev,
      );
    },
    [watchlistId],
  );

  const refresh = useCallback(async () => {
    setIsRefreshing(true);
    setError(null);
    try {
      const result = await ratesApi.refresh(watchlistId);
      applyRateSnapshots(result.snapshots);
      return result;
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to refresh rates.");
      throw err;
    } finally {
      setIsRefreshing(false);
    }
  }, [watchlistId, applyRateSnapshots]);

  return { watchlist, isLoading, isRefreshing, error, addItem, removeItem, refresh };
}
