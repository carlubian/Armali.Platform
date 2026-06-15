import { expect, test, type Page } from "@playwright/test";

const username = process.env.SEGARIS_E2E_USERNAME;
const password = process.env.SEGARIS_E2E_PASSWORD;

// An optional second, non-administrator account. When it is present the
// non-admin guard below runs as a real browser journey; otherwise it is skipped,
// matching the single-account end-to-end contract used by the other specs.
const userUsername = process.env.SEGARIS_E2E_USER_USERNAME;
const userPassword = process.env.SEGARIS_E2E_USER_PASSWORD;

async function signIn(page: Page, name: string, secret: string) {
  await page.goto("/login");
  await page.getByLabel("Username").fill(name);
  await page.getByLabel("Password").fill(secret);
  await page.getByRole("button", { name: "Sign in" }).click();
}

/** Indices of two known names within the rendered catalog name column. */
async function nameOrder(page: Page, first: string, second: string) {
  const names = await page.locator(".seg-catalog__name").allTextContents();
  return [names.indexOf(first), names.indexOf(second)] as const;
}

/**
 * Critical single-administrator Configuration journey against the full stack:
 * sign in, open the admin-only launcher card, navigate the flat Global/Capex
 * sections and Global catalog tabs, then exercise the non-currency management
 * surface end to end — create, rename, reorder (silent on success), direct
 * delete of an unreferenced value, and replace-or-clear of a value that a Capex
 * entry references. Everything it touches is disposable, so the seeded catalogs
 * stay intact.
 *
 * Currency conversion and deletion are covered by the Wave 5 backend integration
 * suite (ConfigurationManagementEndpointTests / PostgresPersistenceTests) and the
 * conversion-dialog component tests; the browser-level conversion journey is
 * recorded as deferred in ROADMAP.md alongside the multi-account gap.
 *
 * Skipped without seeded credentials (SEGARIS_E2E_USERNAME /
 * SEGARIS_E2E_PASSWORD), matching the other end-to-end specs.
 */
