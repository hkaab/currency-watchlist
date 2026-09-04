import { renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useLiveUpdates } from "./useLiveUpdates";
import {
  ensureConnectionStarted,
  getNotificationsConnection,
  joinWatchlistGroup,
  leaveWatchlistGroup,
} from "@/lib/signalr/connection";

vi.mock("@/lib/signalr/connection", () => ({
  getNotificationsConnection: vi.fn(),
  ensureConnectionStarted: vi.fn(),
  joinWatchlistGroup: vi.fn(),
  leaveWatchlistGroup: vi.fn(),
}));

beforeEach(() => {
  vi.clearAllMocks();
  // Every effect cleanup calls this - default it so tests that don't care about leaving
  // don't have to configure it, and so call counts start clean per test.
  vi.mocked(leaveWatchlistGroup).mockResolvedValue(undefined);
});

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
    vi.mocked(joinWatchlistGroup).mockResolvedValue(undefined);

    const onRatesUpdated = vi.fn();
    renderHook(() => useLiveUpdates(1, { onRatesUpdated }));

    await waitFor(() => expect(joinWatchlistGroup).toHaveBeenCalledWith(1));

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
    vi.mocked(joinWatchlistGroup).mockResolvedValue(undefined);
    vi.mocked(leaveWatchlistGroup).mockResolvedValue(undefined);

    const { unmount } = renderHook(() => useLiveUpdates(1, {}));
    await waitFor(() => expect(joinWatchlistGroup).toHaveBeenCalledWith(1));

    unmount();

    expect(connection.off).toHaveBeenCalledWith("RatesUpdated", expect.any(Function));
    expect(leaveWatchlistGroup).toHaveBeenCalledWith(1);
  });

  it("does not join after unmount when the connection resolves after cleanup", async () => {
    const connection = createFakeConnection();
    vi.mocked(getNotificationsConnection).mockReturnValue(connection as never);
    let resolveStart: () => void = () => {};
    vi.mocked(ensureConnectionStarted).mockReturnValue(
      new Promise((resolve) => {
        resolveStart = resolve;
      }),
    );
    vi.mocked(joinWatchlistGroup).mockResolvedValue(undefined);
    vi.mocked(leaveWatchlistGroup).mockResolvedValue(undefined);

    const { unmount } = renderHook(() => useLiveUpdates(1, {}));
    unmount();
    resolveStart();
    await Promise.resolve();
    await Promise.resolve();

    expect(joinWatchlistGroup).not.toHaveBeenCalled();
  });
});
