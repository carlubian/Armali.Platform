# Configuration Acceptance Record (Wave 6)

This document records the Wave 6 end-to-end acceptance pass for the Configuration
implementation against `docs/requirements/CONFIGURATION_REQUIREMENTS.md` and the
exit criteria in `docs/planning/CONFIGURATION_IMPLEMENTATION_PLAN.md`.

## Verification Approach

Wave 6 was executed as a focused acceptance and documentation pass over the
behaviour delivered in Waves 0-5:

- The full backend and frontend suites were run locally through the repository
  scripts, including the Docker-backed PostgreSQL and migration suites.
- The HTTP/OpenAPI surface, antiforgery metadata, administrator authorization,
  and privacy-neutral response shapes were verified statically against the
  implemented endpoints.
- An administrator Playwright journey was added for the non-currency management
  surface. Referenced-currency conversion and the non-administrator browser
  guard are covered by integration and component tests and recorded as deferred
  browser journeys (see Deferred Items).

## Test Run Results

Run on Windows with Docker available, via the repository scripts.

Backend (`backend-restore` → `backend-format -Verify` → `backend-build` →
`backend-test`, i.e. `dotnet test Segaris.slnx`):

| Suite | Result |
| --- | --- |
| `Segaris.UnitTests` | 108 passed, 0 failed, 0 skipped |
| `Segaris.ArchitectureTests` | 10 passed |
| `Segaris.Api.IntegrationTests` | 159 passed |
| `Segaris.Postgres.IntegrationTests` | 11 passed (Testcontainers PostgreSQL) |
| `Segaris.Migrations.IntegrationTests` | 6 passed |

Formatting verification and build completed without errors.

Frontend (`frontend-restore` → `frontend-lint` → `frontend-format -Verify` →
`tsc -b` → `frontend-build` → `frontend-test`):

| Step | Result |
| --- | --- |
| Lint, format verify, type-check | passed |
| Production build | passed |
| Unit/component tests (Vitest) | 170 passed across 32 files |

The administrator Playwright journey (`tests/frontend/e2e/configuration.spec.ts`)
compiles and lists under `playwright test --list`. Like every other end-to-end
spec it runs against the full stack and is skipped without seeded credentials
(`SEGARIS_E2E_USERNAME` / `SEGARIS_E2E_PASSWORD`); it executes in CI where those
are provided.

## Static Verification Results

### HTTP / OpenAPI Surface

Every management route is mapped with explicit OpenAPI metadata and never exposes
EF Core entities:

- Configuration: `ConfigurationEndpoints.MapConfigurationEndpoints` maps the
  three authenticated read routes (`Produces<T>`) plus, per shared catalog, the
  create, update, move, deletion-impact, delete, and replace-and-delete routes.
  Each carries `WithName`/`WithSummary` and typed `Produces<T>`, and error paths
  declare `ProducesProblem` for `400`/`404`/`409` as applicable.
- Capex categories: `CapexEndpoints` maps the equivalent category management
  routes with the same metadata and the shared
  `CatalogDeletionImpactResponse` impact contract.

### Antiforgery And Authorization

- Read routes (`GET /api/configuration/{suppliers,cost-centers,currencies}` and
  `GET /api/capex/categories`) sit under `RequireAuthorization()` only, so
  authenticated business forms keep reading catalogs.
- Every management subgroup applies `RequireAuthorization(IdentityPolicies.Admin)`,
  and every state-changing route (`POST`/`PUT`/`DELETE`) adds
  `AntiforgeryEndpointFilter`. Deletion-impact (`GET`) requires `Admin` but no
  antiforgery. Normal-user rejection is proven by
  `Management_routes_reject_normal_users`.

### Privacy-Neutral Responses

`CatalogDeletionImpactResponse` is a record of five booleans (`IsReferenced`,
`CanDeleteDirectly`, `CanClearReferences`, `RequiresExchangeRate`,
`HasReplacementCandidates`). It exposes no counts, titles, owners, or per-module
totals. `Referenced_values_report_private_neutral_impact_and_reject_direct_delete`,
`Cost_center_references_can_be_cleared_without_disclosing_entries`, and the
public/private migration tests confirm private records participate without
disclosure.

### Module Boundaries

`ModuleBoundaryTests` enforces `Configuration_does_not_depend_on_capex`,
`Capex_depends_on_configuration_contracts`, and
`Reference_management_contract_is_owned_by_configuration`.

## Acceptance Criteria

Each criterion from `CONFIGURATION_REQUIREMENTS.md` and its primary covering
evidence:

| # | Criterion | Status | Primary evidence |
| --- | --- | --- | --- |
| 1 | Admin-only Configuration UI/APIs; authenticated users still read catalogs | Met | `Management_routes_reject_normal_users`; read groups under `RequireAuthorization()`; `ConfigurationPage.test.tsx` launcher/route visibility |
| 2 | Flat Global/Capex sections, Global tabs, URL-backed selection, safe fallback to Global Suppliers | Met | `ConfigurationPage.tsx`; `ConfigurationPage.test.tsx` (tab switch, bare/unknown section and catalog fallback); `configuration.spec.ts` |
| 3 | Create/rename all four catalogs, edit currency codes, live names without changing IDs | Met | `Supplier_crud_trims_appends_moves_and_deletes`; `Seeding_is_idempotent_and_preserves_identifiers`; `ConfigurationPage.test.tsx` create/edit |
| 4 | Case-insensitive uniqueness, trimming, length, and code format on both providers | Met | `Normalization_trims_and_folds_invariant_without_collapsing_inner_whitespace`; `A_case_insensitive_duplicate_name/currency_code_violates_the_unique_index`; `Duplicate_names_and_invalid_currency_codes_return_stable_problems`; `PostgresPersistenceTests` |
| 5 | Move up/down; reads and form defaults use `SortOrder` then `Id`; new rows last | Met | `Reader_lists_bounded_models_in_sort_order_and_validates_identifiers`; `Supplier_crud_trims_appends_moves_and_deletes`; `ConfigurationPage.test.tsx` reordering; `configuration.spec.ts` |
| 6 | IDs/values survive migration, non-currency codes removed, one-time init never restores | Met | `Sqlite_upgrade_backfills_normalization_order_and_initialization_markers`; `Catalog_model_upgrade_follows_the_capex_domain_baseline`; `A_deliberately_emptied_catalog_is_never_restored`; `A_catalog_that_already_has_rows_is_marked_without_seeding` |
| 7 | Unreferenced rows delete after confirmation; last currency/category cannot be removed | Met | `Capex_categories_support_create_and_direct_delete`; `Required_catalogs_reject_deleting_the_last_value`; `ConfigurationPage.test.tsx` direct delete |
| 8 | Referenced suppliers/cost centres replaced or cleared; categories require another category | Met | `Supplier_replacement_migrates_public_and_private_entries_and_audits_the_admin`; `Cost_center_references_can_be_cleared_without_disclosing_entries`; `Category_replacement_migrates_references_and_deletes_the_source`; `configuration.spec.ts` clear path |
| 9 | Referenced currencies replaced via explicit rate, authoritative two-decimal recalculation | Met | `Currency_conversion_recalculates_public_and_private_entries_and_audits_the_admin`; `Postgres_converts_currency_recalculates_decimals_and_deletes_the_source_atomically`; conversion-dialog component tests |
| 10 | Migrations include private records without exposing identity/count, update metadata with acting admin | Met | Supplier/currency migration tests (public + private + audit); `CatalogDeletionImpactResponse` boolean-only shape |
| 11 | Concurrent references re-evaluated, direct delete never cascades, migration failure rolls back fully | Met | `Referenced_values_report_private_neutral_impact_and_reject_direct_delete`; atomic replace/convert integration + PostgreSQL tests |
| 12 | Popup forms, ordering, confirmations, retry/focus/feedback without app reload | Met | `ConfigurationPage.test.tsx` (validation, dirty close, focus return after reorder, empty state, accessibility/axe); `configuration.spec.ts` |
| 13 | SQLite/PostgreSQL migrations, backend tests, frontend component tests, architecture tests, and an admin Playwright journey | Met (currency-conversion and non-admin browser journeys deferred) | `MigrationTests`; `PostgresPersistenceTests`; `ModuleBoundaryTests`; integration + component suites; `configuration.spec.ts` |

## Deferred Items

Recorded in `ROADMAP.md`:

- **Browser-level currency conversion E2E journey.** Referenced-currency
  conversion and deletion are covered by API integration and PostgreSQL parity
  tests and the conversion-dialog component tests. The irreversible conversion
  journey is kept out of the browser run to leave the seeded catalogs intact.
- **Non-administrator Configuration browser journey.** Non-admin enforcement is
  covered by router/component tests and `Management_routes_reject_normal_users`.
  The browser-level guard is authored in `configuration.spec.ts` but skipped
  until multi-account Playwright infrastructure seeds a second account (the same
  gap recorded for the Capex privacy journey).

## Documentation Outcomes

- `README.md`: no change; repository-wide commands and setup were unaffected by
  the Configuration waves.
- `ROADMAP.md`: Configuration implementation marked accepted, with the two
  deferred browser journeys recorded.
- `docs/planning/CONFIGURATION_IMPLEMENTATION_PLAN.md`: Wave 6 status updated to
  point at this record.
