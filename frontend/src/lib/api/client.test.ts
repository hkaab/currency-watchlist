import { afterEach, describe, expect, it, vi } from "vitest";
import { apiClient, ApiError } from "./client";

describe("apiClient", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("returns parsed JSON on success", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify({ id: 1 }), { status: 200 }),
      ),
    );

    const result = await apiClient.get<{ id: number }>("/api/watchlists/1");

    expect(result).toEqual({ id: 1 });
  });

  it("returns undefined for a 204 No Content response", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 204 })));

    const result = await apiClient.delete("/api/watchlists/1");

    expect(result).toBeUndefined();
  });

  it("throws an ApiError with the ProblemDetails message on failure", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify({ title: "Not Found", detail: "Watchlist with id '1' was not found." }), {
          status: 404,
        }),
      ),
    );

    await expect(apiClient.get("/api/watchlists/1")).rejects.toMatchObject({
      message: "Watchlist with id '1' was not found.",
      status: 404,
    });
  });

  it("prefers the specific field message(s) over the generic validation title", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        new Response(
          JSON.stringify({
            title: "One or more validation errors occurred.",
            status: 400,
            errors: { QuoteCurrency: ["'ZZZ' is not a currency the rate provider supports."] },
          }),
          { status: 400 },
        ),
      ),
    );

    await expect(apiClient.get("/api/watchlists/1/items")).rejects.toMatchObject({
      message: "'ZZZ' is not a currency the rate provider supports.",
      status: 400,
      fieldErrors: { QuoteCurrency: ["'ZZZ' is not a currency the rate provider supports."] },
    });
  });

  it("falls back to a generic message when the error body isn't JSON", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response("oops", { status: 500 })));

    await expect(apiClient.get("/api/watchlists/1")).rejects.toBeInstanceOf(ApiError);
  });

  it("sends a JSON content-type header when posting a body", async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({}), { status: 200 }));
    vi.stubGlobal("fetch", fetchMock);

    await apiClient.post("/api/watchlists", { name: "Test" });

    const [, init] = fetchMock.mock.calls[0];
    expect(init.headers["Content-Type"]).toBe("application/json");
    expect(init.body).toBe(JSON.stringify({ name: "Test" }));
  });
});
