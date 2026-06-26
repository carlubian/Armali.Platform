# Analytics Requirements

## Status

Phase 2 functional definition is in discussion. This document records the
accepted initial decisions for the Analytics module and should become the
functional source of truth before implementation begins.

## Purpose

Analytics provides one yearly financial-trend surface for Segaris. It aggregates
income and expense information published by the financial modules so users can
understand spending patterns, income patterns, supplier concentration, category
distribution, cost-centre distribution, and year-over-year movement without
opening every source module separately.

Analytics is a cross-domain read module. Source modules remain authoritative for
their records, statuses, categories, visibility rules, deletion behavior,
currency references, and edit workflows. Analytics does not mutate financial
records and does not query source-module internal entities, tables, or EF Core
types directly.

## Initial Scope

- Present a yearly Analytics view from January through December.
- Let users navigate to previous and next years.
- Compare the selected year with the previous year for every chart where the
  comparison is meaningful.
- Normalize every monetary amount to EUR using the currently configured
  exchange rate to EUR.
- Include Capex, Opex, Inventory, and Travel financial data.
- Include all accepted single-module charts from the initial idea list.
- Include all accepted cross-module expense charts from the initial idea list.
- Lazy-load tab data so opening Analytics does not compute every chart at once.
- Add administrator-managed current exchange rates to EUR on Configuration
  currencies.
- Use a dedicated React charting library for Analytics visualizations rather than
  hand-building every chart primitive.
- Preserve source-module privacy filtering before data reaches Analytics.

## Excluded Scope

The initial Analytics implementation excludes:

- Historical exchange-rate snapshots.
- External exchange-rate providers or automatic exchange-rate lookup.
- Export to CSV, Excel, PDF, images, or printable reports.
- User-authored dashboards or configurable chart builders.
- Drill-down from chart segments to source record lists.
- Editing source-module records inside Analytics.
- Forecasting, budgets, targets, anomaly detection, or predictive trends.
- Persisted aggregate tables or background materialization jobs.
- Admin-only all-household aggregates that include another user's private data.
- Launcher attention, notifications, or Calendar integration.
- Spanish translations.
- Modules outside Capex, Opex, Inventory, and Travel.

## Currency Normalization

Analytics displays monetary values in EUR.

Configuration currencies gain an administrator-managed `ExchangeRateToEur`
value. The value means:

```text
1 source currency = ExchangeRateToEur EUR
```

The EUR currency has an exchange rate of `1`. EUR does not require special
calculation rules beyond that value.

Exchange rates are current configuration values, not historical records. Every
time a user opens Analytics, all metrics are recalculated using the exchange
rates currently configured at that time. Changing a currency's exchange rate
therefore changes Analytics results for historical years. This is accepted for
the initial household reporting model.

Exchange-rate validation:

- The value is required for every currency used by Analytics source data.
- The value must be positive.
- The value may contain up to eight decimal places.
- The EUR row must have value `1`.

If Analytics detects accessible data in a currency that does not have a usable
exchange rate to EUR, it must not silently omit or partially sum those amounts.
The affected Analytics response should identify the missing currency codes in a
safe configuration-incomplete state so the frontend can guide an administrator to
Configuration. Non-administrator users see the same incomplete-state explanation
without an edit action.

Source modules continue to store amounts in their own currencies. Analytics
normalization is a read-time calculation.

## Financial Projection Model

Analytics consumes purpose-built financial projections published by source
modules. Each source module applies current-user authorization and source-specific
status filtering before returning data to Analytics.

An indicative source financial projection contains:

- Stable source module code.
- Stable source financial record type.
- Source record identifier or opaque source reference for deterministic ordering
  and tests.
- Accounting date.
- Movement direction: `Income` or `Expense`.
- Original amount.
- Currency identifier and code.
- EUR-normalized amount or enough currency metadata for Analytics to normalize.
- Optional category label.
- Optional supplier label.
- Optional cost-centre label.
- Optional source-specific grouping labels such as item category, item name, or
  destination name.

Projection contracts are application contracts, not shared domain entities. They
must not expose EF Core entities, tracked navigation properties, inaccessible
record identifiers, private notes, attachment metadata, or mutation behavior.

## Source Module Rules

### Capex

Capex publishes accessible entries whose status is `Completed`.

The accounting date is the entry due date. Capex uses the entry movement type to
classify projections as income or expense. The projected amount is the
authoritative entry total. Capex provides category, supplier, and cost-centre
labels where present.

Planning entries are excluded from the initial Analytics scope even when their
date falls inside the selected year, because Analytics reports realized financial
history rather than pending attention.

### Opex

Opex publishes accessible realized occurrences.

The accounting date is the occurrence date. Opex uses the parent contract
movement type to classify occurrences as income or expense. The projected amount
is the occurrence actual amount. Opex provides category, supplier, and
cost-centre labels from the parent contract where present.

