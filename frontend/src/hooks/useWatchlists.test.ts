import { act, renderHook, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { useWatchlists } from "./useWatchlists";
import { watchlistsApi } from "@/lib/api/watchlists";
import { ApiError } from "@/lib/api/client";

vi.mock("@/lib/api/watchlists", () => ({
  watchlistsApi: { list: vi.fn(), create: vi.fn(), remove: vi.fn() },
}));

describe("useWatchlists", () => {
  it("loads watchlists on mount", async () => {
    vi.mocked(watchlistsApi.list).mockResolvedValue([
      { id: 1, name: "Travel", createdAt: "2026-01-01T00:00:00Z", itemCount: 0 },
    ]);

    const { result } = renderHook(() => useWatchlists());

    expect(result.current.isLoading).toBe(true);

    await waitFor(() => expect(result.current.isLoading).toBe(false));
    expect(result.current.watchlists).toHaveLength(1);
    expect(result.current.error).toBeNull();
  });

  it("sets an error message when loading fails", async () => {
    vi.mocked(watchlistsApi.list).mockRejectedValue(new ApiError("Server error", 500));

    const { result } = renderHook(() => useWatchlists());

    await waitFor(() => expect(result.current.error).toBe("Server error"));
  });

  it("create prepends the new watchlist to local state", async () => {
    vi.mocked(watchlistsApi.list).mockResolvedValue([]);
    vi.mocked(watchlistsApi.create).mockResolvedValue({
      id: 2,
      name: "New",
      createdAt: "2026-01-01T00:00:00Z",
      itemCount: 0,
    });

    const { result } = renderHook(() => useWatchlists());
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    await act(async () => {
      await result.current.create("New");
    });

    expect(result.current.watchlists.map((w) => w.id)).toEqual([2]);
  });

  it("remove filters the watchlist out of local state", async () => {
    vi.mocked(watchlistsApi.list).mockResolvedValue([
      { id: 1, name: "Travel", createdAt: "2026-01-01T00:00:00Z", itemCount: 0 },
    ]);
    vi.mocked(watchlistsApi.remove).mockResolvedValue(undefined);

    const { result } = renderHook(() => useWatchlists());
    await waitFor(() => expect(result.current.watchlists).toHaveLength(1));

    await act(async () => {
      await result.current.remove(1);
    });

    expect(result.current.watchlists).toHaveLength(0);
  });
});
