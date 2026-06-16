# Opex Acceptance Record (Wave 8)

This document records the Wave 8 hardening and acceptance pass for the Opex
module against `docs/requirements/OPEX_REQUIREMENTS.md` and the exit criteria
in `docs/planning/OPEX_IMPLEMENTATION_PLAN.md`.

## Verification Approach

Wave 8 was executed as a focused documentation and acceptance pass:

- Functional behaviour is covered by the automated suites delivered in Waves 0-7
  and gated on every pull request through the required CI checks
  (`Segaris Backend`, `Segaris PostgreSQL`, `Segaris Compose`; see
  `docs/planning/BACKEND_CI_DECISIONS.md`). This pass relies on that coverage
  rather than re-running the full suites locally.
- The OpenAPI surface and the database indexes/query shape were verified
  statically against the implemented code and the paired provider migrations.
- PostgreSQL query-plan verification was limited to confirming the recommended
  indexes exist and that the queries are expressed at the database level. A
  representative-volume `EXPLAIN ANALYZE` benchmark was intentionally deferred
  (see Deferred Items).

## Static Verification Results

### HTTP / OpenAPI Surface

All Wave 0 frozen routes are mapped with explicit OpenAPI metadata and never
expose EF Core entities:

- **Categories**: `GET /api/opex/categories` and the six administrator-only
  category management routes (`POST`, `PUT /{id}`, `POST /{id}/move`,
  `GET /{id}/deletion-impact`, `DELETE /{id}`, `POST /{id}/replace-and-delete`)
  all carry `WithName`/`WithSummary` and typed `Produces<T>`; the management
  routes declare `ProducesProblem` for `400`, `404`, and `409` as applicable and
  apply `AntiforgeryEndpointFilter`.
- **Contracts**: the five contract routes (`GET /api/opex/contracts`,
  `GET /api/opex/contracts/{id}`, `POST`, `PUT /{id}`, `DELETE /{id}`) carry
  `WithName`/`WithSummary`, typed `Produces<T>`, and `ProducesProblem` for
  `400`, `403`, `404`, and `409` where applicable; all state-changing routes
  apply `AntiforgeryEndpointFilter`.
- **Contract attachments**: four attachment routes (`GET`, `POST`, `GET /{aid}`,
  `DELETE /{aid}`) under `/api/opex/contracts/{id}/attachments` are declared with
  `WithName`/`WithSummary`, typed `Produces<T>`, and `ProducesProblem` for `400`
  and `404`; the upload route adds `WithRequestBodyLimit(AttachmentPolicy.MaximumFileSize + 1 MiB)`.
- **Occurrences**: the five occurrence routes (`GET`, `POST`, `GET /{oid}`,
  `PUT /{oid}`, `DELETE /{oid}`) under `/api/opex/contracts/{id}/occurrences`
  carry full metadata, typed responses, and `ProducesProblem` for `400` and
  `404`; all mutations apply `AntiforgeryEndpointFilter`.
- **Occurrence attachments**: four attachment routes under
  `/api/opex/contracts/{id}/occurrences/{oid}/attachments` follow the same
  pattern as contract attachments.
- `401` is enforced through `RequireAuthorization()` on the group; private and
  missing records return an indistinguishable `opex.contract.not_found` or
  `opex.occurrence.not_found` so private identifiers are not disclosed.
- Configuration presents the Opex category section without additional routes;
  its read and migration surface is shared with the category management routes
  above and the existing `ConfigurationEndpoints` catalog reads.

### Indexes And Query Shape

The recommended indexes exist identically in both provider migrations
(`OpexDomainPersistence` for SQLite and PostgreSQL) and are matched by the
query shapes in `OpexReadService`, `OpexContractListQuery`, and
`OpexOccurrenceListQuery`:

| Index | Query that uses it |
| --- | --- |
| `IX_opex_contracts_CreatedBy_Visibility_Id` | `OpexContractPolicies.AccessibleTo` privacy filter |
| `IX_opex_contracts_NormalizedName` (unique) | Global case-insensitive name uniqueness check |
| `IX_opex_contracts_Name_Id` | Default name-ascending ordering with deterministic tie-breaker |
| `IX_opex_contracts_CategoryId` | Category exact filter; category reference migration |
| `IX_opex_contracts_SupplierId` | Supplier exact filter; supplier reference migration |
| `IX_opex_contracts_CostCenterId` | Cost-center exact filter; cost-center reference migration |
| `IX_opex_contracts_CurrencyId` | Currency exact filter; currency conversion migration |
| `IX_opex_contracts_UpdatedBy` | UpdatedBy audit display name resolution |
| `IX_opex_occurrences_ContractId_EffectiveDate_Id` | Paginated occurrence listing; current-year aggregation `SUM` filtered by `EffectiveDate` range |
| `IX_opex_occurrences_CreatedBy` | Occurrence audit display name resolution |
| `IX_opex_occurrences_UpdatedBy` | Occurrence audit display name resolution |
| `IX_opex_categories_NormalizedName` | Category name uniqueness |
| `IX_opex_categories_SortOrder` | Default catalog ordering |

List filtering, sorting, pagination, partial search across name and notes, the
current-year realized `SUM`, and the occurrence chronological listing all run as
`IQueryable` translated to SQL; the client never loads the full result set.
Name/notes partial search is an intentional `LIKE` scan consistent with the
accepted database-backed search baseline.

