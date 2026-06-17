# Travel Implementation Plan

## Purpose

This plan delivers the initial Travel module defined in
`docs/requirements/TRAVEL_REQUIREMENTS.md`. It translates the accepted functional
decisions into dependency-ordered Waves with explicit backend, frontend,
migration, and test work.

The requirements document remains authoritative for behavior. This plan owns
delivery order and technical scope.

## Delivery Principles

- Preserve Travel as an independent business module.
- Reuse established Configuration, Launcher, Attachments, privacy, REST,
  pagination, and frontend conventions where their semantics match.
- Do not introduce bookings as entities, packing lists, travellers, budgets,
  automatic currency conversion, external calendar integration, or
  cross-business-module write dependencies.
- Keep visibility inheritance, currency-per-expense, and the manual status model
  explicit in backend validation rather than inferred only by the frontend.
- Keep each Wave independently testable and record deferrals in `ROADMAP.md`
  instead of hiding them in implementation notes.

## Fixed Technical Contracts

### Backend Module

Travel lives under `Segaris.Api.Modules.Travel` and owns trip, itinerary entry,
expense, trip type, expense category, attachment authorization, Configuration
reference handling, and launcher attention logic.

Indicative resource routes are:

```text
GET    /api/travel/trips
POST   /api/travel/trips
GET    /api/travel/trips/{tripId}
PUT    /api/travel/trips/{tripId}
DELETE /api/travel/trips/{tripId}

GET    /api/travel/trips/{tripId}/expenses
POST   /api/travel/trips/{tripId}/expenses
GET    /api/travel/trips/{tripId}/expenses/{expenseId}
PUT    /api/travel/trips/{tripId}/expenses/{expenseId}
DELETE /api/travel/trips/{tripId}/expenses/{expenseId}

GET    /api/travel/trip-types
GET    /api/travel/expense-categories
```

The embedded itinerary is delivered as part of the trip representation and is
replaced through the trip create and update payloads; it has no independent
route. Administrative trip-type and expense-category routes follow the existing
module-owned catalog management pattern exposed through Configuration.

All writes require antiforgery. Missing and inaccessible records share the
platform not-found behavior so private data is not disclosed.

### Persistence

The model contains module-owned entities with provider-specific migrations:

- `TravelTrip`
- `TravelItineraryEntry`
- `TravelExpense`
- `TravelTripType`
- `TravelExpenseCategory`

Trips store the type, destination, civil start and end dates, status,
visibility, and notes. Itinerary entries store the owning trip, civil date,
optional time-of-day, title, place, reservation locator, note, and a stable
ordering key. Expenses store the owning trip, category, description, civil date,
amount, currency, optional supplier, optional cost centre, and notes.

Itinerary entries and expenses have no visibility column; they inherit the trip.
Itinerary entries and expenses are deleted by cascade when their trip is deleted.

The initial model has no booking table, no budget column, and no normalised trip
total.

Indexes must support trip filters, deterministic sorting, launcher attention
(status plus start date), expense lookups per trip, per-currency aggregation, and
trip-type/expense-category/supplier/currency/cost-centre reference migration.

### Frontend Route

Travel uses the protected lazy route `/travel`.

The initial UI should support URL-backed list state and dialog state for trips,
following the Capex, Opex, and Inventory pattern. One practical route shape is:

```text
/travel
/travel?tripId=123
/travel?newTrip=true
```

Expense create and edit happen inside the trip dialog context. If the shared
shell or router ergonomics suggest a slightly different parameter layout,
preserve the same behavior: list state must survive dialog open and close
without a reload.

### Configuration Integration

Configuration remains the owner of shared suppliers, currencies, and cost
centres. Travel owns its trip types and expense categories while exposing them
through the established Configuration presentation boundary.

Travel must register narrow catalog reference handlers for:

- Trip type
- Expense category
- Supplier
- Currency
- Cost centre

Currency is required on every expense, so a referenced currency may only be
replaced, and replacement converts the affected expense amounts using the
authoritative manual exchange rate. Supplier and cost centre are optional, so a
referenced supplier or cost centre may be either replaced or cleared. Trip-type
and expense-category replacement re-points the affected trips and expenses.

## Waves

### Wave 0: Contracts And Test Skeleton

Establish stable contracts before persistence or UI work begins.

Tasks:

1. Add the Travel module shell and registration after Inventory.
2. Freeze trip, itinerary, and expense routes, enums, DTOs, query contracts,
   stable error codes, attention contracts, and attachment owner kinds
   (`Trip`, `Expense`).
