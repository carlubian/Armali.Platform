import { expect, test } from "@playwright/test";

const username = process.env.SEGARIS_E2E_USERNAME;
const password = process.env.SEGARIS_E2E_PASSWORD;

test.describe("self-service profile", () => {
  test.skip(
    !username || !password,
    "Requires a running backend and seeded credentials (SEGARIS_E2E_USERNAME / SEGARIS_E2E_PASSWORD).",
  );

  test("updates the display name and avatar in the shared shell", async ({
    page,
  }) => {
    await page.goto("/login");
    await page.getByLabel("Username").fill(username!);
    await page.getByLabel("Password").fill(password!);
    await page.getByRole("button", { name: "Sign in" }).click();

    await page.getByRole("button", { name: "My profile" }).click();
    const displayName = page.getByLabel("Display name");
    const originalName = await displayName.inputValue();
    const changedName = `${originalName} E2E`;

    await displayName.fill(changedName);
    await page.getByRole("button", { name: "Save profile" }).click();
    await expect(page.getByRole("img", { name: changedName })).toHaveCount(2);

    await page.getByLabel("Choose a profile photo").setInputFiles({
      name: "profile-e2e.png",
      mimeType: "image/png",
      buffer: Buffer.from(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Y9ZQmcAAAAASUVORK5CYII=",
        "base64",
      ),
    });
    await expect(page.getByAltText(changedName)).toHaveCount(2);

    await page.getByRole("button", { name: "Remove photo" }).click();
    await expect(page.getByRole("img", { name: changedName })).toHaveCount(2);

    await displayName.fill(originalName);
    await page.getByRole("button", { name: "Save profile" }).click();
    await expect(page.getByRole("img", { name: originalName })).toHaveCount(2);
  });
});
