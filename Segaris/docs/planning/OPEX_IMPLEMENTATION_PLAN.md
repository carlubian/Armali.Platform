# Opex Implementation Plan

## Purpose

This plan delivers the initial Opex module defined in
`docs/requirements/OPEX_REQUIREMENTS.md`. It turns the completed Phase 2
functional decisions into implementation Waves with explicit dependencies,
tests, and exit criteria.

The requirements document remains authoritative for behavior. This plan owns
delivery order and technical scope.

## Delivery Principles

- Preserve Opex as an independent business module.
- Reuse established Capex, Configuration, Attachments, Launcher, privacy, REST,
  and frontend conventions where their semantics match.
- Do not introduce a recurrence engine, scheduler, generic Money abstraction,
  or cross-business-module write dependency.
- Keep occurrences subordinate to contracts in persistence, API, routing, and
  user experience.
- Implement database-level filtering, pagination, and current-year aggregation
  for both SQLite and PostgreSQL.
- Keep every Wave independently testable and avoid hiding deferred behavior in
  implementation notes.

## Fixed Technical Contracts

### Backend Module

Opex lives under `Segaris.Api.Modules.Opex` and owns contracts, occurrences,
categories, mappings, queries, mutations, attachment authorization, and
Configuration reference handling.

Indicative resource routes are:

```text
GET    /api/opex/contracts
POST   /api/opex/contracts
GET    /api/opex/contracts/{contractId}
PUT    /api/opex/contracts/{contractId}
DELETE /api/opex/contracts/{contractId}

GET    /api/opex/contracts/{contractId}/occurrences
POST   /api/opex/contracts/{contractId}/occurrences
GET    /api/opex/contracts/{contractId}/occurrences/{occurrenceId}
PUT    /api/opex/contracts/{contractId}/occurrences/{occurrenceId}
DELETE /api/opex/contracts/{contractId}/occurrences/{occurrenceId}

GET    /api/opex/categories
```

Administrative category routes follow the existing module-owned Capex category
management pattern. Attachment routes follow the established platform contract
while preserving contract-level and occurrence-level ownership.

All writes require antiforgery. Missing and inaccessible records share the
platform not-found behavior so private data is not disclosed.

### Persistence

The model contains module-owned `OpexContract`, `OpexOccurrence`, and
`OpexCategory` entities with provider-specific migrations.

The contract stores stable Configuration identifiers for supplier, cost center,
and currency. Occurrences reference their parent contract and do not duplicate
inherited properties.

The normalized contract name is persisted or indexed using a provider-compatible
strategy that enforces global case-insensitive uniqueness consistently on
SQLite and PostgreSQL. Both providers require explicit tests for trimming,
capitalization, and conflict behavior.

Indexes must support the contract query filters, deterministic sorting,
current-year occurrence aggregation, parent occurrence listing, and category or
Configuration reference migration.

### Frontend Route

Opex uses the protected lazy route `/opex`. Contract editor state is URL-backed,
for example `/opex?contractId=123`. Occurrence editor selection remains internal
to the open contract dialog.

The launcher card uses the existing Opex presentation metadata and has no
attention contributor in the initial implementation.

### Configuration Integration

Configuration gains a flat Opex section at `/configuration/opex`. Opex category
management follows the Capex category behavior without transferring domain
ownership.

Opex registers the narrow reference-management handlers required for supplier,
cost-center, currency, and category deletion. Currency conversion updates annual
estimates, occurrence amounts, and modification metadata atomically.

## Waves

### Wave 0: Contracts And Test Skeleton

Status: **Complete**. The backend now freezes Opex routes, fixed vocabularies,
creation defaults, DTOs, pagination and sorting contracts, stable error codes,
attachment owner kinds, Configuration reference dependencies, normalization,
validation, and currency-rounding rules. Opex is registered in module
composition with architecture coverage. The frontend provides typed API,
validation-schema, route, pagination, and TanStack Query-key contracts without
introducing the deferred user interface. No requirements deviation was needed.

Establish stable transport, domain, route, error, query, and frontend contracts
before provider migrations or user-facing implementation.

Tasks:

1. Add Opex route constants, enums, request and response DTOs, pagination shapes,
   sorting keys, stable error codes, and attachment owner kinds.
2. Define contract and occurrence domain validation boundaries, including
   normalized-name comparison and amount rounding conventions.
3. Define Configuration category and reference-management contracts needed by
   Opex without exposing Opex entities.
4. Add module registration and architecture boundary expectations.
5. Add backend contract tests and frontend API/schema/query-key skeletons.
6. Record any unavoidable contract deviation back into the requirements before
   persistence work begins.

