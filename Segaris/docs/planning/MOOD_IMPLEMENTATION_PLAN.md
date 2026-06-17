# Mood Implementation Plan

## Purpose

This plan delivers the initial Mood module defined in
`docs/requirements/MOOD_REQUIREMENTS.md`. It translates the accepted functional
decisions into dependency-ordered Waves with explicit backend, frontend,
migration, and test work.

The requirements document remains authoritative for behavior. This plan owns
delivery order and technical scope.

## Delivery Principles

- Preserve Mood as an independent, privacy-first business module.
- Treat every Mood entry and aggregate as owner-only current-user data.
- Do not introduce public/private visibility, administrator review, Attachments,
  Launcher attention, Analytics integration, Calendar integration,
  Configuration catalogs, audit history, or soft deletion.
- Keep the derived-emotion matrix code-backed and test-covered rather than
  administrator-configurable.
- Keep period calculations explicit and based on `Europe/Madrid` civil dates.
- Keep each Wave independently testable and record deferrals in `ROADMAP.md`
  instead of hiding them in implementation notes.

## Fixed Technical Contracts

### Backend Module

Mood lives under `Segaris.Api.Modules.Mood` and owns Mood entries, fixed
criteria enums, the derived-emotion matrix, weekly log queries, dashboard
aggregate queries, and owner-only authorization.

Indicative resource routes are:

```text
GET    /api/mood/entries?from=2026-01-05&to=2026-01-11
POST   /api/mood/entries
GET    /api/mood/entries/{entryId}
PUT    /api/mood/entries/{entryId}
DELETE /api/mood/entries/{entryId}

GET    /api/mood/dashboard?scale=year&period=2026
GET    /api/mood/options
```

`/api/mood/options` may expose fixed enum values and derived-emotion codes
needed by the frontend. It must not expose another user's entries or any
administrator-managed data.

All writes require antiforgery. Missing and inaccessible entries share the
platform not-found behavior so private data is not disclosed.

### Persistence

The model contains one module-owned persisted entity with provider-specific
migrations:

- `MoodEntry`

`MoodEntry` stores:

- Integer identifier.
- `EntryDate` as a civil date.
- `Score` as an integer from `1` to `5`.
- Energy.
- Alignment.
- Direction.
- Source.
- Optional notes with a 1,000-character maximum.
- Standard creation and modification metadata.

The table does not store a time of day, concrete emotion, visibility value,
attachment relationship, tenant identifier, or audit history.

Indexes must support current-user date-range lookups, deterministic insertion
ordering within a day, entry detail lookup by owner, and dashboard aggregation by
owner and entry date.

### Derived Emotion Matrix

The initial product source for the matrix is:

```text
D:\Proyectos Locales\SegarisMood.csv
```

Implementation translates that CSV into module-owned code. The runtime does not
load the external CSV, and administrators cannot edit the matrix through
Configuration.

The matrix must cover exactly:

- 3 Energy values.
- 3 Alignment values.
- 4 Direction values.
- 2 Source values.

The resulting 72 combinations return stable derived-emotion codes. The frontend
translates those codes through the Mood translation namespace.

### Frontend Route

Mood uses the protected lazy route `/mood`.

The module owns two internal screens:

```text
/mood/log
/mood/dashboard
```

`/mood` may redirect to `/mood/log`.

The initial UI should preserve URL-backed state for the selected log week and
dashboard scale/period where practical. One practical route shape is:

```text
/mood/log?week=2026-01-05
/mood/log?week=2026-01-05&entryId=123
/mood/log?week=2026-01-05&newEntry=true
/mood/dashboard?scale=year&period=2026
```

If router ergonomics suggest a slightly different parameter layout, preserve the
same behavior: selected period state must survive refreshes and dialog open or
close without a full page reload.

## Waves

### Wave 0: Contracts And Test Skeleton

Establish stable contracts before persistence or UI work begins.

Tasks:

1. Add the Mood module shell and registration after Travel.
2. Freeze entry routes, dashboard routes, enum names, DTOs, query contracts,
   stable error codes, and owner-only authorization behavior.
3. Define backend period contracts for `Year`, `Semester`, `Quarter`, and
   `Month`, including strict period identifiers.
4. Define frontend API, validation-schema, route-state, chart-data, and query-key
   skeletons.
