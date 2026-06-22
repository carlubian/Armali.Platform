# Destinations Requirements

## Status

Phase 2 functional definition is complete. This document is the functional
source of truth for the initial Destinations implementation plan.

## Purpose

Destinations records the places the household has visited and the individual
spots inside them that are worth rating, such as hotels, restaurants, bars, or
museums. A destination is a visited place (a city, a region, a country, a natural
area); a place is a rateable spot located in one destination.

`Destinations` is the interface name of the module. Its primary REST resource is
`destinations` (`/api/destinations`), and each destination owns a managed
sub-resource of places (`/api/destinations/{destinationId}/places`).

Destinations is an independent business module. It introduces one
cross-business-module reference, but in the opposite direction to the previous
two: the existing Travel module gains an optional reference to a `Destination`.
The dependency direction is Travel to Destinations, following the same
contract-inversion pattern so Destinations never depends on Travel.

## Initial Scope

- Maintain a `Destination` collection with a name, a required category, an
  optional free-text country, optional free-text entry requirements, a Schengen
  Area flag, free-text notes, attachments with an optional primary image, and
  visibility.
- Let each destination own a managed collection of `Place` sub-resources, each
  with a name, a required category, an optional 1-5 rating, an optional free-text
  review, and an optional free-text address.
- Present the destination collection as a server-paginated thumbnail gallery with
  search, filters, and sorting, where cards surface a small European flag for
  Schengen-area destinations and the average rating of the destination's places.
- Present each destination's places on a dedicated, immersive page scoped to that
  destination, with its own search, filters, and sorting; places of different
  destinations are never shown together.
- Own the `DestinationCategory` and `PlaceCategory` catalogues, managed through
  Configuration with replace-only deletion.
- Replace Travel's free-text destination field with an optional live reference to
  a `Destination`, selected through the shared entity-selection components.

## Excluded Scope

The initial Destinations implementation excludes:

- A wishlist or "want to visit" state; the module records only known or visited
  places, not aspirations.
- Visit dates, visit counts, or trip history on the destination itself; a future
  internal reading of linked Travel trips may provide this.
- Ratings on destinations; only places carry a rating. The destination shows only
  a derived average of its places' ratings.
- Multiple ratings or a review history per place; a place carries one optional
  rating and one optional review.
- Attachments on places; only the destination carries attachments and a primary
  image.
- A geographic catalogue of countries, regions, or cities; country and address
  are free text.
- Map integration, geocoding, coordinates, or any outbound integration.
- Any launcher attention signal for the module.
- Analytics or Calendar integration.
- Spanish translations; the module ships English strings under an i18n namespace
  prepared for future translation.

## Destination

A `Destination` contains:

- A required name.
- A required `DestinationCategory` reference.
- An optional free-text country.
- Optional free-text entry requirements.
- A required `IsSchengenArea` boolean flag, defaulting to `false`.
- Optional free-text notes.
- Attachments with an optional primary image.
- Visibility.
- Standard ownership and audit metadata.

A destination has no lifecycle status. Destinations that are no longer wanted are
deleted.

### Schengen Area Flag

`IsSchengenArea` is a simple boolean that marks whether the destination is inside
the Schengen Area. It is purely descriptive, blocks no operation, and drives a
small European flag badge on the destination's gallery card. It is independent of
Travel's `TripType` catalogue, which is a separate, unrelated classification.

### Derived Place Rating

Each destination exposes a derived average of the ratings of its places, together
with the number of rated places. The average is computed on demand from the
current places and is never persisted. A destination with no rated places shows
no average. The derived average appears on the gallery card and on the places
page header.

### Image And Attachments

- A destination carries zero or more attachments through the shared platform
  attachment storage with the owner kind `Destination`, under the shared
  attachment size and policy bounds.
- One attachment may be marked as the primary image and is shown as the gallery
  thumbnail; a destination without a primary image falls back to its first image
  and then to a placeholder, following the Clothes, Assets, and Recipes pattern.
- Attachments are removed when their destination is deleted.

## Place

Each destination owns a managed collection of `Place` sub-resources. A `Place`
contains:

- A required free-text name, trimmed and non-whitespace.
- A required `PlaceCategory` reference.
- An optional rating, an integer from 1 to 5.
- An optional free-text review.
- An optional free-text address.
- An identifier and standard persistence metadata.

A destination contains between 0 and any number of places; a destination with no
places is valid. A place always belongs to exactly one destination and is never
shared between destinations.

Unlike Travel's embedded itinerary or a recipe's ingredient list, places are a
managed sub-resource edited individually (create, edit, delete) rather than
through full-collection replacement, because the user explores and filters them as
first-class records. Places have no attachments of their own and no rating
history; a place carries one optional rating and one optional review.

