# Opex Requirements

## Status

Phase 2 functional definition is complete. This document is the functional
source of truth for the initial Opex implementation plan.

## Purpose

Opex records recurrent income and expenses through contracts. A contract groups
related effective movements, called occurrences, such as subscription charges,
utility bills, payroll payments, insurance premiums, or other continuing
arrangements.

Recurrence is descriptive rather than prescriptive. Opex does not generate
occurrences automatically or require them to follow a schedule. Users create
effective occurrences manually whenever appropriate.

## Initial Scope

- Manage income and expense contracts.
- Record manually created effective occurrences inside a contract.
- Preserve a simple estimated annual amount for future analysis.
- Show the realized amount for the current calendar year on each contract.
- Support contract-level and occurrence-level attachments.
- Keep Opex independent from other business modules in the initial release.

## Contract Lifecycle

Every contract has one of these fixed Opex statuses:

- `Planning`
- `Active`
- `OnHold`
- `Closed`

Statuses are fixed domain values and are not managed through Configuration.
They remain independent from contract dates. Changing a status does not set or
clear a date, and setting a date does not change the status.

Contracts remain fully editable in every status. The initial implementation
does not automatically synchronize statuses and dates or perform lifecycle
transitions.

## Movement Type

Every contract is either:

- `Income`
- `Expense`

Movement type belongs to the contract. Every occurrence inherits the current
movement type of its parent contract and does not persist an independent type.

Opex does not model refunds, reversals, corrections, or compensating movements
as explicit concepts. Users decide how to represent those cases while all
entered monetary values remain zero or greater.

## Contract Properties

A contract contains at least:

- A required globally unique name.
- Movement type.
- Lifecycle status.
- Optional start date.
- Optional closed date.
- Optional estimated annual amount.
- Optional expected frequency.
- Opex category.
- Supplier.
- Cost center.
- Currency.
- Notes.
- Visibility.
- Attachments.
- Zero or more occurrences.

The name is trimmed and compared case-insensitively for global uniqueness.
Capitalization entered by the user is preserved for display, and internal
spaces remain significant. For example, `Netflix`, ` netflix `, and `NETFLIX`
conflict, while `Netflix Premium` is distinct.

Global uniqueness intentionally applies across public and private contracts.
A duplicate-name error may therefore reveal only that the name is already in
use. It must not reveal the existing contract identifier, owner, visibility, or
other details.

Start and closed dates are optional civil dates. They do not constrain each
other, lifecycle status, or occurrence dates.

## Expected Frequency

Expected frequency is optional, informational contract metadata. Supported
values are:

- `None`
- `Weekly`
- `Monthly`
- `Quarterly`
- `SemiAnnual`
- `Annual`
- `Irregular`

Frequency does not generate occurrences, validate occurrence dates, activate
attention, or impose any other behavior. It is a fixed Opex value rather than
an administrator-managed catalog.

## Estimated Annual Amount

`EstimatedAnnualAmount` is optional and, when supplied, is zero or greater. It
always describes an estimated complete calendar year, regardless of contract
creation, start, closed, or status dates.

The value is not prorated and does not produce occurrences. It is displayed as
contract information and reserved for future Analytics use. Effective income
or expense is derived exclusively from occurrences.

## Occurrences

An occurrence represents an effective income or expense movement. It always
belongs to exactly one contract and contains:

- A required effective date.
- A required actual amount.
- An optional description.
- Optional notes.
- Attachments.
- Standard creation and modification metadata.

Occurrences have no lifecycle status, expected schedule, independent movement
type, currency, category, supplier, cost center, or visibility. They always use
the current properties and authorization of their parent contract.

No historical snapshots are stored. Editing a contract's classification,
movement type, currency, or other inherited property reinterprets its existing
occurrences. Deleting a contract removes its complete occurrence history.

Contracts may contain no occurrences. Occurrences may use any past or future
date and are not required to fall between the contract's optional dates.

Occurrences remain fully editable. Deletion is physical, immediate, and
irreversible after explicit confirmation.

## Classification Configuration