test.describe("configuration administrator journey", () => {
  test.skip(
    !username || !password,
    "Requires a running backend and a seeded administrator (SEGARIS_E2E_USERNAME / SEGARIS_E2E_PASSWORD).",
  );

  test("manages shared catalogs through the deployed frontend", async ({ page }) => {
    const token = Date.now().toString(36);
    const supplierA = `E2E supplier A ${token}`;
    const supplierB = `E2E supplier B ${token}`;
    const supplierARenamed = `${supplierA} (renamed)`;
    const referenced = `E2E referenced ${token}`;
    const entryTitle = `E2E cfg entry ${token}`;

    await signIn(page, username!, password!);

    // The admin-only launcher card opens Configuration.
    await page.getByRole("button", { name: /Configuration/i }).click();
    await expect(
      page.getByRole("heading", { level: 1, name: "Configuration" }),
    ).toBeVisible();

    // Flat section navigation plus URL-backed Global catalog tabs.
    const sections = page.getByRole("tablist", { name: "Configuration sections" });
    await expect(sections.getByRole("tab", { name: "Global" })).toBeVisible();
    await expect(sections.getByRole("tab", { name: "Capex" })).toBeVisible();
    const catalogs = page.getByRole("tablist", { name: "Global catalogs" });
    await expect(catalogs.getByRole("tab", { name: "Suppliers" })).toBeVisible();
    await expect(catalogs.getByRole("tab", { name: "Cost centres" })).toBeVisible();
    await expect(catalogs.getByRole("tab", { name: "Currencies" })).toBeVisible();

    // Create two suppliers; new rows append last.
    for (const name of [supplierA, supplierB]) {
      await page.getByRole("button", { name: "New supplier" }).click();
      const createDialog = page.getByRole("dialog", { name: "New supplier" });
      await createDialog.getByLabel("Name").fill(name);
      await createDialog.getByRole("button", { name: "Create" }).click();
      await expect(page.getByText("Added")).toBeVisible();
      await expect(page.getByRole("cell", { name })).toBeVisible();
    }

    // B was created last, so it sits after A.
    const [initialA, initialB] = await nameOrder(page, supplierA, supplierB);
    expect(initialA).toBeLessThan(initialB);

    // Reorder: move B up so it now precedes A. Success is intentionally silent
    // (no toast); the moved row simply swaps with its neighbour.
    await page.getByRole("button", { name: `Move ${supplierB} up` }).click();
    await expect
      .poll(async () => {
        const [a, b] = await nameOrder(page, supplierA, supplierB);
        return b < a;
      })
      .toBe(true);

    // Rename supplier A and confirm the live name replaces the old one.
    await page.getByRole("button", { name: `Edit ${supplierA}` }).click();
    const editDialog = page.getByRole("dialog", { name: "Edit supplier" });
    await editDialog.getByLabel("Name").fill(supplierARenamed);
    await editDialog.getByRole("button", { name: "Save changes" }).click();
    await expect(page.getByText("Saved")).toBeVisible();
    await expect(page.getByRole("cell", { name: supplierARenamed })).toBeVisible();
    await expect(page.getByRole("cell", { name: supplierA, exact: true })).toHaveCount(0);

    // Direct-delete the unreferenced supplier B: a plain irreversible confirmation.
    await page.getByRole("button", { name: `Delete ${supplierB}` }).click();
    const directDialog = page.getByRole("dialog", { name: `Delete ${supplierB}?` });
    await directDialog.getByRole("button", { name: "Delete" }).click();
    await expect(page.getByText("Removed")).toBeVisible();
    await expect(page.getByRole("cell", { name: supplierB })).toHaveCount(0);

    // Create a supplier that a Capex entry will reference.
    await page.getByRole("button", { name: "New supplier" }).click();
    const refDialog = page.getByRole("dialog", { name: "New supplier" });
    await refDialog.getByLabel("Name").fill(referenced);
    await refDialog.getByRole("button", { name: "Create" }).click();
    await expect(page.getByRole("cell", { name: referenced })).toBeVisible();

    // Reference it from a new Capex entry.
    await page.goto("/capex");
    await expect(page.getByRole("heading", { name: "Entries" })).toBeVisible();
    await page.getByRole("button", { name: "New entry" }).click();
    const entryDialog = page.getByRole("dialog", { name: "New entry" });
    await entryDialog.getByLabel("Title").fill(entryTitle);
    await entryDialog.getByLabel("Date").fill("2030-01-15");
    await entryDialog.getByLabel("Amount").fill("50");
    await entryDialog.getByLabel("Supplier").selectOption({ label: referenced });
    await entryDialog.getByRole("button", { name: "Create entry" }).click();
    await expect(page.getByText("Entry created")).toBeVisible();

    // Back in Configuration the referenced supplier offers replace-or-clear and
    // never reveals counts or record details. Clear the optional reference.
    await page.goto("/configuration");
    await page.getByRole("button", { name: `Delete ${referenced}` }).click();
    const referencedDialog = page.getByRole("dialog", { name: `Remove ${referenced}` });
    await referencedDialog
      .getByRole("radio", { name: "Leave the value empty on those records" })
      .check();
    await referencedDialog.getByRole("button", { name: "Delete" }).click();
    await expect(page.getByText("Removed")).toBeVisible();
    await expect(page.getByRole("cell", { name: referenced })).toHaveCount(0);

    // Clean up the disposable Capex entry so the run leaves no residue.
    await page.goto("/capex");
    await page.getByRole("row", { name: new RegExp(entryTitle) }).click();
    const reopened = page.getByRole("dialog", { name: "Edit entry" });
    await reopened.getByRole("button", { name: "Delete entry" }).click();
    const confirm = page.getByRole("dialog", { name: "Delete this entry?" });
    await confirm.getByRole("button", { name: "Delete entry" }).click();
    await expect(page.getByText("Entry deleted")).toBeVisible();
  });
});

/**
 * Non-administrator guard: the Configuration launcher card is hidden and the
 * protected route renders Access Denied rather than the management surface.
 *
 * Runs only when an optional non-admin account is seeded
 * (SEGARIS_E2E_USER_USERNAME / SEGARIS_E2E_USER_PASSWORD); otherwise it is
 * skipped, and non-admin enforcement remains covered by the router and
 * ConfigurationPage component tests. The multi-account E2E gap is recorded in
 * ROADMAP.md.
 */
test.describe("configuration non-administrator guard", () => {
  test.skip(
    !userUsername || !userPassword,
    "Requires a seeded non-admin account (SEGARIS_E2E_USER_USERNAME / SEGARIS_E2E_USER_PASSWORD).",
  );

  test("hides the launcher card and blocks the route", async ({ page }) => {
    await signIn(page, userUsername!, userPassword!);

    await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
    await expect(
      page.getByRole("button", { name: /Configuration/i }),
    ).toHaveCount(0);

    await page.goto("/configuration");
    await expect(
      page.getByRole("heading", { name: "This area is for administrators" }),
    ).toBeVisible();
  });
});
