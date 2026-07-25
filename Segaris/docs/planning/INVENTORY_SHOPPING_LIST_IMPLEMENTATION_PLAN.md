# Inventory Shopping List Implementation Plan

## Purpose

This plan delivers the Inventory shopping list: a read-only, computed-on-demand
view of the items that need replenishing, opened from a button on the Orders
screen. It also corrects a defect in the Inventory launcher attention rule that
the same replenishment condition exposes.

The feature adds no entity, no column, and no persisted state. It is an
increment on the module delivered by `INVENTORY_IMPLEMENTATION_PLAN.md`, not a
new module.

## Accepted Functional Decisions

- The Orders tab of the Inventory page gains a **Shopping list** button. The
  button does not appear on the Items tab.
- The button opens a dialog that computes the list on demand from current stock.
  Nothing is persisted and nothing is scheduled.
- The list has two blocks: **Required**, for items whose `CurrentStock` is
  strictly below `MinimumStock`, and **Optional**, for items whose `CurrentStock`
  equals `MinimumStock`.
- Only `Active` items participate. `Candidate` and `Deprecated` items are never
  listed.
- Items whose `MinimumStock` is zero are excluded from both blocks. A zero
  minimum means the household does not track a replenishment threshold for that
  item, so it is never a purchase need.
- Within each block, items are grouped by category. Categories follow the
  catalogue `SortOrder`, categories with no items are omitted, and items inside a
  category are ordered alphabetically.
- Each listed item shows its name, the quantity required to reach the minimum
  stock, and its allowed suppliers on a separate line. The quantity is worded as
  "at least N units" and is **omitted entirely** in the Optional block.
- Suppliers are rendered as a single comma-separated line, ordered
  alphabetically.
- The list is global: it ignores every Items and Orders screen filter and offers
  no filters of its own.
- The list is not paginated. The dialog scrolls.
- A user sees only the items they may access under the existing Inventory
  privacy rules: public items plus their own private items, with no
  administrator bypass.
- Orders in progress are ignored. An item already covered by a `Planning` or
  `Active` order still appears, with the full quantity, because the list
  describes current stock against the minimum and nothing else.
- Exporting the list and creating orders from it are out of scope for this
  increment and are recorded as deferred in `ROADMAP.md`.

## Corrected Attention Defect

The launcher attention rule in `InventoryAttentionContributor` currently
activates for any accessible `Active` item where
`CurrentStock <= MinimumStock`. That includes every item with a zero minimum and
zero stock, which is the documented default shape for an item that is not
stock-tracked against a threshold. Those items produce permanent, unactionable
attention.

The rule gains the same `MinimumStock > 0` condition the shopping list uses, so
launcher attention becomes exactly "the shopping list is not empty". This
changes accepted behaviour: acceptance criterion 12 in
`docs/requirements/INVENTORY_REQUIREMENTS.md` and its row in
`INVENTORY_ACCEPTANCE.md` are corrected as part of this plan rather than left
describing the defect.

## Delivery Principles

- Add no entity, column, index, or migration. The existing
  `inventory_items (Status, Visibility)` index covers the new query.
- Compute the list in the backend. The item list projection carries no
  suppliers and is paginated, so the frontend cannot derive the list from
  existing reads without N+1 detail calls.
- Return a flat, fully ordered list rather than a nested block/category/item
  structure. The contract stays shallow and testable, and the dialog groups by
  consuming the order the backend guarantees.
- Reuse the existing read-only dialog pattern established by the item price
  history surface.
- Keep the attention fix isolated so it can ship, and be reverted,
  independently of the shopping list.

## Fixed Technical Contracts

### Route

```text
GET /api/inventory/shopping-list
```

Mapped on the `inventory` group rather than under `/items`, because the resource
is a derived replenishment view and not a paginated item collection. It is a
read, so it carries no antiforgery filter.

### Response

```csharp
InventoryShoppingListEntryResponse(
    int ItemId,
    string Name,
    int CategoryId,
    string CategoryName,
    int CategorySortOrder,
    string Block,
    decimal? RequiredQuantity,
    IReadOnlyList<string> Suppliers);

InventoryShoppingListResponse(
    IReadOnlyList<InventoryShoppingListEntryResponse> Entries);
```

`Block` is `Required` or `Optional`. `RequiredQuantity` is
`MinimumStock - CurrentStock` for `Required` entries and `null` for `Optional`
entries. `Suppliers` carries display names, already ordered; the separator and
its wording belong to the frontend.

`Entries` is ordered by block (`Required` first), then `CategorySortOrder`, then
`CategoryId`, then item name case-insensitively, then `ItemId`. The frontend
never re-sorts.

### Query Shape

The query filters on `InventoryItemPolicies.AccessibleTo(userId)`,
`Status == Active`, `MinimumStock > 0`, and `CurrentStock <= MinimumStock`, then
resolves category name and sort order through correlated sub-queries in the
established style. Allowed suppliers are read in a second flat query over
`InventoryItemSupplier` restricted to the resulting item identifiers.

Final ordering is applied **in memory after materialization**, not in SQL. The
default collation differs between SQLite and PostgreSQL, and the alphabetical
order must be identical on both providers. The result set is small and
unpaginated, so this costs nothing.

