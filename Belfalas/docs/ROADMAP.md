# Belfalas — Roadmap

> Wave-based delivery plan, mirroring the rest of Armali.Platform. Each wave is a
> shippable, verifiable increment. Decisions backing this plan live in
> [REQUIREMENTS.md](REQUIREMENTS.md). Last updated: 2026-06-29.

## Guiding constraints

- Single-user app; **C#/.NET (layered, standalone) + EF/SQLite** backend; **React +
  PixiJS** frontend; **Docker Compose** (backend + frontend + SQLite volume).
- Backend-first within each capability: contracts → persistence → logic → UI.

---

## Wave 0 — Scaffolding & contracts

Stand up the skeleton both ends can build against.

- .NET solution (API + domain + persistence projects), React+Vite+TS app, pnpm.
- Docker Compose skeleton (backend, frontend, SQLite volume); CI wired into the
  platform's workflows.
- **Frozen API surface** (route stubs returning 501) for eras, quests, progression,
  world, admin.

## Wave 1 — Domain & persistence

- Entities from §4.4 (era config + runtime state) as EF model.
- SQLite migration; seed helpers; no game logic yet.

## Wave 2 — Era & quest authoring (backend)

- Era lifecycle APIs: create (wizard payload), configure areas, archive.
- Daily-habit list + weekly-goal pool CRUD.
- Weekly-set **rotation** (draw from pool) with manual override.

## Wave 3 — Progression engine (backend)

- Mark action complete (binary), **live XP accrual** per area.
- **Immediate level-up** on threshold; weekly XP budget enforcing ~1 level/week.
- Daily reset of habits, weekly refresh of the set (Europe/Madrid boundaries).

## Wave 4 — World model & evolution engine (backend)

- World template data model: districts (one per area), plots+categories, variant sets,
  per-district evolution sequence.
- **Organic growth picker** (free plot adjacent to a built one; random variant,
  persisted), **denizen socket** counts/identities.
- Level-up → one evolution stage; persist `BuiltPlot` / `DenizenCount`.

## Wave 5 — Quest UI (frontend)

- React shell, app navigation.
- Daily checklist + weekly goals view; global level (average) display; per-area progress.

## Wave 6 — World rendering (frontend)

Turn the authored world-template model into a navigable PixiJS scene. v1 ships one
complete template (`tropical-v1`) and a repeatable asset/template contract for future
themes.

### Wave 6.1 — Rendering & template contract

- Specify isometric coordinate conversion, tile size, camera bounds, anchors, z-order
  rules, and the asset naming/loading contract.
- Formalise the distinction between **building plots** (persisted once built) and
  **denizen sockets** (runtime-only placement positions).
- Define category contracts: expected footprint, anchor point, compatible sprite
  variants, optional sorting offset, and whether the category supports denizens.

### Wave 6.2 — Tropical template assets

- Replace placeholder `tropical-v1` content with a fixed tile-based base map authored
  for the template; Belfalas does not expose a user-facing map editor in v1.
- Author district plot/socket coordinates over the base map, with categories assigned
  to each plot and denizen socket.
- Create or integrate the minimal tropical asset set: terrain tiles, building/flora/
  landmark variants, denizens, shadows, and atlas metadata.

### Wave 6.3 — PixiJS world canvas

- Render the base tile map, district layers, built plots, and persisted sprite variants.
- Add RTS-style camera panning and optional zoom with sensible bounds.
- Keep rendering theme-agnostic: no hardcoded tropical assumptions in the renderer.

### Wave 6.4 — Denizen placement

- Place denizens randomly when the world opens, using compatible sockets from the
  template; persist identity/count only, not position.
- Ensure denizens layer correctly against terrain and buildings.

### Wave 6.5 — Scalability pass

- Document how to add another world template from authored map data and assets.
- Validate that fantasy, sci-fi, magic, or other future templates can reuse the same
  engine contract without backend or renderer redesign.

Wave 6.5 deliverables live in
[WORLD_TEMPLATE_AUTHORING.md](WORLD_TEMPLATE_AUTHORING.md), with contract validation
covered by the backend world-template tests.

## Wave 7 — Admin panel (frontend)

- Era creation wizard; daily-habit + weekly-pool authoring; per-area XP calibration view;
  manual weekly-set override.

## Wave 8 — Era archival & history

- Archive an era → read-only snapshot (progress + world).
- History viewer to browse/navigate past eras and their worlds.

## Wave 9 — Deploy & acceptance

- Finalise Docker Compose + SQLite volume; resolve the single-user **auth** decision.
- Acceptance pass against REQUIREMENTS; deploy docs.

---

## Deferred (post-v1)

- Per-era area **weighting** (non-equal areas).
- Denizen **animation** (movement, emotes).
- Action **quantities** (e.g. "walk 3×") beyond binary completion.
- Additional world **templates** beyond the first.
- Optional **zoom** polish; UI i18n.
