"use client";

import { useCallback, useEffect, useState } from "react";
import { alertsApi } from "@/lib/api/alerts";
import { ApiError } from "@/lib/api/client";
import { useLiveUpdates } from "@/hooks/useLiveUpdates";
import type {
  AlertCondition,
  AlertEvaluationResult,
  AlertRuleResponse,
  AlertTriggeredMessage,
} from "@/lib/types";

export function useAlerts(watchlistId: number) {
  const [alerts, setAlerts] = useState<AlertRuleResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [recentEvents, setRecentEvents] = useState<AlertTriggeredMessage[]>([]);

  useEffect(() => {
    let cancelled = false;

    (async () => {
      try {
        const data = await alertsApi.listByWatchlist(watchlistId);
        if (!cancelled) {
          setAlerts(data);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof ApiError ? err.message : "Failed to load alerts.");
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

  useLiveUpdates(watchlistId, {
    onAlertTriggered: (event) => setRecentEvents((prev) => [event, ...prev].slice(0, 10)),
  });

  const create = useCallback(
    async (watchlistItemId: number, condition: AlertCondition, threshold: number) => {
      const created = await alertsApi.create(watchlistItemId, condition, threshold);
      setAlerts((prev) => [created, ...prev]);
      return created;
    },
    [],
  );

  const evaluate = useCallback(
    (alertId: number): Promise<AlertEvaluationResult> => alertsApi.evaluate(alertId),
    [],
  );

  return { alerts, isLoading, error, recentEvents, create, evaluate };
}
