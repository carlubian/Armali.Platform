# Destinations Implementation Plan

## Purpose

This plan delivers the initial Destinations module defined in
`docs/requirements/DESTINATIONS_REQUIREMENTS.md`. It translates the accepted
functional decisions into dependency-ordered Waves with explicit backend,
frontend, migration, and test work.

The requirements document remains authoritative for behaviour. This plan owns
delivery order and technical scope.

## Delivery Principles

- Preserve Destinations as an independent business module. Its only
  cross-business-module relationship is that the existing Travel module gains an
  optional reference to a `Destination`; the dependency direction is Travel to
  Destinations.
- Reuse established Configuration, Attachments, privacy, REST, pagination,
  entity-selection, and frontend conventions where their semantics match.
- Reuse the shared `EntityReferenceField` and `EntitySelectorDialog` with a thin
  per-entity adapter for the Travel trip-to-destination link, without forking the
  selector or introducing a generic backend association model.
- Implement the Travel destination-deletion handling by contract inversion so the
  dependency direction stays Travel to Destinations. Destination deletion clears
  the trip link; it never reassigns and never blocks.
- Keep the visibility rules, the trip reference rule, and the destination-deletion
  behaviour explicit in backend validation rather than inferred only by the
  frontend.
- Do not introduce a wishlist, visit dates, destination ratings, place
  attachments, place rating history, a geographic catalogue, map integration,
  launcher attention, or any other cross-module dependency.
- Keep each Wave independently testable and record deferrals in `ROADMAP.md`
  instead of hiding them in implementation notes.

## Fixed Technical Contracts

### Backend Module

Destinations lives under `Segaris.Api.Modules.Destinations` and owns the
destination, the place sub-resource collection, the `DestinationCategory` and
`PlaceCategory` catalogues, destination attachment authorization, the Destinations
read contract consumed by Travel, and the destination-deletion reference contract
that Travel implements. It consumes only the Configuration presentation boundary
and platform contracts. It does not depend on any business module, including
Travel, and it contributes no launcher attention.

Indicative resource routes are:

```text
GET    /api/destinations
POST   /api/destinations
GET    /api/destinations/{destinationId}
PUT    /api/destinations/{destinationId}
DELETE /api/destinations/{destinationId}

GET    /api/destinations/{destinationId}/attachments
POST   /api/destinations/{destinationId}/attachments
GET    /api/destinations/{destinationId}/attachments/{attachmentId}
DELETE /api/destinations/{destinationId}/attachments/{attachmentId}
PUT    /api/destinations/{destinationId}/attachments/{attachmentId}/primary

GET    /api/destinations/{destinationId}/places
POST   /api/destinations/{destinationId}/places
GET    /api/destinations/{destinationId}/places/{placeId}
PUT    /api/destinations/{destinationId}/places/{placeId}
DELETE /api/destinations/{destinationId}/places/{placeId}

GET    /api/destinations/categories
GET    /api/destinations/place-categories
```

Administrative category routes follow the existing module-owned catalogue
management pattern exposed through Configuration. All writes require antiforgery.
Missing and inaccessible records share the platform not-found behaviour so private
data is not disclosed. Place routes are always scoped to their owning destination
and never return places across destinations.

### Persistence

The model contains module-owned entities with provider-specific migrations:

- `Destination`
- `Place`
- `DestinationCategory`
- `PlaceCategory`

Destinations store the name, category reference, optional country, optional entry
requirements, the `IsSchengenArea` flag, notes, visibility, and standard audit
metadata. Places store the owning destination, the free-text name, the category
reference, the optional 1-5 rating, the optional review, the optional address, and
standard metadata. Owned places and attachments are removed when their destination
is deleted.

The derived average place rating is computed on demand from the destination's
places and is never persisted.

Indexes must support destination filters and deterministic sorting (name, then
identifier), the destination and place category reference migrations, the
place lookup and filtering scoped by destination, and the trip-reference lookup
used by the deletion handler on the Travel side.

### Frontend Routes

Destinations uses the protected lazy route `/destinations` for the gallery and an
internal, destination-scoped places surface under `/destinations/{destinationId}/places`.

The destination collection is a server-paginated thumbnail gallery with URL-backed
list state and dialog state, following the Clothes, Assets, and Recipes pattern.
The places page is a server-paginated list scoped to one destination with
URL-backed list state and dialog state. One practical route shape is:

```text
/destinations
/destinations?destinationId=123
/destinations?newDestination=true
/destinations/123/places
/destinations/123/places?placeId=45
/destinations/123/places?newPlace=true
```

