import { beforeEach, describe, expect, it, vi } from "vitest";

const startMock = vi.fn();
const buildMock = vi.fn();

/** Minimal fake HubConnection: records the onreconnected/onclose handlers so tests can fire them. */
function createFakeConnection(state: string) {
  return {
    state,
    start: startMock,
    invoke: vi.fn().mockResolvedValue(undefined),
    onreconnected: vi.fn(),
    onclose: vi.fn(),
  };
}

vi.mock("@microsoft/signalr", () => {
  class FakeHubConnectionBuilder {
    withUrl() {
      return this;
    }
    withAutomaticReconnect() {
      return this;
    }
    build() {
      return buildMock();
    }
  }
  return {
    HubConnectionBuilder: FakeHubConnectionBuilder,
    HubConnectionState: { Connected: "Connected", Disconnected: "Disconnected" },
  };
});

beforeEach(() => {
  vi.resetModules();
  startMock.mockReset().mockResolvedValue(undefined);
  buildMock.mockReset().mockReturnValue(createFakeConnection("Disconnected"));
});

describe("signalr connection", () => {
  it("builds the connection once and reuses it across calls", async () => {
    const { getNotificationsConnection } = await import("./connection");

    const first = getNotificationsConnection();
    const second = getNotificationsConnection();

    expect(first).toBe(second);
    expect(buildMock).toHaveBeenCalledTimes(1);
  });

  it("starts the connection only once even when called concurrently", async () => {
    const { ensureConnectionStarted } = await import("./connection");

    await Promise.all([ensureConnectionStarted(), ensureConnectionStarted()]);

    expect(startMock).toHaveBeenCalledTimes(1);
  });

  it("skips starting when the connection is already connected", async () => {
    buildMock.mockReturnValue(createFakeConnection("Connected"));
    const { ensureConnectionStarted } = await import("./connection");

    await ensureConnectionStarted();

    expect(startMock).not.toHaveBeenCalled();
  });

  it("restarts after the connection closes for good (stale startPromise would otherwise block recovery)", async () => {
    const connection = createFakeConnection("Disconnected");
    buildMock.mockReturnValue(connection);
    const { ensureConnectionStarted } = await import("./connection");

    await ensureConnectionStarted();
    expect(startMock).toHaveBeenCalledTimes(1);

    // Automatic reconnect gave up; the connection is closed for good.
    const onCloseHandler = connection.onclose.mock.calls[0][0];
    onCloseHandler();

    await ensureConnectionStarted();
    expect(startMock).toHaveBeenCalledTimes(2);
  });

  it("rejoins tracked watchlist groups after a reconnect", async () => {
    const connection = createFakeConnection("Connected");
    buildMock.mockReturnValue(connection);
    const { joinWatchlistGroup } = await import("./connection");

    await joinWatchlistGroup(1);
    await joinWatchlistGroup(2);
    connection.invoke.mockClear();

    const onReconnectedHandler = connection.onreconnected.mock.calls[0][0];
    onReconnectedHandler();
    await Promise.resolve();

    expect(connection.invoke).toHaveBeenCalledWith("JoinWatchlist", 1);
    expect(connection.invoke).toHaveBeenCalledWith("JoinWatchlist", 2);
  });

  it("leaveWatchlistGroup stops it from being rejoined on the next reconnect", async () => {
    const connection = createFakeConnection("Connected");
    buildMock.mockReturnValue(connection);
    const { joinWatchlistGroup, leaveWatchlistGroup } = await import("./connection");

    await joinWatchlistGroup(1);
    await leaveWatchlistGroup(1);
    connection.invoke.mockClear();

    const onReconnectedHandler = connection.onreconnected.mock.calls[0][0];
    onReconnectedHandler();
    await Promise.resolve();

    expect(connection.invoke).not.toHaveBeenCalledWith("JoinWatchlist", 1);
  });

  it("keeps a watchlist group joined while a second subscriber is still using it", async () => {
    const connection = createFakeConnection("Connected");
    buildMock.mockReturnValue(connection);
    const { joinWatchlistGroup, leaveWatchlistGroup } = await import("./connection");

    // Two independent consumers (e.g. useWatchlistDetail and useAlerts) subscribe to the same id.
    await joinWatchlistGroup(1);
    await joinWatchlistGroup(1);

    // The first consumer unmounts; the second is still using the group.
    await leaveWatchlistGroup(1);
    connection.invoke.mockClear();

    const onReconnectedHandler = connection.onreconnected.mock.calls[0][0];
    onReconnectedHandler();
    await Promise.resolve();

    expect(connection.invoke).toHaveBeenCalledWith("JoinWatchlist", 1);
  });

  it("leaves the group once the last subscriber for that id unsubscribes", async () => {
    const connection = createFakeConnection("Connected");
    buildMock.mockReturnValue(connection);
    const { joinWatchlistGroup, leaveWatchlistGroup } = await import("./connection");

    await joinWatchlistGroup(1);
    await joinWatchlistGroup(1);

    await leaveWatchlistGroup(1);
    expect(connection.invoke).not.toHaveBeenCalledWith("LeaveWatchlist", 1);

    await leaveWatchlistGroup(1);
    expect(connection.invoke).toHaveBeenCalledWith("LeaveWatchlist", 1);
  });

  it("leaveWatchlistGroup is a no-op when the id was never joined", async () => {
    const connection = createFakeConnection("Connected");
    buildMock.mockReturnValue(connection);
    const { leaveWatchlistGroup } = await import("./connection");

    await leaveWatchlistGroup(99);

    expect(connection.invoke).not.toHaveBeenCalled();
  });
});
