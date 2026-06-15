# Capex Requirements

## Status

Phase 2 functional definition is complete. This document is the functional
source of truth for the initial Capex implementation plan.

## Purpose

Capex records atomic movements of money: income or expenses that make sense as
individual entries. Within Armali, the name `Capex` intentionally covers all
such non-recurring movements and is not limited to the narrower accounting
meaning of capital expenditure.

The users already understand this product-specific meaning. The normal user
interface does not need to explain or qualify the module name.

## Initial Scope

- Record atomic income and expenses.
- Allow entries dated in the past or future.
- Support lightweight planning through the entry date and lifecycle status.
- Allow an entry and all of its editable fields to change at any time.
- Optionally describe an entry through multiple item lines.
- Keep Capex independent from the other business modules in the initial
  implementation.

## Entry Lifecycle

Every Capex entry has one of these statuses:

- `Planning`
- `Completed`
- `Canceled`

Status and date are independent. A future entry may already be completed, and a
past entry may remain in planning until a user changes it. The system does not
automatically change status when the entry date arrives or passes.

These three statuses are fixed Capex domain values. They are not entries in an
administrator-managed catalog. Other modules may define different fixed status
sets appropriate to their own lifecycle.

Completed entries remain fully mutable. A user may edit any supported field,
including movement type, date, status, amount, and category, regardless of the
current or previous status.

## Movement Type And Corrections

Every entry is either:

- `Income`
- `Expense`

The initial module does not model refunds, reversals, corrections, or
compensating transactions as distinct domain concepts. Users may represent a
refund by creating a separate entry of the opposite type, or may remove the
original entry entirely.

## Item Granularity

The normal creation experience produces a simple Capex entry containing one
item. The interface may let the user enable item management to add and edit
multiple item lines.

Internally, every entry has at least one item. The distinction between a simple
entry and an itemized entry is initially a user-interface concern rather than a
separate persisted entry type.

The entry title and the item description are distinct backend properties. When
an entry contains exactly one item, the user interface may keep them
synchronized to simplify normal data entry.

Each item contains:

- A required description.
- A quantity, defaulting to `1`.
- A unit amount.
- A calculated line amount.

The simple-entry experience uses a quantity of `1`, allowing the user to enter
the line amount directly through the unit-amount field. Discounts, delivery
charges, taxes, surcharges, and similar adjustments have no dedicated model;
when they need to be represented separately, they are additional items.

Every entry contains at least one item. The entry amount is calculated from its
item lines rather than entered independently.

Items do not have their own notes in the initial version. Notes belong only to
the Capex entry.

## Entry Properties

A Capex entry contains at least:

- A required title.
- Movement type: `Income` or `Expense`.
- Lifecycle status.
- A date-only `DueDate`.
- Currency.
- Category.
- Supplier.
- Cost center.
- Notes.
- Visibility.
- Attachments.
- One or more items.

Category belongs to the entry rather than to individual items. Capex trends and
future Analytics projections classify the complete atomic movement, not the
products or services listed within it.

`DueDate` is the entry's only functional date. Depending on the entry's use, it
may represent either a planned date or the date attributed to an actual
movement. Completing an entry does not create or require a separate completion
or transaction date.

Currency also belongs to the entry, so every item uses the same currency. The
initial supported values are `EUR`, `USD`, and `GBP`, with `EUR` as the default.
Currency conversion and exchange-rate tracking are not included in the initial
scope.

Supplier is selected from a reusable catalog rather than entered as arbitrary
text. Cost center is also a catalog-backed string whose business meaning is
known by the users; it does not represent a payment method or bank account.

Category is required. In the accepted initial Capex release, currency defaults
to `EUR` and category defaults to `Other`. Once the separately planned
Configuration management behavior is implemented, each defaults to the first
available row by `SortOrder`, then `Id`. Supplier and cost center are optional.
The property remains named `Supplier` for both expenses and income, where it
identifies the source of the money.

## Classification Configuration

Capex category is a module-owned classification persisted through a dedicated
model such as `CapexCategory`.

Supplier, cost center, and currency are owned by the platform Configuration
module and shared by several business modules, especially Capex, Opex, and
Inventory. Their models and persisted names are domain-neutral, for example
`SegarisSupplier`, rather than owned or named as Capex classifications.

Catalog management is not part of the accepted initial Capex version. Its
administrator workflow is defined separately in
`CONFIGURATION_REQUIREMENTS.md`, including creation, rename, ordering,
reference migration, and deletion while preserving Capex ownership of
`CapexCategory`.

