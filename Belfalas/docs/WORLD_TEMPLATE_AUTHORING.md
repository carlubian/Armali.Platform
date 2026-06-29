# Belfalas World Template Authoring Guide

This guide is the Wave 6.5 scalability pass for world templates. It describes how
to add another authored template, such as `fantasy-v1`, `sci-fi-v1`, or
`magic-v1`, without changing the backend world engine or the PixiJS renderer.

The frozen rendering contract lives in [WORLD_TEMPLATE_CONTRACT.md](WORLD_TEMPLATE_CONTRACT.md).
This document is the practical checklist for producing content that satisfies that
contract.

## Template Folder

Each template owns one public asset folder:

```text
src/frontend/public/assets/worlds/{template-id}/
  map.json
  {atlasKey}.json
  {atlas-image}
```

Rules:

- `template-id` is stable content identity, for example `fantasy-v1`.
- `assetBasePath` in the backend seed is `/assets/worlds/{template-id}`.
- `atlasKey` matches `{atlasKey}.json` in the asset folder.
- Sprite keys are stable frame names inside the atlas JSON. Renaming them after an
  era has used them is a content migration because built plots persist variant ids
  and display through those variant sprite keys.

## Backend Seed

Add a new `WorldTemplate` reference-data graph with:

- render metadata: tile size, map size, origin, camera bounds, asset path, atlas key
- districts, with one district slot available per area of focus
- category contracts for buildable plot categories
- variants for every buildable category
- denizen variants using `denizen:{identity}` categories
- plots, denizen sockets, and evolution stages for each district

The existing `tropical-v1` seed is content, not engine logic. A new template should
use the same entity types and API response contracts.

## Authoring Checklist

Use this sequence when adding a template:

1. Pick a stable id and theme name.

   Example: `fantasy-v1`, theme `fantasy`, display name `Moonlit Boroughs`.

2. Author the base map.

   Create `map.json` with `width`, `height`, `tileWidth`, `tileHeight`, `origin`,
   `terrainLegend`, and a rectangular `rows` matrix. All row tokens must exist in
   `terrainLegend`; all terrain sprite keys in `terrainLegend` must exist in the
   atlas frames.

3. Prepare the atlas.

   Create `{atlasKey}.json` in Pixi spritesheet format with `frames` and
   `meta.image`. Add the referenced image beside the JSON. The renderer loads both
   paths from `assetBasePath`, so no renderer code should mention the template id.

4. Define category contracts.

   Categories are visual fit contracts, not theme concepts. A fantasy template may
   use `cottage`, `grove`, and `tower`; a sci-fi template may use `habitat`,
   `generator`, and `beacon`. Each category needs footprint, anchor, sort offset,
   and variants with matching sprite proportions.

5. Place district plots.

   Every plot must be inside the map bounds and use a category defined by the same
   template. Keep variants within a category visually interchangeable enough that
   the backend can pick any one without renderer exceptions.

6. Place denizen sockets.

   Sockets are runtime-only placement positions. Each compatible denizen identity
   must have at least one `denizen:{identity}` variant in the same template. Do not
   persist socket selection or send denizen positions back to the API.

7. Author evolution stages.

   Each district should define orders `1..50`. Building stages consume authored
   plots; denizen stages increment identity/count; upgrade stages are logical
   progression markers and do not require a new rendered object in v1.

8. Expose the template in the catalogue.

   Seed it alongside existing templates so `GET /api/world/templates` returns it.
   Era creation should only accept template ids from this catalogue.

9. Run validation.

   Run backend tests and frontend type checks. The world template tests verify that
   seeded templates are content-complete for the shared renderer contract.

## Theme-Agnostic Engine Boundaries

Adding a template is content work when these boundaries hold:

- Backend world evolution chooses by district, stage kind, plot category, variant,
  and denizen identity. It does not branch on `theme` or `templateId`.
- Frontend asset loading derives paths from `assetBasePath` and `atlasKey`.
- Frontend rendering draws terrain rows, district plot footprints, built plots, and
  denizens from contract fields. It does not know whether a sprite is tropical,
  fantasy, sci-fi, magic, or something else.
- Category names are template-local. Shared names are allowed but not required.
- The only cross-template semantics are stage kinds (`Building`, `Denizen`,
  `Upgrade`) and denizen variant category syntax (`denizen:{identity}`).

## Compatibility Examples

These examples should require no engine redesign:

- Fantasy: terrain frames for grass, stone, river; categories `cottage`, `grove`,
  `tower`; denizens `mage`, `foxfire`.
- Sci-fi: terrain frames for plating, glass, void; categories `habitat`,
  `reactor`, `antenna`; denizens `technician`, `drone`.
- Magic: terrain frames for crystal, moss, mist; categories `sanctum`, `garden`,
  `obelisk`; denizens `adept`, `wisp`.

If a proposed template needs per-template renderer code, a new persisted world state
field, or backend branching by theme, treat that as a contract change and update
[WORLD_TEMPLATE_CONTRACT.md](WORLD_TEMPLATE_CONTRACT.md) before implementing it.
