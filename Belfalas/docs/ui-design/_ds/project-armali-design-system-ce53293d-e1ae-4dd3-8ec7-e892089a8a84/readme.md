# Project Armali — Design System

A design system for **premium desktop & web applications** with a calm, coastal /
Mediterranean feeling. **Light theme only.** Warm bone backgrounds, sea-aqua
primary, sun-gold secondary, generous whitespace, rounded corners everywhere,
frosted acrylic surfaces floating over drifting aurora light, and warm **colored
glow** in place of gray drop shadow. The mood is unhurried, spacious, and warm —
software that feels like a quiet harbour at golden hour.

> **Sources.** This system was authored from a written brand brief — **no external
> codebase, Figma file, or slide decks were provided.** There are therefore no
> reference URLs to record. If/when a product codebase or Figma file exists, link
> it here so future contributors can cross-reference real screens against these
> foundations.

---

## 1. Product context

Project Armali targets **information-dense desktop apps** — dashboards, data
consoles, settings-heavy tools — that nonetheless want to feel relaxed rather than
clinical. The design language exists to make dense functionality feel calm:

- **Premium desktop feel.** Lean on whitespace and padding. Let elements breathe;
  never crowd. Density is achieved through hierarchy and rhythm, not by shrinking
  gaps.
- **Coastal / Mediterranean warmth.** Every neutral is warm (bone, sand, warm
  ink). Accents are drawn from sea, sun, and terracotta. The result is peaceful,
  not corporate.
- **One representative product** is recreated as a UI kit here: **Armali Console**,
  a desktop app for monitoring data replication across "coastal regions." It
  demonstrates the system end-to-end (sidebar, top bar, cards, tables, forms,
  dialogs, toasts).

---

## 2. Content fundamentals

How copy is written across the product.

- **Voice:** calm, plain, confident. Short sentences. We explain, we don't sell.
- **Person:** address the user as **"you"**; the product refers to itself in the
  third person ("Armali keeps your regions in sync"), never as "I".
- **Casing:** **Sentence case everywhere** — buttons, titles, menus, table headers.
  Never Title Case UI, never ALL-CAPS except the small tracked **eyebrow/overline**
  label (e.g. `OVERVIEW`, `CONNECTED REGIONS`).
- **Buttons** are verb-first and short: "Save changes", "New project",
  "Disconnect", "Continue". Avoid "Click here".
- **Empty / status states** are gentle and human: "Nothing here yet — connect a
  region to begin." not "No data available."
- **Errors** are specific and non-blaming: "This key is invalid — check for an
  extra space." not "Invalid input."
- **Numbers & units** read naturally: "Last run 4 minutes ago", "12 regions",
  "99.98% uptime".
- **No emoji** in product UI. Tone is warm through words and color, not emoji.
- **Punctuation:** em dashes for asides, no exclamation marks in system copy.

_Examples:_ "All coastal regions are up to date." · "Replication will pause until
you reconnect." · "Choose a region to begin."

---

## 3. Visual foundations

**Color.** Warm light palette only. Backgrounds are **bone/ivory** (`--bone-100`
app surface). Text is **warm ink**, never pure black (`--ink-900 #2C2823`).
Emphasis ladder: **aqua** (primary — links, primary actions, focus, selection) →
**gold** (secondary emphasis, highlights, warnings) → **azure** (Mediterranean
blue, calls-to-action & info) → **sea green** (success) → **terracotta**
(destructive / error). Soft tinted backgrounds (`*-100`) carry status without
shouting. Nothing is pure black or pure white.

**Type.** Two faces, deliberately split by role:
- **League Spartan SemiBold (600)** — _display face._ Used for **titles, table
  headers, popup/dialog captions, primary button labels, badges, tabs, eyebrows.**
  Geometric and wide; tracking tightens to `-0.02em` at display sizes and opens to
  `0.14em` uppercase for eyebrows.
- **Nunito Medium (500)** — _body face._ Used for **paragraphs, panel content,
  hints, input text, captions.** Soft and rounded; **600** for emphasis only.
- **Never exceed weight 600** for UI. (A 700 face ships only for rare numeric
  emphasis and is essentially unused.)
- Base size **15px**, generous scale up to a 64px display. On large surfaces, text
  is large and airy.

**Spacing & layout.** 4px base unit; **lean generous**. Card interiors use
`--pad-card` (28px); major sections separate by 48px+. Fixed app chrome:
264px sidebar, 64px top bar. Content max ~1200px. The guiding instinct is *more
air*, not more elements.

**Corners.** **Everything is rounded.** Controls 12px, cards 20px, dialogs 24px,
chips/pills 999px. No sharp corners anywhere — panels, buttons, tables, dialogs,
inputs all carry radius.

