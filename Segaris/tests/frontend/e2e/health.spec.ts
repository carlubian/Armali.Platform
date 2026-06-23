import { expect, test, type Page } from "@playwright/test";

const username = process.env.SEGARIS_E2E_USERNAME;
const password = process.env.SEGARIS_E2E_PASSWORD;

const photo = Buffer.from(
  "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMB/ax2Y9kAAAAASUVORK5CYII=",
  "base64",
);

async function signIn(page: Page) {
  await page.goto("/login");
  await page.getByLabel("Username").fill(username!);
  await page.getByLabel("Password").fill(password!);
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(
    page.getByRole("heading", { name: "Choose a module" }),
  ).toBeVisible();
}

async function createPublicInventoryItem(page: Page, name: string) {
  await page.goto("/inventory");
  await expect(page.getByRole("heading", { name: "Inventory" })).toBeVisible();
  await page.getByRole("button", { name: "New item" }).click();

  const dialog = page.getByRole("dialog", { name: "New item" });
  await dialog.getByLabel("Name").fill(name);
  await dialog.getByLabel("Status").selectOption("Active");
  await dialog.getByLabel("Current stock").fill("2");
  await dialog.getByLabel("Minimum stock").fill("0");

  const supplierBoxes = dialog.locator('input[type="checkbox"]');
  const supplierCount = await supplierBoxes.count();
  expect(supplierCount).toBeGreaterThan(0);
  for (let index = 0; index < supplierCount; index += 1) {
    await supplierBoxes.nth(index).check();
  }

  await dialog.getByRole("button", { name: "Create", exact: true }).click();
  await expect(page.getByText("Item created")).toBeVisible();
}

async function createDisease(page: Page, diseaseName: string) {
  await page.goto("/health?tab=diseases");
  await expect(page.getByRole("heading", { name: "Health" })).toBeVisible();
  await page.getByRole("button", { name: "New disease" }).click();

  const dialog = page.getByRole("dialog", { name: "New disease" });
  await dialog.getByLabel("Name").fill(diseaseName);
  await dialog.getByLabel("Category").selectOption({ label: "Acute" });
  await dialog.getByLabel("Average duration").fill("7");
  await dialog.getByLabel("Symptoms").fill("Fever and headache.");
  await dialog.getByRole("button", { name: "Create disease" }).click();
  await expect(page.getByText("Disease created")).toBeVisible();
}

async function createMedicine(
  page: Page,
  medicineName: string,
  diseaseName: string,
  itemName: string,
  photoName: string,
) {
  await page.goto("/health?tab=medicines");
  await expect(page.getByRole("heading", { name: "Health" })).toBeVisible();
  await page.getByRole("button", { name: "New medicine" }).click();

  const dialog = page.getByRole("dialog", { name: "New medicine" });
  await dialog.getByLabel("Name").fill(medicineName);
  await dialog.getByLabel("Category").selectOption({ label: "Analgesic" });
  await dialog.getByLabel("Requires prescription").check();
  await dialog
    .getByLabel("Posology")
    .fill("1 tablet every 8 hours after meals.");

  await dialog.getByRole("button", { name: "Select item" }).click();
  const itemSelector = page.getByRole("dialog", {
    name: /Select inventory item/,
  });
  await itemSelector.getByLabel("Search inventory items").fill(itemName);
  const itemRow = itemSelector.getByRole("row", { name: new RegExp(itemName) });
  await expect(itemRow).toBeVisible();
  await itemRow.getByRole("button", { name: "Select" }).click();
  await expect(dialog.getByText(itemName)).toBeVisible();

  await dialog.getByRole("button", { name: "Add diseases" }).click();
  const diseaseSelector = page.getByRole("dialog", { name: /Select diseases/ });
  await diseaseSelector.getByLabel("Search diseases").fill(diseaseName);
  const diseaseRow = diseaseSelector.getByRole("row", {
    name: new RegExp(diseaseName),
  });
  await expect(diseaseRow).toBeVisible();
  await diseaseRow.getByRole("button", { name: "Add" }).click();
  await diseaseSelector.getByRole("button", { name: "Done" }).click();
  await expect(dialog.getByText(diseaseName)).toBeVisible();

  await dialog.locator('input[type="file"]').setInputFiles({
    name: photoName,
    mimeType: "image/png",
    buffer: photo,
  });
  await expect(dialog.getByText(photoName)).toBeVisible();
  await dialog.getByRole("button", { name: "Create medicine" }).click();

  const uploadDialog = page.getByRole("dialog", {
    name: "Upload medicine attachments",
  });
  await expect(uploadDialog.getByText(photoName)).toBeVisible();
  await uploadDialog
    .getByRole("button", { name: "Make primary image" })
    .click();
  await expect(uploadDialog.getByText("Primary")).toBeVisible();
  await uploadDialog.getByRole("button", { name: "Done" }).click();
  await expect(page.getByText("Medicine created")).toBeVisible();
}

