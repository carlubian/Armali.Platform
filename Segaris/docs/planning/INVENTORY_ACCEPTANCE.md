# Inventory Acceptance Record (Wave 8)

This document records the Wave 8 end-to-end, hardening, and acceptance pass for
the Inventory module against `docs/requirements/INVENTORY_REQUIREMENTS.md` and
the exit criteria in `docs/planning/INVENTORY_IMPLEMENTATION_PLAN.md`.

## Verification Approach

Wave 8 was executed as a focused hardening and acceptance pass, matching the
Capex, Configuration, and Opex precedents:

- Functional behaviour is covered by the automated suites delivered in Waves 0-7
  and gated on every pull request through the required CI checks
  (`Segaris Backend`, `Segaris PostgreSQL`, `Segaris Compose`; see
  `docs/planning/BACKEND_CI_DECISIONS.md`).
- The fast local suites were re-run during this pass and are green: backend
  format verification and build, the backend unit project (229 tests passing),
  and the frontend format, lint, type-check, unit, and production build. The
  representative Playwright journey added below is compiled and listed; it runs
  against the Compose stack in CI when seeded credentials are present.
- The OpenAPI surface and the database indexes/query shape were verified
  statically against the implemented endpoints and the paired provider
  migrations.
- PostgreSQL transactional and decimal behaviour for receipt and reference
  migration is covered by `PostgresPersistenceTests` and the cross-module
  migration suite. A representative-volume `EXPLAIN ANALYZE` benchmark is
  intentionally deferred (see Deferred Items).

## End-To-End Journey

`tests/frontend/e2e/inventory.spec.ts` adds a single-user critical journey
against the full stack: sign in, open Inventory from the launcher, exercise and
clear an items filter, create an active item allowed for every supplier, add
stock through the quick-adjustment popup, switch to Orders, create an active
order with one line for the item, receive the order, confirm the received stock
(`5 + 3 = 8`) on the item, and delete the safe test data (order first, because an
item referenced by any order cannot be deleted). It is skipped without seeded
`SEGARIS_E2E_USERNAME` / `SEGARIS_E2E_PASSWORD` credentials, matching the other
specs. The second-user privacy journey is deferred (see Deferred Items).

## Static Verification Results

### HTTP / OpenAPI Surface

All Wave 0 frozen routes are mapped under `/api/inventory` with explicit OpenAPI
metadata and never expose EF Core entities (`InventoryEndpoints`). The group
applies `RequireAuthorization()`; private and missing records return an
indistinguishable `inventory.item.not_found` / `inventory.order.not_found` so
private identifiers are not disclosed.

- **Items**: `GET /items`, `GET /items/{itemId}`, `POST /items`,
  `PUT /items/{itemId}`, `DELETE /items/{itemId}`, and
  `POST /items/{itemId}/stock-adjustments` all carry `WithName`/`WithSummary`,
  typed `Produces<T>`, and `ProducesProblem` for `400`, `403`, `404`, and `409`
  as applicable; every mutation applies `AntiforgeryEndpointFilter`.
- **Item attachments**: four routes under `/items/{itemId}/attachments`
  (`GET`, `POST`, `GET /{attachmentId}`, `DELETE /{attachmentId}`) follow the
  shared attachment pattern; the upload route adds
  `WithRequestBodyLimit(AttachmentPolicy.MaximumFileSize + 1 MiB)`.
- **Orders**: `GET /orders`, `GET /orders/{orderId}`, `POST /orders`,
  `PUT /orders/{orderId}`, `DELETE /orders/{orderId}`, and
  `POST /orders/{orderId}/receive` carry full metadata, typed responses, and
  `ProducesProblem` for `400`, `403`, `404`, and `409` as applicable; every
  mutation applies `AntiforgeryEndpointFilter`. Receipt declares `404`/`409`
  for inaccessible and non-active orders.
- **Order attachments**: four routes under `/orders/{orderId}/attachments`
  mirror the item attachment pattern, including the upload body limit.
