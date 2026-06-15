# Configuration Implementation Plan

## Purpose

This plan translates `docs/requirements/CONFIGURATION_REQUIREMENTS.md` into a
dependency-ordered implementation backlog for shared Configuration catalogs,
Capex category management, reference migration, the administrative frontend,
provider migrations, and automated acceptance.

It builds on:

- `docs/architecture/backend.md` and
  `docs/planning/BACKEND_MODULE_CONVENTIONS.md`.
- `docs/architecture/domain-organization.md`.
- `docs/architecture/data-and-storage.md`.
- `docs/architecture/frontend.md`, `user-experience.md`, and
  `design-system.md`.
- `docs/requirements/CAPEX_REQUIREMENTS.md`.
- `docs/planning/CONFIGURATION_CONTRACT_DECISIONS.md`.

## Target Outcome

At the end of this plan:

- Administrators open one Configuration experience with flat Global and Capex
  sections.
- Suppliers, cost centers, currencies, and Capex categories support creation,
  editing, deterministic ordering, safe deletion, and reference migration.
- Optional references can be cleared; required references must be replaced.
- Referenced currencies can be replaced with an explicit exchange rate and
  authoritative Capex recalculation.
- Private records participate in structural migration without being disclosed.
- Catalog initialization runs once and never restores administrator changes.
- SQLite and PostgreSQL schemas, APIs, frontend workflows, and critical browser
  journeys are covered by automated tests.

## Scope Boundaries

Included:

- Removal of non-currency catalog codes and addition of normalized uniqueness
  fields and `SortOrder`.
- One-time catalog initialization metadata and revised startup seeding.
- Administrator CRUD, move, impact, direct-delete, and replace-and-delete APIs.
- Supplier and cost-center replacement or clearing.
- Capex-category replacement.
- Currency conversion and replacement as an advanced wave.
- Administrator-only launcher card, route, tables, tabs, dialogs, confirmations,
  toasts, and URL state.
- English (`en-GB`) translations.
- Paired provider migrations and automated acceptance coverage.

Excluded:

- Active/inactive state.
- Generic runtime-defined catalog types.
- Search, pagination, bulk editing, drag and drop, import, or export.
- Explicit default-value configuration.
- Historical catalog labels or migration history.
- External exchange-rate lookup.
- Real-time multi-session synchronization.
- Configuration attention state.
- Spanish translations.

## Fixed Technical Contracts

### Persistence And Upgrade

- Supplier, CostCenter, and CapexCategory retain their integer IDs and names,
  lose `Code`, and gain `NormalizedName` plus `SortOrder`.
- Currency retains editable `Code`, gains `NormalizedName`, `NormalizedCode`,
  and `SortOrder`.
- Existing rows receive `SortOrder` according to ascending ID.
- Existing names and currency codes populate normalized fields.
- Unique normalized indexes are created only after migration validation has
  established that existing rows do not collide case-insensitively.
- An internal initialization table records the four stable catalog keys.
- Both provider histories preserve all existing foreign keys and IDs.

Because dropping code columns and adding uniqueness constraints are potentially
destructive, the release requires the migration safeguards documented in
`data-and-storage.md`, including a current backup and upgrade tests from the
latest production-compatible schema.

### Ownership And Transactions

Configuration owns shared-catalog commands and consumer contracts. Capex
implements shared-reference handlers without exposing records. Capex owns its
category commands directly.

Reference replacement, optional clearing, currency conversion, audit updates,
source deletion, and order normalization execute in one database transaction.
Consumers do not commit independently.

### HTTP And Frontend Contracts

Routes, request shapes, impact responses, frontend URLs, and stable error codes
are frozen in `CONFIGURATION_CONTRACT_DECISIONS.md`.

Read endpoints remain authenticated and available to business forms. Every
management route requires `Admin`; all writes require antiforgery.

## Delivery Strategy

Work is divided into dependency-ordered waves. A wave may span several pull
requests, but its exit criteria should pass before dependent work is considered
stable. Currency conversion is deliberately separated so the normal CRUD and
nonmonetary migration experience can be delivered and tested first.

### Wave 0: Contracts And Test Skeleton

