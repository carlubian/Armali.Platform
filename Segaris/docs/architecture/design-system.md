# Design System And Visual Foundation

This document records the Phase 1 decision on the frontend's visual language and component foundation, based on the screen designs under `docs/ui-design/`.

## Source Material

`docs/ui-design/` contains an interactive design prototype produced outside this repository's normal workflow:

- `_ds/project-armali-design-system-ce53293d-.../` — **Project Armali**, a general-purpose design system: design tokens (`tokens/*.css`), self-hosted fonts, and a set of framework-agnostic React component implementations (Button, IconButton, Input, Select, Checkbox, Switch, Card, Badge, Avatar, Tooltip, Toast, Spinner, Tabs, Dialog).
- `segaris/` — Segaris-specific screens built on top of Project Armali: login, launcher, user management, profile, and system states (`screens-*.jsx`), shared data/icon helpers (`data.jsx`), and Segaris-specific styling (`segaris.css`).
- `design-canvas.jsx`, `tweaks-panel.jsx`, `Segaris Common Pages.html` — the interactive prototype shell used to browse and tweak the designs in a browser. These are tooling for the design review only and are not part of the application.
- `uploads/New Armali Logo.png` — the final Segaris/Armali logo asset.

The prototype is a reference, not a drop-in implementation: it loads React, Babel, and Lucide from CDNs, exposes components on `window`, and uses inline demo data. It is feasible and recommended to build the real frontend from it, with the adaptations below.

## Decision

Segaris adopts **Project Armali** as the basis for its design tokens and shared component library, and the `segaris/` screens as the reference layout for the shared shell (login, launcher, top bar, user management, profile, and system states).

Rationale:

- The token set (warm bone/ink palette, aqua/gold/azure/sea/terracotta accents, League Spartan + Nunito type, 4px spacing scale, rounded corners, glow instead of shadow, aurora/glass surfaces) is complete, internally consistent, and matches the calm "household ERP" tone the project wants.
- The component inventory (Button, IconButton, Input, Select, Checkbox, Switch, Card, Badge, Avatar, Tooltip, Toast, Spinner, Tabs, Dialog) covers the shared controls needed by the platform shell and is a reasonable starting catalog for module screens.
- The Segaris screens already model the shared shell described in `docs/architecture/user-experience.md`: a top bar with a "return to launcher" action (`ShellTopBar`), the launcher as a non-dashboard module grid, an admin user-management area, a self-service profile page, and explicit service-unavailable/404 states.

This resolves the "Design system" item in `ROADMAP.md` (previously deferred pending screen designs).

## Adaptation Plan

The prototype must be re-implemented inside `src/frontend` per `docs/architecture/frontend.md`; nothing under `docs/ui-design/` is consumed at runtime.

- **Tokens.** Port `tokens/*.css` (fonts, colors, typography, spacing, effects, base) into the frontend's global stylesheet largely as-is. Self-host the existing woff2 files from `assets/fonts/`. `--sidebar-w` is not used: Segaris has no persistent sidebar (see Module Navigation in `docs/architecture/user-experience.md`) and this token can be dropped or left unused.
- **Components.** Re-implement each Project Armali component as a typed React/TypeScript component under a shared `components/` (or `ui/`) area, following the same visual behavior (variants, sizes, states) but without the `window`-global / Babel-in-browser pattern. Treat `_ds_bundle.js` and `components/**/*.jsx` as the reference implementation to port from.
- **Overlays.** Shared modal dialogs and future viewport-level overlays must render through a React portal attached to `document.body`. They must not be mounted inside page or shell layout containers, whose positioning, overflow, filtering, or stacking contexts can clip the backdrop or make `position: fixed` behave like in-flow content. Feature screens should use the shared `Dialog` component rather than implementing their own modal wrapper.
- **Icons.** The prototype loads Lucide from `unpkg.com` and upgrades `data-lucide` elements at runtime. The real frontend uses the `lucide-react` package as ordinary icon components instead of the CDN/`createIcons()` pattern.
- **Shared shell.** Re-implement `ShellTopBar`, the launcher grid (`Launcher`/`ModuleCard`), and the system-state screens (`ServiceUnavailable`, `NotFound`) as the application's shared shell, wired to real routing (React Router), session context, and i18n instead of static demo data from `data.jsx`.
- **Brand mark.** The prototype's `BrandMark` component renders an inline SVG house/arch/wave illustration. The real frontend instead uses the supplied raster logo at `docs/ui-design/uploads/New Armali Logo.png` (to be copied into the frontend's asset folder during implementation). The inline SVG mark is not used.
- **Demo data.** `SEG_MODULES` and `SEG_USERS` in `data.jsx` are illustrative only. The launcher's module catalog and the user list come from the backend (`docs/architecture/domain-organization.md`, `/api/admin/users`) once those integrations exist; until a module is implemented its card may be omitted or marked as not yet available rather than hard-coded.

## Screen Variant Decisions

Where the prototype offered alternatives, the following are selected:

| Screen | Chosen variant | Notes |
| --- | --- | --- |
| Login | **A · Centered card** (`LoginCentered`) | Single aurora background with a centered glass card; the split-panel variant (`LoginSplit`) is not used. |
| User management | **B · Cards** (`UserMgmtCards`) | Per-user cards with avatar, role/status badges, and edit/activate actions; the table variant (`UserMgmtTable`) is not used. |

The create/edit user dialog (`UserDialog`), launcher, profile page, and service-unavailable/404 states have no alternative in the prototype and are adopted as designed, subject to the adaptation plan above.

## Open Items For Later Phases

- Translate the prototype's component states (hover, focus, press, disabled) and accessibility behavior into the ported TypeScript components, including keyboard operation and `prefers-reduced-motion` handling, per the accessibility baseline still to be defined in `docs/architecture/user-experience.md`.
- Define the final launcher module catalog, icons, and tones per module as real modules are scoped in Phase 2 (the prototype's 12-module list and tones are a first pass only).
- Define toast placement, duration, and severity conventions (open item in `docs/architecture/user-experience.md`) using the `Toast` component as the building block.
- Confirm whether `Tabs` and `Dialog` patterns from Project Armali are reused as-is inside immersive modules, or whether modules introduce their own navigation patterns as permitted by `docs/architecture/user-experience.md`.
