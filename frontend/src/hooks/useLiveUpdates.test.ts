import { renderHook, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { useLiveUpdates } from "./useLiveUpdates";
import { ensureConnectionStarted, getNotificationsConnection } from "@/lib/signalr/connection";

vi.mock("@/lib/signalr/connection", () => ({
  getNotificationsConnection: vi.fn(),
  ensureConnectionStarted: vi.fn(),
}));

function createFakeConnection() {
  const handlers: Record<string, (...args: unknown[]) => void> = {};
  return {
    on: vi.fn((event: string, handler: (...args: unknown[]) => void) => {
      handlers[event] = handler;
    }),
    off: vi.fn(),
    invoke: vi.fn().mockResolvedValue(undefined),
    trigger: (event: string, payload: unknown) => handlers[event]?.(payload),
  };
}

describe("useLiveUpdates", () => {
  it("joins the watchlist group and forwards RatesUpdated events", async () => {
    const connection = createFakeConnection();
    vi.mocked(getNotificationsConnection).mockReturnValue(connection as never);
    vi.mocked(ensureConnectionStarted).mockResolvedValue(undefined);

    const onRatesUpdated = vi.fn();
    renderHook(() => useLiveUpdates(1, { onRatesUpdated }));

    await waitFor(() => expect(connection.invoke).toHaveBeenCalledWith("JoinWatchlist", 1));

    connection.trigger("RatesUpdated", [{ rate: 1.5 }]);
    expect(onRatesUpdated).toHaveBeenCalledWith([{ rate: 1.5 }]);
  });

  it("does nothing when watchlistId is null", () => {
    const connection = createFakeConnection();
    vi.mocked(getNotificationsConnection).mockReturnValue(connection as never);

    renderHook(() => useLiveUpdates(null, {}));

    expect(connection.on).not.toHaveBeenCalled();
  });

  it("leaves the group and removes handlers on unmount", async () => {
    const connection = createFakeConnection();
    vi.mocked(getNotificationsConnection).mockReturnValue(connection as never);
    vi.mocked(ensureConnectionStarted).mockResolvedValue(undefined);

    const { unmount } = renderHook(() => useLiveUpdates(1, {}));
    await waitFor(() => expect(connection.invoke).toHaveBeenCalledWith("JoinWatchlist", 1));

    unmount();

    expect(connection.off).toHaveBeenCalledWith("RatesUpdated", expect.any(Function));
    expect(connection.invoke).toHaveBeenCalledWith("LeaveWatchlist", 1);
  });
});
