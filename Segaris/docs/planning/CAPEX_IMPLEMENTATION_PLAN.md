# Capex Implementation Plan

## Purpose

This plan translates `docs/requirements/CAPEX_REQUIREMENTS.md` into a
dependency-ordered implementation backlog for the Configuration platform
module, the Capex business module, the frontend experience, migrations, and
automated tests.

It builds on the implemented application foundation and these accepted
documents:

- `docs/architecture/backend.md` and
  `docs/planning/BACKEND_MODULE_CONVENTIONS.md`.
- `docs/architecture/domain-organization.md`.
- `docs/architecture/data-and-storage.md`.
- `docs/architecture/frontend.md` and
  `docs/architecture/user-experience.md`.
- `docs/architecture/design-system.md`.
- `docs/requirements/CAPEX_REQUIREMENTS.md`.

## Target Outcome

At the end of this plan:

- The platform has a backend Configuration module that owns seeded Supplier,
  CostCenter, and Currency catalogs and exposes read-only contracts and APIs.
- Capex is a complete vertical module with entries, ordered items, categories,
  visibility, attachments, filtering, sorting, pagination, and launcher
  attention.
- The launcher exposes a Capex card and current-user attention state.
- Opening Capex shows the Entries table directly.
- Users create and edit entries in a large URL-aware dialog without losing the
  list's search, filters, sorting, or pagination state.
- SQLite and PostgreSQL migrations and tests cover the new schema and upgrade
  path.
- Unit, architecture, API integration, frontend, and Playwright tests cover the
  acceptance criteria.

## Scope Boundaries

Included:

- Configuration persistence, deterministic seed data, read contracts, and
  read-only endpoints for suppliers, cost centers, and currencies.
- Capex categories, entries, ordered items, calculations, authorization, CRUD,
  attachments, list queries, and attention calculation.
- A backend Launcher attention aggregation contract and endpoint suitable for
  additional modules later.
- The Capex launcher card, route, table, filters, pagination, editor dialog,
  attachment controls, confirmations, and toast feedback.
- English (`en-GB`) translations for all new UI strings.
- Paired provider migrations and automated coverage.

Excluded:

- App Configuration screens or catalog mutation APIs.
- Catalog deactivation, deletion, and reference migration.
- Analytics, charts, totals, currency conversion, or a Capex overview.
- Calendar publication.
- Import, export, entry duplication, and a dedicated "my entries" shortcut.
- Relationships with Opex, Inventory, Travel, Assets, or other business
  modules.
- Spanish translations.

## Fixed Technical Contracts

### Persistence Model

Configuration owns these tables and entities:

- `SegarisSupplier`: integer `Id`, stable unique `Code`, display `Name`, and
  creation/modification metadata.
- `SegarisCostCenter`: integer `Id`, stable unique `Code`, display `Name`, and
  creation/modification metadata.
- `SegarisCurrency`: integer `Id`, unique ISO `Code`, display `Name`, and
  creation/modification metadata.

Capex owns:

- `CapexCategory`: integer `Id`, stable unique `Code`, display `Name`, and
  creation/modification metadata.
- `CapexEntry`: integer `Id`, title, movement type, status, `DueDate`, category
  ID, optional supplier ID, optional cost-center ID, currency ID, notes,
  visibility, persisted `TotalAmount`, and standard creation/modification
  metadata.
- `CapexItem`: integer `Id`, entry ID, explicit position, description,
  `Quantity`, `UnitAmount`, and persisted `LineAmount`.

Movement type and status use fixed string persistence with bounded columns and
validation for `Income`/`Expense` and
`Planning`/`Completed`/`Canceled`. Visibility uses the established platform
representation.

Monetary and quantity columns use fixed precision with two decimal places.
Every entry mutation recalculates each rounded line and the persisted total in
one database transaction. The persisted total supports deterministic database
sorting without trusting client calculations.

Foreign keys enforce category and shared-catalog references. Configuration
catalog deletion is absent, so the initial cross-module references use
`Restrict` behavior. Deleting an entry cascades to items; attachment removal is
coordinated through the attachment service rather than a database cascade.

### Initial HTTP Surface

Read-only Configuration endpoints:

- `GET /api/configuration/suppliers`
- `GET /api/configuration/cost-centers`
- `GET /api/configuration/currencies`

Capex endpoints:

