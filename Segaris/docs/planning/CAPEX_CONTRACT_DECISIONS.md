# Capex Contract Decisions (Wave 0)

## Purpose

This companion to `CAPEX_IMPLEMENTATION_PLAN.md` records the contracts frozen in
Wave 0 (the contract and test skeleton) and the cross-cutting decisions taken
before implementation. The authoritative source of these values is the code
under `src/backend/Segaris.Api/Modules/{Configuration,Capex,Launcher}`; the unit
tests in `Segaris.UnitTests` (`CapexContractTests`, `ModuleRegistrationTests`)
and the architecture tests in `Segaris.ArchitectureTests`
(`ModuleBoundaryTests`) keep this document and the code in agreement.

## Module Composition

Three modules were added as `ISegarisModule` shells and registered in
`SegarisModules`, in dependency order after the existing modules:

1. `Platform`
2. `Identity`
3. `Configuration` — owns the shared Supplier, CostCenter, Currency catalogs.
4. `Capex` — business module; may consume Configuration and platform contracts.
5. `Launcher` — aggregates per-module attention.

Wave 0 modules register no services and map no endpoints.

## Resolved Decisions

1. **Cross-module contracts are internal to `Segaris.Api`.** In this single
   assembly modular monolith the catalog reader (`IConfigurationCatalog`) and the
   launcher attention contributor (`ILauncherAttentionContributor`) are
   `internal` interfaces. The boundary is enforced by namespace ownership and the
   architecture tests rather than by separate assemblies. Configuration EF Core
   entities stay internal to the Configuration module.
2. **Catalog identity is auto-increment `Id` + stable `Code`.** Catalog rows use
   a database-assigned auto-increment integer `Id`; the stable identity for
   idempotent seeding and lookup is the `Code`. Wave 1/2 seeding upserts by
   `Code`. Cross-module references and foreign keys still use the integer `Id`.
3. **Launcher attention is a list of `{ module, requiresAttention }`.** The
   aggregated response is `LauncherAttentionResponse { Modules: [{ module,
   requiresAttention }] }`. Adding a later module means registering another
   contributor without changing the Capex contributor or the response shape.

## Frozen Catalog Codes

Codes are upper-case ASCII identifiers. Display names are canonical `en-GB` and
are localizable; they are never identities or API references.

- **Suppliers:** `AMAZON`, `IKEA`, `CARREFOUR`, `EL_CORTE_INGLES`,
  `LEROY_MERLIN`, `OTHER`.
- **Cost centers:** `HOUSEHOLD`, `PERSONAL`, `WORK`, `SHARED`, `OTHER`.
- **Currencies:** `EUR` (default), `USD`, `GBP`.
- **Capex categories:** `FURNITURE`, `APPLIANCES`, `TECHNOLOGY`, `HOME`,
  `FOOD_AND_DINING`, `LEISURE`, `HEALTH`, `TRANSPORT`, `TRAVEL`, `EDUCATION`,
  `GIFTS`, `TAXES_AND_FEES`, `SALARY_AND_INCOME`, `OTHER` (default).

## Fixed Vocabularies

Persisted as bounded strings and exchanged on the wire using these names:

- **Movement type:** `Income`, `Expense`.
- **Entry status:** `Planning`, `Completed`, `Canceled`.
- **Visibility:** the platform `Public` / `Private` values.

## HTTP Surface (route shapes, mapped in later Waves)

- Configuration: `GET /api/configuration/suppliers`,
  `GET /api/configuration/cost-centers`, `GET /api/configuration/currencies`.
- Capex categories: `GET /api/capex/categories`.
- Capex entries: `GET/POST /api/capex/entries`,
  `GET/PUT/DELETE /api/capex/entries/{entryId}`.
- Capex attachments: `GET/POST /api/capex/entries/{entryId}/attachments`,
  `GET/DELETE /api/capex/entries/{entryId}/attachments/{attachmentId}`.
- Launcher: `GET /api/launcher/attention`.

## Entries Query Contract

- Parameters: `search`, `from`, `to`, `type`, `status`, `category`, `supplier`,
  `costCenter`, `currency`, `visibility`, `creator`, plus the platform `page`,
  `pageSize`, `sort`, `sortDirection`.
- Sort allow-list: `title`, `type`, `status`, `dueDate`, `category`, `supplier`,
  `costCenter`, `total`, `currency`; tie-breaker `id`; default `dueDate`
  descending.
- Page sizes: `10`, `25`, `50`, `100` (default `25`).

## Error Codes

Capex domain failures use these stable codes; generic transport failures keep
the platform `ApiErrorCodes`:

- `capex.entry.not_found`
- `capex.entry.validation`
- `capex.catalog.unknown_reference`
- `capex.entry.visibility_forbidden`
- `capex.attachment.not_found`
- `capex.attachment.invalid`

## Attachments

Capex entry attachments use the owner `("Capex", "Entry", entryId)`.
