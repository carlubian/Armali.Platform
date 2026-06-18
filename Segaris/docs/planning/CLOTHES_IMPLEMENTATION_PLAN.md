# Clothes Implementation Plan

## Purpose

This plan delivers the initial Clothes module defined in
`docs/requirements/CLOTHES_REQUIREMENTS.md`. It translates the accepted functional
decisions into dependency-ordered Waves with explicit backend, frontend,
migration, and test work.

The requirements document remains authoritative for behaviour. This plan owns
delivery order and technical scope.

## Delivery Principles

- Preserve Clothes as an independent business module.
- Reuse established Configuration, Attachments, privacy, REST, pagination, and
  frontend conventions where their semantics match.
- Do not introduce a dynamic laundry state, purchase or cost tracking, outfits,
  wear or wash history, a bleaching care axis, launcher attention, or
  cross-business-module dependencies.
- Keep visibility, the optional multi-valued colour relationship, and the fixed
  per-axis care values explicit in backend validation rather than inferred only by
  the frontend.
- Keep each Wave independently testable and record deferrals in `ROADMAP.md`
  instead of hiding them in implementation notes.

## Fixed Technical Contracts

### Backend Module

Clothes lives under `Segaris.Api.Modules.Clothes` and owns the garment, the
`ClothingCategory` and `ClothingColor` catalogues, the garment-colour
relationship, attachment authorization, and Configuration reference handling. It
registers no launcher attention contributor.

Indicative resource routes are:

```text
GET    /api/clothes/garments
POST   /api/clothes/garments
GET    /api/clothes/garments/{garmentId}
PUT    /api/clothes/garments/{garmentId}
DELETE /api/clothes/garments/{garmentId}

GET    /api/clothes/garments/{garmentId}/attachments
POST   /api/clothes/garments/{garmentId}/attachments
GET    /api/clothes/garments/{garmentId}/attachments/{attachmentId}
DELETE /api/clothes/garments/{garmentId}/attachments/{attachmentId}
PUT    /api/clothes/garments/{garmentId}/attachments/{attachmentId}/primary

GET    /api/clothes/categories
GET    /api/clothes/colors
```

Colours and care values are delivered as part of the garment representation and
are replaced through the garment create and update payloads; they have no
independent route. Administrative category and colour routes follow the existing
module-owned catalogue management pattern exposed through Configuration.

All writes require antiforgery. Missing and inaccessible records share the
platform not-found behaviour so private data is not disclosed.

### Persistence

The model contains module-owned entities with provider-specific migrations:

- `ClothesGarment`
- `ClothesGarmentColor` (garment-to-colour join)
- `ClothingCategory`
- `ClothingColor`

Garments store the category, status, optional size, the four optional care axis
values, visibility, notes, optional primary attachment reference, and standard
audit metadata. The garment-colour join stores the owning garment and the colour,
with a uniqueness constraint per pair. `ClothingColor` stores its name, colour
value, and order.

Colour associations and owned attachments are removed when their garment is
deleted. The initial model has no laundry-state column, no purchase or cost
columns, and no outfit table.

Indexes must support garment filters, deterministic sorting, colour filtering and
reference migration, and category reference migration.

### Frontend Route

Clothes uses the protected lazy route `/clothes`.

The initial UI presents a thumbnail gallery with URL-backed list state and dialog
state, following the Capex, Opex, Inventory, and Travel pattern. One practical
route shape is:

```text
/clothes
/clothes?garmentId=123
/clothes?newGarment=true
```

If the shared shell or router ergonomics suggest a slightly different parameter
layout, preserve the same behaviour: gallery state must survive dialog open and
close without a reload.

### Configuration Integration

Configuration presents the Clothes catalogues alongside the other module-owned
catalogues. Clothes owns `ClothingCategory` and `ClothingColor` while exposing
them through the established Configuration presentation boundary.

`ClothingColor` requires extending the catalogue contract and the Configuration
catalogue editor with an optional colour value, surfaced as a swatch and a colour
picker.

Clothes must register narrow catalogue reference handlers for:

- Category — required on every garment, so a referenced category may only be
  **replaced**; replacement re-points the affected garments.
- Colour — optional and multi-valued, so a referenced colour may be **replaced**
  (deduplicating when a garment already references the target) or **cleared**.

## Waves

### Wave 0: Contracts And Test Skeleton

Establish stable contracts before persistence or UI work begins.

Tasks:

1. Add the Clothes module shell and registration after Travel.
2. Freeze garment routes, the status enum, the four care axis enums and their
   fixed values, DTOs, query contracts, stable error codes, and attachment owner
   kind (`Garment`), including the primary-image action.
3. Define Configuration-facing contracts for category and colour reference
   handling without exposing Clothes entities, including the colour value field.
4. Define frontend API, validation-schema, route-state, and query-key skeletons.
5. Add architecture-test expectations for Clothes dependency direction: Clothes
   may consume Configuration and platform contracts but must not depend on Capex,
   Opex, Inventory, or Travel.
6. Add empty backend and frontend test fixtures shared by later Waves.

Tests:

- Unit tests for enum values, defaults, route constants, query bounds, and
  error-code stability.
- Architecture tests for permitted dependencies and published contracts.

Exit criteria:

- Public contracts are explicit and test-covered, and later Waves do not need to
  invent new route or ownership semantics.

### Wave 1: Domain, Persistence, And Catalogues

Implement the Clothes data model and module-owned catalogues on both providers.

Tasks:

1. Add `ClothesGarment`, `ClothesGarmentColor`, `ClothingCategory`, and
   `ClothingColor`.
2. Enforce the required category relationship, the optional multi-valued colour
   relationship with per-pair uniqueness, bounded strings, known status and care
   values, hex colour-value validation, and standard audit metadata.
3. Seed the accepted initial category and colour values once, including colour
   values.
4. Implement module-owned category and colour reads plus administrator mutations
   through Configuration, including ordering, the colour value field, and
   final-row protection.
5. Add provider-specific migrations and model snapshots.
6. Add indexes for garment filters, sorting, and category/colour reference
   migration.

Tests:

- Domain tests for status and care values, colour uniqueness, and hex validation.
- SQLite and PostgreSQL migration tests, including upgrades and idempotent
  catalogue initialization.
- Integration tests for category and colour ordering, the colour value field,
  uniqueness, final-row protection, and administrator authorization.

Exit criteria:

- Both providers persist the complete model and expose configurable category and
  colour catalogues, with colours carrying a validated colour value.

### Wave 2: Garment Read And Mutation APIs

Deliver the full garment backend contract.

Tasks:

1. Implement the paginated garment gallery query and the garment detail query,
   including colours, care values, and the resolved thumbnail attachment.
2. Implement partial search across name, size, and notes.
3. Implement exact filters for category, status, colour, visibility, and creator,
   and deterministic sorting with the default ordering (name ascending, then
   identifier ascending).
4. Implement create, update, and delete for garments with full validation, the
   documented creation defaults, colour-collection replacement with
   deduplication, and care-axis assignment.
5. Enforce category validity, visibility transitions, and standard
   public-collaboration and private-isolation authorization.
6. Cascade colour associations and owned attachments on garment deletion.

Tests:

- API integration tests for pagination, filters, search, sorting, defaults,
  required fields, colour replacement and deduplication, care assignment,
  visibility isolation, and not-found privacy behaviour.
- Two-user privacy tests for public collaboration and private isolation.

Exit criteria:

- Users can browse and fully manage accessible garments through the backend with
  correct validation and privacy.

### Wave 3: Attachments And Primary Image

Deliver garment attachments and the primary-image concept that drives the gallery
thumbnail.

Tasks:

1. Add garment attachment listing, upload, download, and delete routes using the
   shared attachment policies.
2. Implement marking one image attachment as the primary image and the thumbnail
   resolution rule (primary, then first image, then placeholder).
3. Keep the primary reference consistent when the referenced attachment is
   deleted, and clean up attachments on garment deletion.
4. Surface the resolved thumbnail reference in the gallery and detail
   projections.

Tests:

- Attachment tests for round-trip behaviour, authorization, validation failures,
  primary selection and fallback, and filesystem cleanup on garment deletion.

Exit criteria:

- Garments support multiple attachments with a correct, resilient primary-image
  resolution available to the frontend gallery.

### Wave 4: Configuration Reference Migration

Integrate Clothes safely into structural catalogue management.

Tasks:

1. Register reference handlers for Clothes categories and colours using the
   existing module-owned catalogue-management pattern.
2. Implement required-category replacement that re-points the affected garments.
3. Implement optional colour replacement-or-clear semantics, deduplicating colour
   associations on replacement and removing them on clear.
4. Re-evaluate references in the confirming transaction and roll back on any
   failure.
