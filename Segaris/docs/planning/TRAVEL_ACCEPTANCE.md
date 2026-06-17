# Travel Acceptance Record (Wave 7)

This document records the Wave 7 end-to-end, hardening, and acceptance pass for
the Travel module against `docs/requirements/TRAVEL_REQUIREMENTS.md` and the exit
criteria in `docs/planning/TRAVEL_IMPLEMENTATION_PLAN.md`.

## Verification Approach

Wave 7 was executed as a focused hardening and acceptance pass, matching the
Capex, Configuration, Opex, and Inventory precedents:

- Functional behaviour is covered by the automated suites delivered in Waves 0-6
  and gated on every pull request through the required CI checks
  (`Segaris Backend`, `Segaris PostgreSQL`, `Segaris Compose`; see
  `docs/planning/BACKEND_CI_DECISIONS.md`).
- The repository suites were re-run during this pass and are green:
  - Backend `format --verify` and build.
  - Backend unit project: 270 tests passing.
  - Backend API integration project: 398 tests passing.
  - Backend PostgreSQL integration project: 16 tests passing (Docker present).
  - Backend migration integration project: 6 tests passing (SQLite and
    PostgreSQL).
  - Backend architecture project: 19 tests passing.
  - Frontend format, lint, unit (42 files, 225 tests passing), and production
    build.
  - The representative Playwright journey added below is compiled and listed; it
    runs against the Compose stack in CI when seeded credentials are present.
- The OpenAPI surface and the database indexes/query shape were verified
  statically against the implemented endpoints and the paired provider
  migrations.

A pre-existing formatting drift introduced when the Travel namespace was added to
`src/frontend/src/app/i18n/i18n.test.ts` (a `namespaces` array that exceeded the
print width once `'travel'` was appended) was corrected so the frontend
`format --verify` gate passes cleanly.

## End-To-End Journey

`tests/frontend/e2e/travel.spec.ts` adds a single-user critical journey against
the full stack: sign in, open Travel from the launcher, exercise and clear a
trips filter, create a trip with the documented defaults, add an itinerary entry
that carries a reservation locator and save it, reopen the trip and confirm the
itinerary persisted, add two expenses in different currencies, confirm the
per-currency totals render one badge per distinct currency (no automatic
conversion to a single total), and delete the safe test data (the trip removes
its itinerary, expenses, and attachments in one operation). It is skipped without
seeded `SEGARIS_E2E_USERNAME` / `SEGARIS_E2E_PASSWORD` credentials and at least
two Configuration currencies, matching the other specs. The second-user privacy
journey is deferred (see Deferred Items).

## Static Verification Results

### HTTP / OpenAPI Surface

All Wave 0 frozen routes are mapped under `/api/travel` with explicit OpenAPI
metadata and never expose EF Core entities (`TravelEndpoints`). The group applies
`RequireAuthorization()`; private and missing records return an indistinguishable
`travel.trip.not_found` / `travel.expense.not_found` so private identifiers are
not disclosed. The embedded itinerary has no independent route; it is delivered in
the trip representation and replaced through the trip create and update payloads.

- **Trips**: `GET /trips`, `GET /trips/{tripId}`, `POST /trips`,
  `PUT /trips/{tripId}`, and `DELETE /trips/{tripId}` all carry
  `WithName`/`WithSummary`, typed `Produces<T>`, and `ProducesProblem` for `400`,
  `403`, and `404` as applicable; every mutation applies
  `AntiforgeryEndpointFilter`.
- **Trip attachments**: four routes under `/trips/{tripId}/attachments`
  (`GET`, `POST`, `GET /{attachmentId}`, `DELETE /{attachmentId}`) follow the
  shared attachment pattern; the upload route adds
  `WithRequestBodyLimit(AttachmentPolicy.MaximumFileSize + 1 MiB)`.
- **Expenses**: `GET /trips/{tripId}/expenses`,
  `POST /trips/{tripId}/expenses`, `GET /trips/{tripId}/expenses/{expenseId}`,
  `PUT /trips/{tripId}/expenses/{expenseId}`, and
  `DELETE /trips/{tripId}/expenses/{expenseId}` carry full metadata, typed
  responses, and `ProducesProblem` for `400` and `404` as applicable; every
  mutation applies `AntiforgeryEndpointFilter`.
- **Expense attachments**: four routes under
  `/trips/{tripId}/expenses/{expenseId}/attachments` mirror the trip attachment
  pattern, including the upload body limit.
