# Configuration Requirements

## Status

Phase 2 functional definition is complete. This document is the functional
source of truth for the initial Configuration management implementation plan.

## Purpose

Configuration gives household administrators one place to manage the reference
values used by Segaris forms and business records. It presents shared platform
catalogs and module-owned classifications through one coherent administrative
experience without transferring domain ownership to a generic configuration
model.

The initial scope covers four catalogs:

- Global suppliers.
- Global cost centers.
- Global currencies.
- Capex categories.

## Ownership And Boundaries

The platform Configuration module owns suppliers, cost centers, and currencies.
Capex continues to own its categories, persistence, validation, and mutation
endpoints.

The frontend may combine these catalogs under one Configuration route, but the
backend does not expose a generic catalog entity or let Configuration access
Capex tables directly. Shared catalogs publish narrow reference-management
contracts that consuming modules implement. A module-specific classification is
managed through its owning module.

## Authorization

Only users with the `Admin` role may see the Configuration launcher card, open
the Configuration route, or call catalog mutation and deletion-impact APIs.

Authenticated non-administrators continue to read catalog options through the
existing purpose-specific query APIs used by business forms. Administrative
access does not grant permission to inspect private business records referenced
by a catalog value.

## Navigation And Sections

Configuration is available from an administrator-only launcher card and uses
the protected route `/configuration`.

Its first navigation level is flat:

- `Global`
- `Capex`
- Future module sections such as `Opex` at the same level

There is no intermediate `Modules` group. A section with several catalogs uses
tabs inside the section; a section with one catalog does not show a redundant
second navigation level.

The initial route state is:

- `/configuration/global?catalog=suppliers`
- `/configuration/global?catalog=cost-centers`
- `/configuration/global?catalog=currencies`
- `/configuration/capex`

`/configuration` and unknown section or catalog values redirect to Global
Suppliers for the initial implementation.

## Catalog Model

Every catalog row has:

- An internal auto-increment integer `Id` used by foreign keys and APIs.
- A required display `Name`.
- An integer `SortOrder`.
- Standard creation and modification metadata.

The internal identifier and audit metadata are not shown in the normal
Configuration interface.

Currency additionally has a required three-letter `Code`. The code is visible,
editable, normalized to upper case, and used for monetary display. Supplier,
cost-center, and Capex-category technical codes are removed; their integer IDs
are their only persisted identities.

Names are trimmed and limited to 100 characters. Within one catalog, two names
must not be equal after trimming and case-insensitive comparison. Internal
spaces remain significant and are not collapsed. Currency codes are unique
case-insensitively within the currency catalog and consist of exactly three
letters. Segaris does not require the code to appear in a compiled ISO 4217
allow-list.

Catalogs do not support active/inactive state, descriptions, icons, colors, or
custom metadata in the initial version.

## Ordering And Defaults

Catalog queries return rows ordered by `SortOrder`, then by `Id` as a
deterministic tie-breaker.

New rows are appended after the current last row. Administrators reorder rows
with keyboard- and touch-accessible move-up and move-down actions. Moving a row
exchanges it with its current neighbor. Boundary actions are disabled, and no
drag-and-drop dependency is introduced.

The implementation may normalize remaining positions after deletion. It does
not require a database uniqueness constraint on `SortOrder`; the application
maintains normal ordering and `Id` resolves any transient or legacy tie.

When a business form needs an implicit catalog default, it selects the first
available row by `SortOrder`, then `Id`. Deleting or moving the first row may
therefore change the default for subsequently opened forms. There is no
separate configurable-default record in this scope.

## Initial Values And One-Time Initialization

Existing catalog values remain the initial values defined by the Capex
requirements. Initial values are inserted only once for each catalog.

A catalog-initialization record distinguishes a never-initialized empty table
from a table deliberately emptied by administrators:

- If a catalog is not marked initialized and its table is empty, its initial
  values are inserted and the catalog is marked initialized.
- If a catalog already contains rows during upgrade, it is marked initialized
  without replacing or adding rows.
- Once initialized, a catalog is never automatically repopulated, even when it
  later becomes empty.

The current upsert-by-code behavior is retired. Application startup must not
restore deleted values or overwrite administrator names.

## Listing And Table Experience

Each catalog is displayed as a complete table without initial search or
pagination. Catalogs are expected to remain small, normally no more than about
25 rows, but the backend does not impose a 25-row maximum.

Common columns are:

- Name.
- Ordering controls.
- Edit action.
- Delete action.

Currency also displays Code. Tables provide loading, retryable error, empty,
and mutation-in-progress states. Empty optional catalogs offer a clear action
to create their first value.

Successful creation and editing refresh the server-backed catalog and render it
in normal catalog order without a temporary row highlight. Successful move
operations do not produce a toast; failures do and restore the confirmed server
order.

## Creation And Editing

Creation and editing use popup dialogs over the visible table. They use explicit
save actions and no autosave.

The dialog contains Name and, for currency, Code. It preserves input after
validation or server errors, prevents duplicate submission, associates errors
with their fields, and requires confirmation before closing with unsaved
changes. Focus enters the first field and returns to the originating control on
close.

Editing a name or currency code does not change the row ID or any referencing
foreign key. The new value is displayed immediately wherever the catalog is
read; Segaris does not preserve historical catalog names or codes in business
records.

Concurrent ordinary edits follow the platform last-write-wins convention.

## Deletion Impact And Privacy

Deleting a row starts by evaluating its current deletion impact.

If no business record references the row, the administrator receives a simple
irreversible-deletion confirmation and may delete it directly.

