# Calendar Implementation Plan

## Purpose

This plan delivers the initial Calendar module defined in
`docs/requirements/CALENDAR_REQUIREMENTS.md`. It translates the accepted
functional decisions into dependency-ordered Waves with explicit backend,
frontend, migration, and test work.

The requirements document remains authoritative for behaviour. This plan owns
delivery order and technical scope.

## Delivery Principles

- Preserve Calendar as a cross-domain read module. Projected entries remain owned
  by their source modules; Calendar aggregates read projections and owns only its
  manual daily notes.
- Keep the dependency direction explicit: Calendar consumes narrow published
  projection contracts from Firebird, Travel, Inventory, Assets, Maintenance, and
  Processes. Source modules do not query Calendar and do not persist Calendar
  state.
- Do not introduce a generic persisted event table, generic reminders, recurrence
  engine, notification inbox, or shared-core event abstraction.
- Keep Capex and Opex outside the initial projection set.
- Treat trips as date ranges in the Calendar projection while keeping Travel's
  persisted trip model unchanged.
- Make all projection queries current-user scoped at the source-module boundary so
  Calendar never receives inaccessible records.
- Reuse established privacy, REST, pagination/query validation, URL-aware dialog,
  route-state, i18n, TanStack Query, React Hook Form, and Zod conventions where
  their semantics match.
- Keep each Wave independently testable and record deferrals in `ROADMAP.md`
  instead of hiding them in implementation notes.

## Fixed Technical Contracts

### Backend Module

Calendar lives under `Segaris.Api.Modules.Calendar` and owns:

- Calendar API endpoint registration.
- Calendar entry aggregation across source projection providers.
- Calendar daily-note persistence, validation, authorization, and routes.
- Calendar-specific transport DTOs and stable error codes.
- Calendar range and filter validation.
- Calendar frontend-facing source-module and visual-family vocabularies.

Calendar does not own source records, source statuses, source deletion behaviour,
or source-module mutation logic. It contributes no launcher attention in the
initial version.

Indicative resource routes are:

```text
GET    /api/calendar/entries

GET    /api/calendar/notes
POST   /api/calendar/notes
GET    /api/calendar/notes/{noteId}
PUT    /api/calendar/notes/{noteId}
DELETE /api/calendar/notes/{noteId}
```

`GET /api/calendar/entries` accepts an inclusive civil-date range and optional
allow-listed source-module and visual-family filters. It returns normalized
Calendar entry DTOs containing projected entries and accessible daily notes.

Daily note routes are available separately so the frontend can create, update,
delete, and refetch note details without depending on a mixed projection response.
All writes require antiforgery. Missing and inaccessible notes share the platform
not-found behaviour so private notes are not disclosed.

### Projection Contracts

Calendar consumes one narrow projection contract per participating source module.
Each contract is owned by its source module and returns only current-user
authorized projections for an inclusive civil-date range.

The normalized Calendar response contains at least:

- `id`: a stable Calendar response identifier, derived from the source and source
  reference for projected entries and from the note identifier for daily notes.
- `sourceModule`: one of the accepted source-module codes.
- `sourceType`: a source-specific type code.
- `visualFamily`: `Birthday`, `Travel`, `Note`, or `Other`.
- `title`.
- Optional `subtitle`.
- `startDate`.
- Optional `endDate`.
- `isAllDay`, defaulting to `true` for initial projections.
- Optional `status`.
- Optional `targetRoute`.

The normalized DTO is a Calendar API contract, not a shared domain model. Source
modules may expose their own projection records and Calendar maps them into the
frontend contract.

### Source Projection Rules

The initial projection set is fixed as follows:

- Firebird publishes birthdays for accessible people with birthdays, using the
  source-owned birthday occurrence and leap-day behaviour.
- Travel publishes accessible trips whose status is not `Cancelled` and whose
  `StartDate`/`EndDate` range intersects the requested range.
- Inventory publishes accessible orders whose status is `Planning` or `Active`
  and whose `ExpectedReceiptDate` is set.