Configuration publishes read, validation, and narrow reference-management
contracts without exposing its EF Core entities to Capex. Stable integer
identifiers are used as references. Catalog management must migrate or, for an
optional property, clear references before a referenced row can be removed.

## Amount Rules

- Users never enter negative quantities or monetary values. `Income` or
  `Expense` determines the movement direction.
- Item quantity must be greater than zero because it represents the number of
  acquired or expected units.
- User-entered numeric values support at most two decimal places.
- Unit amount may be zero. Consequently, zero-value entries are allowed so that
  a planned movement with an unknown economic amount can be recorded.
- The entry total is calculated from its item lines and uses the entry currency.
- Currency conversion and exchange-rate tracking are outside the initial scope.

## Module Independence

The initial Capex implementation has no references to records owned by Travel,
Inventory, Assets, Opex, or other business modules. Cross-module links and
automatic creation behavior are deferred until the participating modules have
their own functional requirements.

Analytics may later consume a purpose-built Capex read contract as established
by the domain architecture, but its concrete projection is not part of the
current decisions.

## Visibility And Authorization

Every Capex entry has one of the platform-standard visibility values:

- `Public`: visible to every authenticated household user.
- `Private`: visible only to its creator, including from administrators.

New entries default to `Public`, while the creator may choose `Private` during
creation.

Any authenticated user may edit or permanently delete a public entry,
regardless of who created it. Creation and last-modification metadata preserve
the responsible users.

Only the creator may change an entry's visibility in either direction. In
particular, another user may not turn a public entry into a private entry and
thereby hide shared household information. A private entry remains inaccessible
to every other user and administrator.

These rules are expected to be the baseline for most business modules, although
each module remains responsible for explicitly adopting and enforcing them.

## Attachments

- An entry may contain multiple attachments.
- Items do not have their own attachments.
- Attachments use the platform file-size, type-validation, storage, and
  retrieval policies.
- Every user who may access an entry may view, add, and remove its attachments.
- Deleting an entry immediately deletes its attachment metadata and physical
  files using the platform's compensating storage operations.

Attachments inherit the entry's visibility and authorization. They do not
introduce an independent sharing model.

## Deletion

Deletion is physical, immediate, and irreversible in the live application.
There is no trash, archive, soft deletion, or entity restoration workflow.

Any user who may edit an entry may delete it. The interface requires explicit
confirmation before completing the destructive operation.

## Initial Catalog Availability

The initial Capex release contains static seeded values available globally to
all users and no catalog-management capabilities inside Capex. The later
Configuration implementation turns these into administrator-managed catalogs
without changing their integer references.

The initial seeded values are:

### Categories

- `Furniture`
- `Appliances`
- `Technology`
- `Home`
- `Food & Dining`
- `Leisure`
- `Health`
- `Transport`
- `Travel`
- `Education`
- `Gifts`
- `Taxes & Fees`
- `Salary & Income`
- `Other`

### Suppliers

- `Amazon`
- `IKEA`
- `Carrefour`
- `El Corte Inglés`
- `Leroy Merlin`
- `Other`

### Cost Centers

- `Household`
- `Personal`
- `Work`
- `Shared`
- `Other`

### Currencies

- `EUR`, used as the default.
- `USD`.
- `GBP`.

Catalog rows use stable identifiers. User-facing names are localizable and
must not serve as database identities or API references.

## Validation And Calculation

- Title is required, trimmed, not whitespace-only, and at most 200 characters.
- `DueDate` is required and has no artificial past or future boundary.
- Category and currency references are required and must identify seeded
  catalog values.
- Supplier and cost-center references are optional; when present, they must
  identify seeded catalog values.
- Notes are optional and at most 4,000 characters.
- Every entry contains between 1 and 100 items.
- Item description is required, trimmed, not whitespace-only, and at most 300
  characters.
- Item quantity is greater than zero and supports at most two decimal places.
- Item unit amount is zero or greater and supports at most two decimal places.
- Every calculated line amount is rounded to two decimal places using
  `MidpointRounding.AwayFromZero`.
- The entry total is the sum of the individually rounded line amounts and is
  represented with two decimal places.
- Negative values and unknown catalog identifiers are rejected as validation
  errors.

The server is authoritative for calculated line and entry totals. The client
may calculate previews but does not submit a trusted total.

## Module Entry And Navigation

Opening Capex takes the user directly to the Entries view. Capex does not have
an overview, dashboard, summary cards, charts, or trends screen.