3. Define Configuration-facing contracts for trip-type, expense-category,
   supplier, currency, and cost-centre reference handling without exposing Travel
   entities. Shared reference kinds are `Suppliers`, `Currencies`, and
   `CostCenters`.
4. Define frontend API, validation-schema, route-state, and query-key skeletons.
5. Add architecture-test expectations for Travel dependency direction: Travel may
   consume Configuration and platform contracts but must not depend on Capex,
   Opex, or Inventory.
6. Add empty backend and frontend test fixtures shared by later Waves.

Tests:

- Unit tests for enum values, defaults, route constants, query bounds, and
  error-code stability.
- Architecture tests for permitted dependencies and published contracts.

Exit criteria:

- Public contracts are explicit and test-covered, and later Waves do not need to
  invent new route or ownership semantics.

### Wave 1: Domain, Persistence, And Catalogs

Implement the Travel data model and module-owned catalogs on both providers.

Tasks:

1. Add `TravelTrip`, `TravelItineraryEntry`, `TravelExpense`, `TravelTripType`,
   and `TravelExpenseCategory`.
2. Enforce required relationships, the `EndDate >= StartDate` invariant, decimal
   precision, bounded strings, deterministic itinerary and expense ordering, and
   standard audit metadata.
3. Seed the accepted initial trip-type and expense-category values once.
4. Implement module-owned trip-type and expense-category reads plus administrator
   mutations through Configuration, including final-row protection.
5. Add provider-specific migrations and model snapshots.
6. Add indexes for trip filters, attention, per-trip expense lookups, and
   reference migration.

Tests:

- Domain tests for the date invariant, status values, itinerary ordering, and
  expense ownership.
- SQLite and PostgreSQL migration tests, including upgrades and idempotent
  catalog initialization.
- Integration tests for trip-type and expense-category ordering, uniqueness,
  final-row protection, and administrator authorization.

Exit criteria:

- Both providers persist the complete model and expose configurable trip-type and
  expense-category catalogs.

### Wave 2: Trip Read APIs, Totals, And Attention

Deliver the core read workflow first, including per-currency totals and
launcher attention.

Tasks:

1. Implement paginated trip list and trip detail queries, including the embedded
   itinerary and the per-currency expense totals in the detail projection.
2. Implement partial search across trip name, destination, and notes.
3. Implement exact filters for trip type, status, visibility, and creator.
4. Implement deterministic sorting and the default trip ordering (start date
   descending, then identifier descending).
5. Implement the Travel launcher attention contributor using accessible trips
   that are `Ongoing`, or `Planned` with a start date within the next seven days
   in `Europe/Madrid`.

Tests:

- API integration tests for trip pagination, filters, search, sorting, visibility
  isolation, not-found privacy behavior, and per-currency total aggregation.
- Attention tests for ongoing trips, planned trips inside and outside the seven-day
  window, completed and cancelled trips, public/private trips, and the date
  boundary in `Europe/Madrid`.

Exit criteria:

- Users can browse accessible trips with correct totals, and the launcher can
  compute Travel attention authoritatively from the backend.

### Wave 3: Trip Mutations, Itinerary, Attachments, And Privacy Guards

Complete the trip backend contract, including the embedded itinerary.

Tasks:

1. Implement create, update, and delete for trips with full validation and the
   documented creation defaults.
2. Implement embedded itinerary full-collection replacement with ordering,
   bounded entry count, and reservation locators.
3. Enforce trip-type validity, the date invariant, and visibility transitions.
4. Cascade itinerary entries and owned attachments on trip deletion.
5. Add trip attachment listing, upload, download, and delete routes.
6. Review and document OpenAPI metadata and stable validation outcomes.

Tests:

- API tests for defaults, required fields, itinerary replacement and ordering,
  date-invariant rejection, visibility transitions, and deletion cascade.
- Two-user privacy tests for public collaboration and private isolation at the
  trip level, including itinerary access through the trip.
- Attachment tests for round-trip behavior, authorization, validation failures,
  and filesystem cleanup on trip deletion.

Exit criteria:

- The complete trip-plus-itinerary contract behaves correctly for validation,
  privacy, and attachments.

### Wave 4: Expense Sub-Resource

Deliver per-trip expenses with their own lifecycle and attachments.

Tasks:

1. Implement paginated or trip-scoped expense list and expense detail queries.
2. Implement create, update, and delete for expenses with full validation.
3. Enforce required currency and category, and validate optional supplier and
   cost-centre references.
4. Enforce visibility inheritance: expense access is governed entirely by the
   parent trip, and expense mutations follow trip authorization.
