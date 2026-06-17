# Travel Requirements

## Status

Phase 2 functional definition is complete. This document is the functional
source of truth for the initial Travel implementation plan.

## Purpose

Travel manages household trips and the expenses incurred during them, for both
holidays and work. A trip groups a light itinerary, the relevant booking
locators, and the individual expenses such as flights, lodging, or taxis.

The initial module is intentionally focused. It centres on organising trips,
recording a simple per-trip itinerary, and tracking the expenses associated with
each trip. It does not model structured bookings as entities, packing lists,
travellers per household member, budgets, or external calendar synchronisation.

## Initial Scope

- Manage trips with a type, destination, civil start and end dates, a status,
  notes, visibility, and attachments.
- Record a light, embedded itinerary of ordered entries, each able to carry its
  own reservation locator.
- Track individual travel expenses as a managed sub-resource of a trip, each with
  its own category, date, amount, currency, optional supplier, optional cost
  centre, notes, and attachments.
- Show per-trip expense totals grouped by currency, without automatic conversion.
- Organise trips and expenses through Travel-owned catalogs and the shared
  Configuration catalogs.
- Keep Travel independent from Capex, Opex, Inventory, Assets, and other business
  modules in the initial release.

## Excluded Scope

The initial Travel implementation excludes:

- Bookings as first-class entities; a reservation is only a text locator on an
  itinerary entry.
- Packing lists or checklists.
- Per-household-member travellers or participant tracking.
- Trip budgets and over-budget alerts.
- Automatic currency conversion or a single normalised trip total.
- External calendar synchronisation (Google, Outlook, CalDAV) or any outbound
  integration. Trip and itinerary civil dates are stored so a future internal
  calendar may read them.
- Cross-module links to Capex, Opex, Inventory, Archive, Assets, or Maintenance.
- Spanish translations.

## Trip Model

A trip contains at least:

- A required name.
- A trip type from the Travel-owned `TripType` catalog.
- An optional destination text.
- A required civil start date.
- A required civil end date.
- A trip status.
- Optional notes.
- Visibility.
- Attachments.
- A light itinerary of ordered entries.
- A managed collection of expenses.

The trip is the only top-level Travel entity. Itinerary entries are embedded in
the trip; expenses are a managed sub-resource of the trip.

## Trip Status

Every trip has one of these fixed statuses:

- `Planned`
- `Ongoing`
- `Completed`
- `Cancelled`

Statuses are fixed domain values and are not managed through Configuration. The
status is chosen manually by the user; it is not derived from the trip dates and
has no side effects on expenses, itinerary, or stock of any kind.

Launcher attention combines the status with the trip dates (see Attention).

## Trip Dates

Trips store two required civil dates:

- `StartDate`
- `EndDate`

`EndDate` must be greater than or equal to `StartDate`. A one-day trip has the
same start and end date. There is no separate time-of-day on the trip itself;
time-of-day belongs to individual itinerary entries.

## Itinerary

Each trip carries a light, embedded itinerary: an ordered list of entries that
describe the plan and hold reservation locators. The itinerary is edited as part
of the trip through full-collection replacement, in the same way order lines are
replaced in other modules. Itinerary entries are not an independent REST
resource and do not have their own attachments.

Each itinerary entry contains:

- A required civil date.
- An optional time-of-day.
- A required title.
- An optional place text.
- An optional reservation locator text (for example a flight or hotel booking
  reference).
- An optional note.

A trip contains between 0 and 100 itinerary entries. Entries are ordered by date,
then by time-of-day when present, then by a stable insertion order so that
entries sharing a date and time keep a deterministic sequence.

Itinerary entries inherit the visibility and authorization of their parent trip.

## Expense Model

Travel expenses are a managed sub-resource of a trip. Unlike the itinerary, each
expense is created, edited, and deleted individually and may carry its own
attachments.

Each expense contains at least:

- A required expense category from the Travel-owned `TravelExpenseCategory`
  catalog.
- A required description.
- A required civil date.
- A required amount.
- A required currency from the shared Configuration catalog.
- An optional supplier from the shared Configuration catalog.
- An optional cost centre from the shared Configuration catalog.
- Optional notes.
- Attachments.

A trip contains between 0 and any number of expenses; a trip with no expenses is
valid. Expenses inherit the visibility and authorization of their parent trip.

## Expense Currency And Totals

Currency belongs to each expense, not to the trip. A single trip may contain
expenses in several currencies, which is the common case for foreign travel.

