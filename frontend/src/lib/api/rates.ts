import { apiClient } from "@/lib/api/client";
import type { RateSnapshotResponse, RefreshRatesResponse } from "@/lib/types";

export const ratesApi = {
  refresh: (watchlistId: number) =>
    apiClient.post<RefreshRatesResponse>(
      `/api/rates/refresh?watchlistId=${watchlistId}`,
    ),

  history: (base: string, quote: string, from: Date, to: Date) => {
    const params = new URLSearchParams({
      base,
      quote,
      from: from.toISOString(),
      to: to.toISOString(),
    });
    return apiClient.get<RateSnapshotResponse[]>(
      `/api/rates/history?${params.toString()}`,
    );
  },
};
