# Calendar Acceptance Record (Wave 7)

This document records the Wave 7 end-to-end, hardening, and acceptance pass for
the Calendar module against `docs/requirements/CALENDAR_REQUIREMENTS.md` and the
exit criteria in `docs/planning/CALENDAR_IMPLEMENTATION_PLAN.md`.

## Verification Approach

Wave 7 is executed as a focused hardening and acceptance pass:

- Functional behaviour is covered by the automated backend and frontend suites
  delivered in Waves 0-6 and gated through the repository validation scripts.
- The repeatable validation entry points are the repository scripts: backend
  format verification, build, unit/API/PostgreSQL/migration/architecture tests,
  frontend format verification, lint, type-check/build, unit tests, and
  Playwright.
- `tests/frontend/e2e/calendar.spec.ts` adds a representative full-stack
  Playwright journey that runs against the deployed frontend/backend boundary
  when seeded `SEGARIS_E2E_USERNAME` / `SEGARIS_E2E_PASSWORD` credentials are
  present.
- The OpenAPI surface, source projection query shape, provider migrations, route
  contracts, and frontend route state are verified statically against the
  implemented endpoints, contracts, tests, and module registration.

## End-To-End Journey

`tests/frontend/e2e/calendar.spec.ts` adds a single-user critical journey against
the full stack: sign in, create disposable Travel and Maintenance records for
today, open Calendar, navigate the month grid, create a private daily note, make
the note public, verify month indicators and day detail contain Travel, Note, and
Other families, open the Travel source record from Calendar, delete the trip,
delete the note, and delete the maintenance task. It is skipped without seeded
credentials, matching the other E2E specs.

## Static Verification Results

### HTTP / OpenAPI Surface

All Calendar routes are mapped under `/api/calendar` with explicit Minimal API
metadata and DTO contracts; source-module entities are not exposed. The group
requires authentication, write routes apply antiforgery, and missing or
inaccessible daily notes share the module not-found behaviour.

- **Entries**: `GET /api/calendar/entries` accepts inclusive `from`/`to` civil
  dates and allow-listed `sourceModule` and `visualFamily` filters. It enforces
  the maximum range before querying providers and returns normalized
  `CalendarEntryResponse` records for source projections and accessible notes.
- **Daily notes**: `GET /api/calendar/notes`, `POST /api/calendar/notes`,
  `GET /api/calendar/notes/{noteId}`, `PUT /api/calendar/notes/{noteId}`, and
  `DELETE /api/calendar/notes/{noteId}` expose list/detail/mutation contracts
  with validation, public collaboration, private isolation, creator-only
  visibility changes, and privacy-safe problem responses.
- **Source projections**: Firebird, Travel, Inventory, Assets, Maintenance, and
  Processes publish narrow source-owned contracts consumed by Calendar adapters.
  Capex, Opex, Recipes, Destinations, Health, Clothes, Mood, and Projects do not
  participate in the initial projection set.

### Indexes And Query Shape

Calendar persists only `CalendarDailyNote`; projected entries are computed on
request. The Calendar note indexes exist in both SQLite and PostgreSQL
migrations and match the implemented lookup paths:

| Index                                      | Query that uses it                                      |
| ------------------------------------------ | ------------------------------------------------------- |
| `calendar_daily_notes (Date, Id)`          | Range lookup and deterministic note ordering            |
| `calendar_daily_notes (CreatedBy, Date)`   | Creator/private visibility filtering across date ranges |
| `calendar_daily_notes (Visibility, Date)`  | Public note lookup across date ranges                   |
| `calendar_daily_notes (CreatedBy, Id)`     | Detail/mutation privacy checks and creator ownership    |

Source projection providers apply source-owned authorization and status/date
filters before Calendar receives records. Wave 3 deliberately reused existing
source-module indexes at current household volumes; projection-specific
date-range indexes remain deferred until representative data volumes exist.

## Acceptance Criteria

Each criterion from `CALENDAR_REQUIREMENTS.md` and its primary covering evidence:

| #   | Criterion                                                                                                                                              | Status                | Primary evidence                                                                                                                                                                                                 |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------------------ | --------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Authenticated users can open a Monday-first `Europe/Madrid` month grid with current/selected day highlighting, month navigation, and day detail        | Met                   | `CalendarPage`, `calendarState.ts`, `contracts.test.ts`, `CalendarPage.test.tsx`, `calendar.spec.ts`                                                                                                             |
| 2   | Calendar consumes source-module projection contracts rather than querying source entities/tables directly                                               | Met                   | `CalendarModule`, `ICalendarProjectionProvider` adapters, source `*CalendarProjectionProvider` contracts, `ModuleBoundaryTests`, `CalendarSourceProjectionsTests`                                                |
| 3   | Source modules filter projections for the current user and Calendar never displays inaccessible private records                                         | Met                   | `CalendarSourceProjectionsTests`, `CalendarEntriesEndpointTests`, source provider accessibility filters                                                                                                           |
| 4   | Firebird birthdays appear for accessible people with birthdays and source-owned occurrence rules                                                        | Met                   | `FirebirdCalendarProjectionProvider`, `FirebirdBirthday`, `CalendarSourceProjectionsTests`                                                                                                                       |
| 5   | Non-cancelled Travel trips appear as continuous all-day ranges without itinerary entries                                                                | Met                   | `TravelCalendarProjectionProvider`, `CalendarSourceProjectionsTests`, `CalendarPage`, `CalendarPage.test.tsx`, `calendar.spec.ts`                                                                                |
| 6   | Inventory publishes only accessible `Planning` and `Active` orders with expected receipt dates                                                         | Met                   | `InventoryCalendarProjectionProvider`, `CalendarSourceProjectionsTests`                                                                                                                                          |
| 7   | Assets publishes accessible non-`Retired` assets with expected end-of-life dates, including matching past dates                                        | Met                   | `AssetsCalendarProjectionProvider`, `CalendarSourceProjectionsTests`                                                                                                                                             |
| 8   | Maintenance publishes accessible `Pending` and `InProgress` tasks with due dates                                                                       | Met                   | `MaintenanceCalendarProjectionProvider`, `CalendarSourceProjectionsTests`, `calendar.spec.ts`                                                                                                                    |
| 9   | Processes publishes pending step due dates, excludes global due dates, resolved steps, and completed/cancelled processes                               | Met                   | `ProcessesCalendarProjectionProvider`, `CalendarSourceProjectionsTests`                                                                                                                                          |
| 10  | Capex and Opex do not publish Calendar entries in the initial scope                                                                                    | Met                   | `ModuleBoundaryTests`, `CalendarCodes`, `CalendarSourceProjectionsTests`                                                                                                                                         |
| 11  | Users can create, query, edit, and delete Calendar daily notes with validation, privacy rules, and `Private` creation default                           | Met                   | `CalendarDailyNote`, `CalendarNoteEndpointTests`, `CalendarNoteDialog.test.tsx`, `calendar.spec.ts`                                                                                                              |
| 12  | Month indicators distinguish Birthday, Travel, Note, and Other families while preserving complete day detail                                           | Met                   | `CalendarPage`, `CalendarPage.css`, `CalendarPage.test.tsx`, `calendar.spec.ts`                                                                                                                                  |
| 13  | Source-module and visual-family filters affect indicators and day details without changing authorization behaviour                                      | Met                   | `CalendarEntriesEndpointTests`, `calendarState.ts`, `CalendarPage.test.tsx`                                                                                                                                      |
| 14  | Projected entries open source records when a safe route exists, while notes open the Calendar editor                                                   | Met                   | Calendar projection adapters, `CalendarPage`, `CalendarPage.test.tsx`, `NoteDialog.test.tsx`, `calendar.spec.ts`                                                                                                |
| 15  | SQLite/PostgreSQL migrations, backend unit/integration/architecture tests, frontend component tests, and a representative Playwright journey verify it | Met (single-user E2E) | `Segaris.Migrations.IntegrationTests`, `Segaris.Postgres.IntegrationTests`, `ModuleBoundaryTests`, Calendar backend suites, `CalendarPage.test.tsx`, `NoteDialog.test.tsx`, `calendar.spec.ts`                  |

## Deferred Items

Recorded in `ROADMAP.md`:

- **Second-user Calendar privacy E2E journey.** Public collaboration, private
  isolation, source projection visibility, and not-found privacy are covered by
  API integration and component tests. Browser-level multi-account coverage waits
  on shared multi-account Playwright infrastructure, matching earlier modules.
- **Projection-specific date-range indexes and materialization.** Current
  providers use source-module indexes and bounded ranges. Dedicated indexes,
  caching, or projection materialization wait on representative data volumes and
  measured query plans.
- **Future Calendar scope.** Travel itinerary entries, Capex/Opex projections,
  Recipes weekly menus, agenda/week/year views, note recurrence, attachments,
  tags, reminders, source-record links on notes, launcher attention, external
  calendar synchronization, import, export, and ICS feeds remain future versions.

## Documentation Outcomes

- `README.md`: no change; repository-wide commands and setup were unaffected by
  Calendar Wave 7.
- `ROADMAP.md`: Calendar implementation marked accepted and the intentional
  deferrals above recorded.
- `docs/planning/CALENDAR_IMPLEMENTATION_PLAN.md`: Wave 7 status is recorded in
  this document.