If the shared shell or router ergonomics suggest a slightly different parameter
layout, preserve the same behaviour: gallery and places-list state must survive
dialog open and close without a reload, and the places page must never leave its
destination scope.

### Travel Integration

Destinations owns a narrow read contract that Travel consumes to validate a
destination reference, resolve its display name and country, and evaluate
accessibility and visibility for the visibility rule.

Destinations additionally defines a deletion reference contract that consumers
implement to report and resolve references when a destination is deleted. Travel
registers an implementation that clears the destination link on all referencing
trips within the deletion transaction. Destinations enumerates registered
implementations during deletion; it never queries Travel entities. This mirrors
the existing Inventory-to-Recipes and Assets-to-Maintenance inversion patterns,
with Destinations playing the role of the owning module whose entity is deleted.

Travel changes are part of this plan: removing the free-text `destination` column
with a discarding migration, adding the optional destination reference and its
visibility rule, consuming the read contract, implementing the deletion handler,
and adding the `DestinationEntitySelector` frontend adapter.

### Configuration Integration

Configuration presents the Destinations catalogues alongside the other
module-owned catalogues. Destinations owns `DestinationCategory` and
`PlaceCategory` while exposing them through the established Configuration
presentation boundary. Because a category is required on every destination and
every place, a referenced value may only be **replaced**; replacement re-points the
affected records to the target value.

## Waves

### Wave 0: Contracts And Test Skeleton

Establish stable contracts before persistence or UI work begins.

Tasks:

1. Add the Destinations module shell and registration after Recipes.
2. Freeze destination and place routes, the rating bounds (1-5) and the Schengen
   flag as fixed (non-catalogue) concepts, DTOs, query contracts, stable error
   codes, the attachment owner kind (`Destination`), and the absence of any
   launcher attention key.
3. Define Configuration-facing contracts for `DestinationCategory` and
   `PlaceCategory` reference handling without exposing Destinations entities.
4. Define the Destinations read contract and the destination-deletion reference
   contract that Travel will consume and implement, owned by Destinations.
5. Define frontend API, validation-schema, route-state, and query-key skeletons for
   both the destination gallery and the destination-scoped places surface.
6. Add architecture-test expectations: Destinations may consume Configuration and
   platform contracts but must not depend on Capex, Opex, Inventory, Travel,
   Assets, Maintenance, Projects, Processes, Firebird, Clothes, Mood, or Recipes,
   and Destinations must not depend on Travel; Travel may depend on Destinations.
7. Add empty backend and frontend test fixtures shared by later Waves.

Tests:

- Unit tests for rating bounds, route constants, query bounds, and error-code
  stability.
- Architecture tests for permitted dependencies, the Destinations-to-Travel
  non-dependency, and published contracts.

Exit criteria:

- Public contracts are explicit and test-covered, and later Waves do not need to
  invent new route, ownership, or cross-module semantics.

### Wave 1: Domain, Persistence, And Catalogues

Implement the Destinations data model and module-owned catalogues on both
providers.

Tasks:

1. Add `Destination`, `Place`, `DestinationCategory`, and `PlaceCategory`.
2. Enforce the required category relationships, bounded strings, the Schengen flag,
   the optional 1-5 rating, the place-to-destination ownership, and visibility and
   audit metadata.
3. Seed the accepted initial category values once for both catalogues.
4. Implement module-owned category reads plus administrator mutations through
   Configuration for both catalogues, including ordering and final-row protection.
5. Add provider-specific migrations and model snapshots.
6. Add indexes for destination filters and sorting, the two category reference
   migrations, and the place lookup and filtering scoped by destination.

Tests:

- Domain tests for rating bounds, the Schengen flag, place ownership, and
  deterministic ordering.
- SQLite and PostgreSQL migration tests, including upgrades and idempotent
  catalogue initialization.
- Integration tests for category ordering, final-row protection, and administrator
  authorization for both catalogues.

Exit criteria:

- Both providers persist the complete model and expose two configurable category
  catalogues.

### Wave 2: Destination Read And Mutation APIs

Deliver the core destination backend contract, excluding places and attachments.

Tasks:

1. Implement the paginated destination gallery query and the destination detail
   query, including the derived average place rating and rated-place count
   projection.
2. Implement partial search across the destination name.
3. Implement exact filters for category and the Schengen flag and deterministic
   sorting with the default ordering (name ascending, then identifier ascending).
4. Implement create, update, and delete for destinations with full validation and
   the documented creation defaults (country, entry requirements, Schengen flag,
   notes).
5. Enforce category validity, visibility transitions, creator-only visibility
   change, and standard public-collaboration and private-isolation authorization.

Tests:

- API integration tests for pagination, filters, search, sorting, defaults,
  required fields, the derived-average projection, visibility isolation, and
  not-found privacy behaviour.
- Two-user privacy tests for public collaboration and private isolation.

Exit criteria:

- Users can browse and fully manage accessible destinations through the backend
  with correct validation and privacy, without places or attachments.

### Wave 3: Place Sub-Resource APIs

Add the destination-scoped place sub-resource and the derived average rating.

Tasks:

1. Implement the destination-scoped paginated place list query and the place detail
   query, always bounded to a single owning destination.
2. Implement partial search across the place name and exact filters for place
   category and rating, with deterministic sorting by name, category, and rating.
3. Implement create, update, and delete for places with full validation and the
   documented creation defaults (category, no rating/review/address).
4. Recompute and expose the destination's derived average place rating and
   rated-place count after place mutations.
5. Inherit destination visibility and authorization for all place operations and
   return not-found for inaccessible destinations.

Tests:

- API integration tests for scoped listing, pagination, filters, search, sorting,
  defaults, rating bounds, the derived-average recomputation, visibility
  inheritance, and not-found privacy behaviour.
- Tests confirming place routes never return places from another destination.

Exit criteria:

- Users can fully manage a destination's places under destination scope and
  visibility, and the derived average reflects place changes.

### Wave 4: Destination Attachments

Deliver destination attachments with a primary image.

Tasks:

1. Add destination attachment listing, upload, download, delete, and set-primary
   routes using the shared attachment policies.
2. Inherit destination visibility and authorization for attachments.
3. Implement the gallery thumbnail resolution (primary image, then first image,
   then placeholder).
4. Clean up attachments on destination deletion.

Tests:

- Attachment tests for round-trip behaviour, primary-image selection, thumbnail
  fallback, authorization, validation failures, and filesystem cleanup on
  destination deletion.

Exit criteria:

- Destinations support multiple attachments with an optional primary image that
  respect destination visibility and are cleaned up on deletion.

### Wave 5: Travel Cross-Module Integration

Wire the optional Travel trip-to-destination reference through contract inversion.

Tasks:

1. Expose the Destinations read contract (validate, resolve name and country,
   evaluate accessibility and visibility) and the destination-deletion reference
   contract, both owned by Destinations.
2. In Travel, remove the free-text `destination` column with a discarding
   provider-specific migration on both providers.
3. In Travel, add the optional `destinationId` reference, consume the read
   contract, and enforce the visibility rule on create, update, visibility change,
   and destination change: a public trip may reference only public destinations; a
   private trip may reference any destination its creator can access.
4. Surface the resolved destination name in the trip projection, with a neutral
   placeholder when the destination is not resolvable for the viewer.
5. Implement the deletion reference contract in Travel: report the number of trips
   referencing a destination and clear the link on all of them within the deletion
   transaction.
6. Wire Destinations deletion to enumerate registered reference contracts, evaluate
   impact, perform the clearing atomically, and roll back on any failure, with
   privacy-neutral impact reporting that never discloses other users' private trips.

Tests:

- API integration tests for linking, unlinking, the visibility rule and its
  rejection paths, name resolution, and the neutral placeholder.
- Cross-module integration tests for clearing links across mixed-ownership trips,
  rollback, and privacy-neutral impact reporting.
- SQLite and PostgreSQL coverage for the discarding migration and the atomic
  delete-and-clear behaviour.
- Architecture tests confirming Destinations gains no dependency on Travel and that
  Travel depends on Destinations.

Exit criteria:

- Trips can reference an accessible destination under the visibility rule, the link
  never discloses a private destination, and a referenced destination can be
  deleted with its trip links cleared atomically while the dependency direction is
  preserved.

### Wave 6: Destinations Frontend Gallery

Build the user-facing destination collection experience.

Tasks:

1. Add the lazy `/destinations` route, module error boundary, translation
   namespace, and a launcher card with no attention state.
2. Build the server-paginated thumbnail gallery with URL-backed name search,
   category and Schengen filters, name/category sorting, and bounded pagination,
   with cards showing the primary image or placeholder, name, category, country, a
   European flag badge when Schengen, and the derived average rating when present.
3. Build the destination dialog with React Hook Form and Zod, covering name,
   category, country, entry requirements, the Schengen flag, notes, visibility
   guards, and destination attachments with primary-image selection.
4. Add an action that navigates from a destination (card and editor) to its
   destination-scoped places page.
5. Add the Destinations sections to the Configuration UI for `DestinationCategory`
   and `PlaceCategory`, including reorder controls and the replacement dialog with
   a privacy-neutral impact summary.
6. Wire deletion confirmation and list and Configuration cache invalidation.

Tests:

- Frontend API and component tests for route state, filters, sorting, pagination,
  destination validation, the Schengen badge, the derived-average display,
  privacy-safe errors, attachment flows, and category CRUD and reorder.
- Accessibility tests for dialog focus, keyboard operation, and error association.

Exit criteria:

- Users can complete the full destination workflow without page reloads while
  preserving gallery state, and manage both catalogues through the UI.

### Wave 7: Destinations Frontend Places Page

Build the immersive, destination-scoped places page.

Tasks:

1. Build the `/destinations/{destinationId}/places` page as a server-paginated list
   scoped to one destination, with a header showing the destination context (name,
   country, derived average rating) and URL-backed list and dialog state.
2. Implement name search, place category and rating filters, and name/category/
   rating sorting on the page, never mixing places of different destinations.
3. Build the place dialog (name, category, rating, review, address) with React Hook
   Form and Zod, inheriting destination visibility, with individual create, edit,
   and delete.
4. Reflect place mutations in the destination's derived average on this page and in
   the gallery cache.
5. Wire entry from the gallery card and editor, and a clear path back to the
   gallery.

Tests:

- Component tests for scoped listing, route state, filters, sorting, pagination,
  place validation, the derived-average update, and that the page never leaves its
  destination scope.
- Accessibility tests for dialog focus, keyboard operation, and error association.

Exit criteria:

- Users can explore and fully manage a destination's places on a dedicated
  immersive page that stays scoped to that destination and preserves list state.

### Wave 8: Travel Frontend Integration

Surface the trip-to-destination link and the deletion impact in the frontend.

Tasks:

1. Add a `DestinationEntitySelector` adapter over the shared entity-selection
   components, constrained by the trip visibility rule.
2. Replace Travel's free-text destination input in the trip dialog with the
   destination selector, showing the resolved destination name and a neutral
   placeholder when unresolvable.
3. Extend the Destinations deletion flow to present the privacy-neutral
   trip-reference impact and confirm the link-clearing outcome.
4. Invalidate the relevant Travel and Destinations caches after structural
   mutations.

Tests:

- Component tests for the destination selector constrained by visibility, the
  resolved display and placeholder, the deletion-impact confirmation, and cache
  invalidation.

Exit criteria:

- Trips select a destination through the shared selector under the visibility rule,
  and deleting a referenced destination shows a privacy-neutral impact before
  clearing the trip links.

### Wave 9: End-To-End, Hardening, And Acceptance

Validate the implemented behaviour across both providers and the deployed
frontend/backend boundary.

Tasks:

1. Run backend format, build, unit, API, PostgreSQL, migration, and architecture
   suites through repository scripts.
2. Run frontend format, lint, type-check, unit, build, and Playwright suites.
3. Add a representative Playwright journey covering login, opening Destinations,
   creating a destination (with a category, country, entry requirements, and the
   Schengen flag), attaching and selecting a primary image, navigating to its
   places page, adding rated places and observing the derived average, linking a
   Travel trip to the destination, and deleting the destination to confirm the trip
   link is cleared; plus deleting safe test data.
4. Review OpenAPI for Destinations destination, place, category, and attachment
   routes and the Travel destination-reference changes.
5. Verify keyboard behaviour, dialog scrolling, filtered gallery invalidation,
   places-page scoping, and narrow desktop widths.
6. Map every criterion in `docs/requirements/DESTINATIONS_REQUIREMENTS.md` to
   covering code and tests in a Destinations acceptance record.
7. Update `ROADMAP.md` to mark Destinations as implemented and accepted, record the
   Travel field replacement as completed, and record only intentional remaining
   deferrals.

Exit criteria:

- All relevant repository scripts pass, both providers are covered, the Compose
  journey succeeds, and every accepted Destinations requirement is implemented or
  explicitly deferred.

## Suggested Pull Request Boundaries

1. Destinations contracts, persistence, and module-owned catalogues (Waves 0-1).
2. Destination read and mutation APIs and place sub-resource APIs (Waves 2-3).
3. Destination attachments (Wave 4).
4. Travel cross-module integration (Wave 5).
5. Destinations gallery, places page, and Configuration integration (Waves 6-7).
6. Travel frontend integration (Wave 8).
7. End-to-end, hardening, and acceptance (Wave 9).

## Plan Completion Criteria

The plan is complete when all Waves meet their exit criteria and the Destinations
requirements document describes implemented behaviour rather than only functional
intent.

A wishlist or "want to visit" state, visit dates and history, destination
ratings, place attachments, place rating history, a geographic catalogue, map or
geocoding integration, launcher attention, and Analytics or Calendar integration
remain separate future planning topics.
