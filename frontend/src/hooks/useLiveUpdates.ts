"use client";

import { useEffect, useRef } from "react";
import {
  ensureConnectionStarted,
  getNotificationsConnection,
  joinWatchlistGroup,
  leaveWatchlistGroup,
} from "@/lib/signalr/connection";
import type { AlertTriggeredMessage, RateSnapshotResponse } from "@/lib/types";

interface UseLiveUpdatesOptions {
  onRatesUpdated?: (snapshots: RateSnapshotResponse[]) => void;
  onAlertTriggered?: (alertEvent: AlertTriggeredMessage) => void;
}

/**
 * Joins the SignalR group for a watchlist and forwards "RatesUpdated" / "AlertTriggered"
 * pushes from the backend, so the UI reacts live instead of relying on polling.
 */
export function useLiveUpdates(
  watchlistId: number | null,
  options: UseLiveUpdatesOptions,
) {
  const optionsRef = useRef(options);

  useEffect(() => {
    optionsRef.current = options;
  }, [options]);

  useEffect(() => {
    if (watchlistId === null) {
      return;
    }

    const connection = getNotificationsConnection();
    let cancelled = false;

    const handleRatesUpdated = (snapshots: RateSnapshotResponse[]) => {
      optionsRef.current.onRatesUpdated?.(snapshots);
    };
    const handleAlertTriggered = (alertEvent: AlertTriggeredMessage) => {
      optionsRef.current.onAlertTriggered?.(alertEvent);
    };

    connection.on("RatesUpdated", handleRatesUpdated);
    connection.on("AlertTriggered", handleAlertTriggered);

    ensureConnectionStarted()
      .then(() => {
        if (cancelled) {
          return;
        }
        return joinWatchlistGroup(watchlistId);
      })
      .catch((error) => {
        console.error("Live updates unavailable:", error);
      });

    return () => {
      cancelled = true;
      connection.off("RatesUpdated", handleRatesUpdated);
      connection.off("AlertTriggered", handleAlertTriggered);
      // Safe to call even if the join above never resolved - leaving a group you were never
      // added to (or a still-pending join that lands after this) is a harmless no-op server-side.
      leaveWatchlistGroup(watchlistId).catch(() => {});
    };
  }, [watchlistId]);
}