Financial aggregation and trends belong to a future Analytics module that will
consume purpose-built read contracts from Capex and the other financial
modules.

## Entries View

The primary view is a paginated table of entries. It includes at least these
columns:

- Title.
- Movement type.
- Status.
- `DueDate`.
- Category.
- Supplier.
- Cost center.
- Total amount.
- Currency.

The default ordering is `DueDate` descending. The table supports:

- Partial text search across entry title, entry notes, and item descriptions.
- Date-period filtering.
- Exact filters for movement type, status, category, supplier, cost center,
  currency, visibility, and creator.
- User-controlled sorting and bounded pagination following the platform API
  conventions.

The initial view has no date-period restriction and queries all accessible
entries through database-level pagination. A large history increases the page
count but does not cause the client to load the complete result set.

The default page size is `25`. The user may select `10`, `25`, `50`, or `100`
rows per page.

Search, date period, movement type, and status should remain readily visible in
the filtering surface. Less frequently used filters may appear in a visually
consistent expanded or "more filters" area. Active filters should remain clear
and individually removable. The exact controls must follow the Segaris design
system rather than introducing a separate Capex visual language.

A dedicated "my entries" shortcut is optional. It may be included when it
materially improves usability without adding a separate query or navigation
model; the creator filter remains the underlying capability.

Amounts in different currencies are always displayed and summarized
separately. Capex never adds `EUR`, `USD`, and `GBP` values together. Currency
conversion and unified reporting are deferred.

## Entry Editor

Creating, viewing, and editing an entry happens in a popup dialog over the
Entries view rather than on a separate page. The dialog may be represented by
URL state such as an `entryId` query parameter so that an entry can be linked or
reopened through browser history. Closing it updates the client-side route and
returns to the same table page with pagination, filters, search, and sorting
preserved, without a full page refresh.

The dialog opens from at least:

- A `New entry` action.
- Selecting an existing table row.

The dialog is directly editable whenever the user has access under the entry's
authorization rules. There is no separate read-only detail mode in the initial
version.

The editor uses explicit save actions and no autosave. It is organized into
sections for:

- General entry data.
- Items.
- Notes.
- Attachments.

A simple entry initially presents one item through a reduced editing
experience. The user may enable item management and add further lines. Lines
may be removed while at least one item remains.

Saving validates the complete entry. While the request is in progress, actions
that could submit the same changes again are disabled.

After a successful create or update, the dialog closes, the current table query
is refreshed, and a success toast is shown. The table retains its previous
state. If an updated entry no longer matches the active filters or current page,
it naturally disappears from the displayed results.

Validation or server errors keep the dialog open and preserve the user's input.
Concurrent edits use the platform's last-write-wins convention; Capex does not
introduce version-conflict handling.

Closing a dirty editor requires confirmation before discarding changes. The
Escape key and an outside click must not silently lose edits. When the editor is
clean, its detailed keyboard and outside-click behavior may follow the shared
dialog conventions.

### Creation Defaults

A new entry starts with:

- Movement type `Expense`.
- Status `Planning`.
- `DueDate` equal to today in `Europe/Madrid`.
- Currency `EUR`.
- Visibility `Public`.
- Category `Other`.
- No supplier.
- No cost center.
- One item with quantity `1` and unit amount `0`.

`EUR` and `Other` describe the accepted initial Capex release. The Configuration
management plan supersedes those two fixed catalog defaults with the first
available row by `SortOrder`, then `Id`, after that plan is implemented.

The initial title and item description remain empty until supplied by the user.

### Item Ordering

Items have an explicit persisted position. The editor allows users to reorder
them, and queries return them in that order.

### Attachment Upload During Creation

The entry is created before its attachments are uploaded. Attachment uploads
then use the created entry identifier and the platform attachment operations.

If one or more uploads fail, the entry remains successfully created. The editor
identifies the failed files and allows the user to retry or close; a failed
upload does not roll back the entry or attachments that were uploaded
successfully.

The dialog must accommodate item management and attachments without becoming a
small confirmation-style modal. Its exact size, scrolling, focus management,
keyboard behavior, unsaved-change handling, and responsive behavior remain to
be defined with the workflow and design requirements.

## Optional Convenience Actions

Duplicating an entry and providing a dedicated "my entries" shortcut are
follow-up improvements outside the initial implementation scope.

If duplication is added later, its copy rules and initial status must be
defined before implementation; attachments should not be copied by default.

