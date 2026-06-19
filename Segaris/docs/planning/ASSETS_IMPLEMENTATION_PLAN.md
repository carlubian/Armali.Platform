# Assets Implementation Plan

## Purpose

This plan delivers the initial Assets module defined in
`docs/requirements/ASSETS_REQUIREMENTS.md`. It translates the accepted functional
decisions into dependency-ordered Waves with explicit backend, frontend,
migration, and test work.

The requirements document remains authoritative for behaviour. This plan owns
delivery order and technical scope.

## Delivery Principles

- Preserve Assets as an independent business module.
- Reuse established Configuration, Attachments, privacy, REST, pagination,
  launcher-attention, and frontend conventions where their semantics match.
- Do not introduce stock, monetary value, cost, maintenance history,
  depreciation, multiple locations, or cross-business-module dependencies.
- Keep visibility, the required category and location references, the optional
  unique code, and the launcher-attention rule explicit in backend validation
  rather than inferred only by the frontend.
- Keep each Wave independently testable and record deferrals in `ROADMAP.md`
  instead of hiding them in implementation notes.

## Fixed Technical Contracts

### Backend Module

Assets lives under `Segaris.Api.Modules.Assets` and owns the asset, the
`AssetCategory` and `AssetLocation` catalogues, attachment authorization, the
launcher-attention contributor, and Configuration reference handling. It does not
depend on any other business module.

Indicative resource routes are:

```text
GET    /api/assets/assets
POST   /api/assets/assets
GET    /api/assets/assets/{assetId}
PUT    /api/assets/assets/{assetId}
DELETE /api/assets/assets/{assetId}

GET    /api/assets/assets/{assetId}/attachments
POST   /api/assets/assets/{assetId}/attachments
GET    /api/assets/assets/{assetId}/attachments/{attachmentId}
DELETE /api/assets/assets/{assetId}/attachments/{attachmentId}
PUT    /api/assets/assets/{assetId}/attachments/{attachmentId}/primary

GET    /api/assets/categories
GET    /api/assets/locations
```

If the doubled `assets/assets` segment is undesirable, the resource may be exposed
as `/api/assets/items` while preserving the same behaviour; the module slug and
the resource noun are fixed during Wave 0 and used consistently thereafter.

Administrative category and location routes follow the existing module-owned
catalogue management pattern exposed through Configuration.

All writes require antiforgery. Missing and inaccessible records share the
platform not-found behaviour so private data is not disclosed.

### Persistence

The model contains module-owned entities with provider-specific migrations:

- `Asset`
- `AssetCategory`
- `AssetLocation`

Assets store the category, location, status, optional unique code, optional
brand/model, optional serial number, optional acquisition date, optional expected
end of life, visibility, notes, optional primary attachment reference, and
standard audit metadata. `AssetCategory` and `AssetLocation` each store a name and
an order.

Owned attachments are removed when their asset is deleted. The initial model has
no stock, value, cost, or maintenance columns.

Indexes must support asset filters, deterministic sorting, the expected-end-of-life
attention query, the case-insensitive code uniqueness constraint, and
category/location reference migration.

### Frontend Route

Assets uses the protected lazy route `/assets`.

The initial UI presents a server-paginated table with a thumbnail column, with
URL-backed list state and dialog state, following the Capex, Opex, Inventory,
Travel, and Clothes pattern. One practical route shape is:

```text
/assets
/assets?assetId=123
/assets?newAsset=true
```

If the shared shell or router ergonomics suggest a slightly different parameter
layout, preserve the same behaviour: table state must survive dialog open and
close without a reload.

### Configuration Integration

Configuration presents the Assets catalogues alongside the other module-owned
catalogues. Assets owns `AssetCategory` and `AssetLocation` while exposing them
through the established Configuration presentation boundary.

Assets must register narrow catalogue reference handlers for both catalogues.
Because a category and a location are required on every asset, a referenced value
of either catalogue may only be **replaced**; replacement re-points the affected
assets to the target value.

## Waves

### Wave 0: Contracts And Test Skeleton

Establish stable contracts before persistence or UI work begins.

Tasks:

1. Add the Assets module shell and registration after the most recent module.
2. Freeze asset routes, the status enum and its fixed values, DTOs, query
   contracts, stable error codes, the attachment owner kind (`Asset`) including
   the primary-image action, and the launcher attention key.
