# Inventory Requirements

## Status

Phase 2 functional definition is complete. This document is the functional
source of truth for the initial Inventory implementation plan.

## Purpose

Inventory manages stock-tracked household items and the supplier-specific
orders used to replenish them.

The initial module is intentionally simple. It focuses on the current stock of
consumable or replaceable items, low-stock visibility, and explicit reception
of supplier orders. It does not model lots, expiration dates, stock movements
as separate entities, or stock by location tuple.

## Initial Scope

- Manage stock-tracked items with a current stock and minimum stock threshold.
- Organize items through Inventory-owned categories and locations.
- Associate each item with one or more allowed suppliers from Configuration.
- Create and manage supplier-specific orders with ordered lines.
- Receive an active order through an explicit operation that updates stock.
- Support quick stock increase and decrease actions directly on an item.
- Keep Inventory independent from Capex, Opex, Travel, Assets, and other
  business modules in the initial release.

## Excluded Scope

The initial Inventory implementation excludes:

- Expiration dates and batch or lot tracking.
- Stock per location tuple or multi-location quantities for one item.
- Weight, volume, or unit conversion.
- Partial receipt of an order.
- A separate stock-movement history entity or audit timeline.
- Automatic reorder suggestions, forecasts, or recurring purchases.
- Cross-module links to Capex, Opex, Archive, Assets, or Maintenance.
- Spanish translations.

## Inventory Scope

Inventory is intended for stock-tracked consumable or replaceable items such as
food, cleaning products, hygiene products, medicine, office supplies, and pet
supplies.

It is not the initial home for durable assets, warranties, or long-term
document records. Those remain future concerns for other modules.

## Item Model

An inventory item contains at least:

- A required name.
- An optional description or notes field.
- An Inventory category.
- An Inventory location.
- An item status.
- Current stock.
- Minimum stock.
- One or more allowed suppliers.
- Visibility.
- Attachments.

The module uses a single conceptual unit for all items: `unit/package`. The
initial model does not distinguish pieces, packs, kilograms, liters, or other
measures.

## Item Status

Every Inventory item has one of these fixed statuses:

- `Candidate`
- `Active`
- `Deprecated`

The status is mostly descriptive. It does not block editing, ordering, quick
stock adjustment, or visibility changes by itself.

When a deprecated item is added to an order, the user interface should show a
warning indicator. The warning does not prevent the operation and does not
change backend validation.

Launcher attention uses only `Active` items.

## Stock Model

Each item stores:

- `CurrentStock`
- `MinimumStock`

Both values are authoritative properties of the item itself. Inventory does not
store stock changes in a separate movement table, and it does not split stock
across locations.

An item's location describes where the item is normally stored. It does not
participate in stock arithmetic.

## Stock Rules

- Users never enter negative stock quantities.
- `CurrentStock` is zero or greater.
- `MinimumStock` is zero or greater.
- User-entered stock quantities support at most two decimal places.
- Quick stock adjustments accept a positive quantity and either add it to or
  subtract it from `CurrentStock`.
- A stock reduction that would produce a negative result is rejected.

## Suppliers And Currencies

Inventory consumes the shared Configuration catalogs:

- Supplier
- Currency

An item may be associated with more than one supplier. This association defines
which suppliers are allowed for future order lines that reference the item.

Changing the allowed suppliers of an item affects future validation only. It
does not rewrite, invalidate, or migrate existing orders.

Currency belongs to an order, not to an item. Every order line amount is
interpreted in the currency selected on its parent order.

## Categories And Locations

Inventory owns two module-specific catalogs:

- `InventoryCategory`
- `InventoryLocation`

Both are presented through Configuration and follow the established module-owned
catalog behavior:

- Administrator CRUD.
- Explicit ordering.
- Deletion-impact checks.
- Atomic replacement before deleting a referenced value.
- Privacy-neutral impact reporting.

### Initial Categories

The initial ordered category values are:

- `Food`
- `Cleaning`
- `Hygiene`
- `Medicine`
- `Office`
- `Pets`
- `Other`

### Initial Locations

The initial ordered location values are:

- `Kitchen cabinet`
- `Pantry`
- `Bathroom`
- `Storage room`
- `Fridge`
- `Freezer`
- `Other`

The one-time initialization behavior matches the established Configuration and
Opex category pattern: values are initialized once and are not reimposed after
administrative changes.

## Orders