Tests:

- Unit tests for enum values, defaults, route constants, DTO shapes, query
  bounds, normalization, and validation.
- Architecture tests for permitted Opex dependencies and published contracts.

Exit criteria:

- Public contracts are explicit, test-covered, and sufficient for all later
  Waves without relying on internal EF entities.

### Wave 1: Domain, Persistence, And Categories

Status: **Complete**. The backend now persists the encapsulated `OpexContract`,
`OpexOccurrence`, and `OpexCategory` model on SQLite and PostgreSQL through the
`OpexDomainPersistence` migration: audit metadata, decimal precision, bounded
strings, enum check constraints, occurrence cascade deletion, deterministic
`(ContractId, EffectiveDate, Id)` ordering, the filter/privacy/catalog indexes,
and global case-insensitive contract-name uniqueness via a normalized-name unique
index. A one-time initializer seeds the accepted Opex categories once, and the
Opex-owned category catalog exposes the read plus the administrator create,
rename, reorder, privacy-neutral deletion-impact, final-row-protected delete, and
atomic replace-and-delete routes, reusing the Configuration presentation
contracts. Domain, seeding, migration, and category integration tests cover the
invariants; no requirements deviation was needed.

Implement the Opex data model and category lifecycle on both database providers.

Tasks:

1. Add contract, occurrence, and category entities with audit metadata and EF
   mappings.
2. Enforce required relationships, decimal precision, bounded strings,
   occurrence cascade deletion, and deterministic occurrence ordering.
3. Implement global normalized contract-name uniqueness consistently for SQLite
   and PostgreSQL.
4. Add indexes for contract filters, occurrence parent/date queries, realized
   totals, and catalog migrations.
5. Add provider-specific migrations and model snapshots.
6. Add one-time initialization for the accepted Opex category values.
7. Add Opex category reads and administrator mutations using the existing
   Configuration presentation boundary.

Tests:

- Domain tests for contract and occurrence invariants.
- SQLite and PostgreSQL migration tests, including upgrades and idempotent
  category initialization.
- Integration tests for category ordering, uniqueness, final-row protection,
  and administrator authorization.

Exit criteria:

- Both providers persist the complete model, enforce equivalent constraints,
  and expose a configurable Opex category catalog.

### Wave 2: Contract Read APIs

Status: **Complete**. The backend now serves `GET /api/opex/contracts` and
`GET /api/opex/contracts/{contractId}` from `OpexReadService`. The list applies
database-level privacy filtering (`OpexContractPolicies.AccessibleTo`) before the
search across name and notes, the exact movement-type, status, category, supplier,
cost-center, currency, frequency, visibility, and creator filters, bounded
pagination, and deterministic sorting with a descending-id tie-breaker (optional
supplier and annual-estimate sorts place their nulls last). The current-year
realized amount is aggregated from each contract's occurrences using the
`Europe/Madrid` natural-year boundaries and a coalesced decimal `SUM`, returning
`0.00` for contracts without qualifying occurrences while preserving each
contract's currency; it is also a sortable column. Catalog and audit display names
are resolved through correlated sub-queries, and the detail projection returns the
full contract with its attachments and a privacy-safe not-found for missing or
inaccessible records. Coverage spans the list filters/sorting/pagination/search,
privacy isolation, the current-year boundary and aggregation, detail and not-found
behavior, and a representative PostgreSQL read/aggregation shape; no requirements
deviation was needed.

Deliver the contracts table query, detail query, catalogs, privacy filtering,
and current-year realized aggregation.

Tasks:

1. Implement database-level contract search, exact filters, sorting, bounded
   pagination, and deterministic tie-breakers.
2. Calculate realized current-year amounts from occurrence effective dates using
   the `Europe/Madrid` natural year boundaries.
3. Implement contract detail projections and purpose-specific catalog queries.
4. Apply public/private visibility before filtering, aggregation, counts, and
   projection.
5. Return `0.00` for contracts without qualifying occurrences and preserve each
   contract's currency.
6. Document and inspect generated OpenAPI metadata for query and detail routes.

Tests:

- API integration tests for every filter and sortable field, search across name
  and notes, pagination bounds, deterministic ordering, and empty results.
- Privacy tests with multiple users covering public collaboration and private
  isolation.
- Current-year boundary and aggregation tests, including leap years, past and
  future occurrences, zero amounts, and every contract status.
- Provider coverage for representative query shapes.

Exit criteria:

- The complete Contracts view can be driven by authoritative paginated APIs
  without client-side aggregation or private-data leakage.

### Wave 3: Contract Mutations And Attachments

