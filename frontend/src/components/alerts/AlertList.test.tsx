import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { AlertList } from "./AlertList";
import type { AlertRuleResponse } from "@/lib/types";

const alerts: AlertRuleResponse[] = [
  {
    id: 1,
    watchlistItemId: 1,
    baseCurrency: "USD",
    quoteCurrency: "AUD",
    condition: "Above",
    threshold: 1.6,
    isActive: true,
    createdAt: "2026-01-01T00:00:00Z",
  },
];

describe("AlertList", () => {
  it("shows an empty state when there are no alerts", () => {
    render(<AlertList alerts={[]} onEvaluate={vi.fn()} />);

    expect(screen.getByText(/No alert rules yet/)).toBeInTheDocument();
  });

  it("shows a triggered result after evaluating", async () => {
    const onEvaluate = vi.fn().mockResolvedValue({
      alertRuleId: 1,
      isTriggered: true,
      rate: 1.65,
      threshold: 1.6,
      condition: "Above",
      evaluatedAt: "2026-01-01T00:00:00Z",
      alertEventId: 5,
    });
    render(<AlertList alerts={alerts} onEvaluate={onEvaluate} />);

    await userEvent.click(screen.getByRole("button", { name: "Evaluate Now" }));

    expect(onEvaluate).toHaveBeenCalledWith(1);
    expect(await screen.findByText(/Triggered — current rate 1.6500/)).toBeInTheDocument();
  });

  it("shows an error message when evaluation fails", async () => {
    const onEvaluate = vi.fn().mockRejectedValue(new Error("network down"));
    render(<AlertList alerts={alerts} onEvaluate={onEvaluate} />);

    await userEvent.click(screen.getByRole("button", { name: "Evaluate Now" }));

    expect(await screen.findByText("Failed to evaluate alert.")).toBeInTheDocument();
  });
});