### Frontend

The dialog lives beside the existing Inventory dialogs, is opened from
`OrdersPanel`, and reads through a dedicated `inventoryKeys.shoppingList()`
query key with `staleTime: 0` and `refetchOnMount: 'always'`, so every open
recomputes. Receiving an order is the only action on that screen that moves
stock, so the key is invalidated alongside the item keys in that path.

Empty handling has three distinct states: no entries at all, an empty block
(omitted entirely rather than rendered empty), and a load failure.

## Waves

### Wave 0: Documentation

Record the accepted behaviour before implementing it.

Tasks:

1. Add a `## Shopping List` section to
   `docs/requirements/INVENTORY_REQUIREMENTS.md` covering the inclusion rule,
   the two blocks, grouping and ordering, privacy, and the absence of filters
   and pagination.
2. Add the `MinimumStock` greater-than-zero condition to the `## Attention`
   section of the same document, and correct acceptance criterion 12.
3. Add a new acceptance criterion for the shopping list.
4. Add the Inventory `ROADMAP.md` rows: shopping list scope resolved, attention
   minimum-stock correction resolved, list export and automatic order creation
   deferred.
5. Update the affected rows of `INVENTORY_ACCEPTANCE.md`.

Exit criteria:

- The documented Inventory behaviour matches what the following Waves implement,
  and the corrected attention rule is recorded as an intentional change rather
  than a silent one.

### Wave 1: Attention Correction

Fix the launcher attention defect independently of the new surface.

Tasks:

1. Add `MinimumStock > 0` to the predicate in
   `InventoryAttentionContributor.RequiresAttentionAsync`.
2. Update the contributor's XML documentation to state the three conditions.

Tests:

- Integration test that an accessible `Active` item with a zero minimum and zero
  stock does not activate attention, alongside the existing coverage in
  `InventoryAttentionTests`.

Exit criteria:

- Launcher attention activates only for items that a shopping list would list.

### Wave 2: Shopping List Backend

Deliver the computed read.

Tasks:

1. Add `InventoryShoppingListEntryResponse` and
   `InventoryShoppingListResponse` to `InventoryResponses.cs`.
2. Add the `ShoppingList` route constant to `InventoryApiRoutes`.
3. Map the route on the `inventory` group in `InventoryEndpoints`.
4. Implement `InventoryReadService.GetShoppingListAsync` with the filter,
   projection, supplier resolution, block classification, and in-memory
   ordering described above.

Tests:

- Integration tests in `InventoryShoppingListTests` for: private items of
  another user excluded and public items included, no administrator bypass,
  `Candidate` and `Deprecated` excluded, zero minimum excluded with both zero
  and positive stock, the `<` versus `=` block boundary, exact decimal
  quantities, `RequiredQuantity` null in the Optional block, complete and
  alphabetically ordered suppliers, category ordering by `SortOrder` with empty
  categories absent, alphabetical item ordering inside a category, and an empty
  response.

Exit criteria:

- `GET /api/inventory/shopping-list` returns a privacy-correct, fully ordered
  flat list that needs no client-side sorting.

### Wave 3: Shopping List Frontend

Deliver the button and the dialog.

Tasks:

1. Add the response types and the `shoppingList` method to
   `app/api/inventory.ts`, and the `shoppingList` query key to the Inventory
   contracts.
2. Implement the shopping list dialog following the price history dialog
   pattern: scrollable, grouped by block and category from the received order,
   quantity shown only in the Required block, suppliers on their own line.
3. Add the button to the Orders panel header only, and invalidate the shopping
   list key on the order-received path.
4. Add the dialog styles to the Inventory page stylesheet.
5. Add the Spanish and English strings for the button, title, description, both
   block names, the quantity wording, the supplier line, the empty states, the
   load error, and the close action.

Tests:

- Frontend tests that the button appears on Orders and not on Items and opens
  the dialog, and dialog tests for grouping, quantity present in Required and
  absent in Optional, comma-separated suppliers, an omitted empty block, the
  fully empty state, and the error state.
- Key-parity check for the new `inventory` namespace strings.

Exit criteria:

- A user opens the shopping list from Orders and sees the grouped result
  recomputed on every open, in both languages.

### Wave 4: Verification And Acceptance

Tasks:

1. Run backend format verification and the focused backend tests for the
   shopping list and attention, not the full integration suite.
2. Run frontend lint, format verification, and build, and run the frontend tests
   in small batches rather than as one full run.
3. Confirm the `INVENTORY_ACCEPTANCE.md` rows point at the delivered tests.

Exit criteria:

- The documented criteria map to passing, covering tests.

## Suggested Pull Request Boundaries

1. Documentation and attention correction (Waves 0-1).
2. Shopping list backend (Wave 2).
3. Shopping list frontend and acceptance (Waves 3-4).

## Plan Completion Criteria

The plan is complete when both blocks render from the live backend computation
under the accepted rules, launcher attention no longer activates for zero-minimum
items, and the Inventory documentation describes implemented behaviour rather
than intent.

Exporting the shopping list, creating orders from it, discounting quantities
already covered by orders in progress, filters or search inside the dialog, and
any unit-of-measure modelling remain separate future planning topics.
