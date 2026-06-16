# Inventory Implementation Plan

## Purpose

This plan delivers the initial Inventory module defined in
`docs/requirements/INVENTORY_REQUIREMENTS.md`. It translates the accepted
functional decisions into dependency-ordered Waves with explicit backend,
frontend, migration, and test work.

The requirements document remains authoritative for behavior. This plan owns
delivery order and technical scope.

## Delivery Principles

- Preserve Inventory as an independent business module.
- Reuse established Configuration, Launcher, Attachments, privacy, REST,
  pagination, and frontend conventions where their semantics match.
- Do not introduce lots, expiration dates, stock-movement history, partial
  receipt, or cross-business-module write dependencies.
- Keep supplier eligibility and public/private constraints explicit in backend
  validation rather than inferred only by the frontend.
- Keep each Wave independently testable and record deferrals in `ROADMAP.md`
  instead of hiding them in implementation notes.

## Fixed Technical Contracts

### Backend Module

Inventory lives under `Segaris.Api.Modules.Inventory` and owns item, order,
order-line, category, location, quick-stock-adjustment, attachment
authorization, Configuration reference handling, and launcher attention logic.

Indicative resource routes are:

```text
GET    /api/inventory/items
POST   /api/inventory/items
GET    /api/inventory/items/{itemId}
PUT    /api/inventory/items/{itemId}
DELETE /api/inventory/items/{itemId}
POST   /api/inventory/items/{itemId}/stock-adjustments

GET    /api/inventory/orders
POST   /api/inventory/orders
GET    /api/inventory/orders/{orderId}
PUT    /api/inventory/orders/{orderId}
DELETE /api/inventory/orders/{orderId}
POST   /api/inventory/orders/{orderId}/receive

GET    /api/inventory/categories
GET    /api/inventory/locations
```

Administrative category and location routes follow the existing module-owned
catalog management pattern exposed through Configuration.

All writes require antiforgery. Missing and inaccessible records share the
platform not-found behavior so private data is not disclosed.

### Persistence

The model contains module-owned entities with provider-specific migrations:

- `InventoryItem`
- `InventoryOrder`
- `InventoryOrderLine`
- `InventoryCategory`
- `InventoryLocation`
- `InventoryItemSupplier` or equivalent join entity for the many-to-many item
  and supplier relationship

Items store `CurrentStock` and `MinimumStock` directly. Orders store the
supplier, currency, dates, visibility, and status. Order lines store item,
quantity, and line total price.

The initial model has no stock-movement table and no lot table.

Indexes must support item and order filters, deterministic sorting, launcher
attention, supplier eligibility lookups, and category/location/supplier/currency
reference migration.

### Frontend Route

Inventory uses the protected lazy route `/inventory`.

The initial UI should support URL-backed list state and dialog state for both
items and orders, following the Capex and Opex pattern. One practical route
shape is:

```text
/inventory
/inventory?itemId=123
/inventory?newItem=true
/inventory?orderId=123
/inventory?newOrder=true
```

If the shared shell or router ergonomics suggest a slightly different parameter
layout, preserve the same behavior: list state must survive dialog open and
close without a reload.

### Configuration Integration

Configuration remains the owner of shared suppliers and currencies. Inventory
owns its categories and locations while exposing them through the established
Configuration presentation boundary.

Inventory must register narrow catalog reference handlers for:

- Supplier
- Currency
- Inventory category
- Inventory location

Supplier changes validate future eligibility only and do not rewrite historical
orders. Deleting or replacing an in-use supplier through Configuration must keep
existing orders valid after the migration operation completes.

## Waves

### Wave 0: Contracts And Test Skeleton

Establish stable contracts before persistence or UI work begins.

Tasks:

1. Add the Inventory module shell and registration.
2. Freeze item and order routes, enums, DTOs, query contracts, stable error
   codes, attention contracts, and attachment owner kinds.
3. Define Configuration-facing contracts for category, location, supplier, and
   currency reference handling without exposing Inventory entities.
4. Define frontend API, validation-schema, route-state, and query-key
   skeletons.
5. Add architecture-test expectations for Inventory dependency direction:
   Inventory may consume Configuration and platform contracts but must not
   depend on Capex or Opex.
6. Add empty backend and frontend test fixtures shared by later Waves.

Tests:

- Unit tests for enum values, defaults, route constants, query bounds, and
  error-code stability.
- Architecture tests for permitted dependencies and published contracts.

Exit criteria:

- Public contracts are explicit and test-covered, and later Waves do not need to
  invent new route or ownership semantics.

### Wave 1: Domain, Persistence, And Catalogs

Implement the Inventory data model and module-owned catalogs on both providers.

Tasks:

1. Add `InventoryItem`, `InventoryOrder`, `InventoryOrderLine`,
   `InventoryCategory`, `InventoryLocation`, and the item-supplier join model.
2. Enforce required relationships, decimal precision, bounded strings,
   deterministic ordering, and standard audit metadata.
3. Seed the accepted initial category and location values once.
4. Implement module-owned category and location reads plus administrator
   mutations through Configuration.
5. Add provider-specific migrations and model snapshots.
6. Add indexes for item filters, order filters, attention, supplier eligibility,
   and reference migration.

Tests:

- Domain tests for stock invariants, status rules, item-supplier association,
  and order-line ownership.
- SQLite and PostgreSQL migration tests, including upgrades and idempotent
  catalog initialization.
- Integration tests for category and location ordering, uniqueness, final-row
  protection if adopted, and administrator authorization.

Exit criteria:

- Both providers persist the complete model and expose configurable category and
  location catalogs.

### Wave 2: Item Read APIs, Attention, And Quick Stock Adjustment

Deliver the core item workflow first, including low-stock attention.

Tasks:

1. Implement paginated item list and item detail queries.
2. Implement partial search across item name and notes.
3. Implement exact filters for status, category, location, supplier, visibility,
   and creator.
4. Implement deterministic sorting and the default item ordering.
5. Implement the Inventory launcher attention contributor using accessible active
   items whose current stock is less than or equal to minimum stock.
6. Implement the quick stock-adjustment operation for increase and decrease
   without a movement-history table.

Tests:

- API integration tests for item pagination, filters, search, sorting,
  visibility isolation, and not-found privacy behavior.
- Attention tests for public/private items, administrators, candidate and
  deprecated items, zero thresholds, and equal-threshold stock.
- Mutation tests for quick stock increase, decrease, negative-result rejection,
  and concurrent visibility behavior.

Exit criteria:

- Users can browse accessible items, adjust stock quickly, and the launcher can
  compute Inventory attention authoritatively from the backend.

### Wave 3: Item Mutations, Attachments, And Privacy Guards

Complete the item backend contract.

Tasks:

1. Implement create, update, and delete for items with full validation.
2. Enforce allowed-supplier presence and valid category/location references.
3. Enforce the public-item-to-private restriction when the item appears in any
   public order.
4. Block deletion of any item referenced by an order.
5. Add item attachment listing, upload, download, and delete routes.
6. Review and document OpenAPI metadata and stable validation outcomes.

Tests:

- API tests for defaults, required suppliers, status changes, category/location
  edits, visibility transitions, and deletion guards.
- Two-user privacy tests for public collaboration and private isolation.
- Attachment tests for round-trip behavior, authorization, validation failures,
  and filesystem cleanup on item deletion.

Exit criteria:

- The complete item contract behaves correctly for validation, privacy, and
  attachments.

### Wave 4: Order Read And Mutation APIs

Deliver orders before explicit receipt so the standard editing workflow is
stable first.

Tasks:

1. Implement paginated order list and order detail queries.
2. Implement order search across notes and referenced item names without
   duplicating rows.
3. Implement exact filters for supplier, status, currency, visibility, and
   creator.
4. Implement create, update, and delete for orders with full-line replacement.
5. Enforce one supplier per order and supplier eligibility on every line.
6. Enforce public-order and public-item privacy compatibility.
7. Protect received orders from full editing until the user moves them back to
   another state.
8. Add order attachment listing, upload, download, and delete routes.

Tests:

- API tests for defaults, pagination, filters, sorting, detail projection, full
  order edits, line replacement, deprecated-item warning metadata if exposed,
  and delete behavior.
- Privacy tests for public orders, private orders, item-visibility constraints,
  and private-to-public promotion rejection.
- Attachment tests for round-trip behavior and delete cleanup.

Exit criteria:

- Users can manage the full non-receive order workflow through the backend with
  correct validation and privacy.

### Wave 5: Explicit Receive Operation

Add the transaction that turns an active order into received stock.

Tasks:

1. Implement `POST /api/inventory/orders/{orderId}/receive`.
2. Require the order to be accessible and currently `Active`.
3. Increase each referenced item's stock by the line quantity in the same
   transaction that changes the order status.