## Acceptance Criteria

Each criterion from `OPEX_REQUIREMENTS.md` and its primary covering evidence:

| # | Criterion | Status | Primary evidence |
| --- | --- | --- | --- |
| 1 | Authenticated users create, query, edit, and delete visible contracts with documented fields, defaults, validation, and lifecycle | Met | `OpexContractMutationTests`, `OpexContractDetailTests`, `OpexContractListTests`, `OpexDomainTests`, `OpexPage.test.tsx`, `opex.spec.ts` |
| 2 | Contract names globally unique after trimming and case-insensitive comparison on SQLite and PostgreSQL; duplicate-name error does not disclose private contract details | Met | `OpexContractMutationTests` (duplicate-name create/rename conflicts), `OpexDomainTests` (normalization), `OpexContractTests` (domain invariants), `PostgresPersistenceTests` |
| 3 | Movement type, frequency, dates, status, and estimated annual amount are descriptive and mutable; no occurrences generated; no cross-field synchronization | Met | `OpexContractMutationTests` (every editable field), `OpexContractTests`, `OpexDomainTests` |
| 4 | Occurrences managed only inside an accessible parent contract; documented fields, validation, ordering, pagination, inherited properties, no independent state or route | Met | `OpexOccurrenceMutationTests`, `OpexOccurrenceListTests`, `OpexOccurrenceAuthorizationTests` |
| 5 | Contracts view: documented search, filters, sorting, pagination, URL preservation, database-level current-year realized total | Met | `OpexContractListTests` (all filters/sorting/pagination/aggregation), `contractsState.test.ts` (URL backing), `OpexPage.test.tsx`, `PostgresPersistenceTests` (aggregated reads) |
| 6 | Public/private visibility matches platform policy; occurrences and attachments inherit access exclusively from their contract | Met | `OpexContractMutationTests` (visibility enforcement, cross-user public edits, private isolation), `OpexOccurrenceAuthorizationTests`, `OpexContractAttachmentTests`, `OpexOccurrenceAttachmentTests` |
| 7 | Contract and occurrence attachments support creation-time staging, partial upload failure, retry, retrieval, and removal using platform policies | Met | `OpexContractAttachmentTests`, `OpexOccurrenceAttachmentTests`, `OpexPage.test.tsx` (staged create-mode attachments) |
| 8 | Deleting an occurrence removes its attachments; deleting a contract removes all occurrences and attachments with explicit irreversible confirmation and storage consistency | Met | `OpexContractMutationTests` (cascade deletion), `OpexContractAttachmentTests` and `OpexOccurrenceAttachmentTests` (physical cleanup), `OpexPage.test.tsx` (deletion confirmation) |
| 9 | Administrators manage Opex categories through Configuration; values initialized exactly once; referenced deletion requires migration; final category cannot be removed | Met | `OpexCategoryEndpointTests` (create, rename, reorder, deletion-impact, delete, replace-and-delete, final-row protection, one-time initialization) |
| 10 | Supplier, cost-center, category, and currency migrations cover every public and private reference without revealing identity or counts; roll back completely on failure | Met | `OpexConfigurationMigrationTests` (supplier replacement/clearing, cost-center replacement/clearing, category replacement, rollback on missing exchange rate, privacy-neutral deletion-impact) |
| 11 | Currency migration converts every non-null annual estimate and occurrence amount with the documented formula and rounding; updates contract and occurrence modification metadata; deletes source atomically | Met | `OpexConfigurationMigrationTests` (currency conversion with estimates and occurrences), `OpexDomainTests` (conversion rounding boundaries, null-estimate handling) |
| 12 | Popup editors, nested occurrence management, unsaved-change confirmation, focus, loading, errors, and feedback operate without reloading the application or losing contracts-table state | Met | `OpexPage.test.tsx`, `contracts.test.ts`, `contractsState.test.ts`, `opex.spec.ts` |
| 13 | SQLite and PostgreSQL migrations, backend unit/integration/architecture tests, frontend component tests, and a representative Playwright journey verify behaviour and privacy | Met (single-user E2E) | `MigrationTests` (both providers); `ModuleBoundaryTests`; `OpexDomainTests`, `OpexValidationTests`, `OpexContractTests` (unit); full API integration suite; `PostgresPersistenceTests`; frontend component + state suites; `opex.spec.ts` |

## Deferred Items

Recorded in `ROADMAP.md`:

- **Second-user Opex privacy E2E journey.** Public-collaboration and
  private-isolation behaviour is covered by API integration tests
  (`OpexContractMutationTests`, `OpexOccurrenceAuthorizationTests`); the
  browser-level multi-session journey waits on multi-account Playwright
  infrastructure, matching the deferred Capex and Configuration patterns.
- **PostgreSQL representative-volume query-plan benchmark.** Index existence and
  database-level query shape are verified; a large-dataset `EXPLAIN ANALYZE`
  benchmark is deferred until a representative seeding/benchmark harness exists.

## Documentation Outcomes

- `README.md`: no change; repository-wide commands and setup were unaffected by
  the Opex waves.
- `ROADMAP.md`: Opex implementation marked accepted; the two deferred items
  above recorded.
- `docs/planning/OPEX_IMPLEMENTATION_PLAN.md`: Wave 8 status updated to point at
  this record.
