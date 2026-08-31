import { describe, expect, it, vi } from "vitest";
import { apiClient } from "./client";
import { watchlistsApi } from "./watchlists";

vi.mock("./client", () => ({
  apiClient: { get: vi.fn(), post: vi.fn(), delete: vi.fn() },
}));

describe("watchlistsApi", () => {
  it("list fetches the watchlists collection", () => {
    watchlistsApi.list();
    expect(apiClient.get).toHaveBeenCalledWith("/api/watchlists");
  });

  it("getById fetches a single watchlist", () => {
    watchlistsApi.getById(7);
    expect(apiClient.get).toHaveBeenCalledWith("/api/watchlists/7");
  });

  it("create posts the name", () => {
    watchlistsApi.create("My List");
    expect(apiClient.post).toHaveBeenCalledWith("/api/watchlists", { name: "My List" });
  });

  it("remove deletes the watchlist", () => {
    watchlistsApi.remove(7);
    expect(apiClient.delete).toHaveBeenCalledWith("/api/watchlists/7");
  });
});
