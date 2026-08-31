import { apiClient } from "@/lib/api/client";
import type { WatchlistItemResponse } from "@/lib/types";

export const itemsApi = {
  add: (watchlistId: number, baseCurrency: string, quoteCurrency: string) =>
    apiClient.post<WatchlistItemResponse>(
      `/api/watchlists/${watchlistId}/items`,
      { baseCurrency, quoteCurrency },
    ),

  remove: (watchlistId: number, itemId: number) =>
    apiClient.delete(`/api/watchlists/${watchlistId}/items/${itemId}`),
};
