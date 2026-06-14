# Capex Acceptance Record (Wave 8)

This document records the Wave 8 hardening and acceptance pass for the Capex
module against `docs/requirements/CAPEX_REQUIREMENTS.md` and the exit criteria
in `docs/planning/CAPEX_IMPLEMENTATION_PLAN.md`.

## Verification Approach

Wave 8 was executed as a focused documentation and acceptance pass:

- Functional behaviour is covered by the automated suites delivered in Waves 1-7
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

- Capex: `CapexEndpoints.MapCapexEndpoints` maps the category, list, detail,
  create, update, delete, and four attachment routes. Every route carries
  `WithName`/`WithSummary` and typed `Produces<T>`; error paths declare
  `ProducesProblem` for `400`, `403`, and `404` as applicable; all
  state-changing routes apply `AntiforgeryEndpointFilter`; the upload route adds
  `WithRequestBodyLimit(AttachmentPolicy.MaximumFileSize + 1 MiB)`.
- Configuration: `ConfigurationEndpoints` maps the three read-only catalog
  routes with typed `Produces<T>` metadata.
- Launcher: `LauncherEndpoints` maps `GET /api/launcher/attention` with a typed
  `LauncherAttentionResponse`.
- `401` is enforced through `RequireAuthorization()` on each group; hidden and
  absent records return the same not-found problem (`CapexProblem.EntryNotFound`).

### Indexes And Query Shape

The recommended Wave 2 indexes exist identically in both provider migrations
(`...CapexDomainPersistence` for SQLite and PostgreSQL) and are matched by the
query shapes in `CapexReadService` and `CapexAttentionContributor`:

| Recommended index | Migration index | Query that uses it |
| --- | --- | --- |
| Entries by `DueDate`, `Id` | `IX_capex_entries_DueDate_Id` | Default list ordering + `id desc` tie-breaker |
| Status + `DueDate` (attention) | `IX_capex_entries_Status_DueDate_Id` | Attention `Planning && DueDate <= today` |
| Creator + visibility | `IX_capex_entries_CreatedBy_Visibility_Id` | `CapexEntryPolicies.AccessibleTo` privacy filter |
| Entry foreign keys (exact filters) | `IX_capex_entries_{Category,Supplier,CostCenter,Currency}Id` | Exact filters in `ApplyFilters` |
| Items by entry + position | `IX_capex_items_EntryId_Position` (unique) | Ordered item load + position uniqueness |
| Unique catalog codes | `IX_capex_categories_Code`, configuration catalog code indexes | Stable catalog references |

List filtering, sorting, pagination, the partial search across title/notes/item
descriptions (a correlated `EXISTS` over items), and the attention check all run
as `IQueryable` translated to SQL; the client never loads the full result set.
Title/notes partial search is an intentional `LIKE` scan consistent with the
accepted database-backed search baseline.

## Acceptance Criteria

Each criterion from `CAPEX_REQUIREMENTS.md` and its primary covering evidence:

| # | Criterion | Status | Primary evidence |
| --- | --- | --- | --- |
| 1 | Direct Entries table, server pagination, `DueDate desc` default | Met | `CapexReadService.ListEntriesAsync`; `CapexEntryListTests`; `CapexPage.test.tsx` |
| 2 | Backend search/filter/sort/page survive dialog open/close without reload | Met | `useEntriesState` URL backing; `CapexEntryListTests`; `EntryDialog.test.tsx`; `capex.spec.ts` |
| 3 | Public visible to all, private creator-only, no admin bypass | Met | `CapexEntryPolicies.AccessibleTo`; `CapexEntryAuthorizationTests` |
| 4 | Any user edits/deletes public; only creator changes visibility; private creator-only | Met | `CapexEntryWriteService`; `CapexEntryAuthorizationTests` |
| 5 | Create/edit with full fields, fixed types/statuses, seeded catalogs, ordered items, notes, attachments | Met | `CapexEntryMutationTests`; `CapexAttachmentTests`; `EntryDialog.test.tsx` |
| 6 | Server rejects invalid/unknown values, authoritative rounded totals, zero total via nonneg unit + positive qty | Met | `CapexDomainTests`; `CapexEntryMutationTests` |
| 7 | Single-item simplified editor; item management without changing persisted type | Met | `EntryDialog.test.tsx`; `CapexDomainTests` |
| 8 | Dirty-close confirmation; failed validation/request/upload preserves input and created entry | Met | `EntryDialog.test.tsx`; `attachments.test.ts`; `CapexAttachmentTests` |
| 9 | Successful mutations refresh query, toast, handle invalid page/filtered-out row without reload | Met | `CapexPage.test.tsx`; `EntryDialog.test.tsx`; `capex.spec.ts` |
| 10 | Confirmed deletion physically removes entry, items, and attachments | Met | `CapexEntryMutationTests`; `CapexAttachmentTests` (cleanup); `EntryDialog.test.tsx` |
| 11 | Configuration supplies shared seeded catalogs via read-only contracts/APIs; Capex owns its category catalog | Met | `ConfigurationCatalogEndpointTests`; `ConfigurationCatalogTests`; `CapexCategoryEndpointTests` |
| 12 | Attention true iff accessible `Planning` entry with `DueDate <= today` in `Europe/Madrid` | Met | `CapexAttentionContributor`; `LauncherAttentionTests` |
| 13 | SQLite/PostgreSQL migrations, API/component/architecture tests, and an E2E journey verify behaviour | Met (single-user E2E) | `MigrationTests`; `ModuleBoundaryTests`; integration + component suites; `capex.spec.ts` |

## Deferred Items

Recorded in `ROADMAP.md`:

- **Second-user Capex privacy E2E journey.** Public-collaboration and
  private-isolation behaviour is covered at the API integration level
  (`CapexEntryAuthorizationTests`); the browser-level multi-session journey waits
  on multi-account Playwright infrastructure.
- **PostgreSQL representative-volume query-plan benchmark.** Index existence and
  database-level query shape are verified; a large-dataset `EXPLAIN ANALYZE`
  benchmark is deferred until a representative seeding/benchmark harness exists.

## Documentation Outcomes

- `README.md`: no change; repository-wide commands and setup were unaffected by
  the Capex waves.
- `ROADMAP.md`: Capex implementation marked accepted; the two deferred items
  above recorded.
- `docs/planning/CAPEX_IMPLEMENTATION_PLAN.md`: Wave 8 status updated to point at
  this record.
