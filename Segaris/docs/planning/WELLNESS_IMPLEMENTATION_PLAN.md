# Wellness Implementation Plan

## Purpose

This plan delivers the initial Wellness module: a daily set of healthy-habit
tasks that each user completes, producing a persisted per-day score that is also
surfaced inside the Mood weekly log. The functional decisions were accepted in
planning and are captured directly in this document, which owns both the
behaviour and the delivery order for the initial release.

Wellness is a standalone business module. Only part of its data is *visualized*
inside Mood; the two backends stay decoupled.

## Accepted Functional Decisions

- Each user opening the module sees six healthy-habit tasks for the current
  household day. The set is generated once per day, then stays stable for the
  rest of that day, including completion state, across reloads and revisits.
- Past days' task sets are never shown. Only the persisted per-day score survives
  to feed Mood; the task detail is a today-only surface.
- Marking a task completed or pending updates the day's score. The score is the
  percentage of the day's tasks that are completed (`completed / total * 100`,
  rounded to an integer, `0`-`100`).
- The task pool is an administrator-managed, household-shared catalogue. A task
  has a required name and a required fixed category. The concrete daily selection
  is private per user.
- Categories are a fixed enum. v1 values: `HealthAndBody`, `MindAndSleep`,
  `PeopleAndWork` (displayed as "Health & Body", "Mind & Sleep", "People & Work").
- Daily selection is random from the catalogue but includes at least one task of
  each category that has tasks (see the selection rule below).
- The Mood weekly log shows the Wellness score alongside the mood average: the
  per-day Wellness percentage in the weekly chart, distinguished by a dedicated
  icon, plus a weekly summary readout. This is composed in the frontend; the Mood
  backend does not depend on Wellness.

## Delivery Principles

- Keep Wellness an independent business module: it consumes only Identity, the
  Configuration presentation boundary, Launcher, and platform contracts. It does
  not depend on any other business module, and no business module depends on
  Wellness.
- Preserve Mood's autonomy: the Mood/Wellness integration is frontend-only. The
  Mood backend is not modified to read Wellness data, and Wellness publishes no
  contract that Mood consumes in the backend.
- Keep the daily selection, per-day score, and completion state private per user,
  matching the Mood privacy model.
- Persist the day's selected tasks as a snapshot so deleting a catalogue task
  never breaks a user's in-progress day.
- Generate the daily set lazily and idempotently on first read, using the
  household time zone (`Europe/Madrid`) via `IClock`, and enforce one day record
  per user and date.
- Reuse established Configuration, privacy, REST, URL-state, and frontend form
  conventions where their semantics match.
- Keep each Wave independently testable and record deferrals in `ROADMAP.md`
  rather than in implementation notes.

## Fixed Technical Contracts

### Backend Module

Wellness lives under `Segaris.Api.Modules.Wellness` and is registered in
`SegarisModules` after Games and before Launcher. It owns the administrator-managed
`WellnessTask` catalogue, the user-owned `WellnessDay` and `WellnessDayTask`
entities, the fixed `WellnessCategory` enum, the daily selection algorithm, and
the per-day score projection. Its launcher card never requests attention.

Indicative resource routes are:

```text
GET    /api/wellness/today
POST   /api/wellness/today/tasks/{dayTaskId}/toggle
GET    /api/wellness/days?from={YYYY-MM-DD}&to={YYYY-MM-DD}

GET    /api/wellness/catalog/tasks
POST   /api/wellness/catalog/tasks
DELETE /api/wellness/catalog/tasks/{taskId}
```

`GET /today` returns the current household day's selected tasks with their
completion flags and the day score, generating the day record if it does not yet
exist. The toggle route flips one day-task's completion, recomputes and persists
the score, and returns the updated day. `GET /days` returns the per-day score for
each existing day in the requested range and is the endpoint the Mood frontend
consumes. Catalogue routes follow the module-owned catalogue management pattern
exposed through Configuration.

All writes require antiforgery. Today, day-task, and day records are always scoped
to the current user; another user's records are never disclosed. Catalogue
mutations require administrator authorization.

