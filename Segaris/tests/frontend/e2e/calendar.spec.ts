import { expect, test, type Page } from "@playwright/test";

const username = process.env.SEGARIS_E2E_USERNAME;
const password = process.env.SEGARIS_E2E_PASSWORD;

function madridToday() {
  return new Intl.DateTimeFormat("en-CA", {
    timeZone: "Europe/Madrid",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(new Date());
}

function monthOf(date: string) {
  return date.slice(0, 7);
}

function dayLabel(date: string) {
  return new Intl.DateTimeFormat("en-GB", {
    weekday: "long",
    day: "numeric",
    month: "long",
    year: "numeric",
  }).format(new Date(`${date}T00:00:00`));
}

async function signIn(page: Page) {
  await page.goto("/login");
  await page.getByLabel("Username").fill(username!);
  await page.getByLabel("Password").fill(password!);
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(
    page.getByRole("heading", { name: "Choose a module" }),
  ).toBeVisible();
}

async function createTrip(page: Page, name: string, date: string) {
  await page.goto("/travel");
  await expect(page.getByRole("heading", { name: "Travel" })).toBeVisible();
  await page.getByRole("button", { name: "New trip" }).click();

  const dialog = page.getByRole("dialog", { name: "New trip" });
  await dialog.getByLabel("Name").fill(name);
  await dialog.getByLabel("Start date").fill(date);
  await dialog.getByLabel("End date").fill(date);
  await dialog.getByRole("button", { name: "Create", exact: true }).click();
  await expect(page.getByText("Trip created")).toBeVisible();

  await page
    .getByRole("dialog", { name: "Edit trip" })
    .getByRole("button", { name: "Cancel" })
    .click();
}

async function createMaintenanceTask(page: Page, title: string, dueDate: string) {
  await page.goto("/maintenance");
  await expect(page.getByRole("heading", { name: "Maintenance" })).toBeVisible();
  await page.getByRole("button", { name: "New task" }).click();

  const dialog = page.getByRole("dialog", { name: "New maintenance task" });
  await dialog.getByLabel("Title").fill(title);
  await dialog.getByLabel("Type").selectOption({ index: 0 });
  await dialog.getByLabel("Status").selectOption({ label: "Pending" });
  await dialog.getByLabel("Priority").selectOption({ label: "Medium" });
  await dialog.getByLabel("Due date").fill(dueDate);
  await dialog.getByRole("button", { name: "Create" }).click();
  await expect(page.getByText("Task created")).toBeVisible();
}

async function deleteMaintenanceTask(page: Page, title: string) {
  await page.goto("/maintenance");
  await page.getByLabel("Search").fill(title);
  await page.getByRole("button", { name: `Open task ${title}` }).click();
  const dialog = page.getByRole("dialog", { name: "Edit maintenance task" });
  await dialog.getByRole("button", { name: "Delete task" }).click();
  await page
    .getByRole("dialog", { name: "Delete this task?" })
    .getByRole("button", { name: "Delete task" })
    .click();
  await expect(page.getByText("Task deleted")).toBeVisible();
}

async function deleteOpenTrip(page: Page) {
  const dialog = page.getByRole("dialog", { name: "Edit trip" });
  await expect(dialog).toBeVisible();
  await dialog.getByRole("button", { name: "Delete", exact: true }).click();
  await page
    .getByRole("dialog", { name: "Delete trip?" })
    .getByRole("button", { name: "Delete trip" })
    .click();
  await expect(page.getByText("Trip deleted")).toBeVisible();
}

/**
 * Representative Calendar journey against the full stack: sign in, create two
 * disposable projected source records (Travel and Maintenance), open Calendar,
 * navigate the month grid, create a private note, make it public, verify note
 * indicators and selected-day detail alongside projected Travel/Other entries,
 * open a projected source record, then delete the note and safe source records.
 *
 * Skipped without seeded credentials, matching the other end-to-end specs. The
 * second-user browser privacy journey remains covered by API/component suites
 * until multi-account Playwright infrastructure exists.
 */
test.describe("calendar critical journey", () => {
  test.skip(
    !username || !password,
    "Requires a running backend and a seeded user (SEGARIS_E2E_USERNAME / SEGARIS_E2E_PASSWORD).",
  );

  test("shows projected entries and manages a public daily note", async ({
    page,
  }) => {
    const date = madridToday();
    const month = monthOf(date);
    const readableDay = dayLabel(date);
    const suffix = Date.now().toString(36);
    const tripName = `E2E calendar trip ${suffix}`;
    const taskTitle = `E2E calendar task ${suffix}`;
    const noteTitle = `E2E calendar note ${suffix}`;

    await signIn(page);
    await createTrip(page, tripName, date);
    await createMaintenanceTask(page, taskTitle, date);

    await page.goto(`/calendar?month=${month}&day=${date}`);
    await expect(page.getByRole("heading", { name: "Calendar" })).toBeVisible();
    await page.getByRole("button", { name: "Previous month" }).click();
    await expect(page).toHaveURL(/month=/);
    await page.getByRole("button", { name: "Today" }).click();
    await expect(page).toHaveURL(new RegExp(`month=${month}.*day=${date}`));

    const selectedDay = page.getByRole("button", {
      name: new RegExp(`${readableDay}, \\d+ entries`),
    });
    await expect(selectedDay).toBeVisible();
    await expect(selectedDay).toHaveAttribute("aria-pressed", "true");
    await expect(page.getByText(tripName).first()).toBeVisible();
    await expect(page.getByText(taskTitle).first()).toBeVisible();

    await page.getByRole("button", { name: "New note" }).click();
    const createNote = page.getByRole("dialog", { name: "New note" });
    await expect(createNote.getByRole("radio", { name: "Private" })).toBeChecked();
    await createNote.getByLabel("Title").fill(noteTitle);
    await createNote.getByLabel("Note").fill("Created by the Calendar E2E journey.");
    await createNote.getByRole("button", { name: "Save note" }).click();
    await expect(page.getByText("Note created")).toBeVisible();

    const detail = page.getByRole("complementary", {
      name: new RegExp(`Detail for ${readableDay}`),
    });
    await expect(detail.getByRole("region", { name: "Travel" })).toBeVisible();
    await expect(detail.getByRole("region", { name: "Note" })).toBeVisible();
    await expect(detail.getByRole("region", { name: "Other" })).toBeVisible();
    await expect(detail.getByText(noteTitle)).toBeVisible();
    await expect(selectedDay.getByLabel(`Note entry: ${noteTitle}`)).toBeVisible();

    await detail.getByRole("button", { name: `Edit note ${noteTitle}` }).click();
    const editNote = page.getByRole("dialog", { name: "Edit note" });
    await editNote.getByRole("radio", { name: "Public" }).check();
    await editNote.getByRole("button", { name: "Save changes" }).click();
    await expect(page.getByText("Note updated")).toBeVisible();
    await expect(detail.getByText("Public")).toBeVisible();

    await page
      .getByRole("button", { name: `Open ${tripName} in Travel` })
      .click();
    await expect(page).toHaveURL(/\/travel\?tripId=/);
    await deleteOpenTrip(page);

    await page.goto(`/calendar?month=${month}&day=${date}`);
    await page.getByRole("button", { name: `Edit note ${noteTitle}` }).click();
    const cleanupNote = page.getByRole("dialog", { name: "Edit note" });
    await cleanupNote.getByRole("button", { name: "Delete" }).click();
    await page
      .getByRole("dialog", { name: "Delete this note?" })
      .getByRole("button", { name: "Delete note" })
      .click();
    await expect(page.getByText("Note deleted")).toBeVisible();

    await deleteMaintenanceTask(page, taskTitle);
  });
});
