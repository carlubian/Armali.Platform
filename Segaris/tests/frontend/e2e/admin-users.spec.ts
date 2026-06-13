import { expect, test } from "@playwright/test";

const username = process.env.SEGARIS_E2E_USERNAME;
const password = process.env.SEGARIS_E2E_PASSWORD;

test.describe("administrative user management", () => {
  test.skip(
    !username || !password,
    "Requires a running backend and a seeded administrator (SEGARIS_E2E_USERNAME / SEGARIS_E2E_PASSWORD).",
  );

  test("creates, deactivates, and resets a household account", async ({ page }) => {
    await page.goto("/login");
    await page.getByLabel("Username").fill(username!);
    await page.getByLabel("Password").fill(password!);
    await page.getByRole("button", { name: "Sign in" }).click();

    await page.getByRole("button", { name: "Household users" }).click();
    await expect(
      page.getByRole("heading", { name: "Household users" }),
    ).toBeVisible();

    const newUsername = `e2e_${Date.now().toString(36)}`;

    await page.getByRole("button", { name: "New user" }).click();
    const createDialog = page.getByRole("dialog", { name: "New user" });
    await createDialog.getByLabel("Username").fill(newUsername);
    await createDialog
      .getByLabel("Temporary password")
      .fill("TempPassword123!");
    await createDialog.getByRole("button", { name: "Create user" }).click();

    await expect(page.getByText("User created")).toBeVisible();
    const card = page
      .locator(".seg-ucard")
      .filter({ hasText: `@${newUsername}` });
    await expect(card).toBeVisible();

    await card.getByRole("button", { name: "Deactivate" }).click();
    const confirmDialog = page.getByRole("dialog", {
      name: "Deactivate account?",
    });
    await confirmDialog.getByRole("button", { name: "Deactivate" }).click();
    await expect(card.getByText("Inactive")).toBeVisible();

    await card.getByRole("button", { name: "Reset password" }).click();
    const resetDialog = page.getByRole("dialog", { name: "Reset password" });
    await resetDialog.getByLabel("New password").fill("AnotherPass123!");
    await resetDialog
      .getByLabel("Confirm new password")
      .fill("AnotherPass123!");
    await resetDialog.getByRole("button", { name: "Reset password" }).click();

    await expect(page.getByText("Password reset")).toBeVisible();
  });
});