### Persistence

The model contains module-owned entities with provider-specific migrations in
`Segaris.Migrations.Sqlite` and `Segaris.Migrations.Postgres`:

- `WellnessTask` - catalogue row: required non-whitespace bounded name, required
  fixed `WellnessCategory`, creation-order sort key, and catalogue audit metadata.
  Order is creation order and is not user-editable. There is no enable/disable
  flag; tasks are created or deleted only.
- `WellnessDay` - one row per user and household date: owner user, date, integer
  score (`0`-`100`), and audit metadata. Unique on (owner, date).
- `WellnessDayTask` - the snapshot of a day's selected tasks: owning day, copied
  task name and category, completion flag, and stable in-day order. Snapshot rows
  are independent of the catalogue so catalogue deletion cannot alter a persisted
  day.

Day-task rows are removed when their owning day is deleted. Indexes support
day lookup by owner and date, day-range reads by owner, day-task reads by owning
day, and catalogue reads in creation order.

The score is stored denormalized on `WellnessDay` so the `GET /days` range query
Mood consumes reads a single row per day without loading day-tasks, and is
recomputed from the day's task snapshot on every completion toggle.

### Daily Selection Algorithm

`WellnessDaySelector` chooses six tasks from the catalogue for a new day:

1. Group catalogue tasks by category, keeping only categories that have at least
   one task.
2. If the number of such categories is at most six: pick one random task from each
   category, then fill the remaining slots with random tasks drawn from the rest of
   the catalogue without repetition.
3. If the number of such categories exceeds six: pick six categories at random and
   one random task from each.
4. If the catalogue holds six or fewer tasks in total: include them all.

The selection is persisted as `WellnessDayTask` snapshot rows when the day is
generated, so it is stable for the rest of the day. An empty catalogue produces a
day with no tasks and no score (the module shows an empty state).

### Frontend Routes

Wellness uses the protected lazy route `/wellness` for the today surface. State is
minimal: the page renders the current day's tasks and score and does not need
historical navigation.

### Mood Integration

The Mood weekly log (`MoodLogPage`) fetches the visible week's Wellness scores
through `GET /api/wellness/days` and renders them in the existing weekly chart
card:

- Each day's Wellness percentage appears in the weekly chart alongside that day's
  mood average, visually distinguished by a dedicated Wellness icon so the two
  metrics are not confused.
