# Belfalas — Requirements

> **Status:** v1 design complete. All four sections agreed through the design
> discussion; remaining items are explicit "open/deferred" notes within each section.
> Delivery plan in [ROADMAP.md](ROADMAP.md). Last updated: 2026-06-27.

## Summary

Belfalas is a single-user app that **gamifies daily work life**. The user completes
quests tied to different areas of their life; doing so grants experience that levels
up those areas, and that progress is reflected as the **organic growth of a virtual
world** that the user can navigate but not directly edit. Progress is organised into
**eras** (≈ one year each); when an era ends it is archived as a read-only snapshot
and a new one can begin.

## Foundational decisions

- **Scope:** single-user (one person, one world, one progress track).
- **Docs language:** English.
- **Tentative stack** (revisited in the Architecture block): C# backend with Entity
  Framework + HTTP API, TypeScript frontend, PostgreSQL persistence, everything
  deployed via Docker Compose with volumes for persistent data.

---

## 1. Progression model — _agreed_

### 1.1 Entity hierarchy

```
Era (≈1 year, 50 weeks)
 └─ Area of focus (e.g. Work / Social / Side activities)   → one district of the world
     └─ Quests (daily + weekly)
         └─ Actions (concrete items the user marks as done)
```

### 1.2 Eras

- An era lasts ~50 weeks (a working year, holidays excluded).
- An era has **at least one** area of focus; typical range **2–4**.
- Areas, their names, and the quest design are configured per era in the admin panel.
- Ending an era is an explicit admin action: the current era (progress **and** visual
  world state) is archived as a **read-only** snapshot, and a new era can be created,
  potentially with different areas and a different world template. _(Lifecycle detail
  belongs to the Admin block.)_

### 1.3 Areas of focus

- Each area tracks its **own** progress independently: **0 → 50 levels**.
- Areas are of **equal importance** by default (per-era weighting is a future option).
- When an area reaches level 50 it stops emitting quests and its district is shown as
  **complete / flourishing**.

### 1.4 Scoring (XP)

- A **single kind of point — XP — bucketed per area**. Completing an action credits
  XP to the area that action belongs to.
- **No spendable currency and no shop.** The world is non-interactive, so XP only ever
  measures progress; there is nothing to spend.

### 1.5 Levels and pacing

- **Flat curve:** every level within an area costs the same amount of XP.
- **Global level** shown to the user = the **average** of all area levels (0–50). It is
  a headline figure only; the real state lives in the per-area levels and the world.