- Assets publishes accessible assets whose status is not `Retired` and whose
  `ExpectedEndOfLifeDate` is set, including past dates inside the requested range.
- Maintenance publishes accessible tasks whose status is `Pending` or
  `InProgress` and whose `DueDate` is set.
- Processes publishes accessible pending steps with a `DueDate`, excludes process
  global due dates, excludes resolved steps, and excludes completed or cancelled
  processes.

Each source projection query must be provider-compatible on SQLite and PostgreSQL
and must avoid unbounded reads. Calendar enforces a maximum request range before
calling source contracts.

### Persistence

Calendar persists only manual daily notes in provider-specific migrations.

The model contains:

- `CalendarDailyNote`

Daily notes store the note date, optional title, required body, visibility, and
standard audit metadata. Indexes must support date-range lookup, creator/privacy
filtering, and deterministic ordering by date and identifier.

Projected entries are not persisted by Calendar. They are computed from source
module data on request.

### Frontend Routes

Calendar uses the protected lazy route `/calendar`.

One practical route shape is:

```text
/calendar
/calendar?month=2026-06
/calendar?month=2026-06&day=2026-06-24
/calendar?month=2026-06&day=2026-06-24&newNote=true
/calendar?month=2026-06&day=2026-06-24&noteId=12
```

If the shared shell or router ergonomics suggest a slightly different parameter
layout, preserve the same behaviour: selected month, selected day, active filters,
and open note dialogs must survive refresh and dialog open/close without a full
page reload.

## Waves

### Wave 0: Contracts And Test Skeleton

Establish stable Calendar contracts before persistence or UI work begins.

Tasks:

1. Add the Calendar module shell and registration after Health and before future
   Analytics implementation work.
2. Freeze route constants, source-module codes, source-type codes for the initial
   projections, visual-family codes, query parameters, stable error codes, and the
   absence of any launcher attention key.
3. Define the Calendar aggregation DTOs and internal normalized projection model.
4. Define source-owned projection contract shapes for Firebird, Travel, Inventory,
   Assets, Maintenance, and Processes without exposing source entities.
5. Define Calendar frontend API, validation-schema, route-state, filter-state, and
   query-key skeletons.
6. Add architecture-test expectations: Calendar may consume the six participating
   modules and platform contracts; Capex, Opex, Recipes, Destinations, Health,
   Clothes, Mood, and Projects do not participate in initial Calendar projections.
7. Add empty backend and frontend test fixtures shared by later Waves.

Tests:

- Unit tests for route constants, visual-family/source-code stability, date-range
  bounds, filter parsing, and error-code stability.
- Architecture tests for permitted dependencies and the absence of Calendar
  persistence dependencies from source modules.

Exit criteria:

- Public contracts are explicit and test-covered, and later Waves do not need to
  invent route, source-code, visual-family, or dependency semantics.

### Wave 1: Daily Note Domain, Persistence, And APIs

Implement Calendar-owned daily notes on both providers.

Tasks:

1. Add `CalendarDailyNote` with date, optional title, required body, visibility,
   and standard audit metadata.
2. Enforce the `Private` creation default, creator-only visibility change, public
   collaboration, private isolation, and not-found privacy behaviour.
3. Implement date-range note listing and note detail retrieval.
4. Implement create, update, and delete with full validation and antiforgery.
5. Add provider-specific migrations and model snapshots.
6. Add indexes for accessible note date-range queries and deterministic ordering.

Tests:

- Domain tests for defaults, title/body bounds, trimming, and visibility rules.
- API integration tests for create, read, update, delete, date-range filtering,
  public collaboration, private isolation, creator-only visibility change, and
  not-found privacy behaviour.
- SQLite and PostgreSQL migration tests, including fresh creation and upgrade
  coverage.

Exit criteria:

- Users can fully manage Calendar-owned daily notes through the backend with
  correct validation, privacy, and provider-compatible persistence.

