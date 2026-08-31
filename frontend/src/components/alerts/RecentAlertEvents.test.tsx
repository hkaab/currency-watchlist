import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { RecentAlertEvents } from "./RecentAlertEvents";
import type { AlertTriggeredMessage } from "@/lib/types";

describe("RecentAlertEvents", () => {
  it("shows an empty state when there are no events", () => {
    render(<RecentAlertEvents events={[]} />);
    expect(screen.getByText(/No alerts have triggered yet/)).toBeInTheDocument();
  });

  it("renders each event's message", () => {
    const events: AlertTriggeredMessage[] = [
      { id: 1, alertRuleId: 1, triggeredAt: "2026-01-01T00:00:00Z", rate: 1.65, message: "USD->AUD is Above threshold 1.6" },
    ];
    render(<RecentAlertEvents events={events} />);

    expect(screen.getByText("USD->AUD is Above threshold 1.6")).toBeInTheDocument();
  });
});
