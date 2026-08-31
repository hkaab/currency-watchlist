import { apiClient } from "@/lib/api/client";
import type {
  AlertCondition,
  AlertEvaluationResult,
  AlertRuleResponse,
} from "@/lib/types";

export const alertsApi = {
  listByWatchlist: (watchlistId: number) =>
    apiClient.get<AlertRuleResponse[]>(
      `/api/alerts?watchlistId=${watchlistId}`,
    ),

  create: (watchlistItemId: number, condition: AlertCondition, threshold: number) =>
    apiClient.post<AlertRuleResponse>("/api/alerts", {
      watchlistItemId,
      condition,
      threshold,
    }),

  evaluate: (alertId: number) =>
    apiClient.post<AlertEvaluationResult>(`/api/alerts/${alertId}/evaluate`),
};