**Backgrounds.** The signature motif is the **aurora**: large, soft, slowly
**drifting color blobs** (aqua, gold, azure, rose) behind a frosted veil
(`.armali-aurora`). No photography, no hand-drawn illustration, no repeating
texture, no harsh gradients. Foreground surfaces are **acrylic/glass**
(`backdrop-filter: blur + saturate`) with a bright top border highlight, so they
read as panes of frosted sea-glass floating over the aurora.

**Shadows → glow.** We **do not use gray/black drop shadows.** Elevation is a warm
ambient glow (`--glow-soft` / `--glow-card`) plus, on interaction, a **colored
halo** matching the element's intent (`--glow-aqua/-gold/-azure/-danger`). Glow,
never gloom.

**Transparency & blur.** Used purposefully for *floating* surfaces: cards over the
aurora, tooltips, toasts, dialogs, foreground chips, the top bar. Solid bone
(`--surface-card-solid`) is used when a surface sits on a plain background and
legibility matters (e.g. inputs, dense tables).

**Motion.** Easing is gentle: `--ease-out` for entrances, `--ease-spring` for
playful thumbs/dialogs. Durations 140/240/420ms. **Subtle infinite loops** are a
deliberate feature on *live* elements only: the aurora drifts continuously, "live"
switches and notification dots **breathe/pulse** softly. Everything respects
`prefers-reduced-motion`.

**Interaction states.**
- _Hover:_ lift slightly (cards `translateY(-3px)`) and gain a **colored glow**;
  ghost/subtle controls warm to a sand fill. We brighten, never darken to gray.
- _Focus:_ aqua focus ring (`--ring`) — always visible, always aqua.
- _Press:_ shrink slightly (`scale(0.98)`) + a touch dimmer; tactile but soft.
- _Disabled:_ drop to ~50% opacity, remove glow.

**Borders.** Hairlines are **warm and translucent** (`rgba(124,110,86,…)`), never
pure gray. Glass surfaces add a bright white top border (`--border-glass`) for the
sea-glass edge.

**Cards.** Rounded 20px, warm ambient glow (not shadow), 1px warm hairline (or a
bright glass edge when frosted). Optional header (display-font title + body-font
subtitle + top-right action) and footer (action row). Interactive cards lift and
glow on hover.

---

## 4. Iconography

- **Icon set: [Lucide](https://lucide.dev)** — the chosen system. Clean, rounded,
  **stroke-based** (≈2px, round caps/joins), which matches Nunito's softness and
  the airy feel. Used in toolbars, inputs, tabs, toasts, table rows, and the kit.
- **Substitution flag.** No brand-specific icon set was provided, so Lucide is a
  **deliberate substitution** chosen for fit. It is loaded from CDN
  (`unpkg.com/lucide`) in cards and kits via `data-lucide="<name>"` +
  `lucide.createIcons()`. **If Armali has its own icon library, swap it in here.**
- **Stroke color** follows context: `--text-secondary` at rest, `--accent` on
  focus/active, status colors inside toasts/badges.
- **No emoji** as iconography. A few inline SVGs are hand-set inside primitives
  (checkbox tick, select chevron, dialog/toast close, spinner) — these are
  intrinsic control glyphs, not decorative art.
- We **never** hand-draw decorative illustrations or generate imagery; the aurora
  is the only "illustration," and it is pure CSS.

---

## 5. Index / manifest

**Root**
- `styles.css` — the single entry point consumers link. `@import`-only.
- `readme.md` — this guide.
- `SKILL.md` — Agent-Skill front-matter for use in Claude Code.

**`tokens/`** (all reached from `styles.css`)
- `fonts.css` — `@font-face` for League Spartan 600 + Nunito 500/600/700 (self-hosted).
- `colors.css` — palette + semantic aliases.
- `typography.css` — families, scale, weights, tracking.
- `spacing.css` — 4px scale + layout sizes.
- `effects.css` — radii, glass/blur, colored glow, motion tokens + shared keyframes.
- `base.css` — element defaults, `.armali-aurora`, `.armali-glass`, `.armali-eyebrow`.

**`assets/fonts/`** — self-hosted woff2 binaries.

**`components/`** (React primitives — `window.ProjectArmaliDesignSystem_ce5329.<Name>`)
- `buttons/` — **Button**, **IconButton**
- `forms/` — **Input**, **Select**, **Checkbox**, **Switch**
- `surfaces/` — **Card**, **Badge**, **Avatar**
- `feedback/` — **Tooltip**, **Toast**, **Spinner**
- `navigation/` — **Tabs**
- `overlay/` — **Dialog**

**`guidelines/`** — foundation specimen cards (Colors, Type, Spacing, Brand) shown
in the Design System tab.

**`ui_kits/console/`** — **Armali Console**, the representative desktop app
recreation (interactive `index.html` + screen JSX).

**Starting points:** Button, Input, Card, Toast, Dialog, plus the Console screen.
