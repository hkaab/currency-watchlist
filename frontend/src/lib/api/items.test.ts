import { describe, expect, it, vi } from "vitest";
import { apiClient } from "./client";
import { itemsApi } from "./items";

vi.mock("./client", () => ({
  apiClient: { get: vi.fn(), post: vi.fn(), delete: vi.fn() },
}));

describe("itemsApi", () => {
  it("add posts base and quote currency", () => {
    itemsApi.add(1, "USD", "AUD");
    expect(apiClient.post).toHaveBeenCalledWith("/api/watchlists/1/items", {
      baseCurrency: "USD",
      quoteCurrency: "AUD",
    });
  });

  it("remove deletes the item", () => {
    itemsApi.remove(1, 5);
    expect(apiClient.delete).toHaveBeenCalledWith("/api/watchlists/1/items/5");
  });
});
