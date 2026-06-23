# Health Implementation Plan

## Purpose

This plan delivers the initial Health module defined in
`docs/requirements/HEALTH_REQUIREMENTS.md`. It translates the accepted functional
decisions into dependency-ordered Waves with explicit backend, frontend,
migration, and test work.

The requirements document remains authoritative for behaviour. This plan owns
delivery order and technical scope.

## Delivery Principles

- Preserve Health as an independent business module. Its only cross-business-module
  relationship is that a `Medicine` gains an optional reference to an Inventory
  item; the dependency direction is Health to Inventory, mirroring Recipes.
- Reuse established Configuration, Attachments, privacy, REST, pagination,
  entity-selection, and frontend conventions where their semantics match.
- Reuse the shared `EntityReferenceField` and `EntitySelectorDialog` with a thin
  per-entity adapter for the medicine-to-item link, without forking the selector or
  introducing a generic backend association model.
- Implement the Inventory item-deletion handling by contract inversion so the
  dependency direction stays Health to Inventory. Item deletion clears the medicine
  link; it never reassigns and never blocks.
- Model the disease-to-medicine relationship as a module-owned, attribute-free join
  with no generic polymorphic association table. Manage it through individual
  add/remove operations and enforce the association visibility rule (accessible-only
  creation, viewer-filtered reads, publish guard) in backend validation rather than
  inferring it only on the frontend.
- Extend the shared entity selector with a reusable **multi-select** variant for the
  symmetric disease-to-medicine relationship, rather than forking the dialog or
  building a one-off association UI. The single-select Inventory link continues to
  use the existing variant.
- Keep each Wave independently testable and record deferrals in `ROADMAP.md`
  instead of hiding them in implementation notes.

## Fixed Technical Contracts

### Backend Module

Health lives under `Segaris.Api.Modules.Health` and owns the disease entity, the
medicine entity, the attribute-free disease-to-medicine association, the
`DiseaseCategory` and `MedicineCategory` catalogues, medicine attachment
authorization, and the medicine-to-Inventory link including the Inventory
item-deletion reference contract it implements. It consumes the Configuration
presentation boundary, a narrow Inventory read contract, the Inventory item-deletion
reference contract, and platform contracts. It does not depend on any other business
module, and it contributes no launcher attention.

Indicative resource routes are:

```text
GET    /api/health/diseases
POST   /api/health/diseases
GET    /api/health/diseases/{diseaseId}
PUT    /api/health/diseases/{diseaseId}
DELETE /api/health/diseases/{diseaseId}

GET    /api/health/diseases/{diseaseId}/medicines
POST   /api/health/diseases/{diseaseId}/medicines/{medicineId}
DELETE /api/health/diseases/{diseaseId}/medicines/{medicineId}

GET    /api/health/medicines
POST   /api/health/medicines
GET    /api/health/medicines/{medicineId}
PUT    /api/health/medicines/{medicineId}
DELETE /api/health/medicines/{medicineId}

GET    /api/health/medicines/{medicineId}/diseases
POST   /api/health/medicines/{medicineId}/diseases/{diseaseId}
DELETE /api/health/medicines/{medicineId}/diseases/{diseaseId}

GET    /api/health/medicines/{medicineId}/attachments
POST   /api/health/medicines/{medicineId}/attachments
GET    /api/health/medicines/{medicineId}/attachments/{attachmentId}
DELETE /api/health/medicines/{medicineId}/attachments/{attachmentId}
PUT    /api/health/medicines/{medicineId}/attachments/{attachmentId}/primary

GET    /api/health/disease-categories
GET    /api/health/medicine-categories
```

The association POST/DELETE pairs on the two sides operate on the same join row;
`POST /diseases/{d}/medicines/{m}` is equivalent to
`POST /medicines/{m}/diseases/{d}`. Both creation routes enforce the association
visibility rule, and both list routes are viewer-filtered. Administrative category
routes follow the existing module-owned catalogue management pattern exposed through
Configuration. All writes require antiforgery. Missing and inaccessible records share
the platform not-found behaviour so private data is not disclosed.

### Persistence

The model contains module-owned entities with provider-specific migrations:

- `Disease`
- `Medicine`
- `DiseaseMedicine` (attribute-free join)
- `DiseaseCategory`
- `MedicineCategory`