5. Add architecture-test expectations for Mood dependency direction: Mood may
   consume Identity/current-user, shared API primitives, persistence, and
   platform time contracts, but must not depend on Configuration, Attachments,
   Launcher, Analytics, Capex, Opex, Inventory, or Travel internals.
6. Add empty backend and frontend test fixtures shared by later Waves.

Tests:

- Unit tests for enum values, route constants, DTO validation boundaries, query
  bounds, period identifiers, and error-code stability.
- Architecture tests for permitted dependencies and the absence of prohibited
  module dependencies.

Exit criteria:

- Public contracts are explicit and test-covered, and later Waves do not need to
  invent new route, period, or ownership semantics.

### Wave 1: Domain, Persistence, And Emotion Matrix

Implement the Mood data model, code-backed matrix, and provider migrations.

Tasks:

1. Add `MoodEntry` with standard metadata and owner-only persistence shape.
2. Add fixed criteria enums for Energy, Alignment, Direction, and Source,
   including `Offensive` spelling in code and API contracts.
3. Implement domain validation for score, criteria values, entry date, and notes.
4. Translate the initial CSV source into a module-owned code-backed
   derived-emotion matrix.
5. Expose a service or pure function that maps a criteria combination to a
   stable derived-emotion code.
6. Add provider-specific SQLite and PostgreSQL migrations and model snapshots.
7. Add indexes for owner/date range lookup, same-day insertion ordering, and
   aggregate queries.

Tests:

- Unit tests for score validation, note length, enum values, date freedom, and
  insertion-order comparison.
- Matrix tests proving exactly 72 mappings, no duplicate criteria combinations,
  no missing criteria combinations, stable codes, and failure behavior for
  impossible combinations.
- SQLite and PostgreSQL migration tests, including fresh creation and model
  parity.

Exit criteria:

- Both providers persist Mood entries, and the derived-emotion matrix is
  complete, deterministic, and independent from runtime CSV loading.

### Wave 2: Entry APIs And Weekly Log Backend

Deliver the owner-only entry workflow and weekly log query.

Tasks:

1. Implement create, read, update, and delete for current-user Mood entries.
2. Implement inclusive date-range entry queries for the current user.
3. Ensure entry list results include derived emotion codes calculated in the
   read projection.
4. Enforce owner-only access for detail, update, delete, and list queries.
5. Implement `Europe/Madrid` week helpers for Monday-to-Sunday log ranges.
6. Implement weekly average score per day, treating days without entries as
   missing data rather than zero.
7. Review OpenAPI metadata and stable validation outcomes.

Tests:

- API integration tests for create, detail, update, delete, validation failures,
  no date boundary, note length, and derived-emotion projection.
- Two-user privacy tests proving users and administrators cannot view, mutate,
  list, or infer another user's entries.
- Range-query tests for inclusive bounds, Monday-to-Sunday week ranges,
  insertion ordering within the same date, and missing-day chart behavior.

Exit criteria:

- The backend supports the complete Log workflow with authoritative validation,
  owner-only privacy, derived emotions, and weekly chart data.

### Wave 3: Dashboard Aggregate Backend

Implement strict-period trend aggregation for the current user.

Tasks:

1. Implement dashboard period resolution for Year, Semester, Quarter, and Month.
2. Implement previous and next period calculations for every scale.
3. Implement score minimum, average, and maximum by day of week for the selected
   period.
4. Implement score minimum, average, and maximum by month for Year, Semester, and
   Quarter scales.
5. Implement score minimum, average, and maximum by week for Month scale.
6. Implement distribution aggregates for Energy, Alignment, Direction, and Source.
7. Implement criteria evolution aggregates by month for Year, Semester, and
   Quarter scales, and by week for Month scale.
8. Ensure all aggregates operate only on the current user's entries and only
   inside the selected strict calendar period.

Tests:

- Unit tests for strict period boundaries, current-period defaults, previous and
  next period navigation, leap-year behavior, and month weeks.
- API integration tests for no-data periods, future periods, mixed-user data,
  min/average/max calculations, criteria distributions, and criteria evolution.
- PostgreSQL coverage for provider-sensitive grouping and average calculations.

Exit criteria:

- The backend can serve every Dashboard chart from current-user data with stable,
  strict-period semantics.

