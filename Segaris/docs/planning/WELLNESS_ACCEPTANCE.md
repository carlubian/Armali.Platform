# Wellness Acceptance Record (Wave 7)

This document records the Wave 7 end-to-end, hardening, and acceptance pass for
the Wellness module against the accepted functional decisions in
`docs/planning/WELLNESS_IMPLEMENTATION_PLAN.md`.

## Verification Approach

Wave 7 is executed as a focused hardening and acceptance pass:

- Functional behaviour is covered by the automated backend and frontend suites
  delivered in Waves 0-6 and gated through the repository validation scripts.
- The repeatable validation entry points are the repository scripts: backend
  format verification, build, unit/PostgreSQL/migration/architecture tests,
  focused API integration tests, frontend format verification, lint,
  type-check/build, unit tests, and Playwright.
- `tests/frontend/e2e/wellness.spec.ts` adds a representative full-stack
  Playwright journey that runs against the deployed frontend/backend boundary
  when seeded `SEGARIS_E2E_USERNAME` / `SEGARIS_E2E_PASSWORD` credentials are
  present.
- The OpenAPI surface, provider migrations, route contracts, query shape,
  frontend route state, narrow-width rendering, and checkbox accessibility are
  verified against the implemented endpoints, contracts, tests, and module
  registration.

## End-To-End Journey

`tests/frontend/e2e/wellness.spec.ts` adds a single-user administrator journey
against the full stack: sign in, create one Wellness task in each fixed category
through Configuration, open Wellness to generate the current household day's
private task snapshot, reload the page to confirm same-day stability, toggle a
task and verify the daily score readout, open the Mood weekly log, confirm the
same Wellness percentage appears with the Wellness marker, then delete the
disposable catalogue tasks. It is skipped without seeded credentials, matching
the other E2E specs.

## Static Verification Results

### HTTP / OpenAPI Surface

All Wellness routes are mapped under `/api/wellness` with explicit Minimal API
metadata and DTO contracts; EF Core entities are not exposed. The group requires
authentication, write routes apply antiforgery, day reads and mutations scope to
the current user, and catalogue mutations require the administrator policy.

- **Today**: `GET /api/wellness/today` returns the current household day's
  selected task snapshots and score, generating the day lazily on first read.
- **Toggle**: `POST /api/wellness/today/tasks/{dayTaskId}/toggle` flips a
  current user's day-task completion state, recomputes the persisted score, and
  returns the updated day; inaccessible day-task IDs return not found.
- **Days range**: `GET /api/wellness/days?from={date}&to={date}` validates an
  inclusive civil-date range and returns existing score rows for the current user
  only. This is the endpoint consumed by the Mood frontend.
- **Catalogue**: `GET /api/wellness/tasks`, `POST /api/wellness/tasks`, and
  `DELETE /api/wellness/tasks/{taskId}` expose creation-order reads plus
  administrator create/delete of shared catalogue tasks. Deletion is direct
  because persisted days own independent snapshot rows.

### Indexes And Query Shape

The Wellness persistence indexes exist in both SQLite and PostgreSQL migrations
and match the implemented lookup paths:

| Index                                  | Query that uses it                                      |
| -------------------------------------- | ------------------------------------------------------- |
| `wellness_tasks (SortOrder, Id)`       | Catalogue reads in creation order                       |
| `wellness_days (CreatedBy, Date)`      | Unique per-user day generation and today lookup         |
| `wellness_days (CreatedBy, Date, Id)`  | Current-user day-range score reads                      |
| `wellness_day_tasks (WellnessDayId)`   | Loading and toggling a generated day's task snapshot    |

Daily score range and task-name/category validation are enforced in the domain
and by endpoint validation. The days range reads one `WellnessDay` row per
existing day and does not need to load day-task snapshots.

## Acceptance Criteria

Each accepted decision from `WELLNESS_IMPLEMENTATION_PLAN.md` and its primary
covering evidence:

