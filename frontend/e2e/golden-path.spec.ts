import { expect, test } from "@playwright/test";

test.describe("Currency watchlist golden path", () => {
  test("create watchlist, add pair, refresh rates, create alert, evaluate", async ({ page }) => {
    const watchlistName = `E2E Watchlist ${Date.now()}`;

    await page.goto("/");
    await expect(page.getByRole("heading", { name: "Currency Watchlists" })).toBeVisible();

    await page.getByLabel("Watchlist name").fill(watchlistName);
    await page.getByRole("button", { name: "Create watchlist" }).click();

    const watchlistLink = page.getByRole("link", { name: watchlistName });
    await expect(watchlistLink).toBeVisible();
    await watchlistLink.click();

    // First navigation to a dynamic route can be slow in dev mode (on-demand compilation).
    await expect(page.getByRole("heading", { name: watchlistName })).toBeVisible({ timeout: 20_000 });

    await page.getByLabel("Base currency").fill("USD");
    await page.getByLabel("Quote currency").fill("AUD");
    await page.getByRole("button", { name: "Add pair" }).click();

    const pairRow = page.getByRole("listitem").filter({ hasText: "USD → AUD" });
    await expect(pairRow).toBeVisible();

    await page.getByRole("button", { name: "Refresh Rates" }).click();
    await expect(page.getByText(/Refreshed \d+ pair/)).toBeVisible({ timeout: 15_000 });
    await expect(pairRow.getByText(/Latest rate:/)).toBeVisible();

    await page.getByLabel("Threshold").fill("0.01");
    await page.getByRole("button", { name: "Create alert" }).click();

    const alertRow = page.getByRole("listitem").filter({ hasText: "Above 0.01" });
    await expect(alertRow).toBeVisible();

    await alertRow.getByRole("button", { name: "Evaluate Now" }).click();
    await expect(alertRow.getByText(/Triggered|Not triggered/)).toBeVisible({ timeout: 15_000 });
  });

  test("pushes a live rate update to a second tab viewing the same watchlist", async ({ browser }) => {
    const watchlistName = `Live Update Test ${Date.now()}`;

    const contextA = await browser.newContext();
    const pageA = await contextA.newPage();

    await pageA.goto("/");
    await pageA.getByLabel("Watchlist name").fill(watchlistName);
    await pageA.getByRole("button", { name: "Create watchlist" }).click();
    await pageA.getByRole("link", { name: watchlistName }).click();
    await expect(pageA.getByRole("heading", { name: watchlistName })).toBeVisible({ timeout: 20_000 });

    await pageA.getByLabel("Base currency").fill("USD");
    await pageA.getByLabel("Quote currency").fill("EUR");
    await pageA.getByRole("button", { name: "Add pair" }).click();
    const pairRowA = pageA.getByRole("listitem").filter({ hasText: "USD → EUR" });
    await expect(pairRowA).toBeVisible();

    const watchlistUrl = pageA.url();
    const contextB = await browser.newContext();
    const pageB = await contextB.newPage();
    await pageB.goto(watchlistUrl);
    const pairRowB = pageB.getByRole("listitem").filter({ hasText: "USD → EUR" });
    await expect(pairRowB).toBeVisible();
    await expect(pairRowB.getByText(/No rate fetched yet/)).toBeVisible();

    // Refreshing in tab A should push the new rate to tab B via SignalR, with no reload.
    await pageA.getByRole("button", { name: "Refresh Rates" }).click();
    await expect(pageA.getByText(/Refreshed \d+ pair/)).toBeVisible({ timeout: 15_000 });
    await expect(pairRowB.getByText(/Latest rate:/)).toBeVisible({ timeout: 15_000 });

    await contextA.close();
    await contextB.close();
  });

  test("shows validation errors for invalid input", async ({ page }) => {
    const watchlistName = `Validation Test ${Date.now()}`;

    await page.goto("/");

    const createWatchlistButton = page.getByRole("button", { name: "Create watchlist" });
    await expect(createWatchlistButton).toBeDisabled();

    await page.getByLabel("Watchlist name").fill(watchlistName);
    await expect(createWatchlistButton).toBeEnabled();
    await createWatchlistButton.click();

    await page.getByRole("link", { name: watchlistName }).click();
    await expect(page.getByRole("heading", { name: watchlistName })).toBeVisible({ timeout: 20_000 });

    await page.getByLabel("Base currency").fill("US");
    await page.getByLabel("Quote currency").fill("AUD");
    await expect(page.getByRole("button", { name: "Add pair" })).toBeDisabled();
  });
});
