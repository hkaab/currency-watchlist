import { act, renderHook, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { useAlerts } from "./useAlerts";
import { alertsApi } from "@/lib/api/alerts";
import { useLiveUpdates } from "@/hooks/useLiveUpdates";

vi.mock("@/lib/api/alerts", () => ({
  alertsApi: { listByWatchlist: vi.fn(), create: vi.fn(), evaluate: vi.fn() },
}));
vi.mock("@/hooks/useLiveUpdates", () => ({ useLiveUpdates: vi.fn() }));

const alertRule = {
  id: 1,
  watchlistItemId: 10,
  baseCurrency: "USD",
  quoteCurrency: "AUD",
  condition: "Above" as const,
  threshold: 1.6,
  isActive: true,
  createdAt: "2026-01-01T00:00:00Z",
};

describe("useAlerts", () => {
  it("loads alerts for the watchlist", async () => {
    vi.mocked(alertsApi.listByWatchlist).mockResolvedValue([alertRule]);

    const { result } = renderHook(() => useAlerts(1));

    await waitFor(() => expect(result.current.isLoading).toBe(false));
    expect(result.current.alerts).toEqual([alertRule]);
  });

  it("create prepends the new alert", async () => {
    vi.mocked(alertsApi.listByWatchlist).mockResolvedValue([]);
    vi.mocked(alertsApi.create).mockResolvedValue(alertRule);

    const { result } = renderHook(() => useAlerts(1));
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    await act(async () => {
      await result.current.create(10, "Above", 1.6);
    });

    expect(result.current.alerts).toEqual([alertRule]);
  });

  it("registers an onAlertTriggered handler that accumulates recent events", async () => {
    vi.mocked(alertsApi.listByWatchlist).mockResolvedValue([]);
    const { result } = renderHook(() => useAlerts(1));
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    const { onAlertTriggered } = vi.mocked(useLiveUpdates).mock.calls.at(-1)![1];
    const event = { id: 1, alertRuleId: 1, triggeredAt: "2026-01-01T00:00:00Z", rate: 1.65, message: "triggered" };

    act(() => onAlertTriggered?.(event));

    expect(result.current.recentEvents).toEqual([event]);
  });
});