- `GET /api/capex/categories`
- `GET /api/capex/entries`
- `GET /api/capex/entries/{entryId}`
- `POST /api/capex/entries`
- `PUT /api/capex/entries/{entryId}`
- `DELETE /api/capex/entries/{entryId}`
- `GET /api/capex/entries/{entryId}/attachments`
- `POST /api/capex/entries/{entryId}/attachments`
- `GET /api/capex/entries/{entryId}/attachments/{attachmentId}`
- `DELETE /api/capex/entries/{entryId}/attachments/{attachmentId}`

Launcher endpoint:

- `GET /api/launcher/attention`

All writes use antiforgery protection. Hidden records return the same not-found
problem as absent records. Requests and responses use explicit DTOs and never
expose EF Core entities.

The list endpoint accepts the platform pagination contract plus allow-listed
search, filter, and sort parameters. It returns `PaginatedResponse<T>` and uses
`id desc` as the final tie-breaker.

### Frontend Route Contract

- `/capex` opens the Entries view.
- `/capex?entryId={id}` opens an existing entry over the preserved Entries
  view.
- `/capex?new=true` opens the creation dialog over the preserved Entries view.

Search, filter, sort, page, and page-size state should also be URL-backed where
practical so browser navigation and direct links are predictable. Closing the
dialog removes only its dialog parameter and does not reload the application.

## Delivery Strategy

The work is divided into dependency-ordered Waves. A Wave may be split across
several pull requests, but its exit criteria should pass before dependent work
is treated as complete. Backend Waves 3 and 4 may overlap after the persistence
model and shared authorization helpers from Wave 2 are stable. Frontend work
starts after the read and mutation contracts are fixed.

### Wave 0: Contract And Test Skeleton

Status: **Complete**. Decisions and frozen contracts are recorded in
`docs/planning/CAPEX_CONTRACT_DECISIONS.md`.

Tasks:

1. Add the Configuration and Capex module folders and `ISegarisModule`
   registrations without behavior.
2. Define explicit enum/value contracts, request/response DTOs, query parameter
   names, error codes, catalog codes, and seed identifiers.
3. Define a narrow Configuration catalog-reader contract consumed by Capex;
   keep Configuration entities internal.
4. Define a Launcher attention-contributor contract and aggregated response
   shape that can accept later modules without changing the Capex contract.
5. Add architecture-test expectations for the dependency direction:
   Capex may consume Configuration and platform contracts; Configuration must
   not depend on Capex.
6. Add empty test fixtures/builders for Configuration and Capex API integration
   tests so later Waves share one setup.

Deliverables:

- Registered module shells and frozen public contracts.
- Stable catalog codes and endpoint/error-code documentation in code tests or a
  focused companion decision file if implementation reveals extra detail.

Exit criteria:

- The solution builds, module registration remains deterministic, OpenAPI has
  no duplicate routes, and later Waves do not need to invent cross-module
  ownership or route shapes.

### Wave 1: Configuration Backend Foundation

Status: **Complete**.

Tasks:

1. Implement Configuration entities and EF Core mappings through a module model
   contributor.
2. Seed suppliers, cost centers, and currencies idempotently with stable codes
   and the values defined by the requirements.
3. Implement the catalog-reader contract with existence validation and bounded
   read models.
4. Add the three authenticated read-only endpoints with OpenAPI metadata.
5. Add paired SQLite and PostgreSQL migrations for the Configuration schema.
6. Cover fresh creation and upgrade of both provider histories.

Tests:

- Unit tests for stable seed definitions and catalog lookup behavior.
- API integration tests for all catalog endpoints and authentication.
- Migration tests on SQLite and PostgreSQL.
- Architecture tests proving Configuration remains independent from business
  modules.

Exit criteria:

- Both providers contain identical logical catalogs, repeated seeding is safe,
  and Capex can validate catalog IDs without reading Configuration entities.

### Wave 2: Capex Domain And Persistence

Status: **Not started**.

Tasks:

1. Implement `CapexCategory`, `CapexEntry`, and `CapexItem` with module-owned EF
   Core mappings and standard audit/visibility fields.
2. Add deterministic Capex category seeds with stable codes.
3. Add domain/application validation for required strings, lengths, item-count
   bounds, enum values, positive quantities, nonnegative unit amounts, catalog
   references, and creator-only visibility changes.
4. Implement one authoritative calculation routine using
   `MidpointRounding.AwayFromZero` per line and summing rounded lines.