- **Catalogs**: `GET /categories` and `GET /locations` are authenticated reads;
  the six administrator-only management routes for each catalog (`POST`,
  `PUT /{id}`, `POST /{id}/move`, `GET /{id}/deletion-impact`, `DELETE /{id}`,
  `POST /{id}/replace-and-delete`) require `IdentityPolicies.Admin`, carry
  `WithName`/`WithSummary` and typed responses, declare `ProducesProblem` for
  `400`/`404`/`409` as applicable, and apply `AntiforgeryEndpointFilter`.
  Configuration presents these catalogs through the existing module-owned
  catalog boundary without additional routes.

### Indexes And Query Shape

The recommended indexes exist identically in both provider migrations
(`InventoryDomainPersistence` for SQLite and PostgreSQL) and match the query
shapes in `InventoryReadService`, `InventoryItemListQuery`, and
`InventoryOrderListQuery`:

| Index | Query that uses it |
| --- | --- |
| `inventory_items (Name, Id)` | Default item ordering (name asc, id asc tie-breaker) |
| `inventory_items (CreatedBy, Visibility, Id)` | `InventoryItemPolicies.AccessibleTo` privacy filter and attention scope |
| `inventory_items (Status, Visibility)` | Status exact filter; launcher attention (`Active` and low stock) |
| `inventory_items (CategoryId)` / `(LocationId)` | Category/location exact filters and reference migration |
| `inventory_items (Visibility)` / `(UpdatedBy)` | Visibility filter; audit display-name resolution |
| `inventory_item_suppliers (SupplierId)` | Supplier eligibility lookups and supplier reference migration |
| `inventory_orders (OrderDate, Id)` | Default order ordering (order date desc, id desc) |
| `inventory_orders (CreatedBy, Visibility, Id)` | `InventoryOrderPolicies` privacy filter |
| `inventory_orders (SupplierId)` / `(CurrencyId)` | Supplier/currency filters and reference/conversion migration |
| `inventory_orders (Status)` / `(Visibility)` / `(UpdatedBy)` | Status/visibility filters; audit resolution |
| `inventory_order_lines (OrderId, Id)` | Line projection and ordered receipt |
| `inventory_order_lines (ItemId)` | Item-referenced-by-order deletion guard and order item-name search |
| `inventory_categories`/`inventory_locations (NormalizedName)` unique | Catalog name uniqueness |
| `inventory_categories`/`inventory_locations (SortOrder)` | Default catalog ordering |

List filtering, sorting, pagination, and partial search (item name/notes, order
notes/referenced item names) run as `IQueryable` translated to SQL; the client
never loads the full result set. Partial search is an intentional `LIKE` scan
consistent with the accepted database-backed search baseline.

## Acceptance Criteria

Each criterion from `INVENTORY_REQUIREMENTS.md` and its primary covering
evidence:

| # | Criterion | Status | Primary evidence |
| --- | --- | --- | --- |
| 1 | Create, query, edit, and irreversibly delete visible items and orders with documented fields, defaults, validation, and privacy | Met | `InventoryItemMutationTests`, `InventoryItemDetailTests`, `InventoryItemListTests`, `InventoryOrderMutationTests`, `InventoryOrderListTests`, `InventoryDomainTests`, `InventoryPage.test.tsx`, `inventory.spec.ts` |
| 2 | Items store current/minimum stock, one descriptive location, and one or more allowed suppliers without lots, expirations, or stock-location tuples | Met | `InventoryDomainTests`, `InventoryItemMutationTests` (`Create_persists_the_item_with_suppliers_and_defaults`), `InventoryModelContributor`, `MigrationTests` |
| 3 | Statuses `Candidate`/`Active`/`Deprecated` available; deprecated items remain orderable while producing a UI warning | Met | `InventoryDomainTests`, `InventoryOrderMutationTests` (deprecated lines accepted), `OrderDialog` deprecated-line warning + `InventoryPage.test.tsx` |
| 4 | Public collaboration and private isolation for items and orders, including the special public-order/public-item constraints | Met | `InventoryItemAuthorizationTests`, `InventoryOrderMutationTests` (`...public_private_mix`, `Private_order_visibility_changes_are_creator_only_and_require_public_items`) |
| 5 | A public item cannot become private while referenced by any public order, and an item referenced by any order cannot be deleted | Met | `InventoryItemAuthorizationTests` (`Creator_cannot_make_item_private_when_it_appears_in_public_order`), `InventoryItemMutationTests` (`Delete_removes_unreferenced_item_and_blocks_order_referenced_item`) |
| 6 | Orders belong to exactly one supplier and currency, and every line references an item allowed for that supplier | Met | `InventoryOrderMutationTests` (`Create_persists_the_order_with_lines_and_defaults`, `...supplier_mismatch...`), `InventoryDomainTests` |
| 7 | Explicit receive changes an active order to received and updates stock in one transaction; manual status changes do not affect stock | Met | `InventoryOrderReceiveTests` (`Receive_active_order_increases_stock_sets_received_and_updates_metadata`, `Manual_status_update_to_received_does_not_move_stock`), `PostgresPersistenceTests`, `inventory.spec.ts` |
| 8 | Received orders are protected from full editing until the user moves them back to another status | Met | `InventoryOrderMutationTests` (`Received_orders_allow_only_status_unlock_before_full_editing`), `InventoryOrderReceiveTests` (`Receive_rejects_non_active_orders_without_touching_stock`) |
| 9 | Quick stock increase and decrease update only the item stock and reject negative results | Met | `InventoryStockAdjustmentTests` (increase, decrease, `Decrease_below_zero_is_rejected_and_leaves_stock_unchanged`, invalid input), `InventoryDomainTests`, `inventory.spec.ts` |
| 10 | Inventory-owned categories and locations are initialized once and managed through Configuration with CRUD, reorder, and atomic reference migration before deletion | Met | `InventoryCatalogEndpointTests` (seeded order, admin create/move/delete, normal-user rejection, duplicate conflict), `InventoryConfigurationMigrationTests`; final-row protection in `InventoryCategoryManagementService`/`InventoryLocationManagementService` |
| 11 | Shared Supplier and Currency catalogs come from Configuration through published contracts rather than direct entity access | Met | `InventoryConfigurationMigrationTests`, `ModuleBoundaryTests` (Inventory depends on Configuration only), `InventoryContractTests` |
| 12 | Inventory attention is true exactly when the current user can access at least one active item whose current stock is at or below minimum | Met | `InventoryAttentionTests` (no-attention baseline, `Attention_activates_only_for_active_low_or_equal_stock_items`, public vs. another user's private low-stock item) |
| 13 | SQLite and PostgreSQL migrations, backend unit/integration/architecture tests, frontend component tests, and a representative Playwright journey verify behaviour and privacy | Met (single-user E2E) | `MigrationTests` (both providers), `PostgresPersistenceTests`, `ModuleBoundaryTests`/`ModuleRegistrationTests`, the Inventory unit suite, the full Inventory API integration suite, `contracts.test.ts`/`itemsState.test.ts`/`ordersState.test.ts`/`InventoryPage.test.tsx`, `inventory.spec.ts` |

Attachment behaviour underlying criteria 1 and 13 is covered by
`InventoryItemAttachmentTests` and `InventoryOrderAttachmentTests` (upload, list,
download, delete round-trip, private-record hiding, and filesystem cleanup on
item/order deletion).

## Deferred Items

Recorded in `ROADMAP.md`:

- **Second-user Inventory privacy E2E journey.** Public-collaboration and
  private-isolation behaviour is covered by API integration tests
  (`InventoryItemAuthorizationTests`, `InventoryOrderMutationTests`,
  `InventoryOrderReceiveTests`); the browser-level multi-session journey waits on
  multi-account Playwright infrastructure, matching the deferred Capex,
  Configuration, and Opex patterns.
- **PostgreSQL representative-volume query-plan benchmark.** Index existence and
  database-level query shape are verified; a large-dataset `EXPLAIN ANALYZE`
  benchmark waits on a representative seeding/benchmark harness.

## Documentation Outcomes

- `README.md`: no change; repository-wide commands and setup were unaffected by
  the Inventory waves.
- `ROADMAP.md`: Inventory implementation marked accepted; the two deferred items
  above recorded.
- `docs/planning/INVENTORY_IMPLEMENTATION_PLAN.md`: Wave 8 status updated to
  point at this record.