Planning contract estimates, inactive planned contracts, and future generated
expectations are not Analytics projections unless they have a realized occurrence.

### Inventory

Inventory publishes accessible order spending for orders whose status is neither
`Planning` nor `Cancelled`.

The accounting date is the order receipt or realization date recorded by the
implemented Inventory model. If implementation discovers that received orders do
not currently persist a distinct receipt date, the implementation plan may use
the best existing authoritative received-date field and record any data-model
extension explicitly.

Inventory projections are expenses only. The projected amount is the order total
in the order currency. Inventory provides supplier labels, item-category labels
for category charts, and item labels for top-item charts.

Planning and cancelled orders are excluded.

### Travel

Travel publishes accessible expenses whose parent trip status is not
`Cancelled`.

The accounting date is the expense date. Travel projections are expenses only.
The projected amount is the expense amount in the expense currency. Travel
provides expense category, supplier, cost-centre, and destination labels where
present.

Cancelled trips and their expenses are excluded. Non-cancelled completed,
ongoing, and planned trips may contribute expenses when those expenses fall
inside the selected year.

## Privacy And Authorization

Source modules enforce visibility before returning projections. Analytics never
receives inaccessible private records and does not infer access rights from
source identifiers.

The standard Segaris visibility rules apply:

- Public source records may contribute to every authenticated user's aggregates.
- Private source records contribute only for their creator.
- Administrators do not receive another user's private source records through
  Analytics.

Aggregated responses must avoid exposing private-data existence through
configuration or validation details. Missing exchange-rate states may identify
currency codes because currencies are shared Configuration data, but they must
not identify private source records, record owners, private titles, or per-user
private counts.

## Module Entry And Navigation

Analytics uses the protected lazy route:

```text
/analytics
```

One practical route shape is:

```text
/analytics
/analytics?year=2026
/analytics?year=2026&tab=capex
```

If router ergonomics suggest a slightly different parameter layout, preserve the
same behavior: selected year and selected tab should survive refresh and browser
history.

Opening Analytics takes the user directly to the yearly view. Analytics does not
have an initial landing page separate from the reporting surface.

The module provides:

- Previous-year and next-year controls.
- A current-year action.
- Tabs or equivalent sub-pages for `Overview`, `Capex`, `Opex`, `Inventory`,
  `Travel`, and `Cross-module`.
- Loading, empty, error, and configuration-incomplete states per tab.

## Analytics Views

### Overview

The Overview tab provides a high-level yearly summary across participating
modules.

It should include at least:

- Total expenses by month for selected year and previous year.
- Total income by month for selected year and previous year where source data has
  income projections.
- Net balance by month for selected year and previous year.
- Year total expense, year total income, and year net balance.

Overview exists to orient the user before moving into detailed module tabs. It
does not replace the detailed charts listed below.

### Capex

The Capex tab includes:

1. Capex expenses grouped by category.
2. Capex incomes grouped by category.
3. Capex expenses grouped by supplier.
4. Capex incomes grouped by supplier.
5. Capex expenses grouped by cost centre.
6. Capex incomes grouped by cost centre.

Only completed Capex entries are included.

### Opex

The Opex tab includes:

1. Opex expenses grouped by category.
2. Opex incomes grouped by category.
3. Opex expenses grouped by supplier.
4. Opex incomes grouped by supplier.
5. Opex expenses grouped by cost centre.
6. Opex incomes grouped by cost centre.

Only realized Opex occurrences are included.

### Inventory

The Inventory tab includes:

1. Inventory expenses grouped by item category.
2. Inventory expenses grouped by supplier.
3. Average order amount grouped by supplier.
4. Top five items by amount spent in the selected year, including each item's
   relative percentage of total Inventory expense.
5. Top five suppliers by amount spent in the selected year, including each
   supplier's relative percentage of total Inventory expense.

Planning and cancelled orders are excluded.

### Travel

The Travel tab includes:

1. Travel expenses grouped by category.
2. Travel expenses grouped by supplier.
3. Travel expenses grouped by cost centre.
4. Travel expenses grouped by destination for trips that link to a destination.

Cancelled trips are excluded. Expenses without a destination-linked trip are
excluded from the destination chart but may still appear in the other Travel
charts.

### Cross-Module

The Cross-module tab includes:

1. Total expenses grouped by supplier.
2. Total expenses grouped by category.
3. Total expenses grouped by cost centre.

Category matching across modules uses string comparison of the display label for
the initial version. Matching should use the same normalized string rules across
all participating modules and should be documented in the implementation plan.
This does not create shared category ownership.

## Chart Behavior

Most charts show year-over-year comparison: selected year `N` and previous year
`N-1`.

Charts where year-over-year comparison is expected:

- Monthly overview series.
- Grouped totals by category, supplier, cost centre, item category, and
  destination.
- Average order amount by supplier.
- Top-five supplier and item charts, where practical.

