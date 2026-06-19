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

async function createAsset(
  page: Page,
  name: string,
  code: string,
  expectedEndOfLife = "2031-06-30",
) {
  await page.goto("/assets");
  await expect(page.getByRole("heading", { name: "Assets" })).toBeVisible();
  await page.getByRole("button", { name: "New asset" }).click();

  const dialog = page.getByRole("dialog", { name: "New asset" });
  await dialog.getByLabel("Name").fill(name);
  await dialog.getByLabel("Category").selectOption({ index: 0 });
  await dialog.getByLabel("Location").selectOption({ index: 0 });
  await dialog.getByLabel("Code").fill(code);
  await dialog.getByLabel("Expected end of life").fill(expectedEndOfLife);
  await dialog.getByRole("button", { name: "Create" }).click();

  await expect(page.getByText("Asset created")).toBeVisible();
}

async function deleteVisibleAsset(page: Page, name: string) {
  await page.getByRole("button", { name: `Open asset ${name}` }).click();
  const dialog = page.getByRole("dialog", { name: "Edit asset" });
  await dialog.getByRole("button", { name: "Delete asset" }).click();
  const confirmDelete = page.getByRole("dialog", {
    name: "Delete this asset?",
  });
  await confirmDelete.getByRole("button", { name: "Delete asset" }).click();
  await expect(page.getByText("Asset deleted")).toBeVisible();
}

/**
 * Critical Maintenance journey against the full stack: sign in, create two safe
 * Assets, open Maintenance, exercise filtering, create a task with type,
 * priority, due date, and an Asset link, complete it, verify filtering, delete
 * the referenced Asset through the reassignment dialog, confirm the completed
 * task now points at the replacement Asset, and delete all safe test data.
 *
 * Skipped without seeded credentials (SEGARIS_E2E_USERNAME /
 * SEGARIS_E2E_PASSWORD), matching the other end-to-end specs. Public/private
 * browser privacy remains covered by API/component suites until multi-account
 * Playwright infrastructure is available.
 */
test.describe("maintenance critical journey", () => {
  test.skip(
    !username || !password,
    "Requires a running backend and a seeded user (SEGARIS_E2E_USERNAME / SEGARIS_E2E_PASSWORD).",
  );

  test("creates and completes an asset-linked task, then reassigns it during asset deletion", async ({
    page,
  }) => {
    const suffix = Date.now().toString(36);
    const sourceAsset = `E2E maint source ${suffix}`;
    const replacementAsset = `E2E maint replacement ${suffix}`;
    const sourceCode = `E2E-MAINT-SRC-${suffix}`;
    const replacementCode = `E2E-MAINT-REPL-${suffix}`;
    const taskTitle = `E2E maintenance task ${suffix}`;
    const dueDate = "2031-05-20";

    await signIn(page);
    await createAsset(page, sourceAsset, sourceCode);
    await createAsset(page, replacementAsset, replacementCode);

    const modules = page.getByRole("region", { name: "Available modules" });
    await page.goto("/");
    await modules.getByRole("button", { name: /Maintenance/i }).click();
    await expect(
      page.getByRole("heading", { name: "Maintenance" }),
    ).toBeVisible();
    await expect(page.getByRole("button", { name: "New task" })).toBeVisible();

    await page.getByLabel("Search").fill("nothing-matches-xyz");
    await expect(
      page.getByText("No tasks match the current filters."),
    ).toBeVisible();
    await page.getByLabel("Search").fill("");
    await expect(
      page.getByRole("button", { name: /Remove Search:/ }),
    ).toHaveCount(0);

    await page.getByRole("button", { name: "New task" }).click();
    const createTask = page.getByRole("dialog", {
      name: "New maintenance task",
    });
    await createTask.getByLabel("Title").fill(taskTitle);
    await createTask.getByLabel("Type").selectOption({ index: 0 });
    await createTask.getByLabel("Priority").selectOption({ label: "High" });
    await createTask.getByLabel("Due date").fill(dueDate);
    await createTask.getByLabel("Asset").selectOption({ label: sourceAsset });
    await createTask.getByRole("button", { name: "Create" }).click();
    await expect(page.getByText("Task created")).toBeVisible();

    await page.getByRole("button", { name: `Open task ${taskTitle}` }).click();
    const editTask = page.getByRole("dialog", {
      name: "Edit maintenance task",
    });
    await editTask.getByLabel("Status").selectOption({ label: "Completed" });
    await editTask.getByRole("button", { name: "Save changes" }).click();
    await expect(page.getByText("Task updated")).toBeVisible();

    await page.getByLabel("Search").fill(taskTitle);
    await page.getByRole("button", { name: "More filters" }).click();
    await page.getByLabel("Status").selectOption({ label: "Completed" });
    await page.getByLabel("Priority").selectOption({ label: "High" });
    await expect(
      page.getByRole("button", { name: `Open task ${taskTitle}` }),
    ).toBeVisible();
    await expect(page.getByRole("cell", { name: sourceAsset })).toBeVisible();

    await page.goto("/assets");
    await page.getByLabel("Search").fill(sourceCode);
    await expect(
      page.getByRole("button", { name: `Open asset ${sourceAsset}` }),
    ).toBeVisible();
    await page
      .getByRole("button", { name: `Open asset ${sourceAsset}` })
      .click();
    const sourceEditor = page.getByRole("dialog", { name: "Edit asset" });
    await sourceEditor.getByRole("button", { name: "Delete asset" }).click();
    const reassignDialog = page.getByRole("dialog", {
      name: `Reassign and delete ${sourceAsset}`,
    });
    await expect(
      reassignDialog.getByText(
        "1 maintenance task currently references this asset. The task will be moved to the replacement asset.",
      ),
    ).toBeVisible();
    await reassignDialog
      .getByLabel("Replacement asset")
      .selectOption({ label: `${replacementAsset} (${replacementCode})` });
    await reassignDialog
      .getByRole("button", { name: "Reassign and delete" })
      .click();
    await expect(page.getByText("Asset deleted")).toBeVisible();
    await expect(
      page.getByRole("button", { name: `Open asset ${sourceAsset}` }),
    ).toHaveCount(0);

    await page.goto("/maintenance");
    await page.getByLabel("Search").fill(taskTitle);
    await expect(
      page.getByRole("button", { name: `Open task ${taskTitle}` }),
    ).toBeVisible();
    await expect(
      page.getByRole("cell", { name: replacementAsset }),
    ).toBeVisible();

    await page.getByRole("button", { name: `Open task ${taskTitle}` }).click();
    const cleanupTask = page.getByRole("dialog", {
      name: "Edit maintenance task",
    });
    await cleanupTask.getByRole("button", { name: "Delete task" }).click();
    const confirmTaskDelete = page.getByRole("dialog", {
      name: "Delete this task?",
    });
    await confirmTaskDelete
      .getByRole("button", { name: "Delete task" })
      .click();
    await expect(page.getByText("Task deleted")).toBeVisible();

    await page.goto("/assets");
    await page.getByLabel("Search").fill(replacementCode);
    await expect(
      page.getByRole("button", { name: `Open asset ${replacementAsset}` }),
    ).toBeVisible();
    await deleteVisibleAsset(page, replacementAsset);
    await expect(
      page.getByRole("button", { name: `Open asset ${replacementAsset}` }),
    ).toHaveCount(0);
  });
});