Import and export are outside the initial Capex scope. Their formats, privacy
behavior, and relationship to Analytics or App Configuration must be defined
before implementation.

## Catalog Queries

Capex exposes the read-only catalog data required by its forms and filters.
Categories, suppliers, cost centers, and currencies are available through
purpose-appropriate query endpoints or contracts. No catalog mutation endpoint
is included in the initial module.

## Sorting And Pagination Details

The Entries query supports sorting by:

- Title.
- Movement type.
- Status.
- `DueDate`.
- Category.
- Supplier.
- Cost center.
- Total amount.
- Currency.

Every selected ordering uses entry identifier descending as a deterministic
tie-breaker. The default ordering remains `DueDate` descending, then identifier
descending.

Paginated responses include items, current page, page size, and total result
count. After deletion or a filtering change, if the current page is beyond the
last available page, the client navigates to the last valid page and refreshes
the query without reloading the application.

## Due-Date Filtering And Calendar

Date-range filters operate on `DueDate`. Both `From` and `To` bounds are
optional and inclusive.

Capex does not initially publish events to a shared Calendar module. `DueDate`
is used for filtering, ordering, attention, and future financial analysis. A
calendar projection may be added only after the shared Calendar experience and
its contracts are defined.

## Launcher Attention

The Capex launcher card requires the current user's attention when at least one
entry accessible to that user meets both conditions:

- Status is `Planning`.
- `DueDate` is today or earlier.

Future planning entries do not activate attention. A qualifying zero-value
entry still activates it because its economic amount may remain unknown.

The launcher exposes only the platform-standard boolean attention state, not a
count or entry details. Attention clears when no accessible entry qualifies,
including after relevant entries are completed, canceled, deleted, moved to a
future date, or made inaccessible to the current user.

Because `DueDate` is a civil date, "today" is evaluated using the application's
`Europe/Madrid` household time zone rather than by converting the date through
UTC.

## Future Analytics Semantics

Capex itself does not calculate dashboard totals or trends. Its future
Analytics contract must preserve currency separation and expose enough status
information to distinguish `Planning` from `Completed` movements.

`Canceled` entries are excluded from economic results by default. Any future
analysis that includes them must do so explicitly rather than treating them as
normal planned or completed money movements.

## Acceptance Criteria

The initial Capex definition is satisfied when:

1. An authenticated user can open Capex directly on a server-paginated Entries
   table, with no date restriction and deterministic `DueDate` descending
   ordering.
2. Search, filters, sorting, page size, and page number are applied by the
   backend and remain intact while an entry dialog is opened and closed without
   a full page reload.
3. Users see every public entry and only their own private entries;
   administrators receive no privacy bypass.
4. Any user can edit or delete a public entry, while only the creator can change
   its visibility. Private entries remain creator-only for every operation.
5. A user can create and edit entries with the defined fields, fixed movement
   types and statuses, seeded catalogs, one or more ordered items, and optional
   notes and attachments.
6. The server rejects invalid or unknown values, calculates rounded line totals
   and the entry total authoritatively, and accepts a zero monetary total only
   through nonnegative unit amounts and strictly positive quantities.
7. A single-item entry uses the simplified editor, while item management can be
   enabled without changing the persisted entry type.
8. Dirty-dialog closure requires confirmation; failed validation, server
   requests, or attachment uploads preserve recoverable user input and do not
   silently discard a successfully created entry.
9. Successful mutations refresh the current table query, show transient
   feedback, and handle a now-invalid page or filtered-out row without reloading
   the application.
10. Entry deletion is explicitly confirmed, physically removes the entry and
    items, and immediately removes all associated attachment metadata and
    files.
11. The Configuration module supplies the shared seeded Supplier, CostCenter,
    and Currency catalogs through read-only contracts and APIs; Capex owns its
    separate seeded category catalog.
12. The launcher attention state is true exactly when the current user can see
    at least one `Planning` entry whose `DueDate` is today or earlier in
    `Europe/Madrid`.
13. SQLite and PostgreSQL migrations, API integration tests, frontend component
    tests, architecture tests, and an end-to-end Capex journey verify the
    behavior on the supported application stack.

## Deferred Decisions

- Define the Analytics read contract when Analytics is planned.
- Define import and export only if either capability enters a future scope.
- Implement the separately defined Configuration management plan; activation
  and deactivation are explicitly outside that plan.
- Define duplication and a dedicated "my entries" shortcut as optional follow-up
  improvements.
