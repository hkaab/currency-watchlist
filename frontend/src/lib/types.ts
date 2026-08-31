export type AlertCondition = "Above" | "Below";

export interface WatchlistResponse {
  id: number;
  name: string;
  createdAt: string;
  itemCount: number;
}

export interface RateSnapshotResponse {
  id: number;
  baseCurrency: string;
  quoteCurrency: string;
  rate: number;
  sourceTimestamp: string;
  fetchedAt: string;
}

export interface WatchlistItemResponse {
  id: number;
  watchlistId: number;
  baseCurrency: string;
  quoteCurrency: string;
  createdAt: string;
  latestRate: RateSnapshotResponse | null;
}

export interface WatchlistDetailResponse {
  id: number;
  name: string;
  createdAt: string;
  items: WatchlistItemResponse[];
}

export interface RefreshRatesResponse {
  refreshedPairCount: number;
  snapshots: RateSnapshotResponse[];
}

export interface AlertRuleResponse {
  id: number;
  watchlistItemId: number;
  baseCurrency: string;
  quoteCurrency: string;
  condition: AlertCondition;
  threshold: number;
  isActive: boolean;
  createdAt: string;
}

export interface AlertEvaluationResult {
  alertRuleId: number;
  isTriggered: boolean;
  rate: number;
  threshold: number;
  condition: AlertCondition;
  evaluatedAt: string;
  alertEventId: number | null;
}

export interface AlertTriggeredMessage {
  id: number;
  alertRuleId: number;
  triggeredAt: string;
  rate: number;
  message: string;
}

export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
}