Opex owns a dedicated `OpexCategory` classification. Configuration presents it
through an Opex section without taking ownership of its persistence, validation,
or mutation behavior.

Administrators can create, rename, reorder, and delete Opex categories under
the same conventions as Capex categories. A referenced category requires an
atomic replacement before deletion, and the final remaining category cannot be
deleted.

Supplier, cost center, and currency remain shared catalogs owned by
Configuration. Opex consumes their published read, validation, and reference
management contracts through stable integer identifiers.

The first available category and currency by `SortOrder`, then `Id`, are the
defaults for a newly opened contract form.

## Initial Opex Categories

Opex categories use the Configuration one-time initialization behavior. The
initial ordered values are:

- `Housing`
- `Utilities`
- `Telecommunications`
- `Subscriptions`
- `Insurance`
- `Taxes & Fees`
- `Health`
- `Education`
- `Transport`
- `Employment`
- `Professional Services`
- `Financial Services`
- `Memberships`
- `Other`

Once initialized, administrator customization or deliberate emptying is never
reversed by application startup. The catalog cannot normally become empty
because its final row is protected from deletion.

## Catalog Reference Migration

Opex participates in Configuration deletion-impact and migration operations
without exposing public or private contract details or reference counts.

The replacement rules are:

- Opex category: replace with another Opex category.
- Supplier: replace with another supplier or clear the optional reference.
- Cost center: replace with another cost center or clear the optional reference.
- Currency: replace with another currency and convert monetary values.

Currency conversion uses the existing Configuration formula:

`1 source currency = exchange rate target currency`

The exchange rate is positive and supports up to eight decimal places. For each
affected Opex contract, the operation:

1. Converts `EstimatedAnnualAmount` when it has a value; `null` remains `null`.
2. Converts every occurrence amount independently.
3. Rounds each converted value to two decimal places using
   `MidpointRounding.AwayFromZero`.
4. Changes the contract currency reference.
5. Updates `UpdatedAt` and `UpdatedBy` on the contract and every occurrence,
   using the acting administrator.

Migration includes private contracts without granting the administrator read
access. Source, replacement, and references are re-evaluated in the confirming
transaction. Conversion, reference migration, audit updates, and source
deletion are atomic; any failure rolls back the complete operation.

## Amount Rules

- Users never enter negative monetary values.
- `Income` or `Expense` determines direction.
- Estimated and actual amounts may be zero.
- User-entered amounts support at most two decimal places.
- Currency belongs to the contract and applies to its estimated amount and all
  occurrences.
- The server is authoritative for validation and currency conversion.

## Visibility And Authorization

Every contract uses the platform-standard visibility values:

- `Public`: visible to every authenticated household user.
- `Private`: visible only to its creator, including from administrators.

New contracts default to `Public`. Any authenticated user may edit or
permanently delete a public contract and manage its occurrences and attachments.
Only the contract creator may change visibility. A private contract and all its
occurrences and attachments remain inaccessible to every other user.

Occurrence creator information is audit metadata only and does not introduce
separate authorization. Access always follows the parent contract.

## Attachments

- A contract may contain multiple attachments for general documentation.
- An occurrence may contain multiple attachments for its concrete invoice,
  receipt, payslip, or other evidence.
- Attachments use the platform size, validation, storage, retrieval, and
  authorization policies.
- Every user with access to the contract may view, add, and remove attachments
  at either level.
- Deleting an occurrence immediately deletes its attachments.
- Deleting a contract immediately deletes its occurrences and all attachments
  at both levels through the platform compensating storage operations.

During creation, the contract or occurrence is persisted before its attachments
are uploaded. Partial upload failure does not roll back the entity or files
uploaded successfully; failed files remain identifiable and retryable.

## Deletion

Contract and occurrence deletion is physical, immediate, and irreversible.
There is no trash, archive, soft deletion, or restoration workflow.

Deleting a contract cascades to all occurrences and attachments. The interface
warns clearly about this impact but does not need to expose an occurrence count.
Any user who may edit the contract may delete it after explicit confirmation.

## Validation

Contract validation includes:

- Name is required, trimmed, not whitespace-only, at most 200 characters, and
  globally unique after case-insensitive comparison.
