# Design system

## Where the visual language comes from

The Armali design language is defined once for the whole platform. Its canonical
source lives in the Olyssia vault, under the Blackwing project's `odm/ui/_ds/`
folder, and it has already been ported to production code by Segaris in
`Segaris/src/frontend/src/styles/` and `Segaris/src/frontend/src/assets/fonts/`.

Blackwing takes it from Segaris, not from scratch: the tokens and the primitives
in this repository are copies of the Segaris implementation, renamed where
necessary. When something is ambiguous, Segaris is the reference.

The screen prototypes for milestone 1 live in the vault as
`odm/ui/Blackwing - Pantallas M1.dc.html`. **Fidelity to that prototype in
typography and color is not negotiable.** Layout may be adapted where the
implementation demands it; fonts and colors may not.

## What is ported, and what is not

Only the *foundations* are reused. This phase ports:

- The four self-hosted `.woff2` faces: `LeagueSpartan-600`, `Nunito-500`,
  `Nunito-600`, `Nunito-700`, under `src/assets/fonts/`.
- The token layer under `src/styles/tokens/`: `fonts`, `colors`, `typography`,
  `spacing`, `effects`, `base`. `src/styles/global.css` is nothing but the chain
  of `@import`s in that order — primitive tokens first, then semantic aliases,
  then base styles.
- Four UI primitives from `Segaris/src/frontend/src/components/ui/`: `Button`,
  `IconButton`, `Card`, `Spinner`, each with its `.css` and its `.test.tsx`, plus
  the barrel `index.ts`.
- `src/components/shell/AppShell.tsx`, adapted to Blackwing's own navigation.

Deliberately **not** ported yet: `Input`, `Select`, `Checkbox`, `Switch`,
`Badge`, `Avatar`, `Dialog`, `Toast`, `Tooltip`, `Tabs`, `SegmentedControl`.
Each later phase ports the primitive it actually needs, from Segaris, unchanged.
A primitive that nothing uses is dead weight that still has to be maintained.

Unlike Segaris, Blackwing keeps `--sidebar-w`: the prototype gives Blackwing a
persistent side navigation. Before pruning any token, check the prototype.

## Rules the code must respect

These are properties of the design language, not preferences:

- **Light theme only.** There is no dark mode.
- **No pure black and no pure white.** Every neutral comes from the palette.
- **Elevation is a warm glow, never a gray shadow.**
- **Everything is rounded:** controls 12px, cards 20px, dialogs 24px, pills
  999px.
- **Aurora background:** drifting blobs beneath acrylic surfaces.
- **Focus is always aqua**, and always visible.
- **Sentence case** everywhere in the interface.
- **Icons are Lucide**, through `lucide-react` — never a CDN, never an icon font.
- **No emoji.**
- Every infinite animation respects `prefers-reduced-motion`.

## Text

No component contains a visible literal string. Everything goes through i18next;
see `frontend.md`. That rule interacts with the design system in one place worth
naming: sentence case is a property of the translated resource, not something a
component applies with CSS `text-transform`.
