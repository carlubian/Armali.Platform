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

async function createRecipe(
  page: Page,
  recipeName: string,
  ingredientName: string,
  options: { linkedItem?: string; photoName?: string } = {},
) {
  await page.goto("/recipes");
  await expect(page.getByRole("heading", { name: "Recipes" })).toBeVisible();
  await page.getByRole("button", { name: "New recipe" }).click();

  const dialog = page.getByRole("dialog", { name: "New recipe" });
  await dialog.getByLabel("Name").fill(recipeName);
  await dialog.getByLabel("Category").selectOption({ label: "Main" });
  await dialog.getByLabel("Difficulty").selectOption("Easy");
  await dialog.getByLabel("Servings").fill("4");
  await dialog.getByLabel("Prep time").fill("10");
  await dialog.getByLabel("Cook time").fill("20");

  await dialog.getByRole("button", { name: "Add ingredient" }).click();
  await dialog.getByLabel("Ingredient").fill(ingredientName);
  await dialog.getByLabel("Quantity").fill("2");

  if (options.linkedItem != null) {
    await dialog.getByRole("button", { name: "Link inventory item" }).click();
    const selector = page.getByRole("dialog", {
      name: /Select inventory item/,
    });
    await selector
      .getByLabel("Search inventory items")
      .fill(options.linkedItem);
    const row = selector.getByRole("row", {
      name: new RegExp(options.linkedItem),
    });
    await expect(row).toBeVisible();
    await row.getByRole("button", { name: "Select" }).click();
    await expect(dialog.getByText(options.linkedItem)).toBeVisible();
  }

  await dialog.getByRole("button", { name: "Add step" }).click();
  await dialog.getByLabel("Instruction").fill("Prepare and cook gently.");

  if (options.photoName != null) {
    await dialog.locator('input[type="file"]').setInputFiles({
      name: options.photoName,
      mimeType: "image/png",
      buffer: photo,
    });
    await expect(dialog.getByText(options.photoName)).toBeVisible();
  }

  await dialog.getByRole("button", { name: "Create recipe" }).click();

  if (options.photoName != null) {
    const uploadDialog = page.getByRole("dialog", {
      name: "Upload recipe attachments",
    });
    await expect(uploadDialog.getByText(options.photoName)).toBeVisible();
    await uploadDialog
      .getByRole("button", { name: "Make primary image" })
      .click();
    await expect(uploadDialog.getByText("Primary")).toBeVisible();
    await uploadDialog.getByRole("button", { name: "Done" }).click();
  }

  await expect(page.getByText("Recipe created")).toBeVisible();
}

async function addRecipeToFirstMenuSlot(page: Page, recipeName: string) {
  const menuDialog = page.getByRole("dialog", { name: "Plan a week" });
  await menuDialog.getByRole("button", { name: "Add" }).first().click();
  const selector = page.getByRole("dialog", { name: /Select recipe/ });
  await selector.getByLabel("Search recipes").fill(recipeName);
  const row = selector.getByRole("row", { name: new RegExp(recipeName) });
  await expect(row).toBeVisible();
  await row.getByRole("button", { name: "Select" }).click();
  await expect(menuDialog.getByText(recipeName)).toBeVisible();
}

async function deleteRecipe(page: Page, recipeName: string) {
  await page.goto("/recipes");
  await page.getByLabel("Search recipes").fill(recipeName);
  await expect(
    page.getByRole("button", { name: `Open ${recipeName}` }),
  ).toBeVisible();
  await page.getByRole("button", { name: `Open ${recipeName}` }).click();
  const dialog = page.getByRole("dialog", { name: "Edit recipe" });
  await dialog.getByRole("button", { name: "Delete" }).click();
  const confirm = page.getByRole("dialog", { name: "Delete recipe?" });
  await confirm.getByRole("button", { name: "Delete recipe" }).click();
  await expect(page.getByText("Recipe deleted")).toBeVisible();
}

/**
 * Critical single-user Recipes journey against the full stack: sign in, create a
 * disposable public Inventory item, create two recipes (one with an ingredient
 * linked to that item plus a staged primary image), filter the gallery, plan a
 * weekly menu with both recipes in one slot, navigate weeks, delete the menu,
 * delete the referenced item through the privacy-neutral impact dialog, verify
 * the recipe ingredient link is cleared, and delete the safe test data.
 *
 * Skipped without seeded credentials (SEGARIS_E2E_USERNAME /
 * SEGARIS_E2E_PASSWORD), matching the other end-to-end specs. The environment
 * must also expose at least one Inventory supplier so the disposable item can be
 * created.
 */
