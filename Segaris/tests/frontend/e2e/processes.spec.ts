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

async function createProcessCategory(page: Page, name: string) {
  await page.goto("/configuration/processes");
  await expect(
    page.getByRole("heading", { level: 1, name: "Configuration" }),
  ).toBeVisible();
  await page.getByRole("button", { name: "New category" }).click();
  const dialog = page.getByRole("dialog", { name: "New process category" });
  await dialog.getByLabel("Name").fill(name);
  await dialog.getByRole("button", { name: "Create" }).click();
  await expect(page.getByText("Added")).toBeVisible();
  await expect(page.getByRole("cell", { name })).toBeVisible();
}

async function deleteProcessCategory(page: Page, name: string) {
  await page.goto("/configuration/processes");
  await page.getByRole("button", { name: `Delete ${name}` }).click();
  const dialog = page.getByRole("dialog", { name: `Delete ${name}?` });
  await dialog.getByRole("button", { name: "Delete" }).click();
  await expect(page.getByText("Removed")).toBeVisible();
  await expect(page.getByRole("cell", { name })).toHaveCount(0);
}

/**
 * Representative Processes journey against the full stack: sign in, create safe
 * process categories through Configuration, open Processes, exercise table
 * filtering, create a process with a global due date and attachment, add steps,
 * complete/skip/undo them in frontier order, cancel and reopen the process,
 * replace its referenced category in Configuration, then delete all disposable
 * process and category data.
 *
 * Skipped without seeded administrator credentials because the category
 * replacement path uses Configuration. Public/private browser privacy remains
 * covered by API and component suites until multi-account Playwright
 * infrastructure is available.
 */