Places inherit the visibility and authorization of their owning destination. A
user who may access a destination may access and manage all of its places; a user
who may not access a destination cannot access any of its places, and the backend
returns the platform not-found behaviour so private destinations are not
disclosed.

## Travel Integration

Destinations replaces Travel's existing optional free-text `destination` field
with an optional live reference to one `Destination`.

- Travel consumes a narrow Destinations read contract to validate the referenced
  destination, resolve its display name and country, and evaluate accessibility
  for the current user. Destinations owns this contract.
- A `Public` trip may reference only `Public` destinations; a `Private` trip may
  reference any destination its creator can access. This mirrors the
  Maintenance-to-Assets and Recipes-to-Inventory visibility rule.
- When a destination cannot be resolved for the viewer, the trip shows a neutral
  placeholder instead of disclosing a private destination.
- The reference is optional, so a trip without a destination link is valid.

### Field Replacement And Migration

The free-text `destination` column on a trip is removed. Existing free-text
destination values are discarded rather than migrated into entities; the household
re-links trips to destinations manually as needed. This intentionally accepts the
loss of the current free-text values, which are minimal, in exchange for a clean
single representation of a trip's destination.

This supersedes the Travel requirement that a trip carries an optional free-text
destination. All other Travel behaviour is unchanged.

### Destination Deletion

Deleting a `Destination` referenced by trips is **not** blocked. Destinations
defines a deletion reference contract that consumers implement to report and
resolve references when a destination is deleted. Travel registers an
implementation that clears the destination link on every affected trip within the
deletion transaction. Destinations enumerates registered implementations during
deletion; it never queries Travel entities. The behaviour is implemented by
contract inversion so the dependency direction stays Travel to Destinations.
Impact reporting is privacy-neutral and never discloses another user's private
trips.

## Visibility And Authorization

Destinations use the platform-standard visibility values:

- `Public`
- `Private`

New destinations default to `Public`. The standard Segaris baseline applies:

- A user can view and edit their own destinations and public destinations.
- A private destination remains creator-only, including from administrators.
- Any authenticated user may edit a public destination.
- Only the creator may change a destination's visibility.

Places and attachments inherit the visibility and authorization of their owning
destination. These constraints are enforced by the backend regardless of the
client. Missing and inaccessible records share the platform not-found behaviour so
private data is not disclosed.

## Catalogues And Configuration Integration

Destinations owns two catalogues, presented alongside the other module-owned
catalogues through the established Configuration presentation boundary:

- `DestinationCategory`: a required name and an order. Because every destination
  requires a category, a referenced value may only be **replaced**; replacement
  re-points the affected destinations to the target value.
- `PlaceCategory`: a required name and an order. Because every place requires a
  category, a referenced value may only be **replaced**; replacement re-points the
  affected places to the target value.

Administrator CRUD, ordering, final-row protection, and the replacement dialog
with a privacy-neutral impact summary follow the established module-owned
catalogue pattern. The Schengen flag and the 1-5 rating scale are fixed, not
managed through Configuration.

Accepted initial catalogue values, seeded once:

- `DestinationCategory`: `City`, `Region`, `Country`, `Natural Area`, `Other`.
- `PlaceCategory`: `Hotel`, `Restaurant`, `Bar`, `Café`, `Museum`, `Attraction`,
  `Shop`, `Other`.

The one-time initialization behaviour matches the established Configuration
catalogue pattern: values are initialized once and are not reimposed after
administrative changes.

## Attention

Destinations contributes no launcher attention signal. The launcher card never
requests attention.

## Module Entry And Navigation

Opening Destinations shows the **destination collection** first, presented as a
server-paginated thumbnail gallery of accessible destinations. Each card shows the
primary image (or a placeholder), the name, the category, the country, a small
European flag when `IsSchengenArea` is true, and the derived average place rating
when present.

- Search matches the destination name.
- Filters cover category and the Schengen flag.
- Sorting covers name and category.
- Destinations are created, viewed, edited, and deleted through the established
  Segaris URL-aware popup pattern, so gallery state survives dialog open and close
  without a reload.

Each destination exposes a dedicated, immersive **places page** reached only from
that destination (from its gallery card and from its editor popup). The places
page is always scoped to a single `destinationId`; places of different
destinations are never shown together. The page presents the destination's places
as a server-paginated list with the destination context in its header (name,
country, derived average rating).

- Search matches the place name.
- Filters cover place category and rating.
- Sorting covers name, category, and rating.
- Places are created, edited, and deleted through URL-aware popups over the places
  page, so list state survives dialog open and close without a reload.

Indicative frontend route shapes:

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

## Validation

- Destination name is required, trimmed, not whitespace-only, and at most 200
  characters.