test.describe("recipes critical journey", () => {
  test.skip(
    !username || !password,
    "Requires a running backend and a seeded user (SEGARIS_E2E_USERNAME / SEGARIS_E2E_PASSWORD) with at least one supplier.",
  );

  test("creates linked recipes, plans a menu, clears item links on deletion, and cleans up", async ({
    page,
  }) => {
    const suffix = Date.now().toString(36);
    const itemName = `E2E recipe item ${suffix}`;
    const linkedRecipe = `E2E linked recipe ${suffix}`;
    const sideRecipe = `E2E side recipe ${suffix}`;
    const menuName = `E2E menu ${suffix}`;
    const photoName = `recipe-${suffix}.png`;

    await signIn(page);
    await createPublicInventoryItem(page, itemName);

    await page.goto("/");
    const modules = page.getByRole("region", { name: "Available modules" });
    await modules.getByRole("button", { name: /Recipes/i }).click();
    await expect(page.getByRole("heading", { name: "Recipes" })).toBeVisible();

    await page.getByLabel("Search recipes").fill("nothing-matches-xyz");
    await expect(
      page.getByText("No recipes match the current filters."),
    ).toBeVisible();
    await page.getByLabel("Search recipes").fill("");
    await expect(
      page.getByRole("button", { name: "New recipe" }),
    ).toBeVisible();

    await createRecipe(page, linkedRecipe, "Linked eggs", {
      linkedItem: itemName,
      photoName,
    });
    await createRecipe(page, sideRecipe, "Side salad");

    await page.getByLabel("Search recipes").fill(linkedRecipe);
    await expect(
      page.getByRole("button", { name: `Open ${linkedRecipe}` }),
    ).toBeVisible();
    await page.getByRole("button", { name: `Open ${linkedRecipe}` }).click();
    const recipeDialog = page.getByRole("dialog", { name: "Edit recipe" });
    await expect(recipeDialog.getByText(itemName)).toBeVisible();
    await expect(recipeDialog.getByText("Primary")).toBeVisible();
    await recipeDialog.getByRole("button", { name: "Cancel" }).click();

    await page.getByRole("link", { name: "Menu planner" }).click();
    await expect(
      page.getByRole("heading", { name: "Menu planner" }),
    ).toBeVisible();
    await page.getByRole("button", { name: "Next week" }).click();
    await expect(page).toHaveURL(/week=/);
    await page.getByRole("button", { name: "Previous week" }).click();

    await page.getByRole("button", { name: "New menu" }).click();
    const menuDialog = page.getByRole("dialog", { name: "Plan a week" });
    await menuDialog.getByLabel("Name").fill(menuName);
    await addRecipeToFirstMenuSlot(page, linkedRecipe);
    await addRecipeToFirstMenuSlot(page, sideRecipe);
    await menuDialog.getByRole("button", { name: "Create menu" }).click();
    await expect(page.getByText(linkedRecipe)).toBeVisible();
    await expect(page.getByText(sideRecipe)).toBeVisible();

    await page.getByRole("button", { name: "Edit menu" }).click();
    const editMenu = page.getByRole("dialog", { name: "Weekly menu" });
    await editMenu.getByRole("button", { name: "Delete menu" }).click();
    const confirmMenu = page.getByRole("dialog", { name: "Delete this menu?" });
    await confirmMenu.getByRole("button", { name: "Delete menu" }).click();
    await expect(page.getByText("No menu for this week")).toBeVisible();

    await page.goto("/inventory");
    await page.getByLabel("Search").fill(itemName);
    await expect(
      page.getByRole("button", { name: `Open item ${itemName}` }),
    ).toBeVisible();
    await page.getByRole("button", { name: `Open item ${itemName}` }).click();
    const itemDialog = page.getByRole("dialog", { name: "Edit item" });
    await itemDialog.getByRole("button", { name: "Delete item" }).click();
    const confirmItem = page.getByRole("dialog", { name: "Delete this item?" });
    await expect(
      confirmItem.getByText(/linked from 1 recipe ingredient/),
    ).toBeVisible();
    await confirmItem.getByRole("button", { name: "Delete item" }).click();
    await expect(page.getByText("Item deleted")).toBeVisible();

    await page.goto("/recipes");
    await page.getByLabel("Search recipes").fill(linkedRecipe);
    await page.getByRole("button", { name: `Open ${linkedRecipe}` }).click();
    const clearedRecipe = page.getByRole("dialog", { name: "Edit recipe" });
    await expect(clearedRecipe.getByText(itemName)).toHaveCount(0);
    await expect(
      clearedRecipe.getByRole("button", { name: "Link inventory item" }),
    ).toBeVisible();
    await clearedRecipe.getByRole("button", { name: "Cancel" }).click();

    await deleteRecipe(page, linkedRecipe);
    await deleteRecipe(page, sideRecipe);
  });
});
