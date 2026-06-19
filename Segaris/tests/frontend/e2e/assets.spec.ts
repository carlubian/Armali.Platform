import { expect, test } from "@playwright/test";

const username = process.env.SEGARIS_E2E_USERNAME;
const password = process.env.SEGARIS_E2E_PASSWORD;

const photo = Buffer.from(
  "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMB/ax2Y9kAAAAASUVORK5CYII=",
  "base64",
);

/**
 * Critical single-user Assets journey against the full stack: sign in, open
 * Assets from the launcher, filter and clear the table, create an asset with
 * category, location, code, and expected end of life, upload a photo and mark it
 * primary, filter by the code, then delete the safe test data.
 *
 * It is skipped without seeded credentials (SEGARIS_E2E_USERNAME /
 * SEGARIS_E2E_PASSWORD), matching the other end-to-end specs. The second-user
 * privacy journey is deferred until multi-account E2E infrastructure exists.
 */
test.describe("assets critical journey", () => {
  test.skip(
    !username || !password,
    "Requires a running backend and a seeded user (SEGARIS_E2E_USERNAME / SEGARIS_E2E_PASSWORD).",
  );

  test("creates an asset with a primary photo, filters it, and deletes it", async ({
    page,
  }) => {
    const suffix = Date.now().toString(36);
    const assetName = `E2E asset ${suffix}`;
    const assetCode = `E2E-ASSET-${suffix}`;
    const photoName = `asset-${suffix}.png`;

    await page.goto("/login");
    await page.getByLabel("Username").fill(username!);
    await page.getByLabel("Password").fill(password!);
    await page.getByRole("button", { name: "Sign in" }).click();

    const modules = page.getByRole("region", { name: "Available modules" });
    await modules.getByRole("button", { name: /Assets/i }).click();
    await expect(page.getByRole("heading", { name: "Assets" })).toBeVisible();
    await expect(page.getByRole("button", { name: "New asset" })).toBeVisible();

    await page.getByLabel("Search").fill("nothing-matches-xyz");
    await expect(
      page.getByText("No assets match the current filters."),
    ).toBeVisible();
    await page.getByLabel("Search").fill("");
    await expect(
      page.getByRole("button", { name: /Remove Search/ }),
    ).toHaveCount(0);

    await page.getByRole("button", { name: "New asset" }).click();
    const createDialog = page.getByRole("dialog", { name: "New asset" });
    await createDialog.getByLabel("Name").fill(assetName);
    await createDialog.getByLabel("Category").selectOption({ index: 0 });
    await createDialog.getByLabel("Location").selectOption({ index: 0 });
    await createDialog.getByLabel("Code").fill(assetCode);
    await createDialog.getByLabel("Expected end of life").fill("2030-06-30");
    await createDialog.locator('input[type="file"]').setInputFiles({
      name: photoName,
      mimeType: "image/png",
      buffer: photo,
    });
    await expect(createDialog.getByText(photoName)).toBeVisible();
    await createDialog.getByRole("button", { name: "Create" }).click();

    const uploadDialog = page.getByRole("dialog", {
      name: "Upload attachments",
    });
    await expect(uploadDialog.getByText(photoName)).toBeVisible();
    await uploadDialog
      .getByRole("button", { name: `Make ${photoName} the primary image` })
      .click();
    await expect(uploadDialog.getByText("Primary image")).toBeVisible();
    await uploadDialog.getByRole("button", { name: "Done" }).click();
    await expect(page.getByText("Asset created")).toBeVisible();

    await page.getByLabel("Search").fill(assetCode);
    await expect(
      page.getByRole("button", { name: `Open asset ${assetName}` }),
    ).toBeVisible();

    await page.getByRole("button", { name: `Open asset ${assetName}` }).click();
    const editDialog = page.getByRole("dialog", { name: "Edit asset" });
    await expect(editDialog.getByLabel("Code")).toHaveValue(assetCode);
    await expect(editDialog.getByText("Primary image")).toBeVisible();

    await editDialog.getByRole("button", { name: "Delete asset" }).click();
    const confirmDelete = page.getByRole("dialog", {
      name: "Delete this asset?",
    });
    await confirmDelete.getByRole("button", { name: "Delete asset" }).click();
    await expect(page.getByText("Asset deleted")).toBeVisible();
    await expect(
      page.getByRole("button", { name: `Open asset ${assetName}` }),
    ).toHaveCount(0);
  });
});