The trip detail presents expense totals grouped by currency. There is no
automatic conversion between currencies and no single normalised trip total in
the initial implementation. Each currency subtotal is the sum of the expense
amounts recorded in that currency.

## Suppliers, Currencies, And Cost Centres

Travel consumes the shared Configuration catalogs:

- Supplier
- Currency
- CostCenter

Currency is required on every expense. Supplier and cost centre are optional.

Because supplier and cost centre are optional, deleting a referenced supplier or
cost centre through Configuration may either replace the reference or clear it.
Because currency is required, a referenced currency may only be replaced, and the
replacement converts the affected expense amounts using the authoritative manual
exchange rate, matching the established Capex and Inventory currency behavior.

## Categories And Trip Types

Travel owns two module-specific catalogs:

- `TripType`
- `TravelExpenseCategory`

Both are presented through Configuration and follow the established module-owned
catalog behavior:

- Administrator CRUD.
- Explicit ordering.
- Deletion-impact checks.
- Atomic replacement before deleting a referenced value.
- Final-row protection: the last remaining value cannot be deleted.
- Privacy-neutral impact reporting.

### Initial Trip Types

The initial ordered trip-type values are:

- `Regional`
- `National`
- `European`
- `Non-Schengen`

### Initial Expense Categories

The initial ordered expense-category values are:

- `Flight`
- `Lodging`
- `Ground transport`
- `Meals`
- `Activities`
- `Other`

The one-time initialization behavior matches the established Configuration, Opex,
and Inventory catalog pattern: values are initialized once and are not reimposed
after administrative changes.

## Visibility And Authorization

Every trip uses the platform-standard visibility values:

- `Public`
- `Private`

New trips default to `Public`.

These rules apply:

- A user can view and edit their own trips and public trips.
- A private trip remains creator-only, including from administrators.
- Public collaboration follows the standard Segaris rule: any authenticated user
  may edit a public record.

Itinerary entries and expenses do not carry their own visibility. They inherit
the visibility and authorization of the parent trip. A user who may access a trip
may access its itinerary and all of its expenses; a user who may not access a
trip cannot access any of its itinerary or expenses, and the backend returns the
platform not-found behavior so private trips are not disclosed.

## Attachments

- Trips may contain multiple attachments.
- Expenses may contain multiple attachments.
- Itinerary entries do not have their own attachments in the initial version.
- Attachments use the shared platform attachment policies and authorization
  model.
- Any user who may access the owning trip may view, add, and remove its
  attachments and the attachments of its expenses.

Attachments inherit the visibility and authorization of their owning trip.

## Deletion

Deletion is physical, immediate, and irreversible.

### Trip Deletion

Deleting a trip deletes the trip together with its itinerary entries, all of its
expenses, and every attachment owned by the trip and its expenses, in one
operation. Travel has no external references into other business modules, so a
trip can always be deleted.

### Expense Deletion

An expense may be deleted individually. Deleting an expense removes it and its
attachments and updates the trip's currency subtotals. It does not affect any
other entity.

### Itinerary Deletion

Itinerary entries are removed through full-collection replacement while editing
the trip; they have no individual delete operation.

## Validation

### Trip Validation

- Name is required, trimmed, not whitespace-only, and at most 200 characters.
- Trip type reference is required and valid.
- Destination is optional and at most 200 characters.
- `StartDate` and `EndDate` are required civil dates, and `EndDate` is greater
  than or equal to `StartDate`.
- Status and visibility are known values.
- Notes are optional and at most 4,000 characters.
- The itinerary contains between 0 and 100 entries.

### Itinerary Entry Validation

- Date is a required civil date.
- Time-of-day is optional.
- Title is required, trimmed, not whitespace-only, and at most 200 characters.
- Place is optional and at most 200 characters.
- Reservation locator is optional and at most 200 characters.
- Note is optional and at most 1,000 characters.

### Expense Validation

- Category reference is required and valid.
- Description is required, trimmed, not whitespace-only, and at most 200
  characters.
- Date is a required civil date with no artificial boundary relative to the trip
  dates.
- Amount is zero or greater and has at most two decimal places.
- Currency reference is required and valid.
- Supplier reference, when present, must be valid.
- Cost centre reference, when present, must be valid.
- Notes are optional and at most 4,000 characters.

## Creation Defaults

### New Trip

A new trip starts with:

- Status `Planned`.
- Visibility `Public`.
- The first available trip type by `SortOrder`, then `Id`.
- `StartDate` equal to today in `Europe/Madrid`.
- `EndDate` equal to `StartDate`.
- No destination.
- No notes.
- An empty itinerary.
- No expenses.

### New Itinerary Entry

