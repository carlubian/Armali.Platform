import { expect, test } from "@playwright/test";

const username = process.env.SEGARIS_E2E_USERNAME;
const password = process.env.SEGARIS_E2E_PASSWORD;

/**
 * Critical single-user Capex journey against the full stack: sign in, open the
 * module, filter, create a simple entry, itemise it, attach a file, observe the
 * launcher attention it raises, then delete it and return to the same list.
 *
 * It is skipped without seeded credentials (SEGARIS_E2E_USERNAME /
 * SEGARIS_E2E_PASSWORD), matching the other end-to-end specs. The second-user
 * privacy journey is deferred until multi-account E2E infrastructure exists.
 */
test.describe("capex critical journey", () => {
  test.skip(
    !username || !password,
    "Requires a running backend and a seeded user (SEGARIS_E2E_USERNAME / SEGARIS_E2E_PASSWORD).",
  );

  test("creates, itemises, attaches, and deletes an entry", async ({ page }) => {
    const title = `E2E entry ${Date.now().toString(36)}`;
    // A past due date in Planning is what raises launcher attention.
    const pastDueDate = "2020-01-15";

    await page.goto("/login");
    await page.getByLabel("Username").fill(username!);
    await page.getByLabel("Password").fill(password!);
    await page.getByRole("button", { name: "Sign in" }).click();

    // Open Capex from the launcher.
    await page.getByRole("button", { name: /Capex/i }).click();
    await expect(page.getByRole("heading", { name: "Entries" })).toBeVisible();

    // Exercise a filter and clear it so the list state round-trips.
    await page.getByLabel("Search").fill("nothing-matches-xyz");
    await expect(page.getByText("No entries match the current filters.")).toBeVisible();
    await page.getByLabel("Search").fill("");

    // Create a simple entry.
    await page.getByRole("button", { name: "New entry" }).click();
    const createDialog = page.getByRole("dialog", { name: "New entry" });
    await createDialog.getByLabel("Title").fill(title);
    await createDialog.getByLabel("Date").fill(pastDueDate);
    await createDialog.getByLabel("Amount").fill("120");
    await createDialog.getByRole("button", { name: "Create entry" }).click();
    await expect(page.getByText("Entry created")).toBeVisible();

    // Reopen the entry to itemise and attach a file.
    const row = page.getByRole("row", { name: new RegExp(title) });
    await row.click();
    const editDialog = page.getByRole("dialog", { name: "Edit entry" });
    await expect(editDialog.getByLabel("Title")).toHaveValue(title);

    await editDialog.getByRole("button", { name: "Add item" }).click();
    const lines = editDialog.getByRole("listitem");
    await lines.last().getByLabel("Description").fill("Itemised line");
    await lines.last().getByLabel("Quantity").fill("2");
    await lines.last().getByLabel("Unit amount").fill("30");

    // Attach a file through the (visually hidden) file input.
    await editDialog
      .locator('input[type="file"]')
      .setInputFiles({
        name: "receipt.txt",
        mimeType: "text/plain",
        buffer: Buffer.from("E2E attachment body"),
      });
    await expect(editDialog.getByText("receipt.txt")).toBeVisible();

    await editDialog.getByRole("button", { name: "Save changes" }).click();
    await expect(page.getByText("Entry updated")).toBeVisible();

    // The overdue Planning entry raises launcher attention.
    await page.goto("/");
    await expect(
      page.getByRole("status", { name: /attention/i }),
    ).toBeVisible();

    // Return to Capex and delete the entry.
    await page.getByRole("button", { name: /Capex/i }).click();
    await page.getByRole("row", { name: new RegExp(title) }).click();
    const reopened = page.getByRole("dialog", { name: "Edit entry" });
    await reopened.getByRole("button", { name: "Delete entry" }).click();
    const confirm = page.getByRole("dialog", { name: "Delete this entry?" });
    await confirm.getByRole("button", { name: "Delete entry" }).click();

    await expect(page.getByText("Entry deleted")).toBeVisible();
    await expect(page.getByRole("heading", { name: "Entries" })).toBeVisible();
    await expect(page.getByRole("row", { name: new RegExp(title) })).toHaveCount(0);
  });
});