### Wave 4: Frontend Foundation And Log View

Build the Mood route, API client, translations, and weekly Log experience.

Tasks:

1. Add the lazy `/mood` route, module error boundary, translation namespace, and
   launcher card without attention.
2. Implement Mood API client functions, TanStack Query keys, Zod schemas, enum
   option mapping, and derived-emotion translation keys.
3. Build the internal Mood navigation between Log and Dashboard.
4. Build the weekly Log view with URL-backed selected week, previous/next/today
   controls, Monday-to-Sunday display, and all owned entries for the selected
   week.
5. Build the compact weekly average score chart.
6. Build the entry dialog with React Hook Form and Zod, including create, edit,
   delete, dirty-close confirmation, and validation/error mapping.
7. Keep notes visible only inside the entry dialog.
8. Refresh the selected week and affected dashboard queries after successful
   mutations.

Tests:

- Frontend API and component tests for route state, week navigation, default
  today date, enum rendering, derived-emotion translation, missing-day chart
  display, dialog validation, dirty-close confirmation, mutation feedback, and
  privacy-safe errors.
- Accessibility tests for tab navigation, dialog focus, field labels, error
  associations, and chart alternatives.

Exit criteria:

- Users can complete the full weekly Log workflow without page reloads while
  preserving week and dialog state.

### Wave 5: Dashboard Frontend

Build the user-facing Dashboard trend experience.

Tasks:

1. Build the Dashboard screen with scale selection for Year, Semester, Quarter,
   and Month.
2. Implement current-period defaults, previous/next navigation, and scale-change
   reset to the current period.
3. Wire dashboard aggregate queries and no-data states.
4. Build score min/average/max charts by day of week and by month or week,
   depending on selected scale.
5. Build separate distribution charts for Energy, Alignment, Direction, and
   Source.
6. Build criteria evolution charts by month or week, depending on selected scale.
7. Verify chart labels, legends, colour use, responsive bounds, and localized
   period labels follow the design system and remain readable.

Tests:

- Frontend component tests for scale selection, period navigation, query
  parameters, no-data states, score chart rendering, distribution chart
  rendering, and evolution chart rendering.
- Accessibility and layout tests for controls, chart summaries, keyboard
  navigation, and narrow desktop widths.

Exit criteria:

- Users can inspect Mood trends across all supported strict calendar periods,
  with no cross-user or cross-module data dependencies.

### Wave 6: End-To-End, Hardening, And Acceptance

Validate the implemented behavior across both providers and the deployed
frontend/backend boundary.

Tasks:

1. Run backend format, build, unit, API, PostgreSQL, migration, and architecture
   suites through repository scripts.
2. Run frontend format, lint, type-check, unit, build, and Playwright suites.
3. Add a representative Playwright journey covering login, opening Mood,
   creating multiple entries on one day, verifying insertion order and derived
   emotion display, editing an entry, deleting an entry, navigating weeks, and
   viewing dashboard periods.
4. Add or update backend tests for second-user and administrator isolation.
5. Review OpenAPI for Mood entry, options, and dashboard routes.
6. Verify keyboard behavior, dialog scrolling, chart no-data states, selected
   period URL state, and narrow desktop widths.
7. Map every criterion in `docs/requirements/MOOD_REQUIREMENTS.md` to covering
   code and tests in a Mood acceptance record.
8. Update `ROADMAP.md` to mark Mood as implemented and accepted and to record
   only intentional remaining deferrals.

Exit criteria:

- All relevant repository scripts pass, both providers are covered, the Compose
  journey succeeds, and every accepted Mood requirement is implemented or
  explicitly deferred.

## Suggested Pull Request Boundaries

1. Mood contracts, persistence, and derived-emotion matrix (Waves 0-1).
2. Entry APIs, weekly log backend, and dashboard aggregate backend (Waves 2-3).
3. Mood frontend Log view (Wave 4).
4. Mood frontend Dashboard view (Wave 5).
5. End-to-end, hardening, and acceptance (Wave 6).

## Plan Completion Criteria

The plan is complete when all Waves meet their exit criteria and the Mood
requirements document describes implemented behavior rather than only functional
intent.

Launcher attention, attachments, Analytics integration, configurable criteria,
correlation matrices, weekly filters, richer calendar visualisations, and
personal-data export remain separate future planning topics.