If references exist, the interface offers a single replace-and-delete
operation. It does not expose record titles, owners, counts, per-module totals,
or other information that could reveal private records. It states only that the
value is in use and requires a migration.

Deletion-impact results are advisory. The backend re-evaluates the source,
replacement, and current references when the administrator confirms. A direct
delete fails rather than cascading when a concurrent reference has appeared.

## Reference Migration

Reference migration and source deletion form one atomic operation:

1. Validate the source and requested replacement behavior.
2. Re-evaluate all current references.
3. Ask every registered consumer to migrate its references.
4. Update affected entities' `UpdatedAt` and `UpdatedBy` metadata.
5. Delete the source row.
6. Commit the transaction.

If any consumer or validation step fails, no reference, amount, audit field, or
catalog row is changed.

The operation includes public and private records. Administrators may perform
this structural maintenance without gaining read access to private records.

The replacement rules are:

- Supplier: replace with another supplier or clear the optional reference to
  `null`.
- Cost center: replace with another cost center or clear the optional reference
  to `null`.
- Currency: replace with another currency and convert monetary values.
- Capex category: replace with another Capex category.

A replacement must belong to the same catalog, exist when the transaction is
executed, and differ from the source. Clearing is valid only for catalogs whose
consumer references are optional.

## Required And Optional Catalogs

Currency and Capex category are required by Capex entries and may not be left
empty. Their final remaining row cannot be deleted, even when it has no current
references.

Supplier and cost center are optional and may be left empty. An empty,
previously initialized optional catalog remains empty after application restart.

## Currency Conversion

Deleting a referenced currency requires another currency and a manually entered
exchange rate. The fixed interpretation is:

`1 source currency = exchange rate target currency`

The exchange rate must be positive and may contain up to eight decimal places.
The confirmation dialog displays the formula explicitly. No external exchange
rate provider, automatic lookup, rate history, or reversal workflow is included.

For Capex entries, conversion keeps item quantities unchanged, multiplies each
item `UnitAmount` by the exchange rate, rounds the resulting unit amount to two
decimal places using `MidpointRounding.AwayFromZero`, recalculates each
`LineAmount`, recalculates `TotalAmount`, changes `CurrencyId`, and updates audit
metadata. The server remains authoritative for all calculations.

Currency conversion may be delivered in an advanced implementation wave. Until
that wave is complete, referenced currencies may be edited and reordered but
not deleted.

## Concurrent Structural Operations

Move operations use the order that exists when their transaction begins.
Deletion and migration always use current references rather than trusting an
earlier impact response. References created before the confirming transaction
evaluates consumers are included in the migration.

If the source or replacement disappeared, the replacement became invalid, or a
consumer cannot complete its migration, the operation returns a conflict or
domain error and rolls back in full. Segaris does not add real-time catalog
synchronization between administrator sessions.

## Open Forms And Cache Refresh

After a catalog mutation, the frontend invalidates the affected catalog and the
known consumer queries. An already open business form is not silently rewritten.

When that form is saved, the backend validates catalog references again. If its
selected value was deleted, the form remains open and asks the user to choose a
valid value. Existing records migrated by an administrator display their new
live catalog value the next time they are queried.

## Feedback And Accessibility

Creation, editing, deletion, and migration produce success or failure toast
feedback. Move success remains quiet. Destructive actions require explicit
confirmation and disable duplicate submission while in progress.

Tables, tabs, dialogs, selectors, and movement controls follow the shared
Segaris accessibility and focus-management conventions. Move actions remain
usable by keyboard and touch.

## Excluded Scope

The initial Configuration implementation excludes:

- Activation or deactivation.
- Search, pagination, bulk selection, and bulk editing.
- Drag-and-drop ordering.
- Import, export, or restoration of default values.
- Historical display-name snapshots.
- A catalog-change or migration history beyond standard audit metadata.
- A Configuration launcher attention indicator.
- Real-time synchronization between administrators.
- External exchange-rate services.
- Spanish translations.

## Acceptance Criteria

The initial Configuration definition is satisfied when:

1. Only administrators can see and open Configuration or invoke its management
   APIs, while authenticated users can still read catalogs required by business
   forms.
2. Configuration provides flat Global and Capex sections, Global catalog tabs,
   URL-backed selection, and safe fallback to Global Suppliers.
3. Administrators can create and rename all four catalog types, edit currency
   codes, and see live names without changing referenced integer IDs.
4. Names and currency codes enforce the defined case-insensitive uniqueness,
   trimming, length, and format rules on both supported database providers.
5. Administrators can move rows up or down; catalog reads and form defaults use
   `SortOrder` then `Id`, and new rows start last.
6. Existing values and IDs survive schema migration, non-currency technical
   codes are removed, and one-time initialization never restores a customized
   or deliberately empty catalog.
7. Unreferenced rows can be physically deleted after confirmation, while the
   last currency or Capex category cannot be removed.
8. Referenced suppliers and cost centers can be atomically replaced or cleared
   to `null`; referenced Capex categories require another category.
9. Referenced currencies can be atomically replaced using an explicit exchange
   rate, with authoritative two-decimal Capex recalculation.
10. Migrations include private records without exposing their identity or
    count, and update modification metadata with the acting administrator.
11. Concurrent references are re-evaluated, direct deletion never cascades, and
    any migration failure rolls the complete transaction back.
12. Popup forms, ordering controls, confirmations, retry states, focus handling,
    and feedback satisfy the documented workflow without reloading the app.
13. SQLite and PostgreSQL migrations, backend tests, frontend component tests,
    architecture tests, and an administrator Playwright journey verify the
    supported behavior.