- A weekly Wellness summary (the mean of the week's daily percentages) is shown in
  the chart card lead next to the mood weekly average, also icon-marked.
- Days with no Wellness record show no Wellness marker; a visited day with zero
  completed tasks shows `0`.

No Mood backend code, contract, or persistence changes. If the Wellness request
fails or returns nothing, the mood chart renders exactly as it does today.

### Configuration Integration

Configuration presents the Wellness task catalogue alongside the other
module-owned catalogues as a new `wellness` catalogue section. Wellness owns
`WellnessTask` while exposing create, list, and delete through the established
Configuration presentation boundary. The task editor captures a name and a fixed
category. Because days store task snapshots, deleting a catalogue task has no
impact on existing or in-progress days and needs no replacement flow.

## Waves

### Wave 0: Contracts And Test Skeleton

Establish stable contracts before persistence or UI work begins.

Tasks:

1. Add the Wellness module shell and registration after Games and before Launcher.
2. Freeze today, toggle, days-range, and catalogue routes; the fixed
   `WellnessCategory` enum values; DTOs; query contracts; stable error codes; and
   the absence of any launcher attention key.
3. Define Configuration-facing contracts for task catalogue management without
   exposing Wellness entities.
4. Define frontend API, validation-schema, route-state, and query-key skeletons for
   the today surface, the day-task toggle, the days-range read consumed by Mood,
   and the catalogue section.
5. Add architecture-test expectations: Wellness may consume Configuration, Launcher,
   Identity, and platform contracts but must not depend on any other business
   module, no module depends on Wellness, and Mood in particular gains no
   dependency on Wellness.
6. Add empty backend and frontend test fixtures shared by later Waves.

Tests:

- Unit tests for enum values, route constants, query bounds, and error-code
  stability.
- Architecture tests for permitted dependencies, the Wellness non-dependency rules,
  and the preserved Mood autonomy.

Exit criteria:

- Public contracts are explicit and test-covered, and later Waves do not need to
  invent new route, ownership, enum, selection, or score semantics.

### Wave 1: Domain, Persistence, And Migrations

Implement the Wellness data model on both providers.

Tasks:

1. Add `WellnessTask`, `WellnessDay`, and `WellnessDayTask`.
2. Enforce bounded, non-whitespace task names; fixed category values; the integer
   score range; the per-user per-date uniqueness of `WellnessDay`; and standard
   audit and ownership metadata.
3. Store the day's selection as `WellnessDayTask` snapshot rows independent of the
   catalogue, and remove them when their owning day is deleted.
4. Implement the constant non-attention launcher contributor.
5. Add provider-specific migrations and model snapshots.
6. Add indexes for catalogue creation-order reads, day lookup by owner and date,
   owner day-range reads, and day-task reads by owning day.
7. Add an optional development seed with a small set of tasks per category.

Tests:

- Domain tests for name and score validation, category values, day uniqueness, and
  snapshot independence from the catalogue.
- SQLite and PostgreSQL migration tests, including upgrades and provider parity for
  uniqueness constraints.

Exit criteria:

- Both providers persist the complete Wellness model with correct ownership,
  uniqueness, and snapshot semantics.

### Wave 2: Task Catalogue Admin Backend

Deliver administrator management of the shared task pool through Configuration.

Tasks:

1. Implement catalogue reads in creation order.
2. Implement administrator create and delete of tasks with name and category
   validation, exposed through the Configuration presentation boundary.
3. Confirm task deletion is impact-free because days hold snapshots, with no
   replacement flow.
4. Enforce administrator authorization and antiforgery on catalogue writes.

Tests:

- Integration tests for catalogue creation order, task create and delete, name and
  category validation, snapshot-preserving deletion, and administrator
  authorization.

Exit criteria:

- Administrators can manage the shared task pool through the Configuration boundary,
  and deletions never affect persisted days.

### Wave 3: Daily Generation, Scoring, And Day APIs

Implement the per-user daily surface and the Mood-facing score read.

Tasks:

1. Implement `WellnessDaySelector` with the six-slot, at-least-one-per-category
   rule, including the more-categories-than-slots and small-catalogue cases.
2. Implement `GET /today`: resolve the household day through `IClock` and
   `Europe/Madrid`, generate and persist the day and its snapshot on first read,
   and stay idempotent under concurrent first reads via the day uniqueness
   constraint.
3. Implement the day-task completion toggle, recomputing and persisting the day
   score.
4. Implement `GET /days` returning per-day scores for existing days in the range,
   scoped to the current user.
5. Handle the empty-catalogue case as a day with no tasks and no score.

Tests:

- Unit tests for the selection algorithm across category counts, small and empty
  catalogues, and the at-least-one-per-category guarantee, plus score computation
  and rounding.
- Integration tests for first-read generation, same-day stability, day-rollover
  regeneration, concurrent-first-read idempotency, toggle-driven score updates,
  per-user privacy of days and today, and the days-range read.

Exit criteria:

- Users get a stable daily task set with a correct, persisted score, and the
  days-range read exposes only the current user's scores.

### Wave 4: Wellness Frontend

Build the user-facing today surface.

Tasks:

1. Add the lazy `/wellness` route, module error boundary, `wellness` translation
   namespace with category labels, and a launcher card wired to the constant
   non-attention state.
2. Build the today view: the day's tasks with completion checkboxes and the day
   score, with mutation feedback and query invalidation on toggle.
3. Render the empty-catalogue and no-score states clearly.
4. Add the frontend API client and query keys for today and the toggle.

Tests:

- Frontend API and component tests for today rendering, completion toggling, score
  display, empty state, and privacy-safe errors.
- Accessibility tests for checkbox operation, keyboard interaction, and error
  association.

Exit criteria:

- Users can complete and un-complete the day's tasks and see the resulting score
  without page reloads.

### Wave 5: Configuration Frontend For Wellness

Surface task catalogue management in the Configuration UI.

Tasks:

1. Add the `wellness` section to the Configuration UI for the `WellnessTask`
   catalogue, with create and delete actions.
2. Build the task editor with a name field and a fixed category selector.
3. Surface the empty-catalogue state clearly, since Wellness ships no seeded
   catalogue in production.
4. Invalidate the relevant Wellness and Configuration caches after catalogue
   mutations.

Tests:

- Component tests for task catalogue create and delete, category selection, name
  validation feedback, empty state, and cache invalidation.

Exit criteria:

- Administrators can manage Wellness tasks through Configuration, including their
  category.

### Wave 6: Mood Integration Frontend

Surface the Wellness score inside the Mood weekly log without backend coupling.

Tasks:

1. Extend the Mood weekly log to fetch the visible week's Wellness scores through
   `GET /api/wellness/days`, tolerating absence or failure without degrading the
   mood chart.
2. Render each day's Wellness percentage in the weekly chart alongside the mood
   average, distinguished by a dedicated Wellness icon.
3. Add the weekly Wellness summary readout in the chart card lead next to the mood
   weekly average, also icon-marked.
4. Extend the Mood translation namespace for the new readout and icon labels, and
   register any new namespace usage in `i18n.test.ts`.
5. Keep the change frontend-only: no Mood backend, contract, or persistence edits.

Tests:

- Component tests for the combined chart with mood and Wellness values, the weekly
  Wellness summary, the missing-Wellness-day and request-failure fallbacks, and
  icon-based accessibility labelling.
- A test asserting the mood chart still renders correctly when the Wellness request
  returns nothing.

Exit criteria:

- The Mood weekly log shows daily and weekly Wellness scores, clearly distinct from
  the mood average, while the Mood backend remains independent of Wellness.

### Wave 7: End-To-End, Hardening, And Acceptance

Validate the implemented behaviour across both providers and the deployed
frontend/backend boundary.

Tasks:

1. Run backend format, build, unit, PostgreSQL, migration, and architecture
   suites through repository scripts.
2. Run frontend format, lint, type-check, unit, build, and Playwright suites.
3. Add a representative Playwright journey covering login, creating Wellness tasks
   across categories through Configuration, opening Wellness, completing tasks and
   observing the score, and confirming the score appears in the Mood weekly log
   with its own icon.
4. Verify household-day generation, same-day stability, and day-task privacy at the
   boundary.
5. Review OpenAPI for the Wellness today, toggle, days-range, and catalogue routes.
6. Verify checkbox interaction, empty states, and narrow desktop widths.
7. Map every accepted functional decision in this plan to covering code and tests in
   a Wellness acceptance record.
8. Update `ROADMAP.md` to mark Wellness as implemented and accepted and to record
   only intentional remaining deferrals.

Exit criteria:

- All relevant repository scripts pass, both providers are covered, the Compose
  journey succeeds, and every accepted Wellness decision is implemented or
  explicitly deferred.

## Suggested Pull Request Boundaries

1. Wellness contracts, persistence, and catalogue admin backend (Waves 0-2).
2. Daily generation, scoring, and day APIs (Wave 3).
3. Wellness today frontend and Configuration integration (Waves 4-5).
4. Mood integration frontend (Wave 6).
5. End-to-end, hardening, and acceptance (Wave 7).

## Plan Completion Criteria

The plan is complete when all Waves meet their exit criteria and the accepted
functional decisions in this document describe implemented behaviour rather than
only functional intent.

Historical task-set browsing, per-task streaks or history, weekly or monthly
Wellness dashboards, configurable daily task count, configurable or per-user
categories, task enable/disable, weighting of tasks, reminders or launcher
attention, Analytics integration, and any Mood dashboard (non-weekly-log)
integration remain separate future planning topics.