Tasks:

1. Freeze DTOs, route names, catalog kinds, move directions, replacement modes,
   impact shape, error codes, and initialization keys.
2. Define Configuration-owned reference-management interfaces and transaction
   responsibilities.
3. Extend architecture tests to preserve `Capex -> Configuration` and prohibit
   Configuration dependencies on Capex internals.
4. Add shared test fixtures and builders for administrator catalog operations.
5. Record the current database upgrade baseline for both providers.

Tests:

- Contract tests for route constants, error codes, catalog keys, and DTO
  serialization.
- Module-registration and architecture tests.

Exit criteria:

- Later waves can implement behavior without inventing route, ownership,
  privacy, or transaction semantics.

### Wave 1: Catalog Model And One-Time Initialization

Tasks:

1. Add `SortOrder` and normalized uniqueness fields to shared catalogs and
   Capex categories.
2. Remove `Code` from supplier, cost center, and Capex category models and read
   contracts; retain editable currency code.
3. Add catalog domain validation and normalization helpers local to the owning
   modules.
4. Add the internal catalog-initialization model and one-time initialization
   service.
5. Replace upsert-by-code seeding with initialize-empty-once behavior.
6. Update catalog reads and all Capex selectors/defaults to order by
   `SortOrder`, then `Id`.
7. Create paired SQLite and PostgreSQL migrations preserving IDs and existing
   references while backfilling order and normalization.

Tests:

- Unit tests for normalization, case-insensitive duplicates, one-time seeding,
  customized rows, and deliberately empty initialized catalogs.
- Migration tests for fresh databases and upgrades containing existing Capex
  data.
- Provider tests for normalized unique constraints.
- Existing Capex form and API tests updated for the new read shape and default
  selection rule.

Exit criteria:

- Existing installations upgrade without reference changes, startup never
  restores customized catalogs, and all catalog readers use deterministic
  order.

### Wave 2: Shared And Capex CRUD APIs

Tasks:

1. Implement administrator create and update commands for all four catalogs.
2. Implement move-up and move-down transactions using current neighbors.
3. Implement privacy-neutral deletion-impact queries.
4. Implement direct deletion for unreferenced rows with minimum-cardinality and
   concurrent-reference checks.
5. Add explicit OpenAPI metadata and ProblemDetails mappings for every route.
6. Preserve existing authenticated read endpoints for non-admin forms.

Tests:

- API tests proving `Admin` access and normal-user rejection for every
  management route.
- Unit and API tests for trimming, lengths, duplicate names, currency-code
  formatting and editing, append-last behavior, boundary moves, and required
  catalogs.
- Concurrency-focused integration tests showing direct deletion fails if a
  reference appears after impact evaluation.

Exit criteria:

- Administrators can manage unreferenced rows and order through stable APIs,
  while current business forms continue to consume catalogs normally.

### Wave 3: Reference Migration And Optional Clearing

Tasks:

1. Implement Configuration-owned consumer contracts and Capex handlers for
   Supplier and CostCenter.
2. Implement replacement and clear-to-null behavior for optional references.
3. Implement Capex-owned category replacement.
4. Update `UpdatedAt` and `UpdatedBy` for every affected Capex entry.
5. Re-evaluate references, source, and target within the confirming transaction.
6. Delete the source and normalize remaining order only after every migration
   succeeds.
7. Translate stale source/target and consumer failures into stable conflict
   responses.

Tests:

- Integration tests for replacement and clearing across public and private
  entries.
- Tests proving no private counts or record details appear in impact or command
  responses.
- Rollback tests injecting a consumer failure after partial in-memory mutation.
- Audit tests identifying the acting administrator and UTC timestamp.
- PostgreSQL transaction tests for representative reference changes.

Exit criteria:

- Supplier, cost-center, and Capex-category values can be safely removed without
  broken references or privacy disclosure.

### Wave 4: Administrative Frontend

Tasks:

1. Add an administrator-only Configuration launcher card and lazily loaded
   `/configuration` route with module error boundary.
2. Add a `configuration` translation namespace and typed API/query clients.
3. Build flat Global and Capex section navigation with URL-backed Global tabs
   and safe route fallback.
