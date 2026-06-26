# Analytics Acceptance Record (Wave 11)

This document records the Wave 11 end-to-end, hardening, and acceptance pass for
the Analytics module against `docs/requirements/ANALYTICS_REQUIREMENTS.md` and the
exit criteria in `docs/planning/ANALYTICS_IMPLEMENTATION_PLAN.md`.

## Verification Approach

Wave 11 was executed as a focused hardening and acceptance pass, matching the
Capex, Configuration, Opex, Inventory, Travel, and Calendar precedents:

- Functional behaviour is covered by the automated suites delivered in Waves 0-10
  and gated on every pull request through the required CI checks (`Segaris
  Backend`, `Segaris PostgreSQL`, `Segaris Compose`; see
  `docs/planning/BACKEND_CI_DECISIONS.md`).
- The repository suites were re-run during this pass through the documented
  scripts and are green:
  - Backend `format --verify` and build (0 warnings, 0 errors).
  - Backend unit project: 764 tests passing.
  - Backend architecture project: 73 tests passing.
  - Backend API integration project: 414 tests passing.
  - Backend PostgreSQL integration project: 16 tests passing (Docker present).
  - Backend migration integration project: 6 tests passing (SQLite and
    PostgreSQL).
  - Frontend `format --verify`, lint, production build (TypeScript type-check),
    and unit suite (47 files, 287 tests passing).
  - The representative Playwright journey added below is compiled and listed; it
    runs against the Compose stack in CI when seeded credentials are present.
- The OpenAPI surface for the Analytics routes and the Configuration currency
  exchange-rate fields was verified statically against the implemented endpoints
  and contracts.

The frontend `format`/`lint` gates already include the new
`src/frontend/src/app/i18n/i18n.test.ts` Analytics namespace registration added in
Wave 8; no formatting drift was introduced by this pass.

## End-To-End Journey

`tests/frontend/e2e/analytics.spec.ts` adds a single-user representative journey
against the full stack: sign in, open Analytics from the launcher (which lands
directly on the yearly reporting surface), confirm the six-section tablist with
Overview selected, read the three Overview year totals and the monthly-trend chart
through its accessible `role="img"` summary, navigate years through the sub-bar and
assert the selected year round-trips through the URL and toggles the "This year"
control, then open Capex, Opex, Inventory, Travel, and Cross-module in turn and
confirm each lazily renders its own `h2` heading and at least one accessible chart.
It finishes by toggling a chart's data table so the table-equivalent of a chart is
exercised, and asserts the configuration-incomplete banner is absent on a
fully-configured stack. It is skipped without seeded `SEGARIS_E2E_USERNAME` /
`SEGARIS_E2E_PASSWORD` credentials, matching the other specs.

The missing-exchange-rate browser state is not forced in the live journey:
Configuration validation prevents creating an un-rated currency through the UI, so
the state cannot be triggered deterministically from the browser. It is instead
covered by `AnalyticsPage.test.tsx` (configuration-incomplete on Overview and
Inventory, with charts and totals hidden) and by the Analytics API integration
suite (`Capex_reports_configuration_incomplete_for_missing_rate`,
`Inventory_reports_configuration_incomplete_for_missing_rate`). The second-user
privacy journey is deferred (see Deferred Items).

## Static Verification Results

### HTTP / OpenAPI Surface

All six Analytics routes are mapped under `/api/analytics` as authenticated reads
on a group that applies `RequireAuthorization()`, never expose EF Core entities,
and carry explicit OpenAPI metadata (`AnalyticsEndpoints`):

- `GET /overview`, `GET /capex`, `GET /opex`, `GET /inventory`, `GET /travel`,
  and `GET /cross-module` each declare `WithName`/`WithSummary`, a typed
  `Produces<T>` response, and `ProducesProblem(400)` for the stable
  `analytics.year.invalid` problem returned on an unbounded or malformed year.
- The endpoints carry no mutations and therefore no antiforgery filter; the
  selected year is parsed from the query string and defaults to the current
  `Europe/Madrid` year.

The Configuration currency change is surfaced through the existing currency
catalog routes without new endpoints (`ConfigurationEndpoints`): `CurrencyResponse`
now carries `ExchangeRateToEur`, and `CurrencyItemRequest` accepts an optional
`ExchangeRateToEur` validated by `ConfigurationCatalogManagementService`
(`exchange_rate_not_one` for a non-`1` EUR rate, `exchange_rate_required` for a
non-EUR currency without a rate, `exchange_rate_invalid` for non-positive or
over-precise rates). The currency management routes remain admin-only with the
shared antiforgery filter.

### Privacy Boundary

Analytics never queries source-module entities. Each source module exposes a
purpose-built financial projection provider that applies the current user's
visibility filter before any row reaches Analytics. `FinancialProjectionProviderTests`
verifies, per provider, that private records are creator-only and that another user
(including a collaborator) receives none of them, so private-record existence is
never disclosed through Analytics aggregates.

## Acceptance Criteria

Each criterion from `ANALYTICS_REQUIREMENTS.md` and its primary covering evidence:

| # | Criterion | Status | Primary evidence |
| --- | --- | --- | --- |
| 1 | Authenticated users open Analytics directly on a yearly surface with previous/next/current-year and tab navigation | Met | `AnalyticsPage.test.tsx` (default year + Overview, lazy tab open, year navigation through the URL, six-section tablist), `analyticsState` parsing, `analytics.spec.ts` |
| 2 | Analytics consumes source projection contracts, not source entities/tables/services | Met | `FinancialProjectionProviderTests` (per-module providers over published contracts), `AnalyticsContractTests`, `ModuleBoundaryTests`, `AnalyticsFinancialProjection`/`AnalyticsProjectionContracts` |
| 3 | Source projections are filtered per current user; Analytics never receives inaccessible private records | Met | `FinancialProjectionProviderTests` (`*_keeps_private_*_creator_only` for Capex, Opex, Inventory, Travel) |
| 4 | Configuration currencies expose admin-managed current EUR rates, EUR fixed at 1, positive rates to at most eight decimals | Met | `ConfigurationCatalogTests` (`Exchange_rate_resolution_accepts_valid_values`, `_rejects_a_non_one_euro_rate`, `_requires_a_rate_for_non_euro_currencies`, `_rejects_non_positive_or_too_precise_rates`), `Capex/ConfigurationManagementEndpointTests` |
| 5 | Every metric recalculated using current rates on open/refetch; no historical snapshots | Met | `AnalyticsOverviewServiceTests` / `AnalyticsModuleGroupingServiceTests` (read-time EUR normalization), `CurrencyExchangeRateProvider` publishes current catalog rates (`Exchange_rate_provider_publishes_current_rates_in_catalog_order`) |
| 6 | Clear configuration-incomplete state when accessible data uses a currency without a usable EUR rate | Met | `AnalyticsCapexOpexEndpointTests` / `AnalyticsInventoryTravelEndpointTests` (`*_reports_configuration_incomplete_for_missing_rate` returning the missing codes), `AnalyticsPage.test.tsx` (banner replaces charts and totals) |
| 7 | Capex charts include only completed entries, grouped by category, supplier, cost centre, for expense and income | Met | `FinancialProjectionProviderTests` (completed-only, inclusive dates, labels), `AnalyticsModuleGroupingServiceTests` (six Capex grouped charts), `AnalyticsCapexOpexEndpointTests` |
| 8 | Opex charts include only realized occurrences, grouped by category, supplier, cost centre, for expense and income | Met | `FinancialProjectionProviderTests` (occurrence projection with parent labels), `AnalyticsModuleGroupingServiceTests` (six Opex grouped charts), `AnalyticsCapexOpexEndpointTests` |
| 9 | Inventory charts exclude planning/cancelled and report item-category, supplier, average order, top items, top suppliers | Met | `FinancialProjectionProviderTests` (planning/cancelled/out-of-range excluded, one projection per order line), `AnalyticsInventoryTravelServiceTests` (average + top-five share), `AnalyticsInventoryTravelEndpointTests`, `AnalyticsPage.test.tsx` |
| 10 | Travel charts exclude cancelled trips and report category, supplier, cost centre, linked destination | Met | `FinancialProjectionProviderTests` (non-cancelled trips, destination via reference reader, `ExcludeMissing` destination), `AnalyticsInventoryTravelServiceTests`, `AnalyticsInventoryTravelEndpointTests` |
| 11 | Cross-module charts report total expenses by supplier, category label, cost centre across modules | Met | `AnalyticsModuleGroupingServiceTests` (normalized category matching across modules), `AnalyticsInventoryTravelServiceTests`, `analytics.spec.ts` |
| 12 | Year-over-year comparison shown for every chart documented as meaningful (year N and N-1) | Met | `AnalyticsOverviewServiceTests` / `AnalyticsModuleGroupingServiceTests` (selected + previous year series), `format.test.ts` (year-over-year delta), `AnalyticsPage.test.tsx` (directional deltas) |
| 13 | Frontend lazy-loads tab data and handles loading, empty, error, and configuration-incomplete states without blocking other tabs | Met | `AnalyticsPage.test.tsx` (lazy per-tab fetch, error + retry, empty placeholder, configuration-incomplete), `queries.ts` keyed by year and tab |
| 14 | Chart library integrated behind Analytics-owned components with accessible summaries and responsive desktop layouts | Met | `AnalyticsChartCard.test.tsx` (role=img summary, chart/table toggle), `AnalyticsChartSummary`/`AnalyticsDataTable`, module-owned `charts/` wrappers around Recharts |
| 15 | Backend unit/integration/architecture tests, frontend component tests, and a representative Playwright journey verify behaviour, privacy, exchange rates, and all charts | Met (single-user E2E) | The Analytics unit, API integration, and architecture suites; the Analytics frontend component suites; `analytics.spec.ts` |

## Documentation Outcomes

- `README.md`: no change; repository-wide commands and setup were unaffected by
  the Analytics waves.
- `ROADMAP.md`: the Phase 2 reporting-model decision and the Analytics module
  section are marked implemented and accepted; the two deferred items below are
  recorded.
- `docs/planning/ANALYTICS_IMPLEMENTATION_PLAN.md`: Wave 11 status updated to point
  at this record.

## Deferred Items

Recorded in `ROADMAP.md`:

- **Second-user Analytics privacy E2E journey.** Source-module privacy filtering
  is covered by `FinancialProjectionProviderTests` and the Analytics API
  integration suite; the browser-level multi-session journey waits on
  multi-account Playwright infrastructure, matching the deferred Capex,
  Configuration, Opex, Inventory, Travel, and Calendar patterns.
- **Missing-exchange-rate live browser journey.** The configuration-incomplete
  state is covered by component and API integration tests; it is not forced in the
  live journey because Configuration validation prevents creating an un-rated
  currency through the UI.
- **Analytics source projection date-range indexes and aggregate
  caching/materialization.** Carried from Wave 3; dedicated date-range indexes and
  any persisted aggregates wait on representative data volumes.
