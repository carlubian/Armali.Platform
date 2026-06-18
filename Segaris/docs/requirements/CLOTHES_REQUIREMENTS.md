# Clothes Requirements

## Status

Phase 2 functional definition is complete. This document is the functional
source of truth for the initial Clothes implementation plan.

## Purpose

Clothes manages the household wardrobe: the garments and accessories the
household owns, with descriptive attributes and textile care instructions.

The initial module is intentionally simple. It is an enriched catalogue of the
wardrobe: each garment carries a category, an optional size, optional colours,
optional static care instructions, free notes, and photos. It does not track a
dynamic laundry state (clean, dirty, in the wash), does not track purchase or
cost information, and does not group garments into outfits or sets.

## Initial Scope

- Manage garments and accessories as a single `Garment` entity distinguished by
  category.
- Describe each garment with a name, a category, an optional size, zero or more
  colours, optional textile care instructions, optional notes, and attachments.
- Organise garments through a Clothes-owned `ClothingCategory` catalogue.
- Assign garments zero or more colours from a Clothes-owned `ClothingColor`
  catalogue whose values carry both a name and a colour value.
- Present the wardrobe as a thumbnail gallery with search, filters, and a
  URL-aware popup editor.
- Keep Clothes independent from Capex, Opex, Inventory, Travel, Assets, and other
  business modules in the initial release.

## Excluded Scope

The initial Clothes implementation excludes:

- Dynamic laundry state (clean, dirty, in the wash) and any colada workflow.
- Purchase date, price, cost, supplier, or any link to Capex.
- Brand, material/fabric, and season attributes.
- Outfits, sets, lookbooks, or garment-to-garment grouping.
- Wear counts, wash logs, or any per-garment history sub-resource.
- A bleaching (lejía) care axis.
- Launcher attention indicators.
- Cross-module links to Capex, Opex, Inventory, Travel, Archive, Assets, or
  Maintenance.
- Spanish translations.

## Garment Model

A garment contains at least:

- A required name.
- A required `ClothingCategory`.
- A status.
- An optional size (free text).
- Zero or more colours from the `ClothingColor` catalogue.
- An optional washing care value.
- An optional drying care value.
- An optional ironing care value.
- An optional dry-cleaning care value.
- Optional notes.
- Attachments, optionally with one marked as the primary image.
- Visibility.

"Accessories" are not a separate entity; they are simply garments in an
accessory category.

## Garment Status

Every garment has one of these fixed statuses:

- `Active`
- `Unavailable`
- `Deprecated`

The status is descriptive. It does not block editing, colour or care changes,
visibility changes, or deletion by itself, and it is not managed through
Configuration. It exists to keep garments the household no longer actively uses
without deleting them, and to support filtering.

## Care Model

Each garment carries four independent care axes. Every axis holds at most one
value and every axis is optional; an unset axis means "unspecified" and is not
displayed.

The fixed values per axis are:

- **Washing**: `Any`, `Wash30`, `Wash30Delicate`, `Wash40`, `Wash40Delicate`,
  `Wash50`, `Wash50Delicate`, `Wash60`, `Wash60Delicate`, `HandWash`,
  `DoNotWash`.
- **Drying**: `Any`, `Delicate`, `VeryDelicate`.
- **Ironing**: `Any`, `Low`, `Medium`, `DoNotIron`.
- **DryCleaning**: `Any`, `DoNotDryClean`.

These are fixed domain values and are not managed through Configuration. Each
value maps to a standard textile-care symbol. The initial symbol artwork is the
icon set under `docs/icons/`, which maps one-to-one to the values above
(`wh-*` washing, `dr-*` drying, `ir-*` ironing, `dc-*` dry cleaning); the
implementation moves these assets into the frontend so the editor and gallery
can render the symbols.

The initial care model has no bleaching (lejía) axis.

## Colour Model

Colour is an optional, multi-valued attribute. A garment references zero or more
values from the `ClothingColor` catalogue, and colour never blocks saving.

`ClothingColor` differs from the existing module-owned catalogues: in addition to
a name and an explicit order, each colour carries a **colour value** (a hex
string) used to render a swatch in the editor, the gallery, and Configuration.

## Size Model

Size is a single optional free-text field (for example `M`, `42`, or `XL`). The
initial module does not model size systems, measurements, or fit.

## Categories And Colours

Clothes owns two module-specific catalogues:

- `ClothingCategory`
- `ClothingColor`

Both are presented through Configuration and follow the established module-owned
catalogue behaviour:

- Administrator CRUD.
- Explicit ordering.
- Deletion-impact checks.
- Privacy-neutral impact reporting.

`ClothingColor` additionally exposes and validates its colour value.

### Initial Categories

The initial ordered category values are:

- `Tops`
- `Bottoms`
- `Outerwear`
- `Dresses`
- `Footwear`
- `Underwear`
- `Sportswear`
- `Accessories`
- `Other`

### Initial Colours

The initial ordered colour values, each with a representative colour value, are:

- `Black` (`#000000`)
- `White` (`#FFFFFF`)
- `Grey` (`#808080`)
- `Navy` (`#1B2A4A`)
- `Blue` (`#2563EB`)
- `Red` (`#DC2626`)
- `Green` (`#16A34A`)
- `Yellow` (`#FACC15`)
- `Orange` (`#EA580C`)
- `Brown` (`#7C4A1E`)
- `Beige` (`#D8C3A5`)
- `Pink` (`#EC4899`)
- `Purple` (`#7C3AED`)