### Wave 2: Calendar Aggregation Endpoint

Deliver the Calendar backend aggregation endpoint with daily notes and a test
projection provider.

Tasks:

1. Implement `GET /api/calendar/entries` with inclusive `from`/`to` civil-date
   query parameters and bounded maximum range.
2. Implement allow-listed `sourceModule` and `visualFamily` filters.
3. Add aggregation over registered projection providers plus Calendar daily notes.
4. Normalize daily notes into `Note` visual-family entries.
5. Apply deterministic final ordering across mixed sources.
6. Return structured validation errors for malformed dates, reversed ranges,
   unsupported filters, and over-large ranges.
7. Add a fake/test projection provider for backend aggregation tests before source
   modules are wired.

Tests:

- API integration tests for range validation, maximum range enforcement, filters,
  deterministic ordering, daily-note normalization, and mixed-source aggregation
  using the fake provider.
- Privacy tests proving inaccessible private notes are absent from the mixed entry
  response.

Exit criteria:

- Calendar exposes one frontend-facing entries endpoint that correctly validates,
  aggregates, filters, and orders entries before real source-module projections are
  connected.

### Wave 3: Source Projection Providers

Wire the initial source modules into Calendar.

Tasks:

1. Implement Firebird birthday projection using the accepted birthday occurrence
   rules, including leap-day handling delegated to Firebird logic.
2. Implement Travel trip range projection for accessible non-`Cancelled` trips
   intersecting the requested range.
3. Implement Inventory expected-receipt projection for accessible `Planning` and
   `Active` orders with `ExpectedReceiptDate`.
4. Implement Assets expected-end-of-life projection for accessible non-`Retired`
   assets with `ExpectedEndOfLifeDate`.
5. Implement Maintenance due-date projection for accessible `Pending` and
   `InProgress` tasks with `DueDate`.
6. Implement Processes pending-step due-date projection, excluding global process
   due dates, resolved steps, completed processes, and cancelled processes.
7. Provide safe target routes for each projection where existing frontend routes
   can open the source record.
8. Add provider-compatible indexes only where existing source-module indexes are
   insufficient for the new date-range projection queries.

Tests:

- Source-module integration tests for every projection rule, including boundary
  dates, status exclusions, visibility filtering, and target-route shape.
- Two-user tests proving each source projection excludes another user's private
  records.
- SQLite and PostgreSQL coverage for provider-sensitive date-range queries.
- Architecture tests confirming Calendar consumes source contracts without source
  modules depending on Calendar persistence or querying Calendar data.

Exit criteria:

- Calendar returns the complete accepted initial projection set from all source
  modules with correct date, status, visibility, and target-route behaviour.

### Wave 4: Frontend Calendar Shell And Month Grid

Build the user-facing Calendar route and month grid.

Tasks:

1. Add the lazy `/calendar` route, module error boundary, translation namespace,
   and launcher card with no attention state.
2. Build URL-backed month navigation, selected-day state, today action, and
   previous/next month actions.
3. Build the Monday-first month grid with adjacent days filling complete weeks,
   current-day highlighting, selected-day highlighting, and empty/loading/error
   states.
4. Fetch Calendar entries for the visible grid range, including adjacent days.
5. Map entry visual families into compact day indicators for `Birthday`, `Travel`,
   `Note`, and `Other`, showing multiple indicators where practical.
6. Implement the priority-plus-more fallback for constrained layouts without
   changing day-detail contents.
7. Add source-module and visual-family filter controls backed by URL or stable
   route state.

Tests:

- Frontend API and component tests for route state, month calculations,
  Monday-first layout, adjacent-day range queries, current/selected day states,
  loading/error states, filters, and day indicators.
- Accessibility tests for month navigation, focus order, keyboard selection, and
  indicator labelling.

Exit criteria:

- Users can open Calendar, navigate months, select days, see visual indicators,
  and filter visible entries without page reloads.

### Wave 5: Day Detail And Source Navigation

