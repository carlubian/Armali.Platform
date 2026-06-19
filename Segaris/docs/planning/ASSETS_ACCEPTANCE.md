# Assets Acceptance Record (Wave 8)

This document records the Wave 8 end-to-end, hardening, and acceptance pass for
the Assets module against `docs/requirements/ASSETS_REQUIREMENTS.md` and the exit
criteria in `docs/planning/ASSETS_IMPLEMENTATION_PLAN.md`.

## Verification Approach

Wave 8 was executed as a focused hardening and acceptance pass, matching the
Capex, Configuration, Opex, Inventory, and Travel precedents:

- Functional behaviour is covered by the automated suites delivered in Waves 0-7
  and gated on every pull request through the required CI checks
  (`Segaris Backend`, `Segaris PostgreSQL`, `Segaris Compose`; see
  `docs/planning/BACKEND_CI_DECISIONS.md`).
- The fast local suites are expected to remain green through the repository
  scripts: backend format verification, build, unit, API integration,
  architecture, provider migration coverage, frontend format, lint, type-check,
  unit, production build, and Playwright.
- The representative Playwright journey added below is compiled with the
  frontend test suite and runs against the Compose stack when seeded
  `SEGARIS_E2E_USERNAME` / `SEGARIS_E2E_PASSWORD` credentials are present.
- The OpenAPI surface and database indexes/query shape were verified statically
  against the implemented endpoints and paired provider migrations.

## End-To-End Journey

`tests/frontend/e2e/assets.spec.ts` adds a single-user critical journey against
the full stack: sign in, open Assets from the launcher, exercise and clear a
table filter, create an asset with category, location, code, and expected end of
life, stage and upload a PNG photo, mark it as the primary image, filter the
table by the unique code, reopen the asset, verify the primary image state, and
delete the safe test data. It is skipped without seeded credentials, matching the
other specs. The second-user privacy journey is deferred (see Deferred Items).

## Static Verification Results

### HTTP / OpenAPI Surface

All frozen Assets routes are mapped under `/api/assets` with explicit Minimal API
metadata and DTO contracts; EF Core entities are not exposed. The group requires
authentication, write routes apply antiforgery, and private or missing assets
share the module not-found behaviour.

- **Assets**: `GET /items`, `GET /items/{assetId}`, `POST /items`,
  `PUT /items/{assetId}`, and `DELETE /items/{assetId}` carry named OpenAPI
  operations, summaries, typed success responses, and problem responses for
  validation, not-found/privacy, duplicate code, visibility, and conflict paths.
- **Asset attachments**: `GET /items/{assetId}/attachments`,
  `POST /items/{assetId}/attachments`, `GET /items/{assetId}/attachments/{id}`,
  `DELETE /items/{assetId}/attachments/{id}`, and
  `PUT /items/{assetId}/attachments/{id}/primary` follow the shared attachment
  pattern, including upload size limits, owner authorization, and primary-image
  validation.
- **Catalogs**: `GET /categories` and `GET /locations` are authenticated reads.
  Administrator catalog management routes for create, update, move,
  deletion-impact, direct delete, and replace-and-delete use the established
  Configuration module-owned catalog boundary with antiforgery on writes.

### Indexes And Query Shape

The Assets persistence indexes exist in both SQLite and PostgreSQL migrations
(`AssetsDomainPersistence`) and match the implemented query shapes:

| Index | Query that uses it |
| --- | --- |
| `assets (Name, Id)` | Default deterministic ordering (name asc, id asc tie-breaker) |
| `assets (CreatedBy, Visibility, Id)` | Visibility/accessibility filter and creator filter |
| `assets (CategoryId)` / `(LocationId)` | Exact filters and catalog reference migration |
| `assets (Status)` / `(Visibility)` | Exact filters and attention exclusion of retired assets |
| `assets (ExpectedEndOfLifeDate)` | Launcher attention window |
| `assets (NormalizedCode)` unique filtered/indexed | Case-insensitive optional code uniqueness |
| `asset_categories` / `asset_locations (NormalizedName)` unique | Catalog name uniqueness |
| `asset_categories` / `asset_locations (SortOrder)` | Default catalog ordering |

List filtering, sorting, pagination, and partial search run as `IQueryable`
queries translated to SQL; the client never loads the full result set. Partial
search is an intentional database-backed `LIKE` scan across the accepted
identification and notes fields.

## Acceptance Criteria

Each criterion from `ASSETS_REQUIREMENTS.md` and its primary covering evidence:

| # | Criterion | Status | Primary evidence |
| --- | --- | --- | --- |
| 1 | Authenticated users can create, query, edit, and irreversibly delete visible assets with documented fields, defaults, validation, and privacy | Met | `AssetMutationTests`, `AssetDetailTests`, `AssetListTests`, `AssetsDomainTests`, `AssetsPage.test.tsx`, `assets.spec.ts` |
| 2 | An asset is a single individually identified entity with no stock, monetary value, or cross-module references | Met | `Asset`, `AssetsModelContributor`, provider migrations, `AssetsDomainTests`, `ModuleBoundaryTests` |
| 3 | Statuses `Active`, `Stored`, and `Retired` are available and descriptive, blocking no operation by themselves | Met | `AssetsContractTests`, `AssetsDomainTests`, `AssetMutationTests`, `AssetsAttentionTests` |
| 4 | Optional code is unique when present; brand/model and serial number are optional free-text identification fields | Met | `AssetMutationTests` (`Create_rejects_a_duplicate_code_ignoring_case_and_whitespace`, `Multiple_assets_without_a_code_do_not_collide`, `Update_replaces_every_editable_field`), `AssetsDomainTests`, provider migrations |
| 5 | Acquisition date and expected end of life are optional civil dates, and expected end of life is surfaced as "Expected end of life" | Met | `AssetMutationTests`, `AssetsAttentionTests`, `AssetDialog`, `AssetsTable`, `assets.spec.ts` |
| 6 | Public collaboration and private isolation follow the Segaris visibility baseline for assets | Met | `AssetMutationTests` (`Any_user_may_edit_a_public_asset_created_by_another`, `Only_the_creator_may_change_visibility`, private not-found test), `AssetListTests`, `AssetsAttentionTests` |
| 7 | Assets support multiple attachments with optional primary image thumbnail, falling back to first image and placeholder | Met | `AssetAttachmentTests`, `AssetAttachments`, `AssetsTable`, `assets.spec.ts` |
| 8 | Assets-owned `AssetCategory` and `AssetLocation` are initialized once and managed through Configuration with CRUD and reorder | Met | `AssetsCatalogEndpointTests`, `AssetsConfigurationMigrationTests`, Configuration page catalog sections and component coverage |
| 9 | Deleting a referenced category or location requires atomic replacement, never clearing, without disclosing private assets | Met | `AssetsConfigurationMigrationTests` (replacement, clearing rejection, privacy-neutral impact), provider migration coverage |
| 10 | Assets table presents documented columns with search, filters, sorting, bounded pagination, and URL-aware editor preserving table state | Met | `AssetListTests`, `AssetsPage`, `AssetsFilters`, `AssetsTable`, `assetsState`, `AssetsPage.test.tsx`, `assets.spec.ts` |
| 11 | Assets attention is true exactly for accessible non-`Retired` assets with expected end of life in the next 30 days in `Europe/Madrid` | Met | `AssetsAttentionTests`, `AssetsAttentionContributor`, launcher integration |
| 12 | SQLite and PostgreSQL migrations, backend unit/integration/architecture tests, frontend component tests, and a representative Playwright journey verify behaviour and privacy | Met (single-user E2E) | `MigrationTests`, `PostgresPersistenceTests`, `ModuleBoundaryTests`, `ModuleRegistrationTests`, Assets unit/API suites, `contracts.test.ts`, `AssetsPage.test.tsx`, `assets.spec.ts` |

## Deferred Items

Recorded in `ROADMAP.md`:

- **Second-user Assets privacy E2E journey.** Public-collaboration and
  private-isolation behaviour is covered by API integration tests
  (`AssetMutationTests`, `AssetListTests`, `AssetAttachmentTests`,
  `AssetsAttentionTests`); the browser-level multi-session journey waits on
  multi-account Playwright infrastructure, matching the deferred Capex,
  Configuration, Opex, Inventory, and Travel patterns.
- **PostgreSQL representative-volume query-plan benchmark.** Index existence and
  database-level query shape are verified; a large-dataset `EXPLAIN ANALYZE`
  benchmark waits on a representative seeding/benchmark harness.

## Documentation Outcomes

- `README.md`: no change; repository-wide commands and setup were unaffected by
  the Assets waves.
- `ROADMAP.md`: Assets implementation marked accepted; the two deferred items
  above recorded.
- `docs/planning/ASSETS_IMPLEMENTATION_PLAN.md`: Wave 8 status updated to point
  at this record.