The one-time initialization behaviour matches the established Configuration,
Opex, and Inventory category pattern: values are initialized once and are not
reimposed after administrative changes.

### Deletion And Reference Migration

- A `ClothingCategory` value is required on every garment. A referenced category
  may only be **replaced**, never cleared, following the Inventory category
  pattern. Replacement re-points the affected garments to the target category.
- A `ClothingColor` value is optional and multi-valued. A referenced colour may
  be either **replaced** or **cleared**. Replacement re-points the affected
  garments to the target colour, deduplicating when a garment already references
  the target; clearing removes the colour from every garment that referenced it.
- Reference migration is atomic and privacy-neutral: impact reporting never
  discloses private garments, and any failure rolls the whole migration back.

## Attachments And Primary Image

- Garments may contain multiple attachments using the shared platform attachment
  policies and authorization model.
- A garment may mark one of its image attachments as the **primary image**. The
  gallery thumbnail uses the primary image; when none is marked, it falls back to
  the first image attachment; when there is no image, it shows a neutral
  placeholder.
- Any user who may access the garment may view, add, remove, and mark attachments
  as primary.
- Attachments inherit the visibility and authorization of their owning garment.

## Visibility And Authorization

Every garment uses the platform-standard visibility values:

- `Public`
- `Private`

New garments default to `Public`.

These rules apply:

- A user can view and edit their own garments and public garments.
- A private garment remains creator-only, including from administrators.
- Public collaboration follows the standard Segaris rule: any authenticated user
  may edit a public record.

These constraints are enforced by the backend regardless of the client.

## Deletion

Deletion is physical, immediate, and irreversible. A garment can always be
deleted; no other module references garments, so there is no deletion guard.
Deleting a garment removes its colour associations and owned attachments.

## Validation

- Name is required, trimmed, not whitespace-only, and at most 200 characters.
- Category reference is required and valid.
- Status and visibility are known values.
- Size is optional and at most 50 characters.
- Each colour reference must be valid; duplicate colour references on one garment
  are rejected or deduplicated.
- Each care axis is optional and, when present, must be a known value for that
  axis.
- Notes are optional and at most 4,000 characters.
- A colour value is required on every `ClothingColor` and must be a valid hex
  colour string.

## Creation Defaults

A new garment starts with:

- Status `Active`.
- Visibility `Public`.
- The first available category by `SortOrder`, then `Id`.
- No size.
- No colours.
- No care values (all four axes unspecified).
- No notes.
- No attachments and no primary image.

## Module Entry And Navigation

Opening Clothes takes the user directly to the wardrobe gallery. Clothes does not
have an initial overview or dashboard, and its launcher card never requests
attention.

Creating, viewing, and editing garments happens in a popup dialog over the
gallery, following the established Segaris URL-aware dialog pattern, so gallery
state survives dialog open and close without a reload.

## Wardrobe Gallery

The primary view is a server-paginated thumbnail gallery. Each garment card shows
at least:

- The thumbnail (primary image, first image, or placeholder).
- The name.
- The category.
- The colours (as swatches).
- The status.
- The visibility.

The default ordering is name ascending, then identifier ascending.

The gallery supports:

- Partial search across name, size, and notes.
- Exact filters for category, status, colour, visibility, and creator.
- User-controlled sorting and bounded pagination following platform conventions.

Search, key filters, sort, page, and page size should be URL-backed where
practical.

## Attention

Clothes exposes no launcher attention. The launcher card shows the
platform-standard neutral state at all times.

## Acceptance Criteria

The initial Clothes definition is satisfied when:

1. Authenticated users can create, query, edit, and irreversibly delete visible
   garments with the documented fields, defaults, validation, and privacy rules.
2. A garment is a single entity covering both clothes and accessories,
   distinguished only by its category, with no outfit, wear-log, or laundry-state
   modelling.
3. Garment statuses `Active`, `Unavailable`, and `Deprecated` are available and
   descriptive, blocking no operation by themselves.
4. The four care axes (washing, drying, ironing, dry cleaning) accept their fixed
   per-axis values, are independently optional, and render the standard care
   symbols.
5. Size is an optional free-text field, and colour is an optional multi-valued
   reference to the `ClothingColor` catalogue that never blocks saving.
6. Public collaboration and private isolation follow the Segaris visibility
   baseline for garments.
7. Garments support multiple attachments with an optional primary image that
   drives the gallery thumbnail, falling back to the first image and then a
   placeholder.
8. Clothes-owned `ClothingCategory` and `ClothingColor` are initialized once and
   managed through Configuration with CRUD and reorder, and `ClothingColor`
   carries and validates a colour value.
9. Deleting a referenced category requires replacement, while deleting a
   referenced colour supports both replacement (with deduplication) and clearing,
   atomically and without disclosing private garments.
10. The wardrobe gallery presents thumbnails with the documented search, filters,
    sorting, and bounded pagination, and the editor is a URL-aware popup that
    preserves gallery state.
11. Clothes exposes no launcher attention.
12. SQLite and PostgreSQL migrations, backend unit/integration/architecture
    tests, frontend component tests, and a representative Playwright journey
    verify the supported behaviour and privacy boundaries.

## Deferred Decisions

- Whether future versions should track a dynamic laundry state and a colada
  workflow with launcher attention.
- Whether garments should later gain purchase, cost, brand, material, or season
  attributes.
- Whether outfits, sets, or wear/wash history should be introduced.
- Whether a bleaching (lejía) care axis should be added.
- Whether future Analytics will consume Clothes read contracts.