- Category and currency references are required and valid.
- Supplier and cost-center references are optional and valid when supplied.
- Estimated annual amount is optional, zero or greater, and has at most two
  decimal places.
- Start and closed dates are optional and have no artificial boundary or
  relationship validation.
- Notes are optional and at most 4,000 characters.
- Movement type, status, frequency, and visibility are known values.

Occurrence validation includes:

- Effective date is required and has no artificial past or future boundary.
- Actual amount is required, zero or greater, and has at most two decimal
  places.
- Description is optional, trimmed when supplied, and at most 300 characters.
- Notes are optional and at most 4,000 characters.

## Creation Defaults

A new contract starts with:

- Movement type `Expense`.
- Status `Planning`.
- Expected frequency `None`.
- Visibility `Public`.
- First available Opex category by `SortOrder`, then `Id`.
- First available currency by `SortOrder`, then `Id`.
- No start date, closed date, supplier, cost center, or estimated annual amount.

Name and notes remain empty until entered by the user.

A new occurrence starts with:

- Effective date equal to today in `Europe/Madrid`.
- Actual amount `0.00`.
- Empty description and notes.

## Module Entry And Navigation

Opening Opex takes the user directly to the Contracts view. Opex has no initial
overview, dashboard, charts, trends, global occurrence view, or launcher
attention state.

Creating a contract opens a popup dialog. Selecting an existing contract opens
the same editor through URL state such as a `contractId` query parameter.
Closing it returns to the same contract table state without a full-page reload.

The contract dialog is a large editing surface with:

- `Details`: contract fields, notes, and attachments.
- `Occurrences`: the contract's occurrence table and occurrence actions.

Occurrences are consulted and managed only after opening their parent contract.
They have no independent module route or cross-contract search experience.

## Contracts View

The primary view is a database-paginated table containing at least:

- Name.
- Movement type.
- Status.
- Category.
- Supplier.
- Expected frequency.
- Estimated annual amount.
- Realized amount for the current year.
- Currency.

The default ordering is name ascending, then identifier ascending. Supported
sorting includes name, movement type, status, category, supplier, expected
frequency, estimated annual amount, realized current-year amount, and currency.
Every ordering uses the identifier as a deterministic tie-breaker.

The table supports:

- Partial search across contract name and notes.
- Exact filters for movement type, status, category, supplier, cost center,
  currency, expected frequency, visibility, and creator.
- Bounded pagination with 25 rows by default and selectable sizes of 10, 25,
  50, and 100.

Search, movement type, and status remain readily visible. Less common filters
may use the shared expanded filter treatment. Search, filters, sorting, and
pagination are represented in URL state and preserved while opening and closing
a contract.

Filtering contracts by whether they have occurrences in the current year is a
future improvement and is not part of the initial API.

## Realized Current-Year Amount

The contracts table shows the sum of occurrences whose effective dates fall
within the current natural calendar year, from January 1 through December 31
inclusive. The current year is evaluated in `Europe/Madrid`.

The calculation:

- Includes contracts of every status and regardless of contract dates.
- Returns `0.00` for contracts without qualifying occurrences.
- Uses database-level aggregation inside the paginated query.
- Does not prorate, compare with `EstimatedAnnualAmount`, or combine currencies.

Because every contract has one currency, its realized amount has one unambiguous
currency. Cross-contract or cross-currency aggregation belongs to Analytics.

## Contract Editor

The editor is directly editable whenever the user has access. It uses explicit
save actions and no autosave.

Saving validates the complete contract. Pending submissions disable duplicate
actions. Validation and server errors keep the editor open and preserve input.
Successful creation or update refreshes the contracts query and current-year
totals while preserving table state.

Closing a dirty editor requires confirmation. The Escape key and outside click
must not silently discard changes. Concurrent edits follow the platform
last-write-wins convention.

## Occurrences View And Editor

The Occurrences tab contains a paginated table showing at least:

- Effective date.
- Optional description.
- Actual amount.
- Actions.

