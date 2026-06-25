# Analytics Implementation Plan

## Purpose

This plan delivers the initial Analytics module defined in
`docs/requirements/ANALYTICS_REQUIREMENTS.md`. It translates the accepted
functional decisions into dependency-ordered Waves with explicit backend,
frontend, migration, charting, and test work.

The requirements document remains authoritative for behavior. This plan owns
delivery order and technical scope.

## Delivery Principles

- Preserve Analytics as a cross-domain read module. Financial records remain
  owned by Capex, Opex, Inventory, and Travel.
- Keep source-module dependency direction explicit: Analytics consumes narrow
  published financial projection contracts from participating modules.
- Do not query source-module EF Core entities, tables, `DbSet`s, or internal
  implementation services from Analytics.
- Enforce visibility at the source-module boundary so Analytics never receives
  inaccessible private records.
- Normalize amounts to EUR at read time using current Configuration exchange
  rates. Do not introduce historical rates, snapshots, or external providers.
- Include every accepted chart in the initial implementation, but split delivery
  by module and tab so the work stays testable.
- Lazy-load frontend tab data and keep query keys scoped by year and tab.
- Add one dedicated charting library and wrap it in Analytics-owned chart
  primitives.
- Keep each Wave independently testable and record deferrals in `ROADMAP.md`
  instead of hiding them in implementation notes.

## Fixed Technical Contracts

### Backend Module

Analytics lives under `Segaris.Api.Modules.Analytics` and owns:

- Analytics API endpoint registration.
- Year query validation.
- Current exchange-rate resolution and missing-rate detection.
- Aggregation across source financial projection providers.
- Chart response DTOs and stable chart identifiers.
- Analytics-specific stable error codes.
- Final deterministic ordering and top-list selection.

Analytics owns no persisted financial records in the initial version.

Indicative resource routes are:

```text
GET /api/analytics/overview?year=2026
GET /api/analytics/capex?year=2026
GET /api/analytics/opex?year=2026
GET /api/analytics/inventory?year=2026
GET /api/analytics/travel?year=2026
GET /api/analytics/cross-module?year=2026
```

If implementation prefers one endpoint with a `view` parameter, preserve the same
observable behavior: each frontend tab must be independently fetchable and
cacheable, with clear contract names and allow-listed view identifiers.

Analytics does not expose write endpoints in the initial version. It contributes
no launcher attention.

### Configuration Currency Contract

Configuration currencies gain an `ExchangeRateToEur` value.

Configuration owns:

- Persistence and migrations for the new field.
- Administrator create/update validation.
- EUR fixed-rate enforcement.
- Read contracts that expose the current exchange rate to authorized consumers.
- Frontend catalog editing support.

Analytics consumes Configuration's published currency read contract. It does not
query Configuration tables directly.

### Source Financial Projection Contracts

Each participating source module publishes one contract that returns
current-user-authorized financial projections for an inclusive date range.

The contract should support at least:

```text
Task<IReadOnlyList<FinancialProjection>> ListFinancialProjectionsAsync(
    DateOnly from,
    DateOnly to,
    CancellationToken cancellationToken)
```

The concrete record shape is finalized in Wave 0, but it must include source
module, source type, accounting date, movement direction, amount, currency,
category, supplier, cost centre, and the source-specific labels required by the
accepted charts.

The query range for a selected year view is normally:

- `N-01-01` through `N-12-31`.
- `N-1-01-01` through `N-1-12-31`.

The provider may receive one combined two-year range or two separate year ranges
as long as tests prove correct selected-year and previous-year separation.

### Chart Response Contracts

Analytics chart DTOs should be small, explicit, and frontend-oriented.

Indicative shapes:

```text
AnalyticsMoneySeriesPoint
  month
  selectedYearAmountEur
  previousYearAmountEur

AnalyticsGroupedAmountPoint
  label
  selectedYearAmountEur
  previousYearAmountEur

AnalyticsAverageAmountPoint
  label
  selectedYearAverageEur
  previousYearAverageEur
  selectedYearCount
  previousYearCount

AnalyticsTopAmountPoint
  label
  selectedYearAmountEur
  previousYearAmountEur
  selectedYearPercent
  previousYearPercent
```

