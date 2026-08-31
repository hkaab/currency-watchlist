import { render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { RateHistoryChart } from "./RateHistoryChart";
import { ratesApi } from "@/lib/api/rates";
import { ApiError } from "@/lib/api/client";

vi.mock("@/lib/api/rates", () => ({ ratesApi: { history: vi.fn() } }));

describe("RateHistoryChart", () => {
  it("shows an empty state when there is no history yet", async () => {
    vi.mocked(ratesApi.history).mockResolvedValue([]);

    render(<RateHistoryChart baseCurrency="USD" quoteCurrency="AUD" refreshKey={0} />);

    expect(await screen.findByText(/No stored history yet/)).toBeInTheDocument();
  });

  it("shows an error banner when the history request fails", async () => {
    vi.mocked(ratesApi.history).mockRejectedValue(new ApiError("Server error", 500));

    render(<RateHistoryChart baseCurrency="USD" quoteCurrency="AUD" refreshKey={0} />);

    expect(await screen.findByRole("alert")).toHaveTextContent("Server error");
  });

  it("re-fetches when refreshKey changes", async () => {
    vi.mocked(ratesApi.history).mockResolvedValue([]);

    const { rerender } = render(<RateHistoryChart baseCurrency="USD" quoteCurrency="AUD" refreshKey={0} />);
    await screen.findByText(/No stored history yet/);
    const callsAfterFirstLoad = vi.mocked(ratesApi.history).mock.calls.length;

    rerender(<RateHistoryChart baseCurrency="USD" quoteCurrency="AUD" refreshKey={1} />);

    await waitFor(() =>
      expect(vi.mocked(ratesApi.history).mock.calls.length).toBeGreaterThan(callsAfterFirstLoad),
    );
  });
});
