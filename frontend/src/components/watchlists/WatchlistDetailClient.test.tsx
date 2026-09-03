import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { WatchlistDetailClient } from "./WatchlistDetailClient";
import { useWatchlistDetail } from "@/hooks/useWatchlistDetail";
import { useAlerts } from "@/hooks/useAlerts";
import { renderWithToast } from "@/test-utils/renderWithToast";

vi.mock("@/hooks/useWatchlistDetail", () => ({ useWatchlistDetail: vi.fn() }));
vi.mock("@/hooks/useAlerts", () => ({ useAlerts: vi.fn() }));
vi.mock("@/components/charts/RateHistoryChart", () => ({
  RateHistoryChart: () => <div data-testid="chart" />,
}));

const item = {
  id: 10,
  watchlistId: 1,
  baseCurrency: "USD",
  quoteCurrency: "AUD",
  createdAt: "2026-01-01T00:00:00Z",
  latestRate: null,
};

function mockHooks(overrides: Partial<ReturnType<typeof useWatchlistDetail>> = {}) {
  vi.mocked(useWatchlistDetail).mockReturnValue({
    watchlist: { id: 1, name: "Travel", createdAt: "2026-01-01T00:00:00Z", items: [item] },
    isLoading: false,
    isRefreshing: false,
    error: null,
    addItem: vi.fn(),
    removeItem: vi.fn(),
    refresh: vi.fn().mockResolvedValue({ refreshedPairCount: 1, snapshots: [] }),
    ...overrides,
  });
  vi.mocked(useAlerts).mockReturnValue({
    alerts: [],
    isLoading: false,
    error: null,
    recentEvents: [],
    create: vi.fn(),
    evaluate: vi.fn(),
  });
}

describe("WatchlistDetailClient", () => {
  it("shows a loading state while the watchlist is loading", () => {
    mockHooks({ watchlist: null, isLoading: true });

    renderWithToast(<WatchlistDetailClient watchlistId={1} />);

    expect(screen.getByRole("status")).toBeInTheDocument();
  });

  it("shows an error banner when the watchlist failed to load", () => {
    mockHooks({ watchlist: null, isLoading: false, error: "Watchlist not found" });

    renderWithToast(<WatchlistDetailClient watchlistId={1} />);

    expect(screen.getByRole("alert")).toHaveTextContent("Watchlist not found");
  });

  it("renders the watchlist name and items once loaded", () => {
    mockHooks();

    renderWithToast(<WatchlistDetailClient watchlistId={1} />);

    expect(screen.getByRole("heading", { name: "Travel" })).toBeInTheDocument();
    expect(screen.getAllByText("USD → AUD").length).toBeGreaterThan(0);
  });

  it("calls refresh and shows the resulting message when Refresh Rates is clicked", async () => {
    const refresh = vi.fn().mockResolvedValue({ refreshedPairCount: 2, snapshots: [] });
    mockHooks({ refresh });

    renderWithToast(<WatchlistDetailClient watchlistId={1} />);
    await userEvent.click(screen.getByRole("button", { name: "Refresh Rates" }));

    expect(refresh).toHaveBeenCalled();
    expect(await screen.findByText("Refreshed 2 pairs.")).toBeInTheDocument();
  });
});