test.describe("processes critical journey", () => {
  test.skip(
    !username || !password,
    "Requires a running backend and a seeded administrator (SEGARIS_E2E_USERNAME / SEGARIS_E2E_PASSWORD).",
  );

  test("manages a process, its steps, attachments, and category replacement", async ({
    page,
  }) => {
    await page.setViewportSize({ width: 1024, height: 720 });
    const suffix = Date.now().toString(36);
    const sourceCategory = `E2E process source ${suffix}`;
    const replacementCategory = `E2E process replacement ${suffix}`;
    const processName = `E2E process ${suffix}`;
    const processDueDate = "2031-04-15";
    const firstStep = `Collect documents ${suffix}`;
    const optionalStep = `Optional appointment ${suffix}`;
    const attachmentName = `process-${suffix}.txt`;

    await signIn(page);
    await createProcessCategory(page, sourceCategory);
    await createProcessCategory(page, replacementCategory);

    await page.goto("/");
    await page
      .getByRole("region", { name: "Available modules" })
      .getByRole("button", { name: /Processes/i })
      .click();
    await expect(
      page.getByRole("heading", { level: 1, name: "Processes" }),
    ).toBeVisible();

    await page.getByLabel("Search").fill("nothing-matches-xyz");
    await expect(
      page.getByText("No processes match the current filters."),
    ).toBeVisible();
    await page.getByLabel("Search").fill("");

    await page.getByRole("button", { name: "New process" }).click();
    const createDialog = page.getByRole("dialog", { name: "New process" });
    await createDialog.getByLabel("Name").fill(processName);
    await createDialog
      .getByLabel("Category")
      .selectOption({ label: sourceCategory });
    await createDialog.getByLabel("Global due date").fill(processDueDate);
    await createDialog
      .getByLabel("Notes")
      .fill(`Created by Processes E2E ${suffix}`);
    await createDialog.locator('input[type="file"]').setInputFiles({
      name: attachmentName,
      mimeType: "text/plain",
      buffer: Buffer.from("Processes E2E attachment"),
    });
    await expect(createDialog.getByText(attachmentName)).toBeVisible();
    await createDialog.getByRole("button", { name: "Create process" }).click();

    const uploadDialog = page.getByRole("dialog", {
      name: "Upload attachments",
    });
    await expect(uploadDialog.getByText(attachmentName)).toBeVisible();
    await uploadDialog.getByRole("button", { name: "Done" }).click();
    await expect(page.getByText("Process created")).toBeVisible();

    await page.getByLabel("Search").fill(processName);
    await expect(
      page.getByRole("button", { name: `Open process ${processName}` }),
    ).toBeVisible();
    await expect(
      page.getByRole("cell", { name: sourceCategory }),
    ).toBeVisible();

    await page.getByRole("button", { name: "Step timeline" }).click();
    const stepsDialog = page.getByRole("dialog", { name: "Step timeline" });
    await stepsDialog.getByRole("button", { name: "Add step" }).click();
    await stepsDialog.getByRole("button", { name: "Add step" }).click();
    const descriptions = stepsDialog.getByLabel("Description");
    await descriptions.nth(0).fill(firstStep);
    await descriptions.nth(1).fill(optionalStep);
    await stepsDialog.getByLabel("Due date").nth(0).fill("2031-03-01");
    await stepsDialog.getByLabel("Due date").nth(1).fill("2031-03-15");
    await stepsDialog.getByLabel("Notes").nth(1).fill("Optional E2E step");
    await stepsDialog.getByLabel("Optional").nth(1).check();
    await stepsDialog.getByRole("button", { name: "Save step order" }).click();
    await expect(stepsDialog.getByText("0 of 2 steps resolved")).toBeVisible();

    await stepsDialog.getByRole("button", { name: "Complete" }).first().click();
    await expect(stepsDialog.getByText("1 of 2 steps resolved")).toBeVisible();
    await stepsDialog.getByRole("button", { name: "Skip" }).nth(1).click();
    await expect(stepsDialog.getByText("2 of 2 steps resolved")).toBeVisible();
    await stepsDialog.getByRole("button", { name: "Undo" }).nth(1).click();
    await expect(stepsDialog.getByText("1 of 2 steps resolved")).toBeVisible();
    await stepsDialog.getByRole("button", { name: "Close" }).last().click();

    await expect(
      page.getByRole("button", { name: `Open process ${processName}` }),
    ).toBeVisible();
    await page
      .getByRole("button", { name: `Open process ${processName}` })
      .click();
    const editDialog = page.getByRole("dialog", { name: "Edit process" });
    await expect(editDialog.getByText(attachmentName)).toBeVisible();
    await editDialog.getByRole("button", { name: "Cancel process" }).click();
    await expect(page.getByText("Process cancelled")).toBeVisible();
    await expect(
      editDialog.getByRole("button", { name: "Reopen process" }),
    ).toBeVisible();
    await editDialog.getByRole("button", { name: "Reopen process" }).click();
    await expect(page.getByText("Process reopened")).toBeVisible();
    await editDialog
      .getByRole("button", { name: "Cancel", exact: true })
      .last()
      .click();

    await page.getByRole("button", { name: "More filters" }).click();
    await page.getByLabel("Status").selectOption({ label: "In progress" });
    await expect(
      page.getByRole("button", { name: `Open process ${processName}` }),
    ).toBeVisible();

    await page.goto("/configuration/processes");
    await page
      .getByRole("button", { name: `Delete ${sourceCategory}` })
      .click();
    const replaceDialog = page.getByRole("dialog", {
      name: `Remove ${sourceCategory}`,
    });
    await replaceDialog
      .getByRole("radio", { name: "Move them to another value" })
      .check();
    await replaceDialog
      .getByLabel("Replacement")
      .selectOption({ label: replacementCategory });
    await replaceDialog.getByRole("button", { name: "Delete" }).click();
    await expect(page.getByText("Removed")).toBeVisible();
    await expect(page.getByRole("cell", { name: sourceCategory })).toHaveCount(
      0,
    );

    await page.goto("/processes");
    await page.getByLabel("Search").fill(processName);
    await expect(
      page.getByRole("cell", { name: replacementCategory }),
    ).toBeVisible();
    await page
      .getByRole("button", { name: `Open process ${processName}` })
      .click();
    const cleanupProcess = page.getByRole("dialog", { name: "Edit process" });
    await cleanupProcess
      .getByRole("button", { name: "Delete process" })
      .click();
    await page
      .getByRole("dialog", { name: "Delete this process?" })
      .getByRole("button", { name: "Delete process" })
      .click();
    await expect(page.getByText("Process deleted")).toBeVisible();
    await expect(
      page.getByRole("button", { name: `Open process ${processName}` }),
    ).toHaveCount(0);

    await deleteProcessCategory(page, replacementCategory);
  });
});
