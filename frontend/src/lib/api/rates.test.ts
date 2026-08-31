import { describe, expect, it, vi } from "vitest";
import { apiClient } from "./client";
import { ratesApi } from "./rates";

vi.mock("./client", () => ({
  apiClient: { get: vi.fn(), post: vi.fn(), delete: vi.fn() },
}));

describe("ratesApi", () => {
  it("refresh posts to the refresh endpoint scoped to a watchlist", () => {
    ratesApi.refresh(4);
    expect(apiClient.post).toHaveBeenCalledWith("/api/rates/refresh?watchlistId=4");
  });

  it("history builds a query string with base, quote, and ISO date range", () => {
    const from = new Date("2026-01-01T00:00:00Z");
    const to = new Date("2026-01-31T00:00:00Z");

    ratesApi.history("USD", "AUD", from, to);

    expect(apiClient.get).toHaveBeenCalledWith(
      "/api/rates/history?base=USD&quote=AUD&from=2026-01-01T00%3A00%3A00.000Z&to=2026-01-31T00%3A00%3A00.000Z",
    );
  });
});