An order represents a supplier-specific replenishment attempt. Every order
contains:

- A required supplier.
- A required currency.
- A lifecycle status.
- An optional order date.
- An optional expected receipt date.
- Optional notes.
- Visibility.
- One or more order lines.

An order is associated with exactly one supplier, and all of its lines must
reference items that are allowed for that supplier.

### Order Status

Every order has one of these fixed statuses:

- `Planning`
- `Active`
- `Received`
- `Cancelled`

Statuses are fixed domain values and are not managed through Configuration.

Changing the order status manually does not trigger stock changes. Stock changes
occur only through the explicit receive operation.

Received orders remain protected against normal editing. A user may first move a
received order back to another status and then perform full edits.

### Order Dates

Orders store these functional dates:

- `OrderDate`
- `ExpectedReceiptDate`

Both are optional civil dates.

The default values for a new order are:

- `OrderDate`: today in `Europe/Madrid`
- `ExpectedReceiptDate`: today plus seven natural days in `Europe/Madrid`

There is no separate actual-received date field in the initial implementation.
If a user wants the expected date to reflect the final received date, they edit
the same field manually.

### Order Lines

Every order contains between 1 and 100 lines. Each line contains:

- A required item.
- A required quantity.
- A required total price.

The line price is the total price for the whole line, not a unit price.

Every line uses the order currency, and line items do not carry a per-line
currency field.

## Receiving Orders

Inventory exposes an explicit receive action for active orders.

Receiving an order:

1. Requires the order to be accessible and currently `Active`.
2. Runs in one transaction.
3. Changes the order status to `Received`.
4. Increases each referenced item's `CurrentStock` by the line quantity.
5. Updates standard modification metadata on the order and each changed item.

The initial implementation does not support partial receipt. The operation is
all-or-nothing.

Manually changing the status to `Received` does not execute this process.

## Quick Stock Adjustment

In addition to the full item editor, Inventory provides a small dedicated popup
for quick stock updates.

The popup supports:

- Increase stock by a positive quantity.
- Reduce stock by a positive quantity.

The operation updates only the item's stock and related modification metadata.
It does not create a separate stock-movement record.

## Visibility And Authorization

Every item and order uses the platform-standard visibility values:

- `Public`
- `Private`

New items and orders default to `Public`.

These rules apply:

- A user can view and edit their own items and public items.
- A private item remains creator-only, including from administrators.
- A user can view and edit their own orders and public orders.
- A private order remains creator-only, including from administrators.
- Public collaboration follows the standard Segaris rule: any authenticated user
  may edit a public record.

### Privacy Rules Between Items And Orders

- A public order may contain only public items.
- A private order may contain any item accessible to the editing user.
- A private order may be changed to public only if every line item is public.
- A public item cannot be changed to private if it appears in any public order.

These constraints are enforced by the backend regardless of the client.

## Attachments

- Items may contain multiple attachments.
- Orders may contain multiple attachments.
- Order lines do not have their own attachments in the initial version.
- Attachments use the shared platform attachment policies and authorization
  model.
- Any user who may access the owning record may view, add, and remove its
  attachments.

Attachments inherit the visibility and authorization of their owning item or
order.

## Deletion

Deletion is physical, immediate, and irreversible.

### Item Deletion

An item cannot be deleted if it appears in any order, regardless of the order's
visibility or status. In that situation the supported functional alternative is
to keep the item and, where appropriate, mark it as `Deprecated`.

### Order Deletion

Deleting an order does not modify stock or any other entity, even if the order
status is `Received`.

If a user needs to correct stock after deleting or editing a received order,
they use the quick stock adjustment flow.

## Validation

### Item Validation

- Name is required, trimmed, not whitespace-only, and at most 200 characters.
- Category and location references are required and valid.
- Status and visibility are known values.
- `CurrentStock` is zero or greater and has at most two decimal places.
- `MinimumStock` is zero or greater and has at most two decimal places.
- At least one allowed supplier is required.
- Every allowed supplier reference must be valid.
- Notes are optional and at most 4,000 characters.

### Order Validation

- Supplier and currency references are required and valid.
- Status and visibility are known values.
- `OrderDate` and `ExpectedReceiptDate` are optional and have no artificial
  boundary.
- Notes are optional and at most 4,000 characters.
- Every order contains between 1 and 100 lines.
- Each line item reference must be valid and accessible under the order
  visibility rules.
