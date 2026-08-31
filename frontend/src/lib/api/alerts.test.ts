import { describe, expect, it, vi } from "vitest";
import { apiClient } from "./client";
import { alertsApi } from "./alerts";

vi.mock("./client", () => ({
  apiClient: { get: vi.fn(), post: vi.fn(), delete: vi.fn() },
}));

describe("alertsApi", () => {
  it("listByWatchlist filters by watchlistId", () => {
    alertsApi.listByWatchlist(3);
    expect(apiClient.get).toHaveBeenCalledWith("/api/alerts?watchlistId=3");
  });

  it("create posts the alert rule fields", () => {
    alertsApi.create(1, "Above", 1.6);
    expect(apiClient.post).toHaveBeenCalledWith("/api/alerts", {
      watchlistItemId: 1,
      condition: "Above",
      threshold: 1.6,
    });
  });

  it("evaluate posts to the evaluate endpoint", () => {
    alertsApi.evaluate(9);
    expect(apiClient.post).toHaveBeenCalledWith("/api/alerts/9/evaluate");
  });
});