5. Provide privacy-neutral impact reporting that never discloses private
   garments.
6. Invalidate affected Clothes and Configuration frontend queries after
   successful structural mutations.

Tests:

- Cross-module integration tests for category replacement, colour replacement
  with deduplication, colour clearing, rollback, and privacy-neutral impact
  reporting.
- SQLite and PostgreSQL coverage for atomic migration behaviour.

Exit criteria:

- Configuration can safely mutate or delete every catalogue value referenced by
  Clothes without exposing private data or leaving partial updates behind.

### Wave 5: Clothes Frontend Gallery

Build the user-facing wardrobe experience.

Tasks:

1. Add the lazy `/clothes` route, module error boundary, translation namespace,
   and a launcher card with the neutral (no-attention) state.
2. Move the care-symbol icon assets from `docs/icons/` into the frontend and map
   them to the care axis values.
3. Build the thumbnail gallery with URL-backed search, filters, sorting, and
   bounded pagination, including colour swatches, the status, and the placeholder
   fallback.
4. Build the garment dialog with React Hook Form and Zod, covering name,
   category, status, size, the multi-select colour picker with swatches, the four
   care-axis selectors rendering the symbols, notes, visibility guards, and
   garment attachments with primary-image selection.
5. Wire deletion confirmation and list invalidation.

Tests:

- Frontend API and component tests for route state, filters, sorting, pagination,
  garment validation, colour multi-select, care-axis selection, privacy-safe
  errors, thumbnail resolution, and attachment flows.
- Accessibility tests for dialog focus, keyboard operation, error association, and
  the colour and care sub-editors.

Exit criteria:

- Users can complete the full Clothes workflow without page reloads while
  preserving gallery state.

### Wave 6: Clothes Configuration Frontend

Surface the Clothes catalogues in the Configuration experience.

Tasks:

1. Add the Clothes sections to the Configuration UI for `ClothingCategory` and
   `ClothingColor`, including reorder controls.
2. Extend the catalogue editor with the colour value, surfaced as a swatch and a
   colour picker, for `ClothingColor`.
3. Build the deletion and migration dialogs: replacement for categories, and
   replacement-or-clear for colours, with privacy-neutral impact summaries.
4. Invalidate the relevant Clothes and Configuration caches after structural
   mutations.

Tests:

- Component tests for category and colour CRUD, reorder, the colour value editor,
  and the replacement and replacement-or-clear dialogs.

Exit criteria:

- Administrators can manage both Clothes catalogues, including colour values and
  safe deletion, entirely through Configuration.

### Wave 7: End-To-End, Hardening, And Acceptance

Validate the implemented behaviour across both providers and the deployed
frontend/backend boundary.

Tasks:

1. Run backend format, build, unit, API, PostgreSQL, migration, and architecture
   suites through repository scripts.
2. Run frontend format, lint, type-check, unit, build, and Playwright suites.
3. Add a representative Playwright journey covering login, opening Clothes,
   creating a garment with a category, colours, and care values, uploading a photo
   and marking it primary, filtering the gallery, and deleting safe test data.
4. Review OpenAPI for Clothes garment, catalogue, and attachment routes.
5. Verify keyboard behaviour, dialog scrolling, filtered gallery invalidation, and
   narrow desktop widths.
6. Map every criterion in `docs/requirements/CLOTHES_REQUIREMENTS.md` to covering
   code and tests in a Clothes acceptance record.
7. Update `ROADMAP.md` to mark Clothes as implemented and accepted and to record
   only intentional remaining deferrals.

Exit criteria:

- All relevant repository scripts pass, both providers are covered, the Compose
  journey succeeds, and every accepted Clothes requirement is implemented or
  explicitly deferred.

## Suggested Pull Request Boundaries

1. Clothes contracts, persistence, and module-owned catalogues (Waves 0-1).
2. Garment read, mutation, and attachment APIs (Waves 2-3).
3. Configuration reference migration (Wave 4).
4. Clothes gallery and Configuration frontend (Waves 5-6).
5. End-to-end, hardening, and acceptance (Wave 7).

## Plan Completion Criteria

The plan is complete when all Waves meet their exit criteria and the Clothes
requirements document describes implemented behaviour rather than only functional
intent.

A dynamic laundry state, purchase and cost tracking, brand/material/season
attributes, outfits and sets, wear and wash history, a bleaching care axis,
launcher attention, Analytics integration, and cross-module links remain separate
future planning topics.
