import { expect, test, type Page } from "@playwright/test";

const username = process.env.SEGARIS_E2E_USERNAME;
const password = process.env.SEGARIS_E2E_PASSWORD;

async function signIn(page: Page) {
  await page.goto("/login");
  await page.getByLabel("Username").fill(username!);
  await page.getByLabel("Password").fill(password!);
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(
    page.getByRole("heading", { name: "Choose a module" }),
  ).toBeVisible();
}

async function createCatalogValue(
  page: Page,
  url: string,
  addAction: string,
  dialogTitle: string,
  name: string,
) {
  await page.goto(url);
  await expect(
    page.getByRole("heading", { level: 1, name: "Configuration" }),
  ).toBeVisible();
  await page.getByRole("button", { name: addAction }).click();
  const dialog = page.getByRole("dialog", { name: dialogTitle });
  await dialog.getByLabel("Name").fill(name);
  await dialog.getByRole("button", { name: "Create" }).click();
  await expect(page.getByText("Added")).toBeVisible();
  await expect(page.getByRole("cell", { name })).toBeVisible();
}

async function replaceCatalogValue(
  page: Page,
  url: string,
  source: string,
  replacement: string,
) {
  await page.goto(url);
  await page.getByRole("button", { name: `Delete ${source}` }).click();
  const dialog = page.getByRole("dialog", { name: `Remove ${source}` });
  await dialog
    .getByRole("radio", { name: "Move them to another value" })
    .check();
  await dialog.getByLabel("Replacement").selectOption({ label: replacement });
  await dialog.getByRole("button", { name: "Delete" }).click();
  await expect(page.getByText("Removed")).toBeVisible();
  await expect(page.getByRole("cell", { name: source })).toHaveCount(0);
}

async function deleteCatalogValue(page: Page, url: string, name: string) {
  await page.goto(url);
  await page.getByRole("button", { name: `Delete ${name}` }).click();
  const dialog = page.getByRole("dialog", { name: `Delete ${name}?` });
  await dialog.getByRole("button", { name: "Delete" }).click();
  await expect(page.getByText("Removed")).toBeVisible();
  await expect(page.getByRole("cell", { name })).toHaveCount(0);
}

async function openFirebird(page: Page) {
  await page.goto("/");
  await page
    .getByRole("region", { name: "Available modules" })
    .getByRole("button", { name: /People/i })
    .click();
  await expect(
    page.getByRole("heading", { level: 1, name: "People" }),
  ).toBeVisible();
}

const personCategoriesUrl = "/configuration/firebird?catalog=person-categories";
const usernamePlatformsUrl =
  "/configuration/firebird?catalog=username-platforms";

/**
 * Representative Firebird journey against the full stack: sign in, create safe
 * Firebird category/platform catalog values through Configuration, open the
 * People gallery, create a person with status, birthday, and avatar, add a
 * username and interaction through URL-aware popups, exercise gallery
 * filtering/sorting, replace referenced category and platform values, then
 * delete all disposable person and catalog data.
 *
 * Skipped without seeded administrator credentials because the catalog
 * replacement paths use Configuration. Browser-level second-user privacy remains
 * covered by API and component suites until multi-account Playwright
 * infrastructure is available.
 */
