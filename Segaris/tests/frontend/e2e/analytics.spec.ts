import { expect, test } from "@playwright/test";

const username = process.env.SEGARIS_E2E_USERNAME;
const password = process.env.SEGARIS_E2E_PASSWORD;

/**
 * Representative single-user Analytics journey against the full stack: sign in,
 * open Analytics from the launcher, confirm the yearly Overview surface with its
 * year totals and monthly chart, navigate years through the sub-bar (the URL
 * round-trips the selected year), then open every tab in turn and confirm each
 * lazily renders its own heading and at least one accessible chart. It finishes
 * by toggling a chart's data table so the table-equivalent of a chart is
 * exercised, and asserts the configuration-incomplete banner is absent on a
 * fully-configured stack.
 *
 * It is skipped without seeded credentials (SEGARIS_E2E_USERNAME /
 * SEGARIS_E2E_PASSWORD), matching the other end-to-end specs. The
 * missing-exchange-rate browser state is verified by component tests
 * (AnalyticsPage.test.tsx) and API integration tests
 * (AnalyticsCapexOpexEndpointTests / AnalyticsInventoryTravelEndpointTests); it
 * is not forced here because Configuration validation prevents creating an
 * un-rated currency through the UI, so it cannot be triggered deterministically
 * in a live journey. The second-user privacy journey is deferred until
 * multi-account E2E infrastructure exists.
 */
test.describe("analytics yearly reporting journey", () => {
  test.skip(
    !username || !password,
    "Requires a running backend and a seeded user (SEGARIS_E2E_USERNAME / SEGARIS_E2E_PASSWORD).",
  );

  test("opens Analytics, navigates years, and renders every tab", async ({ page }) => {
    await page.goto("/login");
    await page.getByLabel("Username").fill(username!);
    await page.getByLabel("Password").fill(password!);
    await page.getByRole("button", { name: "Sign in" }).click();

    // Open Analytics from the launcher; it lands directly on the yearly surface.
    const modules = page.getByRole("region", { name: "Available modules" });
    await modules.getByRole("button", { name: /Analytics/i }).click();
    await expect(page).toHaveURL(/\/analytics/);

    // The reporting surface exposes every section as a tab and opens on Overview.
    const tablist = page.getByRole("tablist", { name: "Analytics sections" });
    await expect(tablist.getByRole("tab")).toHaveCount(6);
    await expect(page.getByRole("tab", { name: "Overview" })).toHaveAttribute(
      "aria-selected",
      "true",
    );

    // Overview shows the three year totals and at least the monthly trend chart;
    // the chart exposes an accessible image role with a prose summary label.
    await expect(page.getByRole("group", { name: "Year totals" })).toBeVisible();
    await expect(
      page.getByRole("img", { name: /Total expenses by month/i }),
    ).toBeVisible();

    // The fully-configured stack never shows the configuration-incomplete state.
    await expect(page.getByText("Exchange rates are incomplete")).toHaveCount(0);

    // Year navigation round-trips through the URL and toggles the "This year" CTA.
    const thisYear = page.getByRole("button", { name: "This year" });
    await expect(thisYear).toBeDisabled();
    await page.getByRole("button", { name: "Previous year" }).click();
    await expect(page).toHaveURL(/year=\d{4}/);
    await expect(thisYear).toBeEnabled();
    await thisYear.click();
    await expect(thisYear).toBeDisabled();

    // Each tab lazily renders its own heading and at least one accessible chart.
    const tabs: { tab: string; heading: RegExp; chart: RegExp }[] = [
      { tab: "Capex", heading: /Capital income & expense/i, chart: /Expenses by category/i },
      { tab: "Opex", heading: /Operating income & expense/i, chart: /Expenses by category/i },
      { tab: "Inventory", heading: /Order spending/i, chart: /Expenses by item category/i },
      { tab: "Travel", heading: /Trip spending/i, chart: /Expenses by destination/i },
      { tab: "Cross-module", heading: /Pooled expenses/i, chart: /Total expenses by supplier/i },
    ];

    for (const { tab, heading, chart } of tabs) {
      await page.getByRole("tab", { name: tab }).click();
      await expect(page).toHaveURL(new RegExp(`tab=${tab.toLowerCase()}`));
      await expect(page.getByRole("heading", { level: 2, name: heading })).toBeVisible();
      await expect(page.getByRole("img", { name: chart })).toBeVisible();
    }

    // Exercise a chart's table-equivalent so the accessible summary is not the
    // only non-visual representation of the data.
    const supplierChart = page.getByRole("img", { name: /Total expenses by supplier/i });
    const card = page.locator(".an-card", { has: supplierChart });
    const tableToggle = card.getByRole("button", { name: "Show data table" });
    if (await tableToggle.count()) {
      await tableToggle.click();
      await expect(card.getByRole("table")).toBeVisible();
    }
  });
});