Response contracts should include:

- Selected year.
- Previous year.
- Chart identifiers.
- Missing exchange-rate codes when the response cannot safely aggregate all
  accessible data.
- Ordered chart data points.

The implementation may refine names, but it should avoid returning raw source
records or making the frontend assemble core financial aggregates.

### Frontend Route

Analytics uses the protected lazy route `/analytics`.

One practical route shape is:

```text
/analytics
/analytics?year=2026
/analytics?year=2026&tab=inventory
```

`/analytics` defaults to the current `Europe/Madrid` year and the `Overview` tab.
The selected year and tab should be URL-backed.

### Chart Library

Analytics introduces a dedicated React charting dependency. Recharts is the
preferred default unless Wave 0 research finds a concrete blocker.

The selected library must be wrapped behind Analytics-owned primitives, for
example:

- `AnalyticsComparisonBarChart`
- `AnalyticsMonthlyLineChart`
- `AnalyticsTopListChart`
- `AnalyticsChartCard`
- `AnalyticsChartSummary`

These wrappers own formatting, legends, tooltip content, empty states, and
accessible summaries. Analytics pages should not scatter low-level chart-library
configuration across every chart.

## Waves

### Wave 0: Contracts, Chart Library Decision, And Test Skeleton

Establish stable Analytics contracts before persistence, aggregation, or UI work
begins.

Tasks:

1. Add the Analytics module shell and registration after Calendar.
2. Freeze Analytics route constants, tab identifiers, chart identifiers, source
   module codes, movement-direction codes, response DTO names, query parameters,
   stable error codes, and the absence of launcher attention.
3. Define source financial projection contract shapes for Capex, Opex, Inventory,
   and Travel without exposing source entities.
4. Define the current exchange-rate read contract that Analytics needs from
   Configuration.
5. Define frontend API functions, route-state helpers, query keys, chart data
   TypeScript types, and tab identifiers.
6. Choose the charting dependency, with Recharts as the default candidate, and
   document any rejected option only if there is a concrete reason.
7. Add architecture-test expectations: Analytics may consume participating source
   projection contracts and Configuration currency contracts; source modules do
   not depend on Analytics.
8. Add empty backend and frontend test fixtures shared by later Waves.

Tests:

- Unit tests for year parsing, default current-year resolution, tab/chart
  identifier stability, movement-direction stability, and missing-rate error-code
  stability.
- Architecture tests for permitted dependencies and the absence of source-module
  dependencies on Analytics.
- Frontend tests for route-state parsing defaults and invalid tab/year fallback.

Exit criteria:

- Public contracts are explicit and test-covered, and later Waves do not need to
  invent route, chart, source, year, or dependency semantics.

### Wave 1: Configuration Exchange Rates To EUR

Implement administrator-managed current exchange rates on currencies.

Tasks:

1. Add `ExchangeRateToEur` to the Configuration currency entity and mappings.
2. Seed EUR with `1` and other seeded currencies with explicit placeholder or
   accepted initial rates suitable for development. If production rates should be
   administrator-supplied, record that incomplete configuration behavior.
3. Validate positive values with at most eight decimal places.
4. Enforce EUR value `1` on create/update and migration paths.
5. Expose the value through Configuration currency read contracts and API
   responses.
6. Update Configuration frontend currency create/edit forms, tables, validation,
   and translations.
7. Add provider-specific migrations and model snapshots.

Tests:

- Unit tests for exchange-rate validation, EUR fixed-rate behavior, and precision
  limits.
- API integration tests for currency create/update/list behavior, admin-only
  writes, validation failures, and backwards-compatible catalog ordering.
- Frontend component tests for currency form validation and table display.
- SQLite and PostgreSQL migration tests, including fresh creation and upgrade
  coverage.

Exit criteria:

- Administrators can manage current exchange rates to EUR, and Analytics has a
  stable read contract for currency normalization.

### Wave 2: Capex And Opex Financial Projection Providers

Publish the first two financial projection providers.

Tasks:

1. Implement Capex projections for accessible completed entries in an inclusive
   accounting-date range.
2. Use Capex due date as accounting date, movement type as direction, entry total
   as amount, and entry currency as currency.
