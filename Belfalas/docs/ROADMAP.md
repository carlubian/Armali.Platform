# Belfalas — Roadmap

> Wave-based delivery plan, mirroring the rest of Armali.Platform. Each wave is a
> shippable, verifiable increment. Decisions backing this plan live in
> [REQUIREMENTS.md](REQUIREMENTS.md). Last updated: 2026-06-27.

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

- PixiJS isometric canvas: render built plots + variants + denizens.
- RTS camera (pan XY, optional zoom). Denizens re-placed randomly per open.

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