5. Persist item order and enforce a unique entry/position constraint.
6. Add shared query predicates for current-user visibility and mutation access.
7. Add paired provider migrations, indexes, constraints, and upgrade tests.

Recommended indexes:

- Entries by `DueDate` and `Id`.
- Entries by status and `DueDate` for attention queries.
- Entries by creator and visibility.
- Entry foreign keys used by exact filters.
- Items by entry and position.
- Unique catalog codes.

Tests:

- Unit tests for validation, calculations, zero-value entries, maximum item
  count, ordering, and visibility-transition policy.
- Provider tests for decimal persistence and rounded totals.
- Migration tests for fresh creation and upgrade on both providers.

Exit criteria:

- The complete model persists consistently on SQLite and PostgreSQL and all
  business invariants can be exercised without HTTP or frontend code.

### Wave 3: Read APIs, Filtering, And Launcher Attention

Status: **Not started**.

Tasks:

1. Implement `GET /api/capex/categories`.
2. Implement the paginated Entries query with partial search over title, notes,
   and item descriptions without duplicating entries when an item matches.
3. Implement inclusive `DueDate` bounds and exact filters for type, status,
   category, supplier, cost center, currency, visibility, and creator.
4. Implement every allow-listed sort, null ordering for optional supplier and
   cost center, and deterministic `id desc` tie-breaking.
5. Implement entry detail retrieval with ordered items, audit display data, and
   attachment descriptors.
6. Implement the Launcher module's attention aggregation endpoint.
7. Register a Capex attention contributor that evaluates accessible overdue
   `Planning` entries using the injected clock and `Europe/Madrid` civil date.

Tests:

- API integration coverage for pagination bounds, all filters, search fields,
  all sorts, tie-breaking, hidden private rows, and not-found privacy.
- Attention tests for public/private visibility, administrators, today, past,
  future, zero totals, and all statuses.
- PostgreSQL coverage for the production search/query behavior.

Exit criteria:

- The backend supplies every read model needed by the launcher and Entries
  screen with bounded, privacy-correct database queries.

### Wave 4: Mutations, Deletion, And Attachments

Status: **Not started**.

Tasks:

1. Implement create and update handlers in transactions, replacing the ordered
   item collection atomically and recalculating totals server-side.
2. Enforce public collaboration, creator-only private access, and creator-only
   visibility transitions for every mutation.
3. Implement physical deletion with confirmation remaining a frontend concern
   and attachment/file compensation handled through `IAttachmentService`.
4. Implement entry-scoped list, upload, download, and delete attachment routes
   using an owner such as `("Capex", "Entry", entryId)`.
5. Authorize every attachment operation through the owning entry before calling
   the attachment service.
6. Ensure partial upload failure never rolls back an already committed entry or
   previously successful files.
7. Add stable validation, not-found, and attachment error codes and OpenAPI
   response metadata.

Tests:

- API integration tests for create, full-field edits, status/type changes,
  item replacement/reordering, zero totals, invalid catalog references, and
  antiforgery.
- Two-user tests for public edits/deletes, private isolation, admin isolation,
  and forbidden visibility appropriation.
- Attachment upload/list/download/delete tests and entry-deletion filesystem
  cleanup tests.
- Failure-path tests for rejected files and partial upload sequences.

Exit criteria:

- The complete Capex backend contract passes on SQLite-backed API tests and the
  production-critical persistence paths pass against PostgreSQL.

### Wave 5: Frontend Module And Entries Table

Status: **Not started**.

Tasks:

1. Add a lazily loaded `/capex` module route, module error boundary, `capex`
   i18n namespace, and return-to-launcher shell behavior.
2. Add typed API clients and TanStack Query keys for Configuration catalogs,
   Capex categories, entry lists, details, mutations, attachments, and launcher
   attention.
3. Add the Capex launcher card using the agreed icon/tone unless a focused
   design review changes them.
4. Build the Entries table with the required columns, loading, empty, error, and
   unavailable states.
5. Build URL-backed search, visible primary filters, expandable secondary
   filters, removable active-filter chips, sorting, and 10/25/50/100 pagination.
6. Preserve separate amount/currency display and avoid any mixed-currency
   aggregation.
7. Correct the current page after deletion or filtering makes it invalid.

Engineering constraint:

- Build only the shared table/filter/pagination primitives that are genuinely
  reusable. Do not introduce a broad generic data-grid framework for the first
  business module.