3. Include Capex category, supplier, and cost-centre labels.
4. Implement Opex projections for accessible realized occurrences in an
   inclusive accounting-date range.
5. Use Opex occurrence date as accounting date, parent movement type as
   direction, occurrence actual amount as amount, and parent currency as currency.
6. Include Opex category, supplier, and cost-centre labels from the parent
   contract.
7. Add provider-compatible indexes only where existing indexes are insufficient
   for the two-year range queries.

Tests:

- Capex integration tests for status filtering, date bounds, movement direction,
  amount, labels, currency, public collaboration, and private isolation.
- Opex integration tests for realized-occurrence filtering, date bounds, parent
  movement direction, amount, labels, currency, public collaboration, and private
  isolation.
- PostgreSQL coverage for provider-sensitive date and decimal aggregation paths.
- Architecture tests confirming Capex and Opex publish contracts without
  depending on Analytics.

Exit criteria:

- Analytics can consume current-user financial projections from Capex and Opex
  with correct status, date, amount, currency, label, and privacy behavior.

### Wave 3: Inventory And Travel Financial Projection Providers

Publish the remaining financial projection providers.

Tasks:

1. Implement Inventory projections for accessible orders whose status is neither
   `Planning` nor `Cancelled`.
2. Use Inventory's authoritative receipt or realization date as accounting date.
   If a durable receipt-date field is missing, add the smallest compatible data
   model extension and migration needed to support Analytics correctly.
3. Use order total as amount and order currency as currency.
4. Include supplier labels, item-category labels, and item labels required by
   Inventory charts.
5. Implement Travel projections for accessible expenses whose parent trip is not
   `Cancelled`.
6. Use Travel expense date as accounting date, expense amount as amount, and
   expense currency as currency.
7. Include Travel expense category, supplier, cost-centre, and linked destination
   labels where present.
8. Add provider-compatible indexes only where existing indexes are insufficient
   for the two-year range queries.

Tests:

- Inventory integration tests for status exclusions, receipt-date accounting,
  order totals, supplier labels, item-category labels, item labels, public
  collaboration, and private isolation.
- Travel integration tests for cancelled-trip exclusions, expense-date bounds,
  amount, currency, labels, linked destination behavior, public collaboration,
  and private isolation.
- SQLite and PostgreSQL coverage for any Inventory receipt-date migration.
- Architecture tests confirming Inventory and Travel publish contracts without
  depending on Analytics.

Exit criteria:

- Analytics can consume current-user financial projections from Inventory and
  Travel with correct status, date, amount, currency, label, and privacy behavior.

### Wave 4: Analytics Aggregation Core And Overview Endpoint

Deliver the backend aggregation foundation and Overview response.

Tasks:

1. Implement current exchange-rate lookup and missing-rate detection.
2. Implement read-time EUR normalization using current `ExchangeRateToEur`.
3. Implement selected-year and previous-year range resolution.
4. Implement `GET /api/analytics/overview`.
5. Aggregate monthly total expenses, income, and net balance for selected year
   and previous year.
6. Return year total expense, total income, and net balance.
7. Return a stable configuration-incomplete response when accessible source data
   uses missing or invalid exchange rates.
8. Add deterministic ordering and rounding rules for response values.
9. Add a fake/test projection provider for aggregation tests independent from
   real modules.

Tests:

- Unit tests for year range resolution, current-year defaults, EUR normalization,
  missing-rate detection, decimal precision, and month bucket generation.
- API integration tests for overview aggregation, no-data years, previous-year
  comparison, mixed currencies, missing rates, and mixed-source ordering.
- Privacy tests proving inaccessible private source projections do not affect
  overview results.

Exit criteria:

- Analytics exposes a validated Overview endpoint that aggregates authorized
  financial projections across participating modules and normalizes them to EUR
  with current rates.

### Wave 5: Capex And Opex Analytics Endpoints

Implement detailed Capex and Opex charts.

Tasks:

