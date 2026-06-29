# Belfalas World Template Contract

This document freezes the Wave 6.1 rendering/template contract. It is intentionally
theme-agnostic: `tropical-v1` is the first authored template, but future fantasy,
sci-fi, magic, or other themes should use the same shape.

## Coordinate System

- World authoring uses integer isometric grid coordinates: `positionX`, `positionY`.
- One tile is `tileWidth x tileHeight` pixels. v1 uses `128 x 64`.
- `(originX, originY)` is the screen-space position of grid coordinate `(0, 0)`.
- Grid-to-screen conversion:

```text
screenX = originX + (positionX - positionY) * tileWidth / 2
screenY = originY + (positionX + positionY) * tileHeight / 2
```

- Screen-to-grid conversion, useful for picking/debug overlays:

```text
localX = screenX - originX
localY = screenY - originY
positionX = localY / tileHeight + localX / tileWidth
positionY = localY / tileHeight - localX / tileWidth
```

## Camera

- `cameraBounds` is expressed in screen pixels over the authored map.
- The renderer may pan within `minX/minY/maxX/maxY`.
- Zoom is optional in v1; if enabled, clamp the camera after applying zoom.
- Belfalas world interaction is camera-only. The user cannot move, build, delete, or
  otherwise edit world objects directly.

## Assets

- `assetBasePath` points to the template's public asset folder, without a trailing slash.
- `atlasKey` names the Pixi atlas metadata file and atlas namespace.
- The default loading contract is:
  - atlas metadata: `{assetBasePath}/{atlasKey}.json`
  - sprite frames: `spriteKey` values from the template's variant list
- `spriteKey` must be stable content identity. Persisted `BuiltPlot` rows store the
  selected variant id, so renaming keys after an era has been played should be treated
  as a content migration.

## Z-Order

Sprites sort by their ground contact point:

```text
sortKey = (positionX + positionY) * tileHeight + sortOffsetY
```

- Base terrain renders first.
- District overlays, plots, denizens, shadows, and detail sprites share the same
  ground-contact sorting rule unless a layer has an explicit fixed background role.
- `sortOffsetY` nudges visually tall or wide sprites without moving their authored grid
  position.

## Anchors

- `anchorX` and `anchorY` use normalized sprite coordinates, matching Pixi's anchor
  convention.
- Category anchors apply to all variants in that category.
- Denizen sockets carry their own anchors because denizen sprites may sit differently
  from buildings or flora.

## Building Plots

- Building plots are authored, buildable positions in a district.
- A plot has a fixed district, category, and isometric grid position.
- When a building stage is reached, the backend chooses one free plot, chooses a
  compatible category variant, and persists `BuiltPlot`.
- A built plot remains fixed for the lifetime of the era and is included in archived
  era snapshots.

## Denizen Sockets

- Denizen sockets are runtime-only placement positions.
- The backend persists only `DenizenCount`: district, denizen identity, and count.
- The frontend places denizens when the world opens by drawing from compatible sockets.
- Denizen positions should not be sent back to the API or written to persistence in v1.

## Category Contracts

Each category defines:

- `footprintWidth`, `footprintHeight`: expected occupancy in grid tiles.
- `anchorX`, `anchorY`: default sprite anchor for variants in the category.
- `sortOffsetY`: optional z-order adjustment for sprites in the category.
- `supportsDenizens`: whether denizen sockets may be authored adjacent to or visually
  associated with this category.
- compatible sprite variants: all `Variant` rows with the same template id and category.

Variants within a category should be visually interchangeable enough that the backend
can choose one at random without asking the renderer for layout-specific exceptions.