- The destination category reference is required and must exist.
- Country is optional, trimmed, and at most 200 characters.
- Entry requirements are optional and at most 2,000 characters.
- `IsSchengenArea` is a required boolean.
- Destination notes are optional and at most 2,000 characters.
- Attachments, when present, are within the shared attachment policy bounds; at
  most one attachment is the primary image.
- Place name is required, trimmed, not whitespace-only, and at most 200
  characters.
- The place category reference is required and must exist.
- The place rating is either absent or an integer from 1 to 5.
- Place review is optional and at most 2,000 characters.
- Place address is optional, trimmed, and at most 200 characters.
- Destination visibility is a known value.
- Catalogue names are required, trimmed, not whitespace-only, and at most 200
  characters.
- A trip's destination reference, when present, must exist and satisfy the
  visibility rule.

## Creation Defaults

A new destination starts with:

- Visibility `Public`.
- The first available destination category by `SortOrder`, then `Id`.
- `IsSchengenArea` equal to `false`.
- No country, entry requirements, notes, or attachments.

A new place starts with:

- The first available place category by `SortOrder`, then `Id`.
- No rating, review, or address.

## Deletion

Deletion is physical, immediate, and irreversible.

### Destination Deletion

Deleting a destination deletes the destination together with all of its places
and every attachment it owns, in one operation, and clears the destination link
on every Travel trip that references it through the deletion reference contract.

### Place Deletion

A place may be deleted individually. Deleting a place removes it and updates the
destination's derived average rating. It does not affect any other entity.

## Acceptance Criteria

The initial Destinations definition is satisfied when:

1. A `Destination` carries a required name, a required `DestinationCategory`, an
   optional free-text country, optional free-text entry requirements, a required
   `IsSchengenArea` flag defaulting to `false`, optional notes, attachments with
   an optional primary image, and visibility, with standard ownership and audit
   metadata, and has no lifecycle status.
2. A destination owns a managed collection of `Place` sub-resources, each with a
   required name, a required `PlaceCategory`, an optional 1-5 rating, an optional
   free-text review, and an optional free-text address, created, edited, and
   deleted individually, with a place always belonging to exactly one destination.
3. A destination exposes a derived average of its places' ratings and the count of
   rated places, computed on demand and never persisted, shown on the gallery card
   and the places page.
4. A destination carries zero or more attachments accepted under the shared
   policy, with at most one primary image used as the gallery thumbnail and a
   fallback to the first image and then a placeholder, all removed on destination
   deletion; places carry no attachments.
5. Destinations owns the `DestinationCategory` and `PlaceCategory` catalogues
   through Configuration, both required and replace-only, seeded with the accepted
   initial values, while the Schengen flag and the rating scale remain fixed.
6. Visibility follows the Segaris public-collaboration and private-isolation
   baseline, defaults to `Public`, is changed only by the creator, and is
   inherited by places and attachments; inaccessible records return the standard
   not-found behaviour.
7. Travel's free-text destination field is replaced by an optional live reference
   to a `Destination` under the visibility rule (a public trip references only
   public destinations; a private trip references any accessible destination),
   resolving the destination name with a neutral placeholder when it is not
   resolvable, with existing free-text values discarded on migration.
8. Deleting a `Destination` referenced by trips clears the link on every affected
   trip within the deletion transaction, never blocks deletion, reports impact
   privacy-neutrally, and is implemented by contract inversion so Destinations does
   not depend on Travel.
9. The launcher card never requests attention.
10. Destinations opens on a server-paginated destination gallery with name search,
    category and Schengen filters, name/category sorting, and a European flag badge
    for Schengen destinations, using URL-aware dialogs that preserve gallery state.
11. Each destination exposes a dedicated, immersive places page scoped to that
    destination, never mixing places of different destinations, with name search,
    category and rating filters, name/category/rating sorting, and URL-aware place
    dialogs that preserve list state.
12. SQLite and PostgreSQL migrations, backend unit/integration/architecture tests,
    frontend component tests, and a representative Playwright journey verify the
    supported behaviour and privacy boundaries.

## Deferred Decisions

- Whether destinations should later gain a wishlist or "want to visit" state and a
  corresponding launcher attention signal.
- Whether destinations should record visit dates, visit counts, or read linked
  Travel trips to derive a visit history.
- Whether destinations themselves should be rateable rather than only aggregating
  their places' ratings.
- Whether a place should support multiple ratings or a review history over time.
- Whether places should gain their own attachments.
- Whether country, region, or city should become a first-class geographic
  catalogue rather than free text.
- Whether the module should integrate maps, geocoding, or coordinates.
- Whether Destinations should publish read contracts to Analytics or a future
  Calendar module.