- Each line item must be associated with the order supplier.
- Line quantity is greater than zero and has at most two decimal places.
- Line total price is zero or greater and has at most two decimal places.
- A public order rejects private items.
- Turning a private order into a public order rejects any non-public line item.

## Creation Defaults

### New Item

A new item starts with:

- Status `Candidate`.
- Visibility `Public`.
- `CurrentStock` equal to `0`.
- `MinimumStock` equal to `0`.
- The first available category by `SortOrder`, then `Id`.
- The first available location by `SortOrder`, then `Id`.
- No notes.
- One or more suppliers selected by the user before save.

### New Order

A new order starts with:

- Status `Planning`.
- Visibility `Public`.
- `OrderDate` equal to today in `Europe/Madrid`.
- `ExpectedReceiptDate` equal to today plus seven natural days in
  `Europe/Madrid`.
- No notes.
- One empty line until the user supplies values.

## Module Entry And Navigation

Opening Inventory takes the user directly to the items view. Inventory does not
have an initial overview or dashboard.

The module exposes two primary workflows:

- Browse and maintain items, including quick stock adjustment.
- Browse and maintain orders, including explicit receipt.

Creating, viewing, and editing items and orders happens in popup dialogs over
their parent list views, following the established Segaris URL-aware dialog
pattern.

## Items View

The primary items view is a server-paginated table. It includes at least these
columns:

- Name.
- Status.
- Category.
- Location.
- Current stock.
- Minimum stock.
- Visibility.

The default ordering is name ascending, then identifier ascending.

The table supports:

- Partial search across name and notes.
- Exact filters for status, category, location, supplier, visibility, and
  creator.
- User-controlled sorting and bounded pagination following platform
  conventions.

Search, key filters, sort, page, and page size should be URL-backed where
practical.

## Orders View

The orders view is also a server-paginated table. It includes at least these
columns:

- Supplier.
- Status.
- Order date.
- Expected receipt date.
- Currency.
- Visibility.

The default ordering is `OrderDate` descending, then identifier descending.

The table supports:

- Partial search across notes and the names of referenced items.
- Exact filters for supplier, status, currency, visibility, and creator.
- User-controlled sorting and bounded pagination following platform
  conventions.

## Attention

The Inventory launcher card requires attention when at least one accessible
Inventory item satisfies both conditions:

- Status is `Active`.
- `CurrentStock` is less than or equal to `MinimumStock`.

Only accessible items count for the current user. `Candidate` and `Deprecated`
items do not activate attention.

The launcher exposes only the platform-standard boolean attention state.

## Acceptance Criteria

The initial Inventory definition is satisfied when:

1. Authenticated users can create, query, edit, and irreversibly delete visible
   items and orders with the documented fields, defaults, validation, and
   privacy rules.
2. Items store current stock, minimum stock, a single descriptive location, and
   one or more allowed suppliers without modeling lots, expirations, or
   stock-location tuples.
3. Item statuses `Candidate`, `Active`, and `Deprecated` are available, and
   deprecated items remain orderable while producing a warning in the user
   interface.
4. Public collaboration and private isolation follow the Segaris visibility
   baseline for both items and orders, including the special constraints between
   public orders and public items.
5. A public item cannot become private while it is referenced by any public
   order, and an item referenced by any order cannot be deleted.
6. Orders belong to exactly one supplier and one currency, and every order line
   references an item allowed for that supplier.
7. The explicit receive action changes an active order to received and updates
   item stock in one transaction, while manual status changes do not affect
   stock.
8. Received orders are protected from full editing until the user moves them
   back to another status.
9. Quick stock increase and decrease workflows update only the item stock and
   reject negative resulting stock.
10. Inventory-owned categories and locations are initialized once and are
    managed through Configuration with CRUD, reorder, and atomic reference
    migration before deletion.
11. Shared Supplier and Currency catalogs come from Configuration through
    published contracts rather than direct entity access.
12. Inventory attention is true exactly when the current user can access at
    least one active item whose current stock is less than or equal to its
    minimum stock.
13. SQLite and PostgreSQL migrations, backend unit/integration/architecture
    tests, frontend component tests, and a representative Playwright journey
    verify the supported behavior and privacy boundaries.

## Deferred Decisions

- Whether future versions should support partial receipt.
- Whether stock should later gain a movement history or audit timeline.
- Whether expiration dates, lots, or stock-by-location should be introduced.
- Whether future Analytics will consume Inventory read contracts.
