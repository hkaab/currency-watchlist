import * as signalR from "@microsoft/signalr";
import { API_BASE_URL } from "@/lib/api/client";

let connection: signalR.HubConnection | null = null;
let startPromise: Promise<void> | null = null;

export function getNotificationsConnection(): signalR.HubConnection {
  if (!connection) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/notifications`)
      .withAutomaticReconnect()
      .build();
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