async function associateFromDiseaseSide(
  page: Page,
  diseaseName: string,
  medicineName: string,
) {
  await page.goto("/health?tab=diseases");
  await page.getByLabel("Search diseases").fill(diseaseName);
  await page.getByRole("button", { name: `Open ${diseaseName}` }).click();

  const dialog = page.getByRole("dialog", { name: "Edit disease" });
  await expect(dialog.getByText(medicineName)).toBeVisible();
  await dialog.getByRole("button", { name: `Remove ${medicineName}` }).click();
  await dialog.getByRole("button", { name: "Save changes" }).click();
  await expect(page.getByText("Disease updated")).toBeVisible();

  await page.getByRole("button", { name: `Open ${diseaseName}` }).click();
  const reopenDialog = page.getByRole("dialog", { name: "Edit disease" });
  await reopenDialog.getByRole("button", { name: "Add medicines" }).click();
  const selector = page.getByRole("dialog", { name: /Select medicines/ });
  await selector.getByLabel("Search medicines").fill(medicineName);
  const row = selector.getByRole("row", { name: new RegExp(medicineName) });
  await row.getByRole("button", { name: "Add" }).click();
  await selector.getByRole("button", { name: "Done" }).click();
  await expect(reopenDialog.getByText(medicineName)).toBeVisible();
  await reopenDialog.getByRole("button", { name: "Save changes" }).click();
  await expect(page.getByText("Disease updated")).toBeVisible();
}

async function deleteDisease(page: Page, diseaseName: string) {
  await page.goto("/health?tab=diseases");
  await page.getByLabel("Search diseases").fill(diseaseName);
  await page.getByRole("button", { name: `Open ${diseaseName}` }).click();
  const dialog = page.getByRole("dialog", { name: "Edit disease" });
  await dialog.getByRole("button", { name: "Delete" }).click();
  const confirm = page.getByRole("dialog", { name: "Delete disease?" });
  await confirm.getByRole("button", { name: "Delete disease" }).click();
  await expect(page.getByText("Disease deleted")).toBeVisible();
}

async function deleteMedicine(page: Page, medicineName: string) {
  await page.goto("/health?tab=medicines");
  await page.getByLabel("Search medicines").fill(medicineName);
  await page.getByRole("button", { name: `Open ${medicineName}` }).click();
  const dialog = page.getByRole("dialog", { name: "Edit medicine" });
  await dialog.getByRole("button", { name: "Delete" }).click();
  const confirm = page.getByRole("dialog", { name: "Delete medicine?" });
  await confirm.getByRole("button", { name: "Delete medicine" }).click();
  await expect(page.getByText("Medicine deleted")).toBeVisible();
}

/**
 * Critical single-user Health journey against the full stack: sign in, create a
 * disposable public Inventory item, create a disease, create a medicine with a
 * category, posology, prescription flag, primary image, disease association, and
 * Inventory link, verify the symmetric association from the disease side, delete
 * the referenced Inventory item through the privacy-neutral impact dialog, verify
 * the medicine link is cleared, then delete the safe test data.
 *
 * Skipped without seeded credentials (SEGARIS_E2E_USERNAME /
 * SEGARIS_E2E_PASSWORD), matching the other end-to-end specs. The environment
 * must expose at least one Inventory supplier so the disposable item can be
 * created.
 */
test.describe("health critical journey", () => {
  test.skip(
    !username || !password,
    "Requires a running backend and a seeded user (SEGARIS_E2E_USERNAME / SEGARIS_E2E_PASSWORD) with at least one supplier.",
  );

  test("creates linked health records, clears item links on deletion, and cleans up", async ({
    page,
  }) => {
    const suffix = Date.now().toString(36);
    const itemName = `E2E health item ${suffix}`;
    const diseaseName = `E2E migraine ${suffix}`;
    const medicineName = `E2E ibuprofen ${suffix}`;
    const photoName = `health-${suffix}.png`;

    await signIn(page);
    await createPublicInventoryItem(page, itemName);

    await page.goto("/");
    const modules = page.getByRole("region", { name: "Available modules" });
    await modules.getByRole("button", { name: /Health/i }).click();
    await expect(page.getByRole("heading", { name: "Health" })).toBeVisible();

    await page.getByLabel("Search diseases").fill("nothing-matches-xyz");
    await expect(
      page.getByText("No diseases match the current filters."),
    ).toBeVisible();
    await page.getByLabel("Search diseases").fill("");

    await createDisease(page, diseaseName);
    await createMedicine(page, medicineName, diseaseName, itemName, photoName);

    await page.getByLabel("Search medicines").fill(medicineName);
    await expect(
      page.getByRole("button", { name: `Open ${medicineName}` }),
    ).toBeVisible();
    await expect(page.getByText("Prescription")).toBeVisible();
    await expect(page.getByText(itemName)).toBeVisible();

    await associateFromDiseaseSide(page, diseaseName, medicineName);

    await page.goto("/inventory");
    await page.getByLabel("Search").fill(itemName);
    await page.getByRole("button", { name: `Open item ${itemName}` }).click();
    const itemDialog = page.getByRole("dialog", { name: "Edit item" });
    await itemDialog.getByRole("button", { name: "Delete item" }).click();
    const confirmItem = page.getByRole("dialog", { name: "Delete this item?" });
    await expect(
      confirmItem.getByText(/linked from 1 recipe ingredient or medicine/),
    ).toBeVisible();
    await confirmItem.getByRole("button", { name: "Delete item" }).click();
    await expect(page.getByText("Item deleted")).toBeVisible();

    await page.goto("/health?tab=medicines");
    await page.getByLabel("Search medicines").fill(medicineName);
    await page.getByRole("button", { name: `Open ${medicineName}` }).click();
    const clearedMedicine = page.getByRole("dialog", { name: "Edit medicine" });
    await expect(clearedMedicine.getByText(itemName)).toHaveCount(0);
    await expect(
      clearedMedicine.getByRole("button", { name: "Select item" }),
    ).toBeVisible();
    await expect(clearedMedicine.getByText(diseaseName)).toBeVisible();
    await clearedMedicine.getByRole("button", { name: "Cancel" }).click();

    await deleteMedicine(page, medicineName);
    await deleteDisease(page, diseaseName);
  });
});