4. Update order and item modification metadata.
5. Reject repeated receipt, non-active receipt, and invalid line states.
6. Preserve the rule that manual status updates never trigger receipt logic.

Tests:

- API integration tests for successful receipt, stock updates, metadata updates,
  repeated receipt rejection, inactive-order rejection, and rollback on failure.
- PostgreSQL coverage for transactional behavior and provider-sensitive decimal
  persistence.

Exit criteria:

- The receive operation is authoritative, atomic, and distinct from normal order
  editing.

### Wave 6: Configuration Reference Migration

Integrate Inventory safely into structural catalog management.

Tasks:

1. Register reference handlers for Inventory categories and locations using the
   existing module-owned catalog-management pattern.
2. Register supplier and currency reference handlers for Inventory orders and
   item-supplier eligibility.
3. Define and implement supplier replacement semantics that preserve historical
   orders while keeping future supplier eligibility coherent.
4. Re-evaluate references in the confirming transaction and roll back on any
   failure.
5. Invalidate affected Inventory and Configuration frontend queries after
   successful structural mutations.

Tests:

- Cross-module integration tests for category replacement, location replacement,
  supplier replacement, currency replacement, rollback, and privacy-neutral
  impact reporting.
- SQLite and PostgreSQL coverage for atomic migration behavior.

Exit criteria:

- Configuration can safely mutate or delete every catalog value referenced by
  Inventory without exposing private data or leaving partial updates behind.

### Wave 7: Inventory Frontend

Build the user-facing Inventory module experience.

Tasks:

1. Add the lazy `/inventory` route, module error boundary, translation namespace,
   and launcher card.
2. Build the items table with URL-backed search, filters, sorting, pagination,
   and an attention-aware launcher integration.
3. Build the item dialog with React Hook Form and Zod, plus allowed-supplier
   selection, attachments, and visibility guards.
4. Build the quick stock-adjustment popup.
5. Build the orders table with URL-backed search, filters, sorting, and
   pagination.
6. Build the order dialog with line editing, supplier-constrained item
   selection, deprecated-item warnings, attachments, and the received-order edit
   protection.
7. Add the explicit `Receive` action with loading, confirmation if needed, and
   list invalidation.

Tests:

- Frontend API and component tests for route state, filters, sorting, pagination,
  item and order validation, privacy-safe errors, supplier eligibility, quick
  stock updates, received-order protection, and receive action feedback.
- Accessibility tests for dialog focus, keyboard operation, error association,
  and quick-popup behavior.

Exit criteria:

- Users can complete the full Inventory item and order workflow without page
  reloads while preserving list state.

### Wave 8: End-To-End, Hardening, And Acceptance

Validate the implemented behavior across both providers and the deployed
frontend/backend boundary.

Tasks:

1. Run backend format, build, unit, API, PostgreSQL, migration, and architecture
   suites through repository scripts.
2. Run frontend format, lint, type-check, unit, build, and Playwright suites.
3. Add a representative Playwright journey covering login, opening Inventory,
   creating an item, adjusting stock, creating an order, receiving it, checking
   the new stock, and deleting safe test data.
4. Review OpenAPI for Inventory item, stock-adjustment, order, receive, catalog,
   and attachment routes.
5. Verify keyboard behavior, dialog scrolling, attention updates, filtered list
   invalidation, and narrow desktop widths.
6. Map every criterion in `docs/requirements/INVENTORY_REQUIREMENTS.md` to
   covering code and tests in an Inventory acceptance record.
7. Update `ROADMAP.md` to mark Inventory as defined and to record only
   intentional remaining deferrals.

Exit criteria:

- All relevant repository scripts pass, both providers are covered, the Compose
  journey succeeds, and every accepted Inventory requirement is implemented or
  explicitly deferred.

## Suggested Pull Request Boundaries

1. Inventory contracts, persistence, and module-owned catalogs (Waves 0-1).
2. Item reads, attention, mutations, and attachments (Waves 2-3).
3. Order reads, mutations, and receipt (Waves 4-5).
4. Configuration reference migration (Wave 6).
5. Inventory frontend (Wave 7).
6. End-to-end, hardening, and acceptance (Wave 8).

## Plan Completion Criteria

The plan is complete when all Waves meet their exit criteria and the Inventory
requirements document describes implemented behavior rather than only functional
intent.

Lots, expiration dates, stock-location tuples, partial receipt, movement
history, forecasts, Analytics integration, and cross-module links remain
separate future planning topics.