Diseases store the name, category reference, optional symptoms, optional average
duration in days, notes, visibility, and standard audit metadata. Medicines store
the name, category reference, optional posology, the `RequiresPrescription` flag,
the optional opaque Inventory item identifier, notes, visibility, and standard audit
metadata. The `DiseaseMedicine` join stores only the disease and medicine
identifiers with a unique constraint on the pair. Owned association rows and medicine
attachments are removed when their disease or medicine is deleted.

The Inventory item identifier is a stable opaque reference, not a foreign key to
Inventory entities; its integrity is preserved by the deletion reference contract
rather than a database constraint that would couple Health to Inventory tables.

Indexes must support disease and medicine filters and deterministic sorting (name,
then identifier), the two category reference migrations, the join lookup from both
sides, the unique pair constraint, and the medicine item-reference lookup used by the
Inventory deletion handler.

### Frontend Routes

Health uses the protected lazy route `/health` for a single surface with two tabs,
the disease list and the medicine gallery. The active tab, each tab's list state,
and any open dialog are URL-backed, following the established URL-aware popup
pattern. One practical route shape is:

```text
/health
/health?tab=diseases
/health?tab=medicines
/health?tab=diseases&diseaseId=12
/health?tab=diseases&newDisease=true
/health?tab=medicines&medicineId=8
/health?tab=medicines&newMedicine=true
```

If the shared shell or router ergonomics suggest a slightly different parameter
layout, preserve the same behaviour: the active tab and both lists' state must
survive dialog open and close without a reload.

### Inventory Integration

Health consumes a narrow Inventory read contract to validate an item reference,
resolve its display name, and evaluate accessibility for the link visibility rule.
Inventory owns this contract, which already exists from the Recipes integration.

Health implements the Inventory item-deletion reference contract: it reports the
number of medicines referencing an item and clears the item link on all of them
within the deletion transaction. Inventory enumerates registered implementations
during deletion; it never queries Health entities. This mirrors the existing
Recipes-to-Inventory inversion. If a needed Inventory contract is absent, it is
defined on the Inventory side without coupling Inventory to consumers.

### Configuration Integration

Configuration presents the Health catalogues alongside the other module-owned
catalogues. Health owns `DiseaseCategory` and `MedicineCategory` while exposing them
through the established Configuration presentation boundary. Because a category is
required on every disease and every medicine, a referenced value may only be
**replaced**; replacement re-points the affected records to the target value.

## Waves

### Wave 0: Contracts And Test Skeleton

Establish stable contracts before persistence or UI work begins.

Tasks:

1. Add the Health module shell and registration after Destinations.
2. Freeze disease, medicine, association, attachment, and category routes; the
   `RequiresPrescription` flag and the average-duration bounds (1-100,000) as fixed
   (non-catalogue) concepts; DTOs; query contracts; stable error codes; the
   attachment owner kind (`Medicine`); and the absence of any launcher attention key.
3. Define Configuration-facing contracts for `DiseaseCategory` and
   `MedicineCategory` reference handling without exposing Health entities.
4. Declare consumption of the Inventory read contract and the Inventory
   item-deletion reference contract that Health implements.
5. Freeze the association visibility rule semantics as a stated contract:
   accessible-only creation, viewer-filtered reads, and the publish guard.
6. Define frontend API, validation-schema, route-state, and query-key skeletons for
   the two-tab surface, both list states, and both editors.
7. Add architecture-test expectations: Health may consume Configuration, Inventory,
   and platform contracts but must not depend on Capex, Opex, Travel, Assets,
   Maintenance, Projects, Processes, Firebird, Clothes, Mood, Recipes, or
   Destinations; and Inventory must not depend on Health.
8. Add empty backend and frontend test fixtures shared by later Waves.

Tests:

- Unit tests for average-duration bounds, the prescription flag default, route
  constants, query bounds, and error-code stability.
- Architecture tests for permitted dependencies, the Inventory-to-Health
  non-dependency, and published contracts.

Exit criteria:

- Public contracts are explicit and test-covered, and later Waves do not need to
  invent new route, ownership, or cross-module semantics.

### Wave 1: Domain, Persistence, And Catalogues

Implement the Health data model and module-owned catalogues on both providers.