Status: **Complete**. The backend now serves `POST`, `PUT`, and `DELETE` on
`/api/opex/contracts` plus the contract-level attachment routes through
`OpexContractWriteService`. Creation and update map the request into the domain
factory (shape, enum, and amount validation), validate every catalog reference via
`OpexCatalogValidator`, and enforce global case-insensitive name uniqueness with a
pre-check plus a `DbUpdateException` race fallback, both surfaced as a `409`
`opex.contract.duplicate_name`. Updates are last-write-wins, reuse the read-side
privacy filter so a non-accessible contract is an indistinguishable not-found, and
keep the creator-only visibility rule (`403 opex.contract.visibility_forbidden`)
while any authorized user may edit a public contract. Deletion is physical and
cascades to occurrences at the database level, then reconciles contract attachments
through the platform service. Contract attachments reuse the established per-file
upload/list/download/remove contract with antiforgery and platform file-validation
mapped to `opex.attachment.invalid`; all attachment routes resolve authorization
through the parent contract. Coverage spans creation defaults and trimming,
validation, unknown references, duplicate-name create/rename conflicts, every
editable field, cross-user public edits, visibility enforcement, private isolation,
deletion and occurrence cascade, and the attachment round trip, authorization,
rejected files, missing attachments, and physical cleanup. No requirements
deviation was needed.

Implement contract creation, editing, visibility rules, deletion, and general
contract attachments.

Tasks:

1. Add create and update use cases with catalog validation, defaults, normalized
   global-name conflicts, and last-write-wins behavior.
2. Enforce that only the creator may change visibility while any authorized user
   may edit a public contract.
3. Add irreversible contract deletion with occurrence and attachment cleanup
   through platform compensating operations.
4. Register contract attachment authorization and ownership resolution.
5. Support attachment listing, upload, download, and removal for contracts.
6. Return stable machine-readable validation, conflict, not-found, and
   authorization outcomes.

Tests:

- API tests for defaults, validation, every editable field, duplicate names,
  rename conflicts, visibility changes, cross-user public edits, and private
  isolation.
- Attachment tests for authorization, validation, partial storage failure, and
  physical cleanup.
- Deletion tests proving occurrence and attachment cascading without exposing
  private records.

Exit criteria:

- Contracts can be safely managed end to end through the backend, including
  privacy and files.

### Wave 4: Occurrence APIs And Attachments

Status: **Planned**.

Deliver subordinate occurrence management inside an authorized contract.

Tasks:

1. Add paginated occurrence listing ordered by effective date and identifier.
2. Add occurrence creation, detail, update, and irreversible deletion.
3. Validate amounts, dates, optional description, and notes without schedule or
   contract-date constraints.
4. Resolve all occurrence authorization through the parent contract.
5. Add occurrence attachment ownership, upload, listing, download, removal, and
   deletion cleanup.
6. Ensure occurrence mutations make refreshed contract realized totals visible
   immediately to subsequent queries.

Tests:

- API tests for empty contracts, date ordering, pagination, past/future dates,
  zero amounts, editing, and deletion.
- Authorization tests proving occurrence identifiers cannot bypass inaccessible
  parent contracts.
- Attachment and cascade-cleanup tests at occurrence and contract levels.

Exit criteria:

- Every effective movement can be managed only within its authorized parent
  contract, with complete file behavior and no independent occurrence surface.

### Wave 5: Configuration Reference Migration

Status: **Planned**.

Extend structural catalog operations to all Opex references, including atomic
currency conversion.

Tasks:

1. Register Opex category, supplier, cost-center, and currency reference-impact
   handlers.
2. Implement category replacement, supplier replacement or clearing, and cost
   center replacement or clearing across public and private contracts.
3. Convert every non-null annual estimate and every occurrence amount for a
   referenced currency using the explicit rate and required rounding.
4. Update contract and occurrence `UpdatedAt` and `UpdatedBy` metadata with the
   acting administrator, including values unchanged by rounding.
5. Re-evaluate references inside the confirming transaction and roll back all
   modules and source deletion on any failure.
6. Invalidate affected Configuration and Opex frontend queries after successful
   structural mutations.

Tests:

- Unit tests for currency conversion and rounding boundaries.
- Cross-module integration tests for replacement, clearing, private records,
  concurrent references, invalid replacements, rollback, and metadata updates.
- SQLite and PostgreSQL coverage for atomic conversion and deletion.

Exit criteria:

- Configuration can safely mutate or remove every catalog value referenced by
  Opex without exposing business records or leaving partial conversions.

### Wave 6: Contracts Frontend

Status: **Planned**.

