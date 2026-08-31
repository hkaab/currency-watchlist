import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { WatchlistList } from "./WatchlistList";
import type { WatchlistResponse } from "@/lib/types";

const watchlists: WatchlistResponse[] = [
  { id: 1, name: "Travel Money", createdAt: "2026-01-01T00:00:00Z", itemCount: 2 },
  { id: 2, name: "Savings", createdAt: "2026-01-02T00:00:00Z", itemCount: 0 },
];

describe("WatchlistList", () => {
  it("shows an empty state when there are no watchlists", () => {
    render(<WatchlistList watchlists={[]} onDelete={vi.fn()} />);

    expect(screen.getByText(/No watchlists yet/)).toBeInTheDocument();
  });

  it("renders a link and item count for each watchlist", () => {
    render(<WatchlistList watchlists={watchlists} onDelete={vi.fn()} />);

    expect(screen.getByRole("link", { name: "Travel Money" })).toHaveAttribute(
      "href",
      "/watchlists/1",
    );
    expect(screen.getByText(/2 pairs/)).toBeInTheDocument();
    expect(screen.getByText(/0 pairs/)).toBeInTheDocument();
  });

  it("calls onDelete with the watchlist id when Delete is clicked", async () => {
    const onDelete = vi.fn().mockResolvedValue(undefined);
    render(<WatchlistList watchlists={watchlists} onDelete={onDelete} />);

    const [firstDeleteButton] = screen.getAllByRole("button", { name: /Delete/ });
    await userEvent.click(firstDeleteButton);

    expect(onDelete).toHaveBeenCalledWith(1);
  });
});