Tests:

- Component tests for query serialization, filter removal, sorting, pagination,
  empty results, and separate currency rendering.
- Router tests proving Capex loads lazily and returns to the launcher.

Exit criteria:

- Users can browse and refine all accessible entries while URL and server query
  state remain synchronized.

### Wave 6: Entry Dialog And Item Editing

Status: **Not started**.

Tasks:

1. Extend the shared Dialog only as needed for a large editor, accessible focus
   management, controlled close requests, and scrollable content.
2. Open creation through `new=true` and existing entries through `entryId`,
   preserving all unrelated URL/list state when opening and closing.
3. Build the React Hook Form + Zod entry editor with the agreed defaults and
   server-compatible validation.
4. Implement the simplified one-item experience with title/description
   synchronization that stops overwriting either field after the user diverges
   them.
5. Implement optional item management, add/remove/reorder behavior, 1-to-100
   bounds, client total previews, and server totals as authoritative.
6. Add notes and general-data sections with seeded catalog selectors.
7. Add dirty-state close confirmation, disabled duplicate submission, retained
   input after errors, and success toasts.
8. Refresh the active list after save and close the dialog without a full page
   reload.

Tests:

- Component tests for defaults, synchronization/divergence, validation,
  itemization, reorder, totals, dirty close, server errors, and filtered-out
  updates.
- Accessibility tests for dialog naming, focus entry/return, keyboard use, and
  error association.

Exit criteria:

- Users can complete every non-attachment creation and editing workflow from
  the dialog without losing the Entries view state.

### Wave 7: Attachments, Deletion, And End-To-End Integration

Status: **Not started**.

Tasks:

1. Add attachment listing, upload progress/state, download, retry, and removal
   controls to the editor.
2. For creation, save the entry first and then upload selected files; report
   per-file failures while retaining the created entry and successful uploads.
3. Add irreversible entry-deletion confirmation and preserve/correct list state
   after success.
4. Connect launcher attention to the aggregated backend endpoint and invalidate
   it after mutations that can change the result.
5. Add Playwright coverage for login, opening Capex, filtering, creating a
   simple entry, itemizing it, attaching a file, editing it, observing attention,
   deleting it, and returning to the same list state.
6. Add a second-user privacy journey for public collaboration and private
   isolation where the test infrastructure supports multiple sessions.

Tests:

- Frontend tests for partial attachment failure, retry, attachment removal,
  destructive confirmation, and attention invalidation.
- End-to-end tests against the Compose stack for the critical journey.

Exit criteria:

- The complete user workflow satisfies the requirements through the deployed
  frontend/backend boundary.

### Wave 8: Hardening, Documentation, And Acceptance

Status: **Not started**.

Tasks:

1. Run backend format/build/unit/API/PostgreSQL/migration/architecture suites and
   frontend format/lint/type-check/unit/build/Playwright suites through the
   repository scripts.
2. Review generated OpenAPI for every new route, DTO, problem response, and
   request-size exception used by attachments.
3. Verify indexes and query plans for the entry list, item-description search,
   and attention query with representative data on PostgreSQL.
4. Verify keyboard operation, focus restoration, long dialog scrolling, narrow
   desktop widths, localized date/number formatting, and large result counts.
5. Update `README.md` only if repository-wide commands or setup changed.
6. Update focused architecture or operational documentation for any lasting
   implementation decision discovered during the Waves.
7. Execute every acceptance criterion in
   `docs/requirements/CAPEX_REQUIREMENTS.md` and record any intentionally
   deferred item in `ROADMAP.md`.

Exit criteria:

- All relevant repository scripts pass, both database providers are covered,
  the Compose journey succeeds, and no unresolved requirement remains hidden in
  implementation notes.

## Suggested Pull Request Boundaries

1. Configuration foundation and migrations (Waves 0-1).
2. Capex model, migrations, and read APIs (Waves 2-3).
3. Capex mutations and attachments (Wave 4).
4. Capex Entries frontend (Wave 5).
5. Capex editor frontend (Wave 6).
6. Attachments, launcher attention, E2E, and hardening (Waves 7-8).

## Plan Completion Criteria

The plan is complete when all Waves meet their exit criteria and the Capex
requirements document can be treated as implemented behavior rather than only
functional intent. Deferred Analytics, App Configuration mutation, import,
export, duplication, and cross-module links remain separate future planning
topics.