The default ordering is effective date descending, then identifier descending.
The table uses 25 rows by default with selectable sizes of 10, 25, 50, and 100.
It has no initial filters or search.

Creating or editing an occurrence uses a secondary popup over the contract
editor. It uses explicit save, preserves input after failure, prevents duplicate
submission, and confirms discarded dirty changes. There is no inline or bulk
editing.

Successful occurrence creation, update, or deletion refreshes its table and the
parent contract's realized current-year amount without closing the contract.

## Module Independence

The initial Opex implementation has no references to Capex, Travel, Inventory,
Assets, or other business-module records. Similar financial concepts do not
create a write dependency between modules.

Analytics may later consume a purpose-built Opex read contract, but that
technical contract is outside the initial implementation.

## Future Analytics Semantics

- Amounts are exposed as positive values alongside `Income` or `Expense`.
- `EstimatedAnnualAmount` describes a complete natural year and is never
  prorated.
- Realized values come exclusively from occurrences.
- Future aggregation may use year, movement type, category, supplier, cost
  center, and currency.
- Currencies remain separate unless Analytics performs an explicit conversion.
- Contracts of any status remain available for Analytics to filter.
- Current contract properties classify all historical occurrences because no
  snapshots are stored.
- Physical deletion removes the contract's historical contribution.

## Excluded Scope

The initial Opex implementation excludes:

- Automatic occurrence generation or recurring scheduling.
- Frequency enforcement or occurrence date validation against a schedule.
- Automatic synchronization of lifecycle status and dates.
- Planned occurrences or occurrence statuses.
- A global occurrence list, route, search, or cross-contract filter.
- Historical snapshots, revisions, or audit history beyond standard metadata.
- Explicit refund, reversal, or correction models.
- Negative monetary values.
- Proration, budget variance, estimation-versus-actual comparisons, summaries,
  dashboards, charts, or trends.
- Automatic exchange-rate retrieval or cross-currency aggregation.
- Import, export, contract duplication, and bulk editing.
- Launcher attention and Calendar integration.
- Optional future maintenance processes for contract lifecycle.
- Filtering contracts by whether they have current-year occurrences.
- Spanish translations.

## Acceptance Criteria

The initial Opex definition is satisfied when:

1. Authenticated users can create, query, edit, and irreversibly delete visible
   contracts with the documented fields, defaults, validation, and lifecycle.
2. Contract names are globally unique after trimming and case-insensitive
   comparison on SQLite and PostgreSQL without exposing private contract data
   beyond a generic duplicate-name outcome.
3. Contract movement type, expected frequency, dates, status, and estimated
   annual amount remain descriptive and mutable without generating occurrences
   or enforcing cross-field synchronization.
4. Users manage effective occurrences only inside an accessible parent contract;
   occurrences have the documented fields, validation, ordering, pagination,
   inherited properties, and no independent state or route.
5. The Contracts view provides the documented search, filters, sorting,
   pagination, URL preservation, and database-level current-year realized total.
6. Public and private authorization matches the platform visibility policy, and
   occurrences and attachments inherit access exclusively from their contract.
7. Contract and occurrence attachments support creation-time staging, partial
   upload failure, retry, retrieval, and removal using platform policies.
8. Deleting an occurrence removes its attachments, while deleting a contract
   removes all occurrences and attachments with explicit irreversible
   confirmation and storage consistency.
9. Administrators can manage Opex categories through Configuration, initial
   values are initialized exactly once, referenced deletion requires migration,
   and the final category cannot be removed.
10. Supplier, cost-center, category, and currency migrations include every
    public and private Opex reference without revealing record identity or
    counts and roll back completely on failure.
11. Currency migration converts every non-null annual estimate and occurrence
    amount with the documented formula and rounding, updates contract and
    occurrence modification metadata, and deletes the source atomically.
12. Popup editors, nested occurrence management, unsaved-change confirmation,
    focus behavior, loading, errors, and feedback operate without reloading the
    application or losing the contracts-table state.
13. SQLite and PostgreSQL migrations, backend unit/integration/architecture
    tests, frontend component tests, and a representative Playwright journey
    verify the supported behavior and privacy boundaries.

