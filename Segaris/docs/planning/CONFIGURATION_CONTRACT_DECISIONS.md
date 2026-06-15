# Configuration Contract Decisions (Wave 0)

## Purpose

This companion to `CONFIGURATION_IMPLEMENTATION_PLAN.md` freezes the contracts
agreed before implementation. `CONFIGURATION_REQUIREMENTS.md` remains the
functional source of truth. Code and contract tests become authoritative once
the corresponding wave is implemented.

## Ownership And Dependency Direction

- Configuration owns Supplier, CostCenter, and Currency persistence and
  management.
- Capex owns CapexCategory persistence and management.
- The frontend composes both owners under one administrative experience.
- Configuration does not reference Capex namespaces or query Capex tables.
- Configuration owns narrow shared-catalog reference-management interfaces;
  consuming modules implement and register handlers for the catalog types they
  reference.
- All handlers participate in the one `SegarisDbContext` transaction started by
  the owning deletion command.

The existing dependency direction remains `Capex -> Configuration`. Dependency
inversion through a Configuration-owned interface does not introduce
`Configuration -> Capex`.

## Persisted Catalog Shape

Supplier, CostCenter, and CapexCategory:

- `Id`: generated integer primary key.
- `Name`: required, maximum 100 characters.
- `NormalizedName`: required normalized value used for uniqueness.
- `SortOrder`: required integer.
- Standard creation and modification metadata.

Currency adds:

- `Code`: required three-letter upper-case display code.
- `NormalizedCode`: required normalized value used for uniqueness.

Supplier, CostCenter, and CapexCategory no longer persist `Code`. Existing IDs
and foreign keys are preserved by provider-specific migrations.

Normalization trims exterior whitespace and uses an invariant upper-case form.
It does not collapse internal whitespace. Unique indexes protect normalized
names per table and normalized currency codes.

## One-Time Initialization Contract

An internal initialization table records stable catalog keys:

- `configuration.suppliers`
- `configuration.cost-centers`
- `configuration.currencies`
- `capex.categories`

At startup, an unmarked empty catalog receives its initial rows and is marked.
An unmarked nonempty catalog is marked without mutation. Marked catalogs are
never seeded again. The upgrade migration marks all four existing catalogs as
initialized so current rows are preserved exactly.

## Read Contracts

Existing authenticated catalog query routes remain:

- `GET /api/configuration/suppliers`
- `GET /api/configuration/cost-centers`
- `GET /api/configuration/currencies`
- `GET /api/capex/categories`

Non-currency rows return `{ id, name, sortOrder }`. Currency rows return
`{ id, code, name, sortOrder }`. Results are ordered by `sortOrder`, then `id`.

These reads remain available to authenticated users because business forms need
them. Administrative mutation and impact routes require `Admin`.

## Mutation Route Shapes

Shared catalogs use their existing collection paths:

- `POST /api/configuration/{catalog}`
- `PUT /api/configuration/{catalog}/{id}`
- `POST /api/configuration/{catalog}/{id}/move`
- `GET /api/configuration/{catalog}/{id}/deletion-impact`
- `DELETE /api/configuration/{catalog}/{id}`
- `POST /api/configuration/{catalog}/{id}/replace-and-delete`

`{catalog}` is allow-listed to `suppliers`, `cost-centers`, or `currencies`; the
implementation may map explicit endpoint groups rather than dynamically resolve
entity types.

Capex categories use equivalent Capex-owned routes:

- `POST /api/capex/categories`
- `PUT /api/capex/categories/{id}`
- `POST /api/capex/categories/{id}/move`
- `GET /api/capex/categories/{id}/deletion-impact`
- `DELETE /api/capex/categories/{id}`
- `POST /api/capex/categories/{id}/replace-and-delete`

All writes use antiforgery protection.

## Request Contracts

Non-currency create and update:

```json
{ "name": "Household" }
```

Currency create and update:

```json
{ "name": "Euro", "code": "EUR" }
```

Move:

```json
{ "direction": "up" }
```

Allowed directions are `up` and `down`. A boundary move is rejected as a
validation error rather than silently succeeding.

Replacement:

```json
{ "replacementId": 12, "clearReferences": false, "exchangeRate": null }
```

- `replacementId` is required unless `clearReferences` is true.
- `replacementId` and `clearReferences: true` are mutually exclusive.
- `clearReferences` is allowed only for Supplier and CostCenter.
- `exchangeRate` is required only for a referenced Currency and accepts a
  positive value with at most eight decimal places.
- Source and replacement IDs must differ.

## Deletion Impact Contract

The impact response reveals no counts or consumer details:

```json
{
  "isReferenced": true,
  "canDeleteDirectly": false,
  "canClearReferences": true,
  "requiresExchangeRate": false,
  "hasReplacementCandidates": true
}
```

The response is advisory. The command repeats every validation inside its
transaction. Direct `DELETE` succeeds only when the row is unreferenced and the
catalog's minimum-cardinality rule permits removal.

## Reference Management Contracts

Configuration publishes a narrow consumer interface whose implementation is
selected by shared catalog kind. It supports:

- Detecting whether any reference exists without returning records or counts.
- Replacing a source ID with a target ID.
- Clearing references when the consumer property is optional.
- Converting values when the catalog is Currency.
- Updating affected records with actor ID and UTC modification time.

Capex implements handlers for Supplier, CostCenter, and Currency. Capex manages
CapexCategory references directly inside its own command service.

Handlers must not save or commit independently. The owner performs one final
save and transaction commit after every handler succeeds.

## Currency Conversion Contract

The exchange rate always means:

`1 source = exchangeRate target`

For every affected Capex item:

1. `UnitAmount = Round(UnitAmount * exchangeRate, 2, AwayFromZero)`.
2. `LineAmount` is recalculated with the established Capex calculation routine.
3. Entry `TotalAmount` is recalculated from rounded lines.
4. Entry `CurrencyId`, `UpdatedAt`, and `UpdatedBy` are updated.

Quantity does not change. The conversion is not stored as rate history and is
not reversible through a special operation.

## Frontend Route Contract

- `/configuration` redirects to `/configuration/global?catalog=suppliers`.
- `/configuration/global?catalog=suppliers`
- `/configuration/global?catalog=cost-centers`
- `/configuration/global?catalog=currencies`
- `/configuration/capex`

Unknown sections and catalogs redirect to Global Suppliers. The route and
launcher card are available only to administrators.

## Error Codes

Configuration failures use stable namespaced codes:

- `configuration.catalog.not_found`
- `configuration.catalog.validation`
- `configuration.catalog.duplicate_name`
- `configuration.currency.duplicate_code`
- `configuration.currency.invalid_code`
- `configuration.catalog.required_not_empty`
- `configuration.catalog.referenced`
- `configuration.catalog.invalid_replacement`
- `configuration.catalog.exchange_rate_required`
- `configuration.catalog.exchange_rate_invalid`
- `configuration.catalog.migration_conflict`
- `configuration.catalog.migration_failed`

Capex category management uses parallel Capex-owned codes:

- `capex.category.not_found`
- `capex.category.validation`
- `capex.category.duplicate_name`
- `capex.category.required_not_empty`
- `capex.category.referenced`
- `capex.category.invalid_replacement`
- `capex.category.migration_conflict`

Generic authorization, antiforgery, and transport failures continue to use the
platform contracts.