Tasks:

1. Add `Disease`, `Medicine`, `DiseaseMedicine`, `DiseaseCategory`, and
   `MedicineCategory`.
2. Enforce the required category relationships, bounded strings, the optional
   1-100,000 average duration, the `RequiresPrescription` flag, the attribute-free
   join with its unique pair constraint, and visibility and audit metadata.
3. Seed the accepted initial category values once for both catalogues.
4. Implement module-owned category reads plus administrator mutations through
   Configuration for both catalogues, including ordering and final-row protection.
5. Add provider-specific migrations and model snapshots.
6. Add indexes for disease and medicine filters and sorting, the two category
   reference migrations, the join lookups from both sides with the unique pair
   constraint, and the medicine item-reference lookup.

Tests:

- Domain tests for average-duration bounds, the prescription flag, join uniqueness,
  and deterministic ordering.
- SQLite and PostgreSQL migration tests, including upgrades and idempotent catalogue
  initialization.
- Integration tests for category ordering, final-row protection, and administrator
  authorization for both catalogues.

Exit criteria:

- Both providers persist the complete model and expose two configurable category
  catalogues.

### Wave 2: Disease Read And Mutation APIs

Deliver the core disease backend contract, excluding associations.

Tasks:

1. Implement the paginated disease list query and the disease detail query.
2. Implement partial search across the disease name.
3. Implement the exact category filter and deterministic sorting with the default
   ordering (name ascending, then identifier ascending).
4. Implement create, update, and delete for diseases with full validation and the
   documented creation defaults (category, no symptoms/duration/notes).
5. Enforce category validity, visibility transitions, creator-only visibility change,
   and standard public-collaboration and private-isolation authorization.

Tests:

- API integration tests for pagination, filters, search, sorting, defaults, required
  fields, visibility isolation, and not-found privacy behaviour.
- Two-user privacy tests for public collaboration and private isolation.

Exit criteria:

- Users can browse and fully manage accessible diseases through the backend with
  correct validation and privacy, without associations.

### Wave 3: Medicine Read And Mutation APIs

Deliver the core medicine backend contract, excluding the item link, attachments,
and associations.

Tasks:

1. Implement the paginated medicine list query and the medicine detail query.
2. Implement partial search across the medicine name.
3. Implement exact filters for category and the prescription flag and deterministic
   sorting with the default ordering (name ascending, then identifier ascending).
4. Implement create, update, and delete for medicines with full validation and the
   documented creation defaults (category, prescription flag `false`, no
   posology/notes).
5. Enforce category validity, visibility transitions, creator-only visibility change,
   and standard public-collaboration and private-isolation authorization.

Tests:

- API integration tests for pagination, filters (including the prescription flag),
  search, sorting, defaults, required fields, visibility isolation, and not-found
  privacy behaviour.
- Two-user privacy tests for public collaboration and private isolation.

Exit criteria:

- Users can browse and fully manage accessible medicines through the backend with
  correct validation and privacy, without the item link, attachments, or
  associations.

### Wave 4: Disease-To-Medicine Association APIs

Deliver the symmetric many-to-many association and its visibility rule.

Tasks:

1. Implement the viewer-filtered association list queries from both sides (a
   disease's accessible medicines and a medicine's accessible diseases), including
   the associated-count projection used by the disease list.
2. Implement individual association create and delete from both sides over the single
   shared join row, idempotent on create and no-op-safe on delete.