When a chart compares years and the set of labels differs between years, the
chart includes the union of labels needed to explain the selected year's top
entries and their previous-year comparison. The implementation plan may define
the exact display rule per chart type to keep dense charts readable.

Every chart must provide an accessible text alternative or table-equivalent
summary. Tooltips and legends cannot be the only way to understand values.

Amounts are rounded for display using normal currency formatting, but backend
aggregation should preserve enough precision to avoid avoidable cumulative
rounding error before the final response.

## Lazy Loading And Query Range

Analytics queries are scoped to one selected year. Backend calculations may also
query the previous year to produce year-over-year comparison.

The frontend should request only the currently selected tab's data. Switching
tabs may load the new tab on demand. Previously loaded tab data may be cached by
TanStack Query using query keys that include the selected year and tab.

The backend must reject unbounded or malformed year queries. Supported initial
years should be valid four-digit calendar years. The implementation plan may
define practical lower and upper bounds.

## Validation

Analytics query validation includes:

- Year is required or defaults to the current `Europe/Madrid` year.
- Year must be a valid supported calendar year.
- Tab or view identifiers must be allow-listed when represented in API requests.
- Unknown source-module or chart identifiers return structured errors.
- Missing exchange-rate configuration returns a stable configuration-incomplete
  response or problem code that the frontend can translate.

Configuration exchange-rate validation includes:

- Exchange rate to EUR is required when a currency may be used by Analytics.
- Exchange rate must be positive.
- Exchange rate may contain at most eight decimal places.
- EUR exchange rate must remain `1`.

## Chart Library

Analytics introduces a dedicated React charting dependency. Recharts is the
preferred candidate unless implementation research finds a concrete blocker.

The selected library must support:

- Accessible bar and line chart patterns, with application-supplied summaries
  where native semantics are insufficient.
- Responsive desktop layouts.
- Tooltips and legends.
- Multiple data series for year-over-year comparison.
- TypeScript-friendly React integration.
- Server-state driven rendering without a global chart store.

The library decision and package addition are part of the Analytics
implementation plan. Analytics should wrap the library in module-owned chart
components so later chart-library changes do not spread through every tab.

## Attention

Analytics may have a launcher card, but the initial Analytics module does not
request attention through the launcher.

Analytics displays requested reports when opened. It does not proactively notify
users about spending changes, missing data, or unusual patterns.

## Acceptance Criteria

The initial Analytics definition is satisfied when:

1. Authenticated users can open Analytics directly on a yearly reporting surface
   with previous-year, next-year, current-year, and tab navigation.
2. Analytics consumes source-module financial projection contracts rather than
   querying source module entities, tables, or implementation services directly.
3. Source projection results are filtered by each source module for the current
   user, and Analytics never receives or displays inaccessible private records.
4. Configuration currencies expose administrator-managed current exchange rates
   to EUR, with EUR fixed at `1` and positive rates validated to at most eight
   decimal places.
5. Analytics recalculates every metric using current exchange rates whenever the
   module is opened or refetched; no historical exchange-rate snapshots are used.
6. Analytics reports a clear configuration-incomplete state when accessible
   source data uses a currency without a usable exchange rate to EUR.
7. Capex charts include only completed entries and group expenses and incomes by
   category, supplier, and cost centre.
8. Opex charts include only realized occurrences and group expenses and incomes
   by category, supplier, and cost centre.
9. Inventory charts exclude planning and cancelled orders and report expenses by
   item category, supplier, average order amount by supplier, top five items, and
   top five suppliers.
10. Travel charts exclude cancelled trips and report expenses by category,
    supplier, cost centre, and linked destination.
11. Cross-module charts report total expenses by supplier, category label, and
    cost centre across participating modules.
12. Year-over-year comparison is shown for every chart where documented as
    meaningful, using selected year `N` and previous year `N-1`.
13. The frontend lazy-loads tab data and handles loading, empty, error, and
    configuration-incomplete states without blocking unrelated tabs.
14. The selected chart library is integrated behind Analytics-owned frontend
    components with accessible summaries and responsive desktop layouts.
15. Backend unit/integration/architecture tests, frontend component tests, and a
    representative Playwright journey verify supported behavior, privacy
    boundaries, exchange-rate behavior, and all accepted charts.

## Deferred Decisions

- Whether Analytics should support historical exchange rates or rate snapshots.
- Whether Analytics should support exports, printable reports, or scheduled
  reports.
- Whether users should be able to drill down from chart segments to source record
  lists.
- Whether budgets, targets, forecasting, or anomaly detection belong in
  Analytics.
- Whether Analytics should later include Assets, Maintenance, Projects,
  Processes, Recipes, Health, Mood, Clothes, Archive, Firebird, or future modules.
- Whether category matching across modules should evolve from string comparison
  to a shared taxonomy or manual mapping table.
- Whether Analytics aggregates should be cached or materialized after
  representative data volumes exist.
- Whether administrators should ever receive privacy-preserving all-household
  aggregates that include private records without disclosing record details.