5. Recompute and expose per-currency trip totals after expense changes.
6. Add expense attachment listing, upload, download, and delete routes, with
   cleanup on expense and trip deletion.

Tests:

- API tests for expense defaults, validation, amount precision, currency and
  category requirements, optional supplier/cost-centre handling, and total
  recomputation.
- Privacy tests confirming expense access strictly follows the parent trip.
- Attachment tests for round-trip behavior and delete cleanup at both expense and
  trip level.

Exit criteria:

- Users can manage the full per-trip expense workflow through the backend with
  correct validation, privacy inheritance, and per-currency totals.

### Wave 5: Configuration Reference Migration

Integrate Travel safely into structural catalog management.

Tasks:

1. Register reference handlers for Travel trip types and expense categories using
   the existing module-owned catalog-management pattern.
2. Register supplier, currency, and cost-centre reference handlers for Travel
   expenses.
3. Implement required-currency replacement that converts affected expense amounts
   using the authoritative manual exchange rate.
4. Implement optional supplier and cost-centre replacement-or-clear semantics.
5. Re-evaluate references in the confirming transaction and roll back on any
   failure.
6. Invalidate affected Travel and Configuration frontend queries after successful
   structural mutations.

Tests:

- Cross-module integration tests for trip-type replacement, expense-category
  replacement, supplier replacement and clearing, cost-centre replacement and
  clearing, currency replacement with amount conversion, rollback, and
  privacy-neutral impact reporting.
- SQLite and PostgreSQL coverage for atomic migration behavior.

Exit criteria:

- Configuration can safely mutate or delete every catalog value referenced by
  Travel without exposing private data or leaving partial updates behind.

### Wave 6: Travel Frontend

Build the user-facing Travel module experience.

Tasks:

1. Add the lazy `/travel` route, module error boundary, translation namespace,
   and launcher card with attention.
2. Build the trips table with URL-backed search, filters, sorting, and
   pagination.
3. Build the trip dialog with React Hook Form and Zod, covering trip details, the
   embedded itinerary editor with reservation locators, visibility guards, and
   trip attachments.
4. Build the per-trip expense list with add, edit, and delete, optional
   supplier/cost-centre selection, expense attachments, and per-currency totals.
5. Wire deletion confirmation, list invalidation, and attention refresh.

Tests:

- Frontend API and component tests for route state, filters, sorting, pagination,
  trip and expense validation, privacy-safe errors, itinerary ordering, currency
  totals, and attachment flows.
- Accessibility tests for dialog focus, keyboard operation, error association, and
  the itinerary and expense sub-editors.

Exit criteria:

- Users can complete the full Travel trip, itinerary, and expense workflow without
  page reloads while preserving list state.

### Wave 7: End-To-End, Hardening, And Acceptance

Validate the implemented behavior across both providers and the deployed
frontend/backend boundary.

Tasks:

1. Run backend format, build, unit, API, PostgreSQL, migration, and architecture
   suites through repository scripts.
2. Run frontend format, lint, type-check, unit, build, and Playwright suites.
3. Add a representative Playwright journey covering login, opening Travel,
   creating a trip, adding itinerary entries with a locator, adding expenses in
   more than one currency, checking per-currency totals, and deleting safe test
   data.
4. Review OpenAPI for Travel trip, itinerary, expense, catalog, and attachment
   routes.
5. Verify keyboard behavior, dialog scrolling, attention updates, filtered list
   invalidation, and narrow desktop widths.
6. Map every criterion in `docs/requirements/TRAVEL_REQUIREMENTS.md` to covering
   code and tests in a Travel acceptance record.
7. Update `ROADMAP.md` to mark Travel as implemented and accepted and to record
   only intentional remaining deferrals.

Exit criteria:

- All relevant repository scripts pass, both providers are covered, the Compose
  journey succeeds, and every accepted Travel requirement is implemented or
  explicitly deferred.

## Suggested Pull Request Boundaries

1. Travel contracts, persistence, and module-owned catalogs (Waves 0-1).
2. Trip reads, totals, attention, mutations, itinerary, and attachments
   (Waves 2-3).
3. Expense sub-resource (Wave 4).
4. Configuration reference migration (Wave 5).
5. Travel frontend (Wave 6).
6. End-to-end, hardening, and acceptance (Wave 7).

## Plan Completion Criteria

The plan is complete when all Waves meet their exit criteria and the Travel
requirements document describes implemented behavior rather than only functional
intent.

Bookings as entities, packing lists, travellers, budgets, automatic currency
conversion, external calendar integration, Analytics integration, and
cross-module links remain separate future planning topics.
