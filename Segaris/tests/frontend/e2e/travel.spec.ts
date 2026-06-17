import { expect, test } from "@playwright/test";

const username = process.env.SEGARIS_E2E_USERNAME;
const password = process.env.SEGARIS_E2E_PASSWORD;

/**
 * Critical single-user Travel journey against the full stack: sign in, open the
 * module, exercise and clear a trips filter, create a trip, add an itinerary
 * entry that carries a reservation locator, reopen the trip and confirm the
 * itinerary persisted, add two expenses in different currencies, confirm the
 * per-currency totals render one badge per currency, and delete the safe test
 * data (the trip deletes its itinerary, expenses, and attachments in one
 * operation).
 *
 * It is skipped without seeded credentials (SEGARIS_E2E_USERNAME /
 * SEGARIS_E2E_PASSWORD), matching the other end-to-end specs. The environment
 * must also expose at least two Configuration currencies so the trip can hold
 * expenses in more than one currency. The second-user privacy journey is
 * deferred until multi-account E2E infrastructure exists; see ROADMAP.md.
 */
test.describe("travel critical journey", () => {
  test.skip(
    !username || !password,
    "Requires a running backend and a seeded user (SEGARIS_E2E_USERNAME / SEGARIS_E2E_PASSWORD) with at least two currencies in Configuration.",
  );

  test("creates a trip with itinerary and multi-currency expenses, then cleans up", async ({
    page,
  }) => {
    const suffix = Date.now().toString(36);
    const tripName = `E2E trip ${suffix}`;
    const entryTitle = `E2E leg ${suffix}`;
    const locator = `LOC-${suffix.toUpperCase()}`;

    // Sign in.
    await page.goto("/login");
    await page.getByLabel("Username").fill(username!);
    await page.getByLabel("Password").fill(password!);
    await page.getByRole("button", { name: "Sign in" }).click();

    // Open Travel from the launcher; the trips view is the module entry point.
    const modules = page.getByRole("region", { name: "Available modules" });
    await modules.getByRole("button", { name: /Travel/i }).click();
    await expect(page.getByRole("heading", { name: "Travel" })).toBeVisible();
    await expect(page.getByRole("button", { name: "New trip" })).toBeVisible();

    // Exercise a filter and clear it so list state round-trips without a reload.
    await page.getByLabel("Search").fill("nothing-matches-xyz");
    await expect(
      page.getByText("No trips match the current filters."),
    ).toBeVisible();
    await page.getByLabel("Search").fill("");
    await expect(
      page.getByRole("button", { name: /Remove filter Search/ }),
    ).toHaveCount(0);

    // Create a trip with the documented defaults (Planned, Public, today's
    // dates). The dialog reopens in edit mode so attachments, itinerary, and
    // expenses can be added immediately.
    await page.getByRole("button", { name: "New trip" }).click();
    const createDialog = page.getByRole("dialog", { name: "New trip" });
    await createDialog.getByLabel("Name").fill(tripName);
    await createDialog.getByRole("button", { name: "Create", exact: true }).click();
    await expect(page.getByText("Trip created")).toBeVisible();

    // The freshly created trip is now open in edit mode. Add one itinerary entry
    // with a reservation locator, then save the trip to persist the itinerary.
    const editDialog = page.getByRole("dialog", { name: "Edit trip" });
    await expect(editDialog).toBeVisible();
    await editDialog.getByRole("button", { name: "Add entry" }).click();
    await editDialog.getByLabel("Title", { exact: true }).fill(entryTitle);
    await editDialog.getByLabel("Reservation locator").fill(locator);
    await editDialog.getByRole("button", { name: "Save", exact: true }).click();
    await expect(page.getByText("Trip updated")).toBeVisible();

    // Reopen the trip and confirm the itinerary entry and its locator persisted.
    await page.getByRole("button", { name: `Open ${tripName}` }).click();
    const reopened = page.getByRole("dialog", { name: "Edit trip" });
    await expect(reopened.getByLabel("Title", { exact: true })).toHaveValue(
      entryTitle,
    );
    await expect(reopened.getByLabel("Reservation locator")).toHaveValue(locator);

    // Switch to the Expenses tab and add two expenses in different currencies.
    await reopened.getByRole("tab", { name: "Expenses" }).click();
    await reopened.getByRole("button", { name: "Add expense" }).click();

    const firstExpense = page.getByRole("dialog", { name: "New expense" });
    await firstExpense.getByLabel("Description").fill("E2E flight");
    await firstExpense.getByLabel("Amount").fill("120.50");
    const currencyValues = await firstExpense
      .getByLabel("Currency", { exact: true })
      .locator("option")
      .evaluateAll((options) =>
        options.map((option) => (option as HTMLOptionElement).value).filter(Boolean),
      );
    expect(currencyValues.length).toBeGreaterThanOrEqual(2);
    await firstExpense
      .getByLabel("Currency", { exact: true })
      .selectOption(currencyValues[0]);
    await firstExpense.getByRole("button", { name: "Create", exact: true }).click();
    await expect(
      reopened.getByRole("button", { name: "Open expense E2E flight" }),
    ).toBeVisible();

    await reopened.getByRole("button", { name: "Add expense" }).click();
    const secondExpense = page.getByRole("dialog", { name: "New expense" });
    await secondExpense.getByLabel("Description").fill("E2E lodging");
    await secondExpense.getByLabel("Amount").fill("300.00");
    await secondExpense
      .getByLabel("Currency", { exact: true })
      .selectOption(currencyValues[1]);
    await secondExpense.getByRole("button", { name: "Create", exact: true }).click();
    await expect(
      reopened.getByRole("button", { name: "Open expense E2E lodging" }),
    ).toBeVisible();

    // Both expenses are present and the per-currency totals render one badge per
    // distinct currency (no automatic conversion to a single total).
    const totals = page.locator(".seg-trv-expenses__totals");
    await expect(totals).toBeVisible();
    await expect(totals.locator(":scope > *")).toHaveCount(2);

    // Clean up: delete the trip from the Details tab. Deleting a trip removes its
    // itinerary, expenses, and all owned attachments in one operation.
    await reopened.getByRole("tab", { name: "Details" }).click();
    await reopened.getByRole("button", { name: "Delete", exact: true }).click();
    const confirmDelete = page.getByRole("dialog", { name: "Delete trip?" });
    await confirmDelete.getByRole("button", { name: "Delete trip" }).click();
    await expect(page.getByText("Trip deleted")).toBeVisible();
    await expect(
      page.getByRole("button", { name: `Open ${tripName}` }),
    ).toHaveCount(0);
  });
});