test.describe("firebird critical journey", () => {
  test.skip(
    !username || !password,
    "Requires a running backend and a seeded administrator (SEGARIS_E2E_USERNAME / SEGARIS_E2E_PASSWORD).",
  );

  test("manages a person, sub-entities, avatar, filters, and catalog replacements", async ({
    page,
  }) => {
    await page.setViewportSize({ width: 940, height: 720 });
    const suffix = Date.now().toString(36);
    const sourceCategory = `E2E person source ${suffix}`;
    const replacementCategory = `E2E person replacement ${suffix}`;
    const sourcePlatform = `E2E platform source ${suffix}`;
    const replacementPlatform = `E2E platform replacement ${suffix}`;
    const personName = `E2E person ${suffix}`;
    const usernameValue = `e2e-${suffix}@example.test`;
    const interactionDescription = `E2E coffee chat ${suffix}`;

    await signIn(page);
    await createCatalogValue(
      page,
      personCategoriesUrl,
      "New person category",
      "New person category",
      sourceCategory,
    );
    await createCatalogValue(
      page,
      personCategoriesUrl,
      "New person category",
      "New person category",
      replacementCategory,
    );
    await createCatalogValue(
      page,
      usernamePlatformsUrl,
      "New username platform",
      "New username platform",
      sourcePlatform,
    );
    await createCatalogValue(
      page,
      usernamePlatformsUrl,
      "New username platform",
      "New username platform",
      replacementPlatform,
    );

    await openFirebird(page);
    await page.getByLabel("Search").fill("nothing-matches-firebird-e2e");
    await expect(
      page.getByText("No people match your search and filters."),
    ).toBeVisible();
    await page.getByRole("button", { name: "Clear filters" }).click();

    await page.getByRole("button", { name: "New person" }).click();
    const createDialog = page.getByRole("dialog", { name: "New person" });
    await createDialog.getByLabel("Name").fill(personName);
    await createDialog
      .getByLabel("Category")
      .selectOption({ label: sourceCategory });
    await createDialog.getByLabel("Status").selectOption("Active");
    await createDialog.getByLabel("Has a birthday").check();
    await createDialog.getByLabel("Month").selectOption("2");
    await createDialog.getByLabel("Day").selectOption("29");
    await createDialog
      .getByLabel("Notes")
      .fill(`Created by Firebird E2E ${suffix}`);
    await createDialog.locator('input[type="file"]').setInputFiles({
      name: `firebird-${suffix}.png`,
      mimeType: "image/png",
      buffer: Buffer.from(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAFgwJ/l9q3XwAAAABJRU5ErkJggg==",
        "base64",
      ),
    });
    await createDialog.getByRole("button", { name: "Create person" }).click();
    await expect(page.getByText("Person created")).toBeVisible();

    await page.getByLabel("Search").fill(personName);
    await expect(
      page.getByRole("button", { name: `Edit ${personName}` }),
    ).toBeVisible();
    await expect(page.getByText(sourceCategory)).toBeVisible();
    await expect(page.getByText("Active")).toBeVisible();

    await page.getByRole("button", { name: `Edit ${personName}` }).click();
    const editDialog = page.getByRole("dialog", { name: "Edit person" });
    await editDialog.getByRole("button", { name: "Manage usernames" }).click();
    const usernamesDialog = page.getByRole("dialog", { name: "Usernames" });
    await usernamesDialog.getByRole("button", { name: "Add username" }).click();
    const usernameDialog = page.getByRole("dialog", { name: "New username" });
    await usernameDialog
      .getByLabel("Platform")
      .selectOption({ label: sourcePlatform });
    await usernameDialog.getByLabel("Value").fill(usernameValue);
    await usernameDialog.getByLabel("Notes").fill("Disposable E2E username");
    await usernameDialog.getByRole("button", { name: "Save changes" }).click();
    await expect(usernamesDialog.getByText(usernameValue)).toBeVisible();
    await expect(usernamesDialog.getByText(sourcePlatform)).toBeVisible();
    await usernamesDialog.getByRole("button", { name: "Close" }).last().click();

    const reopenedEdit = page.getByRole("dialog", { name: "Edit person" });
    await reopenedEdit.getByRole("button", { name: "Open log" }).click();
    const interactionsDialog = page.getByRole("dialog", {
      name: "Interactions",
    });
    await interactionsDialog
      .getByRole("button", { name: "Add interaction" })
      .click();
    const interactionDialog = page.getByRole("dialog", {
      name: "New interaction",
    });
    await interactionDialog.getByLabel("Date").fill("2026-06-21");
    await interactionDialog
      .getByLabel("Description")
      .fill(interactionDescription);
    await interactionDialog
      .getByRole("button", { name: "Save changes" })
      .click();
    await expect(
      interactionsDialog.getByText(interactionDescription),
    ).toBeVisible();
    await interactionsDialog
      .getByRole("button", { name: "Close" })
      .last()
      .click();
    await page
      .getByRole("dialog", { name: "Edit person" })
      .getByRole("button", { name: "Cancel" })
      .last()
      .click();

    await page.getByLabel("Category").selectOption({ label: sourceCategory });
    await expect(
      page.getByRole("button", { name: `Edit ${personName}` }),
    ).toBeVisible();
    await page.getByLabel("Sort").selectOption("birthday");
    await expect(page).toHaveURL(/sort=birthday/);

    await replaceCatalogValue(
      page,
      personCategoriesUrl,
      sourceCategory,
      replacementCategory,
    );
    await openFirebird(page);
    await page.getByLabel("Search").fill(personName);
    await expect(page.getByText(replacementCategory)).toBeVisible();

    await replaceCatalogValue(
      page,
      usernamePlatformsUrl,
      sourcePlatform,
      replacementPlatform,
    );
    await openFirebird(page);
    await page.getByLabel("Search").fill(personName);
    await page
      .getByRole("button", { name: `Open usernames for ${personName}` })
      .click();
    const replacedUsernames = page.getByRole("dialog", { name: "Usernames" });
    await expect(replacedUsernames.getByText(usernameValue)).toBeVisible();
    await expect(
      replacedUsernames.getByText(replacementPlatform),
    ).toBeVisible();
    await replacedUsernames
      .getByRole("button", { name: "Close" })
      .last()
      .click();

    await page.getByRole("button", { name: `Edit ${personName}` }).click();
    const cleanupPerson = page.getByRole("dialog", { name: "Edit person" });
    await cleanupPerson.getByRole("button", { name: "Delete person" }).click();
    await page
      .getByRole("dialog", { name: "Delete person?" })
      .getByRole("button", { name: "Delete" })
      .click();
    await expect(page.getByText("Person deleted")).toBeVisible();
    await expect(
      page.getByRole("button", { name: `Edit ${personName}` }),
    ).toHaveCount(0);

    await deleteCatalogValue(page, personCategoriesUrl, replacementCategory);
    await deleteCatalogValue(page, usernamePlatformsUrl, replacementPlatform);
  });
});
