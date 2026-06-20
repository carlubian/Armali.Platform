import { expect, test, type Page } from "@playwright/test";

const username = process.env.SEGARIS_E2E_USERNAME;
const password = process.env.SEGARIS_E2E_PASSWORD;

async function signIn(page: Page) {
  await page.goto("/login");
  await page.getByLabel("Username").fill(username!);
  await page.getByLabel("Password").fill(password!);
  await page.getByRole("button", { name: "Sign in" }).click();
}

async function createProgram(page: Page, name: string, code: string) {
  await page.getByRole("button", { name: "New program" }).click();
  const dialog = page.getByRole("dialog", { name: "New program" });
  await dialog.getByLabel("Name").fill(name);
  await dialog.getByLabel("Code").fill(code);
  await dialog.getByRole("button", { name: "Create" }).click();
  await expect(page.getByText("Added")).toBeVisible();
  await expect(page.getByRole("cell", { name })).toBeVisible();
}

async function createAxis(
  page: Page,
  name: string,
  code: string,
  programName: string,
  programCode: string,
) {
  await page.getByRole("button", { name: "New axis" }).click();
  const dialog = page.getByRole("dialog", { name: "New axis" });
  await dialog.getByLabel("Name").fill(name);
  await dialog.getByLabel("Code").fill(code);
  await dialog
    .getByLabel("Program")
    .selectOption({ label: `${programCode} - ${programName}` });
  await dialog.getByRole("button", { name: "Create" }).click();
  await expect(page.getByText("Added")).toBeVisible();
  await expect(page.getByRole("cell", { name })).toBeVisible();
}

async function expandProgramAndAxis(
  page: Page,
  programCode: string,
  programName: string,
  axisCode: string,
  axisName: string,
) {
  await page
    .getByRole("button", {
      name: new RegExp(`${programCode}.*${escapeRegExp(programName)}`),
    })
    .click();
  await page
    .getByRole("button", {
      name: new RegExp(`${axisCode}.*${escapeRegExp(axisName)}`),
    })
    .click();
}

async function deleteStructureNode(page: Page, name: string) {
  await page.getByRole("button", { name: `Delete ${name}` }).click();
  const dialog = page.getByRole("dialog", { name: `Delete ${name}?` });
  await dialog.getByRole("button", { name: "Delete" }).click();
  await expect(page.getByText("Removed")).toBeVisible();
}

function itemIdentifier(codePrefix: string, name: string) {
  return new RegExp(`${codePrefix}-\\d{6} ${escapeRegExp(name)}`);
}

function makeCodes(seed: number) {
  return Array.from({ length: 4 }, (_, index) => codeFrom(seed + index * 9973));
}

function codeFrom(seed: number) {
  const letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
  let value = Math.abs(seed) % (26 * 26 * 26 * 26);
  let code = "";
  for (let index = 0; index < 4; index += 1) {
    code = letters[value % 26] + code;
    value = Math.floor(value / 26);
  }
  return code;
}

function escapeRegExp(value: string) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

/**
 * Representative Projects journey against the full stack: sign in, create safe
 * program/axis structure through Configuration, open Projects, lazily expand the
 * tree, create a project and an activity, update the project status, add a high
 * risk and result file, reassign a non-empty axis in Configuration, verify the
 * recomputed identifiers, then delete all disposable test data.
 *
 * Skipped without seeded administrator credentials, matching the other
 * end-to-end specs. Multi-account privacy remains covered by API integration
 * tests until browser-level multi-session infrastructure exists.
 */