4. Build complete catalog tables with loading, retry, empty, and mutation
   states.
5. Build accessible creation and editing dialogs with dirty-close protection,
   retained input, validation mapping, and focus restoration.
6. Add move controls that preserve focus and stay silent on success.
7. Add direct-delete and replace-or-clear confirmation dialogs without private
   counts or details.
8. Invalidate affected catalogs and known Capex queries after mutations while
   leaving already open forms untouched.

Engineering constraint:

- Reuse the shared table, tabs, dialog, form, button, toast, and focus patterns
  already established by Identity and Capex. Do not create a generic dynamic
  catalog framework for four known catalog types.

Tests:

- Router and launcher visibility tests for admin and normal users.
- Component tests for tabs, URL fallback, empty states, create/edit validation,
  move boundaries/focus, direct deletion, replacement, clearing, error recovery,
  and dirty close.
- Accessibility tests for table actions, dialog naming, keyboard operation,
  focus return, and field-error association.

Exit criteria:

- Administrators can complete all noncurrency management workflows through the
  deployed frontend without reloading the application.

### Wave 5: Currency Conversion And Deletion

Tasks:

1. Extend the shared reference-management contract with currency conversion
   semantics and implement the Capex currency handler.
2. Validate a positive exchange rate with at most eight decimal places.
3. Convert unit amounts, recalculate lines and totals with the existing Capex
   routines, replace `CurrencyId`, and update audit metadata.
4. Keep conversion and currency deletion in one transaction.
5. Extend the migration dialog with the fixed source-to-target formula, target
   selector, exchange-rate input, validation, and irreversible confirmation.
6. Block referenced-currency deletion cleanly until this complete path is
   available; do not ship a partial conversion command.

Tests:

- Unit tests for rate precision, direction, zero/negative rejection, and
  `AwayFromZero` rounding.
- Integration tests for simple and multi-item entries, zero amounts, public and
  private entries, audit updates, and full rollback.
- Provider parity tests for decimal persistence and calculations.
- Frontend tests proving the displayed formula matches the submitted command.

Exit criteria:

- A referenced currency can be removed only through an explicit, correctly
  rounded, privacy-preserving, atomic conversion.

### Wave 6: End-To-End Acceptance And Documentation

Status: delivered. Acceptance evidence is recorded in
`docs/planning/CONFIGURATION_ACCEPTANCE.md`.

Tasks:

1. Run backend format/build/unit/API/PostgreSQL/migration/architecture suites and
   frontend format/lint/type-check/unit/build suites through repository scripts.
2. Add an administrator Playwright journey covering launcher access, section
   navigation, creation, rename, reordering, replacement/clearing, currency
   conversion, and deletion.
3. Add a normal-user journey or route assertion proving Configuration remains
   unavailable.
4. Review generated OpenAPI, antiforgery metadata, admin authorization, and
   privacy-neutral responses.
5. Verify keyboard ordering, dialog focus restoration, tab semantics, narrow
   desktop widths, and table empty/error states.
6. Record acceptance evidence in
   `docs/planning/CONFIGURATION_ACCEPTANCE.md`.
7. Update `README.md` only if repository-wide setup or commands change, and
   update `ROADMAP.md` with delivered or intentionally deferred items.

Exit criteria:

- Every requirement acceptance criterion is mapped to passing evidence, both
  providers are covered, and no deferred behavior is hidden in implementation
  notes.

## Suggested Pull Request Boundaries

1. Contracts, model upgrade, initialization, and provider migrations
   (Waves 0-1).
2. CRUD, ordering, impact, and unreferenced deletion APIs (Wave 2).
3. Reference replacement and optional clearing (Wave 3).
4. Configuration frontend (Wave 4).
5. Currency conversion (Wave 5).
6. End-to-end acceptance and documentation (Wave 6).

## Plan Completion Criteria

The plan is complete when administrators can manage all four catalogs through
the unified experience, every destructive operation preserves references and
privacy, currency conversion uses the agreed calculation rules, one-time
initialization respects administrator customization, both providers pass their
upgrade paths, and all thirteen requirement acceptance criteria have recorded
evidence.

