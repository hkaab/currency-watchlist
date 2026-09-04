import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ItemList } from "./ItemList";
import { renderWithToast } from "@/test-utils/renderWithToast";
import type { WatchlistItemResponse } from "@/lib/types";

const items: WatchlistItemResponse[] = [
  {
    id: 1,
    watchlistId: 1,
    baseCurrency: "USD",
    quoteCurrency: "AUD",
    createdAt: "2026-01-01T00:00:00Z",
    latestRate: {
      id: 1,
      baseCurrency: "USD",
      quoteCurrency: "AUD",
      rate: 1.5,
      sourceTimestamp: "2026-01-01T00:00:00Z",
      fetchedAt: "2026-01-01T00:00:00Z",
    },
  },
  {
    id: 2,
    watchlistId: 1,
    baseCurrency: "EUR",
    quoteCurrency: "USD",
    createdAt: "2026-01-01T00:00:00Z",
    latestRate: null,
  },
];

describe("ItemList", () => {
  it("shows an empty state when there are no items", () => {
    renderWithToast(<ItemList items={[]} selectedItemId={null} onSelect={vi.fn()} onRemove={vi.fn()} />);

    expect(screen.getByText(/No currency pairs yet/)).toBeInTheDocument();
  });

  it("shows the latest rate when available and a prompt when not", () => {
    renderWithToast(<ItemList items={items} selectedItemId={null} onSelect={vi.fn()} onRemove={vi.fn()} />);

    expect(screen.getByText("1.5000")).toBeInTheDocument();
    expect(screen.getByText(/No rate fetched yet/)).toBeInTheDocument();
  });

  it("calls onSelect when a row is clicked", async () => {
    const onSelect = vi.fn();
    renderWithToast(<ItemList items={items} selectedItemId={null} onSelect={onSelect} onRemove={vi.fn()} />);

    await userEvent.click(screen.getByText("USD → AUD"));

    expect(onSelect).toHaveBeenCalledWith(1);
  });

  it("calls onRemove without triggering row selection", async () => {
    const onSelect = vi.fn();
    const onRemove = vi.fn().mockResolvedValue(undefined);
    renderWithToast(<ItemList items={items} selectedItemId={null} onSelect={onSelect} onRemove={onRemove} />);

    const [firstRemoveButton] = screen.getAllByRole("button", { name: /Remove/ });
    await userEvent.click(firstRemoveButton);

    expect(onRemove).toHaveBeenCalledWith(1);
    expect(onSelect).not.toHaveBeenCalled();
  });

  it("shows a toast instead of an unhandled rejection when onRemove fails", async () => {
    const onRemove = vi.fn().mockRejectedValue(new Error("boom"));
    renderWithToast(<ItemList items={items} selectedItemId={null} onSelect={vi.fn()} onRemove={onRemove} />);

    const [firstRemoveButton] = screen.getAllByRole("button", { name: /Remove/ });
    await userEvent.click(firstRemoveButton);

    expect(await screen.findByRole("alert")).toHaveTextContent("Failed to remove item.");
  });
});
