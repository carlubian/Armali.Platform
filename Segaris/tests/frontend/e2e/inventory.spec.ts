import { expect, test } from "@playwright/test";

const username = process.env.SEGARIS_E2E_USERNAME;
const password = process.env.SEGARIS_E2E_PASSWORD;

/**
 * Critical single-user Inventory journey against the full stack: sign in, open
 * the module, filter, create an active item allowed for every supplier, adjust
 * its stock through the quick popup, create an active order with one line,
 * receive the order, confirm the received stock, and delete the safe test data
 * (order first, because an item referenced by any order cannot be deleted).
 *
 * It is skipped without seeded credentials (SEGARIS_E2E_USERNAME /
 * SEGARIS_E2E_PASSWORD), matching the other end-to-end specs. The environment
 * must also expose at least one Configuration supplier and currency so an item
 * and an order can be created. The second-user privacy journey is deferred until
 * multi-account E2E infrastructure exists; see ROADMAP.md.
 */
test.describe("inventory critical journey", () => {
  test.skip(
    !username || !password,
    "Requires a running backend and a seeded user (SEGARIS_E2E_USERNAME / SEGARIS_E2E_PASSWORD) with at least one supplier and currency in Configuration.",
  );

  test("creates an item, adjusts stock, orders and receives it, then cleans up", async ({
    page,
  }) => {
    const itemName = `E2E item ${Date.now().toString(36)}`;

    // Sign in.
    await page.goto("/login");
    await page.getByLabel("Username").fill(username!);
    await page.getByLabel("Password").fill(password!);
    await page.getByRole("button", { name: "Sign in" }).click();

    // Open Inventory from the launcher; the items view is the module entry point.
    const modules = page.getByRole("region", { name: "Available modules" });
    await modules.getByRole("button", { name: /Inventory/i }).click();
    await expect(
      page.getByRole("heading", { name: "Inventory" }),
    ).toBeVisible();
    await expect(page.getByRole("button", { name: "New item" })).toBeVisible();

    // Exercise a filter and clear it so list state round-trips without a reload.
    await page.getByLabel("Search").fill("nothing-matches-xyz");
    await expect(
      page.getByText("No items match the current filters."),
    ).toBeVisible();
    await page.getByLabel("Search").fill("");
    await expect(
      page.getByRole("button", { name: /Remove Search/ }),
    ).toHaveCount(0);

    // Create an active item allowed for every supplier so it is eligible for the
    // order created below regardless of the default supplier.
    await page.getByRole("button", { name: "New item" }).click();
    const itemDialog = page.getByRole("dialog", { name: "New item" });
    await itemDialog.getByLabel("Name").fill(itemName);
    await itemDialog.getByLabel("Status").selectOption("Active");

    const supplierBoxes = itemDialog.locator('input[type="checkbox"]');
    const supplierCount = await supplierBoxes.count();
    expect(supplierCount).toBeGreaterThan(0);
    for (let index = 0; index < supplierCount; index += 1) {
      await supplierBoxes.nth(index).check();
    }

    await itemDialog
      .getByRole("button", { name: "Create", exact: true })
      .click();
    await expect(page.getByText("Item created")).toBeVisible();

    // Quick stock adjustment: add five units (0 -> 5).
    await page
      .getByRole("button", { name: `Adjust stock for ${itemName}` })
      .click();
    const adjustDialog = page.getByRole("dialog", {
      name: "Quick stock adjustment",
    });
    await adjustDialog.getByLabel("Quantity").fill("5");
    await adjustDialog.getByRole("button", { name: "Apply" }).click();
    await expect(page.getByText("Stock updated")).toBeVisible();

    // Switch to Orders and create an active order with a single line for the item.
    await page.getByRole("tab", { name: "Orders" }).click();
    await page.getByRole("button", { name: "New order" }).click();
    const orderDialog = page.getByRole("dialog", { name: "New order" });
    await orderDialog.getByLabel("Status").selectOption("Active");

    // The eligible-item list loads asynchronously from the selected supplier.
    const itemSelect = orderDialog.getByLabel("Item", { exact: true });
    await expect(
      itemSelect.locator("option", { hasText: itemName }),
    ).toHaveCount(1);
    await itemSelect.selectOption({ label: itemName });
    await orderDialog.getByLabel("Quantity").fill("3");
    await orderDialog.getByLabel("Line total").fill("12.00");

    await orderDialog
      .getByRole("button", { name: "Create", exact: true })
      .click();
    await expect(page.getByText("Order created")).toBeVisible();

    // Reopen the newest order (default sort puts it first) and receive it.
    await page
      .getByRole("button", { name: /Open order from/ })
      .first()
      .click();
    const editOrder = page.getByRole("dialog", { name: "Edit order" });
    await editOrder
      .getByRole("button", { name: "Receive", exact: true })
      .click();
    const confirmReceive = page.getByRole("dialog", {
      name: "Receive this order?",
    });
    await confirmReceive.getByRole("button", { name: "Receive order" }).click();
    await expect(page.getByText("Order received")).toBeVisible();

    // Clean up: delete the order first. An item referenced by any order cannot be
    // deleted, and deleting the order leaves the received stock untouched.
    await page
      .getByRole("button", { name: /Open order from/ })
      .first()
      .click();
    const reopenOrder = page.getByRole("dialog", { name: "Edit order" });
    await reopenOrder.getByRole("button", { name: "Delete order" }).click();
    const confirmOrderDelete = page.getByRole("dialog", {
      name: "Delete this order?",
    });
    await confirmOrderDelete
      .getByRole("button", { name: "Delete order" })
      .click();
    await expect(page.getByText("Order deleted")).toBeVisible();

    // Back to Items: the received line quantity raised stock to 8 (5 + 3). Confirm
    // it, then delete the now-unreferenced item.
    await page.getByRole("tab", { name: "Items" }).click();
    await page.getByRole("button", { name: `Open item ${itemName}` }).click();
    const editItem = page.getByRole("dialog", { name: "Edit item" });
    await expect(editItem.getByLabel("Current stock")).toHaveValue("8");
    await editItem.getByRole("button", { name: "Delete item" }).click();
    const confirmItemDelete = page.getByRole("dialog", {
      name: "Delete this item?",
    });
    await confirmItemDelete
      .getByRole("button", { name: "Delete item" })
      .click();
    await expect(page.getByText("Item deleted")).toBeVisible();
    await expect(
      page.getByRole("button", { name: `Open item ${itemName}` }),
    ).toHaveCount(0);
  });
});