3. Define Configuration-facing contracts for category and location reference
   handling without exposing Assets entities.
4. Define frontend API, validation-schema, route-state, and query-key skeletons.
5. Add architecture-test expectations for Assets dependency direction: Assets may
   consume Configuration and platform contracts but must not depend on Capex,
   Opex, Inventory, Travel, or Clothes.
6. Add empty backend and frontend test fixtures shared by later Waves.

Tests:

- Unit tests for enum values, defaults, route constants, query bounds, and
  error-code stability.
- Architecture tests for permitted dependencies and published contracts.

Exit criteria:

- Public contracts are explicit and test-covered, and later Waves do not need to
  invent new route or ownership semantics.

### Wave 1: Domain, Persistence, And Catalogues

Implement the Assets data model and module-owned catalogues on both providers.

Tasks:

1. Add `Asset`, `AssetCategory`, and `AssetLocation`.
2. Enforce the required category and location relationships, bounded strings,
   known status and visibility values, the optional case-insensitive unique code,
   and standard audit metadata.
3. Seed the accepted initial category and location values once.
4. Implement module-owned category and location reads plus administrator
   mutations through Configuration, including ordering and final-row protection.
5. Add provider-specific migrations and model snapshots.
6. Add indexes for asset filters, sorting, the attention query, code uniqueness,
   and category/location reference migration.

Tests:

- Domain tests for status and visibility values and code uniqueness
  normalisation.
- SQLite and PostgreSQL migration tests, including upgrades and idempotent
  catalogue initialization.
- Integration tests for category and location ordering, final-row protection, and
  administrator authorization.

Exit criteria:

- Both providers persist the complete model and expose configurable category and
  location catalogues.

### Wave 2: Asset Read And Mutation APIs

Deliver the full asset backend contract.

Tasks:

1. Implement the paginated asset table query and the asset detail query,
   including the resolved thumbnail attachment.
2. Implement partial search across name, code, brand/model, serial number, and
   notes.
3. Implement exact filters for category, location, status, visibility, and
   creator, and deterministic sorting with the default ordering (name ascending,
   then identifier ascending).
4. Implement create, update, and delete for assets with full validation, the
   documented creation defaults, and code-uniqueness enforcement.
5. Enforce category and location validity, visibility transitions, and standard
   public-collaboration and private-isolation authorization.
6. Cascade owned attachments on asset deletion.

Tests:

- API integration tests for pagination, filters, search, sorting, defaults,
  required fields, code uniqueness, visibility isolation, and not-found privacy
  behaviour.
- Two-user privacy tests for public collaboration and private isolation.

Exit criteria:

- Users can browse and fully manage accessible assets through the backend with
  correct validation and privacy.

### Wave 3: Attachments And Primary Image

Deliver asset attachments and the primary-image concept that drives the table
thumbnail.

Tasks:

1. Add asset attachment listing, upload, download, and delete routes using the
   shared attachment policies.
2. Implement marking one image attachment as the primary image and the thumbnail
   resolution rule (primary, then first image, then placeholder).
3. Keep the primary reference consistent when the referenced attachment is
   deleted, and clean up attachments on asset deletion.
4. Surface the resolved thumbnail reference in the table and detail projections.

Tests:

- Attachment tests for round-trip behaviour, authorization, validation failures,
  primary selection and fallback, and filesystem cleanup on asset deletion.

Exit criteria:

- Assets support multiple attachments with a correct, resilient primary-image
  resolution available to the frontend table.

### Wave 4: Launcher Attention

Deliver the expected-end-of-life launcher attention.

Tasks:

1. Implement the Assets attention contributor: attention is true when the current
   user can access at least one non-`Retired` asset whose `ExpectedEndOfLifeDate`
   falls within the inclusive window from today to today plus 30 natural days in
   `Europe/Madrid`.
2. Reuse the established launcher attention aggregation and accessibility rules so
   only accessible assets count and private assets are never disclosed.
3. Expose the boolean attention state through the launcher contract.

Tests:

- Integration tests for the attention window boundaries (today, today plus 30,
  already-past, and far-future dates), `Retired` exclusion, and accessibility
  filtering across two users.

Exit criteria:

- Assets attention is true exactly under the documented condition and never leaks
  private data.

### Wave 5: Configuration Reference Migration

Integrate Assets safely into structural catalogue management.