Build the lazy Opex module, URL-backed table state, contracts table, and Details
editor.

Tasks:

1. Add the Opex launcher destination, lazy module route, translation namespace,
   error boundary, and return-to-launcher behavior.
2. Implement URL-backed search, visible and expanded filters, sorting,
   pagination, loading, empty, retry, and out-of-range-page correction.
3. Render all required columns with localized dates, amounts, currencies, and
   fixed-value labels.
4. Add `New contract` and row selection through `contractId` URL state while
   preserving table state.
5. Build the large contract dialog and Details tab with React Hook Form and Zod,
   server error mapping, duplicate submission protection, and dirty-close
   confirmation.
6. Add contract attachment staging for creation, upload state, retry, download,
   and removal.
7. Add irreversible contract deletion with a clear cascade warning.

Tests:

- Frontend API and component tests for URL state, filters, sorting, pagination,
  defaults, validation, duplicate-name errors, privacy-safe not found, dirty
  close, uploads, and deletion.
- Accessibility tests for labels, focus entry/restoration, dialog tabs, keyboard
  controls, and error associations.

Exit criteria:

- Users can complete the full contract workflow without page reloads while the
  contracts table retains its state.

### Wave 7: Occurrences Frontend And End-To-End Journey

Status: **Planned**.

Complete nested occurrence management and verify the deployed user journey.

Tasks:

1. Build the Occurrences tab with paginated chronological listing and localized
   amount display.
2. Add secondary create/edit dialogs with defaults, validation, save state,
   server errors, and dirty-close confirmation.
3. Add occurrence attachment staging, progress, retry, download, and removal.
4. Add irreversible occurrence deletion and refresh both the occurrence list
   and parent current-year total after mutations.
5. Verify nested dialog focus, scrolling, keyboard behavior, and narrow desktop
   widths.
6. Add a Playwright journey covering login, opening Opex, filtering, creating a
   contract, attaching a file, adding/editing/deleting an occurrence, observing
   the annual total, closing/reopening with preserved state, and deleting the
   contract.
7. Add a multi-user browser privacy journey when the existing test
   infrastructure supports it; otherwise retain API coverage and record the
   explicit deferral in `ROADMAP.md`.

Tests:

- Component tests for nested dialogs, occurrence pagination, partial uploads,
  retry, deletion, query invalidation, and total refresh.
- End-to-end tests against the Compose stack for the critical workflow.

Exit criteria:

- The complete contract-and-occurrence workflow operates through the deployed
  frontend/backend boundary and respects module navigation and privacy.

### Wave 8: Hardening, Documentation, And Acceptance

Status: **Planned**.

Validate both providers, performance-sensitive queries, accessibility, OpenAPI,
and every functional acceptance criterion.

Tasks:

1. Run backend format, build, unit, API, PostgreSQL, migration, and architecture
   suites through repository scripts.
2. Run frontend format, lint, type-check, unit, build, and Playwright suites.
3. Review OpenAPI for all Opex, category, attachment, and Configuration migration
   routes, DTOs, antiforgery metadata, and problem responses.
4. Verify indexes and representative PostgreSQL query plans for contract search,
   current-year aggregation, occurrence listing, uniqueness, and reference
   migration.
5. Verify keyboard operation, focus restoration, nested dialog scrolling,
   localized formatting, large result counts, and narrow supported widths.
6. Map every criterion in `docs/requirements/OPEX_REQUIREMENTS.md` to covering
   code and tests in an Opex acceptance record.
7. Update `README.md` only if repository-wide commands or setup changed; update
   focused architecture or operations documentation only for lasting decisions.
8. Update `ROADMAP.md` to mark Opex definition and implementation status and to
   record only intentional, still-relevant deferrals.

Exit criteria:

- All relevant repository scripts pass, both providers are covered, the Compose
  journey succeeds, and every Opex acceptance criterion is implemented or
  explicitly deferred in the roadmap.

## Suggested Pull Request Boundaries

1. Opex contracts and persistence foundation (Waves 0-1).
2. Contract read and mutation APIs (Waves 2-3).
3. Occurrence APIs and files (Wave 4).
4. Configuration reference migration (Wave 5).
5. Contracts frontend (Wave 6).
6. Occurrences, E2E, and hardening (Waves 7-8).

## Plan Completion Criteria

The plan is complete when all Waves meet their exit criteria and the Opex
requirements document describes implemented behavior rather than functional
intent.

Automatic recurrence, occurrence planning, lifecycle automation, global
occurrence views, Analytics, Calendar, launcher attention, imports, exports,
duplication, budget variance, and the current-year activity filter remain
separate future planning topics.