- **Self-pacing via a fixed weekly XP budget:** each week offers a bounded amount of
  obtainable XP per area (the week's quest sets). The level threshold is calibrated so
  that completing a **sufficient subset** (not everything) yields ~1 level that week.
  The user cannot outrun ~1 level/week per area because no more XP is available.

### 1.6 Quests

- **Per-week the user sees one global, mixed quest set** (daily + weekly) that blends
  actions from several areas into a single light list. Each action is tagged to an area
  and feeds that area's XP/district. The admin **calibrates the mix** so every area
  receives enough XP for ~1 level/week.
- **Daily quests are recurring:** the daily list resets each day (habit-style, e.g.
  "check email", "go for a walk"), favouring routine and streaks.
- **Weekly quests** are larger, higher-XP goals for that week, completed once.

### 1.7 No-shaming / catch-up

- **No penalty and no decay.** XP not earned in a week is simply not gained.
- Weekly quests provide the margin to recover a slow week.
- The user always has something in progress across the daily and weekly horizons
  without being overwhelmed (nested time layers).

### 1.8 Open / deferred within progression

- Exact XP-per-level number and the daily/weekly XP split (a calibration/tuning detail,
  resolved during implementation, surfaced in the admin panel).
- Per-era area **weighting** (future option; equal for v1).

---

## 2. World evolution — _agreed_

### 2.1 Rendering

- **2D isometric, sprite-based.** Tilesets where each plot **category** maps to a set of
  fitting sprite **variants**. Target libraries: **PixiJS** (WebGL 2D, many sprites +
  pan/zoom) or Phaser. (Frontend integration decided in the Architecture block.)

### 2.2 World layout

- A single contiguous isometric map divided into **districts — one district per area of
  focus**, so progress per area is readable at a glance.
- Each area level (0→50) corresponds to **one evolution stage** in its district; level 50
  = district fully evolved.
- A district's stage may be a **building**, a **denizen**, or an **upgrade**; the exact
  per-stage mix is defined by the world template (asset-phase detail, not 50 buildings).

### 2.3 World templates

- A **catalogue of themed templates** (tropical, fantasy, sci-fi, …); **each era
  instances one**, reinforcing the fresh start between eras. v1 may ship with a single
  template, with the catalogue as the extension point.
- A template defines: the district layout, the plots (with categories) per district, the
  sprite variants per category, and the ordered evolution sequence per district.

### 2.4 Buildings & plots

- **Organic semi-random growth:** each building stage picks a free plot **adjacent to an
  already-built one**; the sprite **variant is drawn at random and then persisted** for
  the era. Gives a natural growth feel.
- Built plots and their chosen variants are persisted for the whole era.

### 2.5 Denizens

- **Random socket system:** only **count and identity** are persisted (e.g. 3 blue, 2
  yellow, 5 cats), never position or action — they re-place randomly each time the world
  is opened.
- Simple animation (moving between spots, emotes) is a **future** enhancement, not v1.

### 2.6 Camera & interaction

- **Non-interactive world:** pan on XY plus optional zoom (RTS-style). The user cannot
  edit or influence the world except by completing quests / gaining levels.

### 2.7 Eras & history

- Archiving an era persists its **full visual state** alongside its progress, as a
  **read-only** snapshot navigable from a historical selector. Past eras cannot be
  altered.

## 3. Architecture & stack — _agreed_

Belfalas is an **independently built and deployed** project within the Armali.Platform
monorepo (sibling to Segaris, not a module of it). It **reuses the platform's tech
family** but keeps its own **simple, standalone architecture** — no Segaris plugin
framework.

### 3.1 Backend

- **C#/.NET**, layered and standalone: **HTTP API + domain + EF persistence**. No plugin
  system.

### 3.2 Persistence

- **SQLite** (single file on a Docker volume) via EF Core. Fits a single-user app: no
  separate DB container, backup = copy a file. (Postgres was considered and dropped for
  simplicity given the single-user scope.)

### 3.3 Frontend

- **React** for the UI (quests, admin, menus) + a **PixiJS** canvas for the isometric
  world. TypeScript, pnpm — consistent with the rest of the platform's frontend.

### 3.4 Deployment

- **Docker Compose**: backend container + frontend container (static served, Caddy-style
  as elsewhere in the platform) + a **volume for the SQLite file**. No DB container.

### 3.5 Open / deferred

- Auth model for the single user (likely none for purely local use, or one configurable
  password) — decided before the deploy wave.
- UI language / i18n (English-only UI is fine for v1).

## 4. Data model & admin panel — _agreed_

### 4.1 Era lifecycle (admin)

- **Guided wizard** to create an era: name → number/names of areas → world template →
  quest design (daily habits + weekly pool).
- Ending an era is an explicit admin action → archive a **read-only snapshot** (progress
  + world visual state) → optionally start a new era.
- A **history viewer** lets the user browse past eras and their worlds (read-only).

### 4.2 Quest authoring

- **Daily habits:** a **fixed list per era**, defined once (habit-style, reset daily).
- **Weekly goals:** a **pool per era**; the system **draws/rotates** the week's set
  automatically, with **manual override** available.
- Each daily/weekly item is tagged to an area and carries an XP value; the admin
  **calibrates the per-area XP mix** so each area can gain ~1 level/week.

### 4.3 Progression engine

- **Live XP accrual:** marking an action (binary checkbox in v1; quantities are future)
  credits its area immediately.
- **Immediate level-up:** crossing an area's level threshold raises its level and
  triggers **one world evolution stage** in that district at once.
- The **weekly XP budget** (sum of obtainable daily+weekly XP per area) bounds gains to
  ~1 level/week per area; daily habits reset daily, the weekly set refreshes weekly.

### 4.4 Data model sketch

**Era configuration**
- `Era` (id, name, startDate, weeks=50, status active|archived, templateId)
- `Area` (id, eraId, name, order)
- `DailyHabit` (id, eraId, areaId, label, xp)
- `WeeklyGoal` (id, eraId, areaId, label, xp)  ← pool
- `WorldTemplate` (id, theme) → `District` (per area slot), `Plot` (category, position/
  adjacency), `VariantSet` (category → variants), `EvolutionSequence` (per district:
  ordered stage types building|denizen|upgrade)

**Era runtime state**
- `AreaProgress` (eraId, areaId, xp, level)
- `WeeklySet` (eraId, weekIndex, selected weekly goal ids) ← rotation result
- `DailyCompletion` (eraId, date, dailyHabitId)
- `WeeklyCompletion` (eraId, weekIndex, weeklyGoalId)
- `BuiltPlot` (eraId, districtId, plotId, variantId) ← persisted buildings
- `DenizenCount` (eraId, districtId, denizenType, count) ← identity/count only
- `ArchivedEra` ← read-only snapshot of progress + world state

> Timezone for daily/weekly boundaries: Europe/Madrid (consistent with the platform).
