import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { WatchlistList } from "./WatchlistList";
import { renderWithToast } from "@/test-utils/renderWithToast";
import type { WatchlistResponse } from "@/lib/types";

const watchlists: WatchlistResponse[] = [
  { id: 1, name: "Travel Money", createdAt: "2026-01-01T00:00:00Z", itemCount: 2 },
  { id: 2, name: "Savings", createdAt: "2026-01-02T00:00:00Z", itemCount: 0 },
];

describe("WatchlistList", () => {
  it("shows an empty state when there are no watchlists", () => {
    renderWithToast(<WatchlistList watchlists={[]} onDelete={vi.fn()} />);

    expect(screen.getByText(/No watchlists yet/)).toBeInTheDocument();
  });

  it("renders a link and item count for each watchlist", () => {
    renderWithToast(<WatchlistList watchlists={watchlists} onDelete={vi.fn()} />);

    expect(screen.getByRole("link", { name: "Travel Money" })).toHaveAttribute(
      "href",
      "/watchlists/1",
    );
    expect(screen.getByText(/2 pairs/)).toBeInTheDocument();
    expect(screen.getByText(/0 pairs/)).toBeInTheDocument();
  });

  it("calls onDelete with the watchlist id when Delete is clicked", async () => {
    const onDelete = vi.fn().mockResolvedValue(undefined);
    renderWithToast(<WatchlistList watchlists={watchlists} onDelete={onDelete} />);

    const [firstDeleteButton] = screen.getAllByRole("button", { name: /Delete/ });
    await userEvent.click(firstDeleteButton);

    expect(onDelete).toHaveBeenCalledWith(1);
  });

  it("shows a toast instead of an unhandled rejection when onDelete fails", async () => {
    const onDelete = vi.fn().mockRejectedValue(new Error("boom"));
    renderWithToast(<WatchlistList watchlists={watchlists} onDelete={onDelete} />);

    const [firstDeleteButton] = screen.getAllByRole("button", { name: /Delete/ });
    await userEvent.click(firstDeleteButton);

    expect(await screen.findByRole("alert")).toHaveTextContent("Failed to delete watchlist.");
  });
});