A new itinerary entry starts with:

- Date equal to the trip `StartDate`.
- No time-of-day.
- An empty title until the user supplies one.
- No place, locator, or note.

### New Expense

A new expense starts with:

- The first available expense category by `SortOrder`, then `Id`.
- Date equal to today in `Europe/Madrid`.
- Amount equal to `0`.
- No supplier and no cost centre.
- No notes.
- Currency selected by the user before save.

## Module Entry And Navigation

Opening Travel takes the user directly to the trips view. Travel does not have an
initial overview or dashboard.

The module exposes one primary workflow: browse and maintain trips. Each trip is
opened in a dialog that manages its details, its embedded itinerary, and its
expenses, including per-currency totals.

Creating, viewing, and editing trips happens in popup dialogs over the trips list
view, following the established Segaris URL-aware dialog pattern. Expenses are
created and edited within the context of their parent trip.

## Trips View

The primary trips view is a server-paginated table. It includes at least these
columns:

- Name.
- Trip type.
- Destination.
- Start date.
- End date.
- Status.
- Visibility.

The default ordering is start date descending, then identifier descending.

The table supports:

- Partial search across name, destination, and notes.
- Exact filters for trip type, status, visibility, and creator.
- User-controlled sorting and bounded pagination following platform conventions.

Search, key filters, sort, page, and page size should be URL-backed where
practical.

## Trip Detail

The trip dialog presents:

- The editable trip details.
- The embedded itinerary editor with ordered entries and reservation locators.
- The managed expense list with per-expense add, edit, and delete, plus
  per-currency totals.
- Trip attachments and per-expense attachments.

List state of the trips table must survive opening and closing the trip dialog
without a reload, following the Capex, Opex, and Inventory pattern.

## Attention

The Travel launcher card requires attention when at least one accessible trip
satisfies either condition:

- Status is `Ongoing`.
- Status is `Planned` and `StartDate` falls within the next seven days,
  inclusive, in `Europe/Madrid`.

Trips with status `Completed` or `Cancelled` never activate attention. Only
accessible trips count for the current user.

The launcher exposes only the platform-standard boolean attention state.

## Acceptance Criteria

The initial Travel definition is satisfied when:

1. Authenticated users can create, query, edit, and irreversibly delete visible
   trips with the documented fields, defaults, validation, and privacy rules.
2. Trips carry a fixed manual status `Planned`, `Ongoing`, `Completed`, or
   `Cancelled`, with required civil start and end dates where the end is not
   before the start.
3. Each trip carries a light embedded itinerary of up to 100 ordered entries,
   each able to hold a reservation locator, edited through full-collection
   replacement.
4. Expenses are a managed per-trip sub-resource with individual create, edit,
   delete, and attachments, and a trip may hold expenses in several currencies.
5. Trip detail reports expense totals grouped by currency without automatic
   conversion or a single normalised total.
6. Currency is required on every expense, while supplier and cost centre are
   optional references from the shared Configuration catalogs.
7. Itinerary entries and expenses inherit the visibility and authorization of
   their parent trip, and private trips are never disclosed through not-found
   behavior.
8. Public collaboration and private isolation follow the Segaris visibility
   baseline at the trip level.
9. Deleting a trip removes its itinerary, expenses, and all owned attachments in
   one operation, and deleting an expense updates the per-currency totals.
10. Travel-owned `TripType` and `TravelExpenseCategory` catalogs are initialized
    once and managed through Configuration with CRUD, reorder, final-row
    protection, and atomic reference migration before deletion.
11. Shared Supplier, Currency, and CostCenter catalogs come from Configuration
    through published contracts, with required-currency replacement converting
    affected expense amounts and optional supplier/cost-centre references either
    replaced or cleared.
12. Travel attention is true exactly when the current user can access at least
    one trip that is `Ongoing` or `Planned` with a start date within the next
    seven days in `Europe/Madrid`.
13. SQLite and PostgreSQL migrations, backend unit/integration/architecture
    tests, frontend component tests, and a representative Playwright journey
    verify the supported behavior and privacy boundaries.

## Deferred Decisions

- Whether future versions should model bookings as first-class entities instead
  of a text locator.
- Whether trips should later gain budgets and over-budget attention.
- Whether expense amounts should be normalised to a single trip currency through
  automatic conversion.
- Whether packing lists, checklists, or per-household-member travellers should be
  introduced.
- Whether a future internal calendar will read Travel trip and itinerary dates,
  and whether external calendar synchronisation should ever be supported.
- Whether future Analytics will consume Travel read contracts.