1. Implement `GET /api/analytics/capex`.
2. Aggregate Capex expenses grouped by category, supplier, and cost centre.
3. Aggregate Capex incomes grouped by category, supplier, and cost centre.
4. Implement `GET /api/analytics/opex`.
5. Aggregate Opex expenses grouped by category, supplier, and cost centre.
6. Aggregate Opex incomes grouped by category, supplier, and cost centre.
7. Include selected-year and previous-year values for every grouped chart.
8. Define and test label fallback for missing optional supplier or cost-centre
   values.

Tests:

- API integration tests for every Capex chart, including selected-year values,
  previous-year values, missing optional labels, mixed currencies, and no-data
  charts.
- API integration tests for every Opex chart with the same coverage.
- Regression tests proving planning Capex entries and unrealized Opex estimates
  are excluded.

Exit criteria:

- The backend serves all accepted Capex and Opex Analytics charts with correct
  grouping, comparison, currency normalization, and privacy behavior.

### Wave 6: Inventory And Travel Analytics Endpoints

Implement detailed Inventory and Travel charts.

Tasks:

1. Implement `GET /api/analytics/inventory`.
2. Aggregate Inventory expenses grouped by item category.
3. Aggregate Inventory expenses grouped by supplier.
4. Calculate average order amount grouped by supplier.
5. Calculate top five Inventory items by selected-year amount spent, including
   selected-year percentage of total expense and previous-year comparison.
6. Calculate top five Inventory suppliers by selected-year amount spent,
   including selected-year percentage of total expense and previous-year
   comparison.
7. Implement `GET /api/analytics/travel`.
8. Aggregate Travel expenses grouped by category, supplier, cost centre, and
   linked destination.
9. Define and test label fallback for missing optional supplier or cost-centre
   values; destination chart excludes expenses without a linked destination.

Tests:

- API integration tests for every Inventory chart, including status exclusions,
  averages, top-five ordering, percentage calculation, tie-breakers,
  previous-year values, mixed currencies, and no-data charts.
- API integration tests for every Travel chart, including cancelled-trip
  exclusion, linked-destination filtering, optional labels, previous-year values,
  mixed currencies, and no-data charts.

Exit criteria:

- The backend serves all accepted Inventory and Travel Analytics charts with
  correct grouping, top-list, comparison, currency normalization, and privacy
  behavior.

### Wave 7: Cross-Module Analytics Endpoint

Implement detailed cross-module expense charts.

Tasks:

1. Implement `GET /api/analytics/cross-module`.
2. Aggregate total expenses grouped by supplier across Capex, Opex, Inventory,
   and Travel.
3. Aggregate total expenses grouped by category label across participating
   modules.
4. Aggregate total expenses grouped by cost centre across participating modules.
5. Define category string normalization for cross-module matching.
6. Include selected-year and previous-year values for every grouped chart.
7. Ensure modules that do not provide a dimension for a chart are excluded from
   that chart rather than counted under misleading labels.

Tests:

- API integration tests for supplier, category, and cost-centre cross-module
  charts.
- Tests for category string normalization across modules.
- Tests for optional supplier/cost-centre handling by source module.
- Tests for mixed privacy, mixed currencies, and previous-year comparison.

Exit criteria:

- The backend serves all accepted cross-module expense charts with deterministic
  grouping and no source-module ownership leakage.

### Wave 8: Frontend Analytics Shell, Route State, And Chart Primitives

Build the route, tab shell, chart dependency integration, and shared primitives.

Tasks:

1. Add the lazy `/analytics` route, module error boundary, translation namespace,
   and launcher card with no attention state.
2. Add the selected-year and selected-tab route-state helpers.
3. Build previous-year, next-year, and current-year controls.
4. Build tab navigation for Overview, Capex, Opex, Inventory, Travel, and
   Cross-module.
5. Add the charting dependency selected in Wave 0 and update lockfiles.
6. Build Analytics-owned chart wrappers for comparison bars, monthly series,
   top-list charts, legends, tooltips, empty states, and accessible summaries.
7. Implement API clients, Zod schemas where useful, TanStack Query keys, and
   loading/error/configuration-incomplete state components.
8. Ensure tab data is fetched lazily and cached by year and tab.

Tests:

- Frontend API and component tests for route-state defaults, year navigation,
  tab navigation, lazy query triggering, loading/error states, missing-rate
  states, and chart wrapper rendering.