| #   | Decision                                                                                             | Status                | Primary evidence                                                                                                                      |
| --- | ---------------------------------------------------------------------------------------------------- | --------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Each user gets six current-day tasks that are generated once and stay stable across reloads/revisits | Met                   | `WellnessDayService`, `WellnessDayApiTests`, `WellnessPage.test.tsx`, `wellness.spec.ts`                                             |
| 2   | Past task sets are not shown; only per-day scores survive for Mood                                   | Met                   | `WellnessEndpoints`, `WellnessDayService.ListDaysAsync`, `WellnessDayApiTests`, `MoodLogPage`, `MoodLogPage.test.tsx`               |
| 3   | Completing/pending a task updates the integer percentage score                                       | Met                   | `WellnessScore`, `WellnessDaySelectionTests`, `WellnessDayApiTests`, `WellnessPage.test.tsx`, `wellness.spec.ts`                    |
| 4   | The shared catalogue is administrator-managed with required name and fixed category                  | Met                   | `WellnessTask`, `WellnessTaskManagementService`, `WellnessTaskCatalogueTests`, `ConfigurationPage.test.tsx`, `wellness.spec.ts`     |
| 5   | Category enum values and labels are fixed                                                            | Met                   | `WellnessCategory`, `WellnessContractTests`, `contracts.test.ts`, `wellness/i18n/resources.ts`, `ConfigurationPage.test.tsx`        |
| 6   | Daily selection is random with at least one task per available category                              | Met                   | `WellnessDaySelector`, `WellnessDaySelectionTests`, `WellnessDayApiTests`                                                            |
| 7   | Wellness remains an independent backend module and Mood integration is frontend-only                 | Met                   | `ModuleBoundaryTests`, `WellnessModule`, `MoodLogPage`, `MoodLogPage.test.tsx`, no Mood backend/persistence changes                |
| 8   | Daily selection, completion, and score are private per user                                          | Met                   | `WellnessDay`, `WellnessDayTask`, `WellnessDayApiTests`, `WellnessDayService`                                                        |
| 9   | Persisted day snapshots are independent from catalogue deletion                                      | Met                   | `WellnessDayTask.CreateSnapshot`, `WellnessTaskCatalogueTests`, provider migrations                                                  |
| 10  | Generation uses household day `Europe/Madrid` through `IClock` and is lazy/idempotent                | Met                   | `WellnessCivilDate`, `WellnessDaySelectionTests`, `WellnessDayApiTests`                                                              |
| 11  | Empty catalogue produces an empty day with no score                                                  | Met                   | `WellnessDayApiTests`, `WellnessPage`, `WellnessPage.test.tsx`, Configuration empty-state tests                                     |
| 12  | Wellness has a protected `/wellness` frontend route, launcher card, checkbox UX, and empty states    | Met                   | `AppRouter`, `WellnessPage`, `WellnessPage.css`, `WellnessPage.test.tsx`, `LauncherPage.test.tsx`, `wellness.spec.ts`              |
| 13  | Configuration surfaces Wellness task create/delete with category selection and cache invalidation     | Met                   | `catalogs.ts`, `CatalogFormDialog`, `CatalogSection`, `ConfigurationPage.test.tsx`, `wellness.spec.ts`                             |
| 14  | Mood weekly log shows daily and weekly Wellness scores with a dedicated icon and failure fallback    | Met                   | `MoodLogPage`, `MoodPrimitives`, `mood/i18n/resources.ts`, `MoodLogPage.test.tsx`, `wellness.spec.ts`                              |
| 15  | SQLite/PostgreSQL migrations, backend/frontend tests, and a representative Playwright journey cover it | Met (single-user E2E) | `Segaris.Migrations.IntegrationTests`, `Segaris.Postgres.IntegrationTests`, Wellness backend suites, frontend unit tests, E2E specs |

## Deferred Items

Recorded in `ROADMAP.md`:

- **Future Wellness scope.** Historical task-set browsing, per-task streaks or
  history, weekly/monthly Wellness dashboards, configurable daily task count,
  configurable or per-user categories, task enable/disable and weighting,
  reminders or launcher attention, Analytics integration, and any Mood dashboard
  integration remain future versions.

## Documentation Outcomes

- `README.md`: no change; repository-wide commands and setup were unaffected by
  Wellness Wave 7.
- `ROADMAP.md`: Wellness implementation marked accepted; intentional future
  scope remains deferred.
- `docs/planning/WELLNESS_IMPLEMENTATION_PLAN.md`: preserved as the historical
  delivery plan.