Tasks:

1. Register reference handlers for Assets categories and locations using the
   existing module-owned catalogue-management pattern.
2. Implement required-reference replacement that re-points the affected assets for
   both catalogues, rejecting the clearing path.
3. Re-evaluate references in the confirming transaction and roll back on any
   failure.
4. Provide privacy-neutral impact reporting that never discloses private assets.
5. Invalidate affected Assets and Configuration frontend queries after successful
   structural mutations.

Tests:

- Cross-module integration tests for category and location replacement, rejection
  of clearing, rollback, and privacy-neutral impact reporting.
- SQLite and PostgreSQL coverage for atomic migration behaviour.

Exit criteria:

- Configuration can safely mutate or delete every catalogue value referenced by
  Assets without exposing private data or leaving partial updates behind.

### Wave 6: Assets Frontend Table

Build the user-facing assets experience.

Tasks:

1. Add the lazy `/assets` route, module error boundary, translation namespace,
   and a launcher card wired to the attention state.
2. Build the server-paginated table with the thumbnail column and URL-backed
   search, filters, sorting, and bounded pagination, including the placeholder
   fallback and the expected-end-of-life column.
3. Build the asset dialog with React Hook Form and Zod, covering name, category,
   location, status, code, brand/model, serial number, acquisition date, expected
   end of life (labelled "Expected end of life"), notes, visibility guards, and
   asset attachments with primary-image selection.
4. Wire deletion confirmation and list invalidation.

Tests:

- Frontend API and component tests for route state, filters, sorting, pagination,
  asset validation, code uniqueness errors, privacy-safe errors, thumbnail
  resolution, and attachment flows.
- Accessibility tests for dialog focus, keyboard operation, and error
  association.

Exit criteria:

- Users can complete the full Assets workflow without page reloads while
  preserving table state.

### Wave 7: Assets Configuration Frontend

Surface the Assets catalogues in the Configuration experience.

Tasks:

1. Add the Assets sections to the Configuration UI for `AssetCategory` and
   `AssetLocation`, including reorder controls.
2. Build the deletion and migration dialogs: replacement for both catalogues,
   with privacy-neutral impact summaries.
3. Invalidate the relevant Assets and Configuration caches after structural
   mutations.

Tests:

- Component tests for category and location CRUD, reorder, and the replacement
  dialogs.

Exit criteria:

- Administrators can manage both Assets catalogues, including safe deletion,
  entirely through Configuration.

### Wave 8: End-To-End, Hardening, And Acceptance

Validate the implemented behaviour across both providers and the deployed
frontend/backend boundary.

Tasks:

1. Run backend format, build, unit, API, PostgreSQL, migration, and architecture
   suites through repository scripts.
2. Run frontend format, lint, type-check, unit, build, and Playwright suites.
3. Add a representative Playwright journey covering login, opening Assets,
   creating an asset with a category, location, code, and expected end of life,
   uploading a photo and marking it primary, filtering the table, and deleting
   safe test data.
4. Review OpenAPI for Assets asset, catalogue, and attachment routes.
5. Verify keyboard behaviour, dialog scrolling, filtered table invalidation, and
   narrow desktop widths.
6. Map every criterion in `docs/requirements/ASSETS_REQUIREMENTS.md` to covering
   code and tests in an Assets acceptance record.
7. Update `ROADMAP.md` to mark Assets as implemented and accepted and to record
   only intentional remaining deferrals.

Exit criteria:

- All relevant repository scripts pass, both providers are covered, the Compose
  journey succeeds, and every accepted Assets requirement is implemented or
  explicitly deferred.

## Suggested Pull Request Boundaries

1. Assets contracts, persistence, and module-owned catalogues (Waves 0-1).
2. Asset read, mutation, and attachment APIs (Waves 2-3).
3. Launcher attention and Configuration reference migration (Waves 4-5).
4. Assets table and Configuration frontend (Waves 6-7).
5. End-to-end, hardening, and acceptance (Wave 8).

## Plan Completion Criteria

The plan is complete when all Waves meet their exit criteria and the Assets
requirements document describes implemented behaviour rather than only functional
intent.

Monetary value, a Capex purchase link, maintenance and repair history, warranty
as a first-class concept, multiple or historical locations, system-assisted code
generation, Analytics integration, and other cross-module links remain separate
future planning topics.
