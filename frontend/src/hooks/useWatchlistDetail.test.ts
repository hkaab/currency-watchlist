import { act, renderHook, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { useWatchlistDetail } from "./useWatchlistDetail";
import { watchlistsApi } from "@/lib/api/watchlists";
import { itemsApi } from "@/lib/api/items";
import { ratesApi } from "@/lib/api/rates";

vi.mock("@/lib/api/watchlists", () => ({ watchlistsApi: { getById: vi.fn() } }));
vi.mock("@/lib/api/items", () => ({ itemsApi: { add: vi.fn(), remove: vi.fn() } }));
vi.mock("@/lib/api/rates", () => ({ ratesApi: { refresh: vi.fn() } }));
vi.mock("@/hooks/useLiveUpdates", () => ({ useLiveUpdates: vi.fn() }));

const baseWatchlist = {
  id: 1,
  name: "Travel",
  createdAt: "2026-01-01T00:00:00Z",
  items: [
    {
      id: 10,
      watchlistId: 1,
      baseCurrency: "USD",
      quoteCurrency: "AUD",
      createdAt: "2026-01-01T00:00:00Z",
      latestRate: null,
    },
  ],
};

describe("useWatchlistDetail", () => {
  it("loads the watchlist detail on mount", async () => {
    vi.mocked(watchlistsApi.getById).mockResolvedValue(baseWatchlist);

    const { result } = renderHook(() => useWatchlistDetail(1));

    await waitFor(() => expect(result.current.isLoading).toBe(false));
    expect(result.current.watchlist?.items).toHaveLength(1);
  });

  it("addItem appends the new item to local state", async () => {
    vi.mocked(watchlistsApi.getById).mockResolvedValue({ ...baseWatchlist, items: [] });
    vi.mocked(itemsApi.add).mockResolvedValue(baseWatchlist.items[0]);

    const { result } = renderHook(() => useWatchlistDetail(1));
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    await act(async () => {
      await result.current.addItem("USD", "AUD");
    });

    expect(result.current.watchlist?.items).toHaveLength(1);
  });

  it("removeItem drops the item from local state", async () => {
    vi.mocked(watchlistsApi.getById).mockResolvedValue(baseWatchlist);
    vi.mocked(itemsApi.remove).mockResolvedValue(undefined);

    const { result } = renderHook(() => useWatchlistDetail(1));
    await waitFor(() => expect(result.current.watchlist?.items).toHaveLength(1));

    await act(async () => {
      await result.current.removeItem(10);
    });

    expect(result.current.watchlist?.items).toHaveLength(0);
  });

  it("refresh merges returned snapshots into matching items", async () => {
    vi.mocked(watchlistsApi.getById).mockResolvedValue(baseWatchlist);
    const snapshot = {
      id: 99,
      baseCurrency: "USD",
      quoteCurrency: "AUD",
      rate: 1.55,
      sourceTimestamp: "2026-01-02T00:00:00Z",
      fetchedAt: "2026-01-02T00:00:00Z",
    };
    vi.mocked(ratesApi.refresh).mockResolvedValue({ refreshedPairCount: 1, snapshots: [snapshot] });

    const { result } = renderHook(() => useWatchlistDetail(1));
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    await act(async () => {
      await result.current.refresh();
    });

    expect(result.current.watchlist?.items[0].latestRate).toEqual(snapshot);
  });
});