test.describe("projects critical journey", () => {
  test.skip(
    !username || !password,
    "Requires a running backend and a seeded administrator (SEGARIS_E2E_USERNAME / SEGARIS_E2E_PASSWORD).",
  );

  test("manages the tree, risks, attachments, and reassignment", async ({
    page,
  }, testInfo) => {
    await page.setViewportSize({ width: 1024, height: 720 });
    const token = Date.now().toString(36);
    const [programCode, spareProgramCode, sourceAxisCode, targetAxisCode] =
      makeCodes(Date.now() + testInfo.workerIndex * 104729);
    const programName = `E2E Projects ${token}`;
    const spareProgramName = `E2E Projects spare ${token}`;
    const sourceAxisName = `E2E Source axis ${token}`;
    const targetAxisName = `E2E Target axis ${token}`;
    const projectName = `E2E Project ${token}`;
    const activityName = `E2E Activity ${token}`;
    const riskDescription = `E2E High risk ${token}`;
    const sourcePrefix = `${programCode}${sourceAxisCode}`;
    const targetPrefix = `${programCode}${targetAxisCode}`;

    await signIn(page);

    await page.goto("/configuration/projects");
    await expect(
      page.getByRole("heading", { level: 1, name: "Configuration" }),
    ).toBeVisible();
    await createProgram(page, programName, programCode);
    await createProgram(page, spareProgramName, spareProgramCode);

    await page.getByRole("tab", { name: "Axes" }).click();
    await createAxis(
      page,
      sourceAxisName,
      sourceAxisCode,
      programName,
      programCode,
    );
    await createAxis(
      page,
      targetAxisName,
      targetAxisCode,
      programName,
      programCode,
    );

    await page.goto("/projects");
    await expect(
      page.getByRole("heading", { level: 1, name: "Projects" }),
    ).toBeVisible();
    await expandProgramAndAxis(
      page,
      programCode,
      programName,
      sourceAxisCode,
      sourceAxisName,
    );

    await page.getByRole("button", { name: "New project" }).click();
    const newProjectDialog = page.getByRole("dialog", { name: "New project" });
    await newProjectDialog.getByLabel("Name").fill(projectName);
    await newProjectDialog.getByRole("button", { name: "Create" }).click();
    await expect(page.getByText("Project created")).toBeVisible();
    await expect(
      page.getByText(itemIdentifier(sourcePrefix, projectName)),
    ).toBeVisible();

    await page.getByRole("button", { name: "New activity" }).click();
    const newActivityDialog = page.getByRole("dialog", {
      name: "New activity",
    });
    await newActivityDialog.getByLabel("Name").fill(activityName);
    await newActivityDialog.getByLabel("Status").selectOption("Active");
    await newActivityDialog.getByRole("button", { name: "Create" }).click();
    await expect(page.getByText("Activity created")).toBeVisible();
    await expect(
      page.getByText(itemIdentifier(sourcePrefix, activityName)),
    ).toBeVisible();

    await page.getByText(itemIdentifier(sourcePrefix, projectName)).click();
    const editProjectDialog = page.getByRole("dialog", {
      name: "Edit project",
    });
    await editProjectDialog.getByLabel("Status").selectOption("Completed");
    await editProjectDialog.locator('input[type="file"]').setInputFiles({
      name: "project-result.txt",
      mimeType: "text/plain",
      buffer: Buffer.from("Projects E2E result file"),
    });
    await expect(
      editProjectDialog.getByText("project-result.txt"),
    ).toBeVisible();
    await editProjectDialog
      .getByRole("button", { name: "Save changes" })
      .click();
    await expect(page.getByText("Project updated")).toBeVisible();

    await page.getByText(itemIdentifier(sourcePrefix, projectName)).click();
    const reopenedProjectDialog = page.getByRole("dialog", {
      name: "Edit project",
    });
    await reopenedProjectDialog
      .getByRole("button", { name: "Open risks" })
      .click();
    const risksDialog = page.getByRole("dialog", { name: "Project risks" });
    await risksDialog.getByRole("button", { name: "New risk" }).click();
    const riskDialog = page.getByRole("dialog", { name: "New risk" });
    await riskDialog.getByLabel("Description").fill(riskDescription);
    await riskDialog.getByLabel("Probability").selectOption("5");
    await riskDialog.getByLabel("Impact").selectOption("5");
    await riskDialog.getByLabel("Mitigation").selectOption("4");
    await expect(riskDialog.getByText("100 · High")).toBeVisible();
    await riskDialog.getByRole("button", { name: "Save changes" }).click();
    await expect(risksDialog.getByText(riskDescription)).toBeVisible();
    await expect(risksDialog.getByText("High 1")).toBeVisible();
    await risksDialog.getByRole("button", { name: "Close" }).click();
    await reopenedProjectDialog.getByRole("button", { name: "Cancel" }).click();

    await page.goto("/configuration/projects");
    await page.getByRole("tab", { name: "Axes" }).click();
    await page
      .getByRole("button", { name: `Delete ${sourceAxisName}` })
      .click();
    const reassignDialog = page.getByRole("dialog", {
      name: `Reassign and remove ${sourceAxisName}`,
    });
    const reassignTarget = reassignDialog.getByLabel("Reassign children to");
    const targetOptionValue = await reassignTarget
      .locator("option", { hasText: `${targetAxisCode} - ${targetAxisName}` })
      .getAttribute("value");
    expect(targetOptionValue).not.toBeNull();
    await reassignTarget.selectOption(targetOptionValue!);
    await reassignDialog
      .getByRole("button", { name: "Reassign and delete" })
      .click();
    await expect(page.getByText("Reassigned")).toBeVisible();
    await expect(page.getByRole("cell", { name: sourceAxisName })).toHaveCount(
      0,
    );

    await page.goto("/projects");
    await expandProgramAndAxis(
      page,
      programCode,
      programName,
      targetAxisCode,
      targetAxisName,
    );
    await expect(
      page.getByText(itemIdentifier(targetPrefix, projectName)),
    ).toBeVisible();
    await expect(
      page.getByText(itemIdentifier(targetPrefix, activityName)),
    ).toBeVisible();

    await page.getByText(itemIdentifier(targetPrefix, projectName)).click();
    const cleanupProjectDialog = page.getByRole("dialog", {
      name: "Edit project",
    });
    await cleanupProjectDialog
      .getByRole("button", { name: "Delete project" })
      .click();
    await page
      .getByRole("dialog", { name: `Delete ${projectName}?` })
      .getByRole("button", { name: "Delete project" })
      .click();
    await expect(page.getByText("Project deleted")).toBeVisible();

    await page.getByText(itemIdentifier(targetPrefix, activityName)).click();
    const cleanupActivityDialog = page.getByRole("dialog", {
      name: "Edit activity",
    });
    await cleanupActivityDialog
      .getByRole("button", { name: "Delete activity" })
      .click();
    await page
      .getByRole("dialog", { name: `Delete ${activityName}?` })
      .getByRole("button", { name: "Delete activity" })
      .click();
    await expect(page.getByText("Activity deleted")).toBeVisible();

    await page.goto("/configuration/projects");
    await page.getByRole("tab", { name: "Axes" }).click();
    await deleteStructureNode(page, targetAxisName);
    await page.getByRole("tab", { name: "Programs" }).click();
    await deleteStructureNode(page, programName);
    await deleteStructureNode(page, spareProgramName);
  });
});