Deliver the selected-day detail experience for projected entries and notes.

Tasks:

1. Build the day detail surface opened from the selected day.
2. List every accessible entry for the selected day, including entries inside a
   Travel trip range.
3. Group or visually label entries by source module or visual family.
4. Show title, supporting text, source/status metadata, and visual family in a
   compact but scannable layout.
5. Wire source-record navigation for projections with a safe `targetRoute`.
6. Show informational-only treatment for projections without a target route.
7. Preserve selected month, selected day, filters, and detail state across source
   navigation and browser history where practical.

Tests:

- Component tests for day detail grouping, mixed entry types, Travel range entries,
  source navigation, informational-only entries, filters, and URL state.
- Accessibility tests for dialog or panel focus, keyboard operation, and action
  labelling.

Exit criteria:

- Selecting a day exposes the complete entry list for that day and lets users move
  to source records when the projection supports it.

### Wave 6: Frontend Daily Note Management

Build the Calendar-owned manual daily-note workflow.

Tasks:

1. Build the note editor with React Hook Form and Zod for date, title, body, and
   visibility.
2. Use `Private` as the frontend creation default.
3. Open create/edit note dialogs through URL-aware state over the Calendar view.
4. Wire create, update, and delete mutations with cache invalidation for note
   detail, day detail, and month entries.
5. Add destructive delete confirmation and dirty-close confirmation.
6. Surface validation, authorization, and not-found failures without losing user
   input.
7. Ensure note indicators and day detail update immediately after note mutations.

Tests:

- Frontend API and component tests for note defaults, validation, create/update,
  delete confirmation, dirty-close confirmation, privacy-safe errors, cache
  invalidation, and URL-aware dialog state.
- Accessibility tests for editor focus, field labels, validation errors, and
  keyboard operation.

Exit criteria:

- Users can create, edit, delete, and share or privatize daily notes from Calendar
  while preserving calendar state.

### Wave 7: End-To-End, Hardening, And Acceptance

Validate the implemented behaviour across both providers and the deployed
frontend/backend boundary.

Tasks:

1. Run backend format, build, unit, API, PostgreSQL, migration, and architecture
   suites through repository scripts.
2. Run frontend format, lint, type-check, unit, build, and Playwright suites.
3. Add a representative Playwright journey covering login, opening Calendar,
   navigating the month grid, creating a private note, making it public, seeing
   note indicators and day detail, observing at least two projected entry families
   from seeded or test-created source records, opening a source record, and
   deleting the note.
4. Review OpenAPI for Calendar entries and note routes plus source projection
   query changes.
5. Verify keyboard behaviour, month-grid layout, day detail scrolling, filters,
   narrow desktop widths, and day indicator fallback behaviour.
6. Map every criterion in `docs/requirements/CALENDAR_REQUIREMENTS.md` to covering
   code and tests in a Calendar acceptance record.
7. Update `ROADMAP.md` to mark Calendar as implemented and accepted and record only
   intentional remaining deferrals.

Exit criteria:

- All relevant repository scripts pass, both providers are covered, the Compose
  journey succeeds, and every accepted Calendar requirement is implemented or
  explicitly deferred.

## Suggested Pull Request Boundaries

1. Calendar contracts, daily-note persistence, and note APIs (Waves 0-1).
2. Calendar aggregation endpoint and source projection providers (Waves 2-3).
3. Calendar frontend month grid and day detail (Waves 4-5).
4. Daily-note frontend workflow (Wave 6).
5. End-to-end, hardening, and acceptance (Wave 7).

## Plan Completion Criteria

The plan is complete when all Waves meet their exit criteria and the Calendar
requirements document describes implemented behaviour rather than only functional
intent.

Travel itinerary entries, Capex/Opex projections, Recipes weekly menus, agenda,
week, or year views, daily-note recurrence, attachments, tags, reminders, links to
source records, launcher attention, external calendar synchronization, import,
export, ICS feeds, and projection materialization remain separate future planning
topics.
