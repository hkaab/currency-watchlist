import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { CreateAlertForm } from "./CreateAlertForm";
import type { WatchlistItemResponse } from "@/lib/types";

const items: WatchlistItemResponse[] = [
  { id: 1, watchlistId: 1, baseCurrency: "USD", quoteCurrency: "AUD", createdAt: "2026-01-01T00:00:00Z", latestRate: null },
];

describe("CreateAlertForm", () => {
  it("prompts to add a pair first when there are no items", () => {
    render(<CreateAlertForm items={[]} onCreate={vi.fn()} />);
    expect(screen.getByText(/Add a currency pair first/)).toBeInTheDocument();
  });

  it("keeps submit disabled until a positive threshold is entered", async () => {
    render(<CreateAlertForm items={items} onCreate={vi.fn()} />);
    const submit = screen.getByRole("button", { name: "Create alert" });

    expect(submit).toBeDisabled();

    await userEvent.type(screen.getByLabelText("Threshold"), "0");
    expect(submit).toBeDisabled();

    await userEvent.clear(screen.getByLabelText("Threshold"));
    await userEvent.type(screen.getByLabelText("Threshold"), "1.6");
    expect(submit).toBeEnabled();
  });

  it("selects the newly available item once one is added after mount", async () => {
    const onCreate = vi.fn().mockResolvedValue(undefined);
    const { rerender } = render(<CreateAlertForm items={[]} onCreate={onCreate} />);

    rerender(<CreateAlertForm items={items} onCreate={onCreate} />);
    await waitFor(() => expect(screen.getByLabelText("Pair")).toHaveValue("1"));

    await userEvent.type(screen.getByLabelText("Threshold"), "1.6");
    await userEvent.click(screen.getByRole("button", { name: "Create alert" }));

    expect(onCreate).toHaveBeenCalledWith(1, "Above", 1.6);
  });

  it("calls onCreate with the selected item, condition, and threshold", async () => {
    const onCreate = vi.fn().mockResolvedValue(undefined);
    render(<CreateAlertForm items={items} onCreate={onCreate} />);

    await userEvent.selectOptions(screen.getByLabelText("Condition"), "Below");
    await userEvent.type(screen.getByLabelText("Threshold"), "1.5");
    await userEvent.click(screen.getByRole("button", { name: "Create alert" }));

    expect(onCreate).toHaveBeenCalledWith(1, "Below", 1.5);
  });
});
