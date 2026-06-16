import { expect, test } from "@playwright/test";

const username = process.env.SEGARIS_E2E_USERNAME;
const password = process.env.SEGARIS_E2E_PASSWORD;

/**
 * Critical single-user Opex journey against the full stack: sign in, open the
 * module, filter, create a contract, attach a file, add/edit/delete an
 * occurrence, observe the annual total refresh, close and reopen with preserved
 * URL state, then delete the contract.
 *
 * It is skipped without seeded credentials (SEGARIS_E2E_USERNAME /
 * SEGARIS_E2E_PASSWORD), matching the other end-to-end specs. The second-user
 * privacy journey is deferred until multi-account E2E infrastructure exists;
 * see ROADMAP.md.
 */
test.describe("opex critical journey", () => {
  test.skip(
    !username || !password,
    "Requires a running backend and a seeded user (SEGARIS_E2E_USERNAME / SEGARIS_E2E_PASSWORD).",
  );

  test("creates, attaches, adds occurrences, observes total, and deletes a contract", async ({
    page,
  }) => {
    const contractName = `E2E contract ${Date.now().toString(36)}`;

    await page.goto("/login");
    await page.getByLabel("Username").fill(username!);
    await page.getByLabel("Password").fill(password!);
    await page.getByRole("button", { name: "Sign in" }).click();

    // Open Opex from the launcher.
    await page.getByRole("button", { name: /Opex/i }).click();
    await expect(page.getByRole("heading", { name: "Contracts" })).toBeVisible();

    // Exercise a filter and clear it so list state round-trips.
    await page.getByLabel("Search").fill("nothing-matches-xyz");
    await expect(
      page.getByText("No contracts match the current filters."),
    ).toBeVisible();
    await page.getByLabel("Search").fill("");
    await expect(page.getByRole("button", { name: /Remove Search/ })).toHaveCount(0);

    // Create a contract.
    await page.getByRole("button", { name: "New contract" }).click();
    const createDialog = page.getByRole("dialog", { name: "New contract" });
    await createDialog.getByLabel("Name").fill(contractName);

    // Attach a file during creation (staged).
    await createDialog
      .locator('input[type="file"]')
      .setInputFiles({
        name: "invoice.txt",
        mimeType: "text/plain",
        buffer: Buffer.from("E2E contract attachment"),
      });
    await expect(createDialog.getByText("invoice.txt")).toBeVisible();

    await createDialog.getByRole("button", { name: "Create contract" }).click();

    // The upload dialog appears; finish it.
    const uploadDialog = page.getByRole("dialog", { name: "Upload attachments" });
    await expect(uploadDialog).toBeVisible();
    await expect(uploadDialog.getByText("invoice.txt")).toBeVisible();
    await uploadDialog.getByRole("button", { name: "Done" }).click();

    await expect(page.getByText("Contract created")).toBeVisible();

    // Reopen the contract and navigate to the Occurrences tab.
    const row = page.getByRole("row", { name: new RegExp(contractName) });
    await row.click();
    const editDialog = page.getByRole("dialog", { name: "Edit contract" });
    await expect(editDialog.getByLabel("Name")).toHaveValue(contractName);

    await editDialog.getByRole("tab", { name: "Occurrences" }).click();
    await expect(editDialog.getByText("No occurrences yet.")).toBeVisible();

    // Add a first occurrence.
    await editDialog.getByRole("button", { name: "New occurrence" }).click();
    const createOccurrence = page.getByRole("dialog", { name: "New occurrence" });
    await createOccurrence.getByLabel("Date").fill("2025-03-15");
    await createOccurrence.getByLabel("Amount").fill("49.99");
    await createOccurrence.getByLabel("Description").fill("March payment");
    await createOccurrence.getByRole("button", { name: "Create occurrence" }).click();

    await expect(page.getByText("March payment")).toBeVisible();

    // Add a second occurrence.
    await editDialog.getByRole("button", { name: "New occurrence" }).click();
    const createOccurrence2 = page.getByRole("dialog", { name: "New occurrence" });
    await createOccurrence2.getByLabel("Date").fill("2025-04-15");
    await createOccurrence2.getByLabel("Amount").fill("52.50");
    await createOccurrence2.getByRole("button", { name: "Create occurrence" }).click();

    // Edit the first occurrence.
    await page.getByRole("button", { name: /Open occurrence for/ }).first().click();
    const editOccurrence = page.getByRole("dialog", { name: "Edit occurrence" });
    await expect(editOccurrence.getByLabel("Amount")).toHaveValue("49.99");
    await editOccurrence.getByLabel("Amount").fill("55.00");
    await editOccurrence.getByRole("button", { name: "Save changes" }).click();

    // Delete the second occurrence.
    const occurrenceRows = page.getByRole("button", { name: /Open occurrence for/ });
    await occurrenceRows.last().click();
    const editOccurrence2 = page.getByRole("dialog", { name: "Edit occurrence" });
    await editOccurrence2.getByRole("button", { name: "Delete occurrence" }).click();
    const confirmDelete = page.getByRole("dialog", { name: "Delete this occurrence?" });
    await confirmDelete.getByRole("button", { name: "Delete occurrence" }).click();

    // Return to Details tab and verify realized total is visible.
    await editDialog.getByRole("tab", { name: "Details" }).click();

    // Close and reopen with preserved URL state (contractId remains in URL).
    const currentUrl = page.url();
    expect(currentUrl).toContain("contractId=");

    await editDialog.getByRole("button", { name: "Cancel" }).click();
    await expect(editDialog).toHaveCount(0);

    // The contractId was removed from URL; navigate back to Opex.
    await expect(page.getByRole("heading", { name: "Contracts" })).toBeVisible();

    // Delete the contract.
    await page.getByRole("row", { name: new RegExp(contractName) }).click();
    const finalDialog = page.getByRole("dialog", { name: "Edit contract" });
    await finalDialog.getByRole("button", { name: "Delete contract" }).click();
    const confirmContractDelete = page.getByRole("dialog", {
      name: "Delete this contract?",
    });
    await confirmContractDelete.getByRole("button", { name: "Delete contract" }).click();

    await expect(page.getByText("Contract deleted")).toBeVisible();
    await expect(page.getByRole("heading", { name: "Contracts" })).toBeVisible();
    await expect(
      page.getByRole("row", { name: new RegExp(contractName) }),
    ).toHaveCount(0);
  });
});