- Accessibility tests for tab controls, chart summaries, keyboard navigation,
  and non-tooltip access to values.

Exit criteria:

- Users can open Analytics, navigate years and tabs, and see stable chart
  containers fed by mocked API data without every tab loading at once.

### Wave 9: Frontend Overview, Capex, And Opex Tabs

Build the first half of the user-facing chart tabs.

Tasks:

1. Build the Overview tab with monthly expense, income, and net balance
   comparison plus yearly totals.
2. Build all six Capex charts.
3. Build all six Opex charts.
4. Apply consistent EUR formatting, year labels, legends, empty states, and
   accessible summaries.
5. Verify dense grouped labels remain readable on supported desktop widths.
6. Wire configuration-incomplete states to the relevant tabs and administrator
   Configuration navigation where available.

Tests:

- Component tests for Overview totals, monthly comparison charts, Capex charts,
  Opex charts, no-data states, missing-rate states, and formatted EUR values.
- Accessibility and layout tests for chart labels, legends, focus order, and
  keyboard tab navigation.

Exit criteria:

- Users can inspect Overview, Capex, and Opex Analytics tabs from real API
  responses with usable chart states and accessible summaries.

### Wave 10: Frontend Inventory, Travel, And Cross-Module Tabs

Build the remaining user-facing chart tabs.

Tasks:

1. Build all five Inventory charts, including average order and top-five
   percentage displays.
2. Build all four Travel charts.
3. Build all three Cross-module charts.
4. Reuse chart primitives from Wave 8 and refine them only when the remaining
   chart types expose a real gap.
5. Verify top-list behavior, label truncation, tooltips, legends, and accessible
   summaries for dense data.

Tests:

- Component tests for Inventory, Travel, and Cross-module chart rendering,
  no-data states, missing-rate states, top-five percentages, averages, and
  formatted EUR values.
- Accessibility and layout tests for dense labels and responsive desktop widths.

Exit criteria:

- Users can inspect every accepted Analytics chart from real API responses.

### Wave 11: End-To-End, Hardening, And Acceptance

Validate the implemented behavior across both providers and the deployed
frontend/backend boundary.

Tasks:

1. Run backend format, build, unit, API, PostgreSQL, migration, and architecture
   suites through repository scripts.
2. Run frontend format, lint, type-check, unit, build, and Playwright suites.
3. Add a representative Playwright journey covering login, opening Analytics,
   year navigation, lazy tab loading, Overview, at least one chart in every tab,
   and a missing-exchange-rate state.
4. Add seeded or test-created Capex, Opex, Inventory, and Travel records covering
   selected year, previous year, mixed currencies, and private isolation.
5. Review OpenAPI for Analytics routes and Configuration currency changes.
6. Verify keyboard behavior, tab state refresh, chart readability, tooltips,
   accessible summaries, no-data states, missing-rate states, and narrow desktop
   widths.
7. Map every criterion in `docs/requirements/ANALYTICS_REQUIREMENTS.md` to
   covering code and tests in an Analytics acceptance record.
8. Update `ROADMAP.md` to mark Analytics as implemented and accepted and record
   only intentional remaining deferrals.

Exit criteria:

- All relevant repository scripts pass, both providers are covered, the Compose
  journey succeeds, and every accepted Analytics requirement is implemented or
  explicitly deferred.

## Suggested Pull Request Boundaries

1. Analytics contracts, chart-library decision, and Configuration exchange rates
   (Waves 0-1).
2. Source financial projection providers (Waves 2-3).
3. Analytics backend aggregation endpoints (Waves 4-7).
4. Frontend shell and chart primitives (Wave 8).
5. Frontend Overview, Capex, and Opex tabs (Wave 9).
6. Frontend Inventory, Travel, and Cross-module tabs (Wave 10).
7. End-to-end, hardening, and acceptance (Wave 11).

## Plan Completion Criteria

The plan is complete when all Waves meet their exit criteria and the Analytics
requirements document describes implemented behavior rather than only functional
intent.

Historical exchange rates, exports, drill-down source lists, budgets, forecasts,
anomaly detection, persisted aggregate materialization, more source modules,
shared category mapping, and privacy-preserving administrator-wide aggregates
remain separate future planning topics.
