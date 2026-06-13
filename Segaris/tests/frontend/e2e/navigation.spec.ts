import { expect, test } from "@playwright/test";

const username = process.env.SEGARIS_E2E_USERNAME;
const password = process.env.SEGARIS_E2E_PASSWORD;

test.describe("shared shell navigation", () => {
  test.skip(
    !username || !password,
    "Requires a running backend and a seeded administrator (SEGARIS_E2E_USERNAME / SEGARIS_E2E_PASSWORD).",
  );

  test("moves through the launcher, profile, administration, and sign out", async ({
    page,
  }) => {
    await page.goto("/login");
    await page.getByLabel("Username").fill(username!);
    await page.getByLabel("Password").fill(password!);
    await page.getByRole("button", { name: "Sign in" }).click();

    const modules = page.getByRole("region", { name: "Available modules" });
    await modules.getByRole("button", { name: /My profile/i }).click();
    await expect(
      page.getByRole("heading", { name: "My profile" }),
    ).toBeVisible();
    await page.getByRole("button", { name: "Launcher" }).click();

    await modules.getByRole("button", { name: /Household users/i }).click();
    await expect(
      page.getByRole("heading", { name: "Household users" }),
    ).toBeVisible();
    await page.getByRole("button", { name: "Launcher" }).click();

    await page.getByRole("button", { name: "Sign out" }).click();
    await expect(
      page.getByRole("heading", { name: "Welcome home" }),
    ).toBeVisible();
  });
});
