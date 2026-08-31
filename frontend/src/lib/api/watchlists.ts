import { apiClient } from "@/lib/api/client";
import type { WatchlistDetailResponse, WatchlistResponse } from "@/lib/types";

export const watchlistsApi = {
  list: () => apiClient.get<WatchlistResponse[]>("/api/watchlists"),

  getById: (id: number) =>
    apiClient.get<WatchlistDetailResponse>(`/api/watchlists/${id}`),

  create: (name: string) =>
    apiClient.post<WatchlistResponse>("/api/watchlists", { name }),

  remove: (id: number) => apiClient.delete(`/api/watchlists/${id}`),
};
