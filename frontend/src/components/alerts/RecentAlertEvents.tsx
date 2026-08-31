"use client";

import type { AlertTriggeredMessage } from "@/lib/types";

export function RecentAlertEvents({ events }: { events: AlertTriggeredMessage[] }) {
  if (events.length === 0) {
    return <p className="empty-state">No alerts have triggered yet.</p>;
  }

  return (
    <ul className="list">
      {events.map((event) => (
        <li key={`${event.id}-${event.triggeredAt}`} className="list-row">
          <div className="list-row-main">
            <span>{event.message}</span>
            <span className="muted">{new Date(event.triggeredAt).toLocaleString()}</span>
          </div>
        </li>
      ))}
    </ul>
  );
}
