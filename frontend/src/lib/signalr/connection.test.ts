import { beforeEach, describe, expect, it, vi } from "vitest";

const startMock = vi.fn();
const buildMock = vi.fn();

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
  buildMock.mockReset().mockReturnValue({ state: "Disconnected", start: startMock });
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
    buildMock.mockReturnValue({ state: "Connected", start: startMock });
    const { ensureConnectionStarted } = await import("./connection");

    await ensureConnectionStarted();

    expect(startMock).not.toHaveBeenCalled();
  });
});