- **Catalogs**: `GET /trip-types` and `GET /expense-categories` are authenticated
  reads; the six administrator-only management routes for each catalog (`POST`,
  `PUT /{id}`, `POST /{id}/move`, `GET /{id}/deletion-impact`, `DELETE /{id}`,
  `POST /{id}/replace-and-delete`) require `IdentityPolicies.Admin`, carry
  `WithName`/`WithSummary` and typed responses, declare `ProducesProblem` for
  `400`/`404`/`409` as applicable, and apply `AntiforgeryEndpointFilter`.
  Configuration presents these catalogs through the existing module-owned catalog
  boundary without additional routes.

### Indexes And Query Shape

The recommended indexes exist identically in both provider migrations
(`TravelModelContributor` applied through the paired SQLite and PostgreSQL
migrations) and match the query shapes in `TravelReadService`,
`TravelTripListQuery`, and `TravelExpenseListQuery`:

| Index | Query that uses it |
| --- | --- |
| `travel_trips (StartDate, Id)` | Default trip ordering (start date desc, id desc tie-breaker) |
| `travel_trips (CreatedBy, Visibility, Id)` | `TravelTripPolicies` privacy filter and creator filter |
| `travel_trips (Status, StartDate)` | Status exact filter; launcher attention (`Ongoing`/`Planned` within seven days) |
| `travel_trips (TripTypeId)` | Trip-type exact filter and trip-type reference migration |
| `travel_trips (Visibility)` / `(UpdatedBy)` | Visibility filter; audit display-name resolution |
| `travel_itinerary_entries (TripId, Date, Time, SortOrder)` | Deterministic embedded itinerary ordering |
| `travel_expenses (TripId, Id)` | Per-trip expense lookups and default expense ordering |
| `travel_expenses (TripId, CurrencyId)` | Per-currency total aggregation per trip |
| `travel_expenses (ExpenseCategoryId)` / `(CurrencyId)` | Category/currency filters and reference/conversion migration |
| `travel_expenses (SupplierId)` / `(CostCenterId)` | Optional supplier/cost-centre reference migration |
| `travel_expenses (UpdatedBy)` | Audit display-name resolution |
| `travel_trip_types`/`travel_expense_categories (NormalizedName)` unique | Catalog name uniqueness |
| `travel_trip_types`/`travel_expense_categories (SortOrder)` | Default catalog ordering |

List filtering, sorting, pagination, and partial search (trip name/destination/
notes, expense description) run as `IQueryable` translated to SQL; the client
never loads the full result set. Partial search is an intentional `LIKE` scan
consistent with the accepted database-backed search baseline. The
`EndDate >= StartDate` and `Amount >= 0` invariants and the status/visibility
value sets are enforced by database check constraints in addition to domain
validation.

## Acceptance Criteria

Each criterion from `TRAVEL_REQUIREMENTS.md` and its primary covering evidence:

| # | Criterion | Status | Primary evidence |
| --- | --- | --- | --- |
| 1 | Create, query, edit, and irreversibly delete visible trips with documented fields, defaults, validation, and privacy | Met | `TravelTripMutationTests`, `TravelTripDetailTests`, `TravelTripListTests`, `TravelDomainTests`, `TravelContractTests`, `TravelPage.test.tsx`, `travel.spec.ts` |
| 2 | Trips carry a fixed manual status with required civil start/end dates where end is not before start | Met | `TravelDomainTests` (date invariant, status values), `TravelTripMutationTests` (date-invariant rejection), `CK_travel_trips_dates`/`CK_travel_trips_status` constraints in `TravelModelContributor` |
| 3 | Each trip carries a light embedded itinerary of up to 100 ordered entries, each able to hold a locator, edited through full-collection replacement | Met | `TravelDomainTests` (ordering, bounded count), `TravelTripMutationTests` (itinerary replacement and ordering), `TravelTripDetailTests` (embedded itinerary projection), `travel.spec.ts` |
| 4 | Expenses are a managed per-trip sub-resource with individual CRUD and attachments, and a trip may hold expenses in several currencies | Met | `TravelExpenseTests` (CRUD, multi-currency list), `TravelTripAttachmentTests` (expense attachments), `TravelDomainTests`, `travel.spec.ts` |
| 5 | Trip detail reports expense totals grouped by currency without automatic conversion or a single normalised total | Met | `TravelExpenseTests` (`Update_and_delete_recompute_trip_currency_totals`), `TravelTripDetailTests`, `TravelPage.test.tsx`, `travel.spec.ts` |
| 6 | Currency is required on every expense, while supplier and cost centre are optional Configuration references | Met | `TravelExpenseTests` (`Create_requires_antiforgery_currency_and_valid_catalog_references`), `TravelDomainTests` (`Expense_accepts_a_zero_amount_and_optional_references_left_unset`, `Expense_rejects_nonpositive_required_references`) |
| 7 | Itinerary entries and expenses inherit parent-trip visibility and authorization, and private trips are never disclosed through not-found behaviour | Met | `TravelTripMutationTests` (two-user privacy), `TravelExpenseTests` (expense access follows the trip), `TravelTripAttachmentTests` (private-record hiding), `TravelReadService` shared not-found behaviour |
| 8 | Public collaboration and private isolation follow the Segaris visibility baseline at the trip level | Met | `TravelTripMutationTests` (public collaboration and private isolation, creator-only visibility transitions), `TravelDomainTests` |
| 9 | Deleting a trip removes its itinerary, expenses, and all owned attachments in one operation, and deleting an expense updates the per-currency totals | Met | `TravelTripMutationTests` (cascade delete), `TravelTripAttachmentTests` (filesystem cleanup on trip/expense deletion), `TravelExpenseTests` (`Update_and_delete_recompute_trip_currency_totals`) |
| 10 | Travel-owned `TripType` and `TravelExpenseCategory` catalogs are initialized once and managed through Configuration with CRUD, reorder, final-row protection, and atomic reference migration before deletion | Met | `TravelCatalogEndpointTests` (seeded order, admin create/move/delete, non-admin rejection, duplicate conflict), `TravelDomainTests` (`Trip_type_management_protects_the_final_row_and_migrates_references_atomically`, `Expense_category_management_protects_the_final_row_and_migrates_references_atomically`) |
| 11 | Shared Supplier, Currency, and CostCenter catalogs come from Configuration through published contracts, with required-currency replacement converting affected amounts and optional supplier/cost-centre references replaced or cleared | Met | `TravelDomainTests` (`Expense_migration_helpers_update_optional_references_and_currency_amounts` covering `ConvertCurrency`, `ReplaceSupplier` clear, `ReplaceCostCenter`), `ModuleBoundaryTests` (Travel depends on Configuration only), `TravelContractTests` |
| 12 | Travel attention is true exactly when the user can access at least one trip that is `Ongoing` or `Planned` with a start date within the next seven days in `Europe/Madrid` | Met | `TravelAttentionTests` (ongoing trips, planned inside/outside the seven-day window, completed/cancelled, public vs. another user's private trip, the `Europe/Madrid` boundary) |
| 13 | SQLite and PostgreSQL migrations, backend unit/integration/architecture tests, frontend component tests, and a representative Playwright journey verify behaviour and privacy | Met (single-user E2E) | `MigrationTests` (both providers), `PostgresPersistenceTests`, `ModuleBoundaryTests`/`ModuleRegistrationTests`, the Travel unit suite, the full Travel API integration suite, `contracts.test.ts`/`TravelPage.test.tsx`, `travel.spec.ts` |

Attachment behaviour underlying criteria 1, 4, and 13 is covered by
`TravelTripAttachmentTests` (trip and expense upload, list, download, delete
round-trip, private-record hiding, and filesystem cleanup on trip/expense
deletion).

## Deferred Items

Recorded in `ROADMAP.md`:

- **Second-user Travel privacy E2E journey.** Public-collaboration and
  private-isolation behaviour is covered by API integration tests
  (`TravelTripMutationTests`, `TravelExpenseTests`, `TravelTripAttachmentTests`);
  the browser-level multi-session journey waits on multi-account Playwright
  infrastructure, matching the deferred Capex, Configuration, Opex, and Inventory
  patterns.
- **PostgreSQL representative-volume query-plan benchmark.** Index existence and
  database-level query shape are verified, and the PostgreSQL integration suite
  is green; a large-dataset `EXPLAIN ANALYZE` benchmark waits on a
  representative seeding/benchmark harness.

## Documentation Outcomes

- `README.md`: no change; repository-wide commands and setup were unaffected by
  the Travel waves.
- `ROADMAP.md`: Travel implementation marked accepted; the two deferred items
  above recorded.
- `docs/planning/TRAVEL_IMPLEMENTATION_PLAN.md`: Wave 7 status updated to point at
  this record.
