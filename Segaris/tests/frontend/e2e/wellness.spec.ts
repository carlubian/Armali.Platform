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

async function createWellnessTask(
  page: Page,
  name: string,
  category: "Health & Body" | "Mind & Sleep" | "People & Work",
) {
  await page.getByRole("button", { name: "New task" }).click();
  const dialog = page.getByRole("dialog", { name: "New Wellness task" });
  await dialog.getByLabel("Name").fill(name);
  await dialog.getByLabel("Category").selectOption({ label: category });
  await dialog.getByRole("button", { name: "Create" }).click();
  await expect(page.getByText("Added")).toBeVisible();
  await expect(page.getByRole("cell", { name })).toBeVisible();
  await expect(page.getByRole("cell", { name: category })).toBeVisible();
}

async function deleteWellnessTask(page: Page, name: string) {
  await page.goto("/configuration/wellness");
  const row = page.getByRole("row", { name: new RegExp(name) });
  if ((await row.count()) === 0) return;

  await page.getByRole("button", { name: `Delete ${name}` }).click();
  const dialog = page.getByRole("dialog", { name: `Delete ${name}?` });
  await dialog.getByRole("button", { name: "Delete" }).click();
  await expect(page.getByText("Removed")).toBeVisible();
  await expect(page.getByRole("cell", { name })).toHaveCount(0);
}

async function visibleTaskNames(page: Page) {
  return page.locator(".seg-wellness-task__name").allTextContents();
}

function expectedScore(completed: number, total: number) {
  return Math.round((completed / total) * 100);
}

/**
 * Representative Wellness acceptance journey against the deployed boundary:
 * sign in as an administrator, create one catalogue task in each fixed category
 * through Configuration, open Wellness to lazily generate today's private set,
 * verify same-day stability, toggle a task and observe the score, then confirm
 * the persisted score appears in the Mood weekly log with the Wellness marker.
 *
 * The journey is skipped without seeded credentials, matching the rest of the
 * full-stack Playwright suite. It expects the usual clean Compose acceptance
 * database: if the current user already generated today's Wellness day before
 * these tasks were created, the backend correctly keeps that earlier snapshot.
 */
test.describe("wellness acceptance journey", () => {
  test.skip(
    !username || !password,
    "Requires a running backend and a seeded administrator (SEGARIS_E2E_USERNAME / SEGARIS_E2E_PASSWORD).",
  );

  test("creates tasks, completes today's Wellness work, and shows the score in Mood", async ({
    page,
  }) => {
    const suffix = Date.now().toString(36);
    const tasks = [
      { name: `E2E hydrate ${suffix}`, category: "Health & Body" as const },
      { name: `E2E breathe ${suffix}`, category: "Mind & Sleep" as const },
      { name: `E2E check in ${suffix}`, category: "People & Work" as const },
    ];

    await signIn(page);

    await page.goto("/configuration/wellness");
    await expect(
      page.getByRole("heading", { name: "Configuration" }),
    ).toBeVisible();
    await expect(
      page.getByRole("heading", { name: "Wellness tasks" }),
    ).toBeVisible();

    for (const task of tasks) {
      await createWellnessTask(page, task.name, task.category);
    }

    await page.goto("/wellness");
    await expect(page.getByRole("heading", { name: "Wellness" })).toBeVisible();
    await expect(
      page.getByRole("heading", { name: "Today's tasks" }),
    ).toBeVisible();

    const firstNames = await visibleTaskNames(page);
    expect(firstNames.length).toBeGreaterThan(0);

    await page.reload();
    await expect(
      page.getByRole("heading", { name: "Today's tasks" }),
    ).toBeVisible();
    await expect.poll(() => visibleTaskNames(page)).toEqual(firstNames);

    const checkboxes = page.getByRole("checkbox");
    const total = await checkboxes.count();
    expect(total).toBeGreaterThan(0);

    const beforeStates = await checkboxes.evaluateAll((nodes) =>
      nodes.map((node) => (node as HTMLInputElement).checked),
    );
    const beforeCompleted = beforeStates.filter(Boolean).length;
    const firstWasChecked = beforeStates[0] ?? false;
    const expectedCompleted = firstWasChecked
      ? beforeCompleted - 1
      : beforeCompleted + 1;
    const score = expectedScore(expectedCompleted, total);

    await checkboxes.first().click();
    await expect(
      page.getByRole("img", {
        name: `${score} percent — ${expectedCompleted} of ${total} tasks completed`,
      }),
    ).toBeVisible();

    await page.goto("/mood/log");
    await expect(
      page.getByRole("heading", { name: "Weekly log" }),
    ).toBeVisible();
    await expect(page.getByText(`Wellness ${score}%`)).toBeVisible();
    await expect
      .poll(() => page.locator(".mood-weekchart__well svg").count())
      .toBeGreaterThan(0);

    for (const task of tasks) {
      await deleteWellnessTask(page, task.name);
    }
  });
});
