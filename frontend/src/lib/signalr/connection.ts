import * as signalR from "@microsoft/signalr";
import { API_BASE_URL } from "@/lib/api/client";

let connection: signalR.HubConnection | null = null;
let startPromise: Promise<void> | null = null;
// Groups a caller has asked to join, kept centrally (rather than per-hook) so a single
// onreconnected handler on the shared connection can rejoin all of them after a drop -
// automatic reconnect gives you a live connection again but not your prior group memberships.
// Reference-counted because more than one hook (e.g. useWatchlistDetail and useAlerts) can
// subscribe to the same watchlistId at once - leaving must only drop the group once the
// last subscriber for that id is gone, not on the first unmount.
const joinedWatchlistIds = new Map<number, number>();

export function getNotificationsConnection(): signalR.HubConnection {
  if (!connection) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/notifications`)
      .withAutomaticReconnect()
      .build();

    connection.onreconnected(() => {
      for (const watchlistId of joinedWatchlistIds.keys()) {
        connection?.invoke("JoinWatchlist", watchlistId).catch((error) => {
          console.error("Failed to rejoin watchlist group after reconnect:", error);
        });
      }
    });

    // Automatic reconnect gives up and closes for good after exhausting its retry attempts -
    // without this, a stale resolved startPromise would make ensureConnectionStarted() think
    // the (now dead) connection is already started and never call start() again.
    connection.onclose(() => {
      startPromise = null;
    });
  }
  return connection;
}

export function ensureConnectionStarted(): Promise<void> {
  const conn = getNotificationsConnection();

  if (conn.state === signalR.HubConnectionState.Connected) {
    return Promise.resolve();
  }

  if (!startPromise) {
    startPromise = conn.start().catch((error) => {
      startPromise = null;
      throw error;
    });
  }

  return startPromise;
}

export async function joinWatchlistGroup(watchlistId: number): Promise<void> {
  await getNotificationsConnection().invoke("JoinWatchlist", watchlistId);
  joinedWatchlistIds.set(watchlistId, (joinedWatchlistIds.get(watchlistId) ?? 0) + 1);
}

export async function leaveWatchlistGroup(watchlistId: number): Promise<void> {
  const subscriberCount = joinedWatchlistIds.get(watchlistId);
  if (subscriberCount === undefined) {
    return;
  }

  if (subscriberCount > 1) {
    joinedWatchlistIds.set(watchlistId, subscriberCount - 1);
    return;
  }

  joinedWatchlistIds.delete(watchlistId);
  const conn = getNotificationsConnection();
  if (conn.state === signalR.HubConnectionState.Connected) {
    await conn.invoke("LeaveWatchlist", watchlistId);
  }
}