3. Enforce the creation rule (the acting user must access both endpoints; any private
   endpoint must be the actor's own) and stable conflict error codes.
4. Implement the publish guard: reject a `Private` to `Public` visibility change on a
   disease or medicine while it still has any association to a non-`Public` record,
   reporting the blocking count privacy-neutrally.
5. Ensure deleting a disease or medicine removes all of its join rows within the same
   transaction without blocking.

Tests:

- API integration tests for symmetric create/delete equivalence across both sides,
  idempotency, the creation rule and its rejection paths, viewer-filtered reads, and
  the associated-count projection.
- Two-user tests proving a public record never exposes another user's private
  associations and that a set mutation by one user never disturbs another user's
  private links.
- Tests for the publish guard rejection and its privacy-neutral count, and for join
  cleanup on disease and medicine deletion.

Exit criteria:

- Diseases and medicines can be associated symmetrically under the visibility rule,
  reads never disclose private associations, and publishing a record linked to a
  private record is blocked.

### Wave 5: Medicine Attachments

Deliver medicine attachments with a primary image.

Tasks:

1. Add medicine attachment listing, upload, download, delete, and set-primary routes
   using the shared attachment policies.
2. Inherit medicine visibility and authorization for attachments.
3. Implement the gallery thumbnail resolution (primary image, then first image, then
   placeholder).
4. Clean up attachments on medicine deletion.

Tests:

- Attachment tests for round-trip behaviour, primary-image selection, thumbnail
  fallback, authorization, validation failures, and filesystem cleanup on medicine
  deletion.

Exit criteria:

- Medicines support multiple attachments with an optional primary image that respect
  medicine visibility and are cleaned up on deletion.

### Wave 6: Medicine-To-Inventory Link

Add the optional live medicine link to Inventory and its deletion handling.

Tasks:

1. Consume the Inventory read contract to validate the referenced item, resolve its
   display name, and evaluate accessibility for the link visibility rule.
2. Add the optional opaque `inventoryItemId` to medicine create and update, enforcing
   the visibility rule on create, update, visibility change, and item change: a
   public medicine may reference only public items; a private medicine may reference
   any item its creator can access.
3. Surface the resolved item name in the medicine projection, with a neutral
   placeholder when the item is not resolvable for the viewer.
4. Implement the Inventory item-deletion reference contract in Health: report the
   number of medicines referencing an item and clear the link on all of them within
   the deletion transaction.
5. Ensure Inventory item deletion enumerates registered reference contracts, evaluates
   impact, performs the clearing atomically, and rolls back on any failure, with
   privacy-neutral impact reporting that never discloses other users' private
   medicines.

Tests:

- API integration tests for linking, unlinking, the visibility rule and its rejection
  paths, item-name resolution, and the neutral placeholder.
- Cross-module integration tests for clearing links across mixed-ownership medicines,
  rollback, and privacy-neutral impact reporting.
- SQLite and PostgreSQL coverage for the atomic delete-and-clear behaviour.
- Architecture tests confirming Inventory gains no dependency on Health and that
  Health depends on Inventory.

Exit criteria:

- A medicine can reference an accessible Inventory item under the visibility rule, the
  link never discloses a private item, and a referenced item can be deleted with its
  medicine links cleared atomically while the dependency direction is preserved.

### Wave 7: Frontend Shell And Diseases Tab

Build the two-tab Health surface and the diseases tab, including association editing
from the disease side.

Tasks:

1. Add the lazy `/health` route, module error boundary, translation namespace, a
   launcher card with no attention state, and the two-tab shell with URL-backed
   active tab and per-tab list state.
2. Build the server-paginated disease list with URL-backed name search, category
   filter, name/category sorting, and bounded pagination, with rows showing the name,
   category, and accessible associated-medicine count.
3. Build the disease dialog with React Hook Form and Zod, covering name, category,
   symptoms, average duration, notes, and visibility guards.
4. Extend the shared entity selector with a reusable multi-select variant and add a
   `MedicineEntitySelector` adapter; wire it into the disease editor to add and remove
   associated medicines by diffing the desired set against the viewer-visible current
   set, honouring the creation rule and surfacing the publish-guard error.
5. Add the Health section to the Configuration UI for `DiseaseCategory`, including
   reorder controls and the replacement dialog with a privacy-neutral impact summary.
6. Wire deletion confirmation and list and Configuration cache invalidation.

Tests:

- Frontend API and component tests for route and tab state, filters, sorting,
  pagination, disease validation, the associated-count display, the multi-select
  association editor (add/remove, delta against visible set, publish-guard error),
  privacy-safe errors, and category CRUD and reorder.
- Accessibility tests for dialog focus, keyboard operation, the multi-select selector,
  and error association.

Exit criteria:

- Users can complete the full disease workflow, including managing associated
  medicines, without page reloads while preserving tab and list state, and manage the
  disease category catalogue through the UI.

### Wave 8: Frontend Medicines Tab

Build the medicine gallery and its editor, including attachments, the Inventory link,
association editing from the medicine side, and Inventory deletion impact.

Tasks:

1. Build the server-paginated thumbnail gallery with URL-backed name search, category
   and prescription filters, name/category sorting, and bounded pagination, with cards
   showing the primary image or placeholder, name, category, a prescription badge, and
   the resolved item name when linked.
2. Build the medicine dialog with React Hook Form and Zod, covering name, category,
   posology, the prescription flag, notes, visibility guards, and medicine attachments
   with primary-image selection.
3. Add an `InventoryItemEntitySelector` adapter (or reuse the Recipes one) over the
   shared single-select selector, constrained by the medicine visibility rule, showing
   the resolved item name and a neutral placeholder when unresolvable.
4. Wire the multi-select `DiseaseEntitySelector` into the medicine editor to add and
   remove associated diseases by diffing against the viewer-visible current set,
   honouring the creation rule and the publish-guard error.
5. Add the Health section to the Configuration UI for `MedicineCategory`, including
   reorder controls and the replacement dialog with a privacy-neutral impact summary.
6. Extend the Inventory item-deletion flow in the Inventory frontend to present the
   privacy-neutral medicine-reference impact and confirm the link-clearing outcome.
7. Wire deletion confirmation and Health, Inventory, and Configuration cache
   invalidation after structural mutations.

Tests:

- Frontend API and component tests for gallery route state, filters, sorting,
  pagination, medicine validation, the prescription badge, attachment flows, the
  item selector constrained by visibility with placeholder, the multi-select disease
  association editor, the Inventory deletion-impact confirmation, category CRUD and
  reorder, and cache invalidation.
- Accessibility tests for dialog focus, keyboard operation, both selectors, and error
  association.

Exit criteria:

- Users can complete the full medicine workflow, including attachments, the Inventory
  item link, and managing associated diseases, while preserving gallery state; and
  deleting a referenced Inventory item shows a privacy-neutral impact before clearing
  the medicine links.

### Wave 9: End-To-End, Hardening, And Acceptance

Validate the implemented behaviour across both providers and the deployed
frontend/backend boundary.

Tasks:

1. Run backend format, build, unit, API, PostgreSQL, migration, and architecture
   suites through repository scripts.
2. Run frontend format, lint, type-check, unit, build, and Playwright suites.
3. Add a representative Playwright journey covering login, opening Health, creating a
   disease (with a category, symptoms, and average duration), creating a medicine
   (with a category, posology, the prescription flag, and a primary image), associating
   the disease and medicine from both sides, linking the medicine to an Inventory item,
   verifying the viewer-filtered association and the resolved item name, and deleting
   the Inventory item to confirm the medicine link is cleared; plus deleting safe test
   data.
4. Review OpenAPI for Health disease, medicine, association, category, and attachment
   routes and the Inventory item-reference changes.
5. Verify keyboard behaviour, dialog scrolling, multi-select selector operation,
   tab/list invalidation, and narrow desktop widths.
6. Map every criterion in `docs/requirements/HEALTH_REQUIREMENTS.md` to covering code
   and tests in a Health acceptance record.
7. Update `ROADMAP.md` to mark Health as implemented and accepted and record only
   intentional remaining deferrals.

Exit criteria:

- All relevant repository scripts pass, both providers are covered, the Compose
  journey succeeds, and every accepted Health requirement is implemented or explicitly
  deferred.

## Suggested Pull Request Boundaries

1. Health contracts, persistence, and module-owned catalogues (Waves 0-1).
2. Disease and medicine read and mutation APIs (Waves 2-3).
3. Disease-to-medicine association APIs (Wave 4).
4. Medicine attachments (Wave 5).
5. Medicine-to-Inventory link and deletion handling (Wave 6).
6. Frontend shell, diseases tab, and medicines tab with Configuration integration
   (Waves 7-8).
7. End-to-end, hardening, and acceptance (Wave 9).

## Plan Completion Criteria

The plan is complete when all Waves meet their exit criteria and the Health
requirements document describes implemented behaviour rather than only functional
intent.

Disease and medicine occurrences, intake schedules and reminders, attributes on the
disease-to-medicine association, disease attachments, a standard medical coding
system or curated drug catalogue, relating diseases or medicines to people, and
Analytics or Calendar integration remain separate future planning topics.
