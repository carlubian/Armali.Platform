# Frontend architecture

React 19 + Vite + TypeScript, under `src/frontend`. Node `24.16.0` and pnpm 11
through Corepack; both are pinned, and `save-exact=true` means every dependency
is fixed to an exact version.

## Structure of `src/`

```
src/
  main.tsx              Entry point: providers, then the router
  app/
    App.tsx             Provider composition
    config/env.ts       Typed, validated access to import.meta.env
    i18n/               i18next setup, resources, formatters
    query/queryClient.ts  TanStack Query client and its defaults
    routing/AppRouter.tsx React Router route table
  assets/fonts/         The four self-hosted .woff2 faces
  components/
    shell/AppShell       Sidebar + topbar + aurora body
    ui/                  Ported design-system primitives
  modules/
    startup/             The startup screen
  styles/               global.css and the token layer
  test/setup.ts         Vitest setup
```

Product features live under `modules/<feature>/`. A module owns its screens, its
CSS and its own i18next namespace under `modules/<feature>/i18n/resources.ts`;
`app/` holds only cross-cutting wiring, and `components/` only what more than
one module uses.

## Public configuration vs. secrets

Everything reachable from `import.meta.env` is compiled into the bundle and
shipped to the browser. **A `VITE_*` variable is public. Never put a secret in
one.** Real secrets stay in the backend.

`app/config/env.ts` is the only place that reads `import.meta.env`. It validates
what it reads and fails loudly on a bad value. `apiBaseUrl` is mandatory and an
**absolute URL is rejected**: the browser must always request `/api/...`
relative, so the session cookie and the antiforgery token behave identically in
local development and behind the Caddy ingress.

`.env.example` documents the two variables (`VITE_API_BASE_URL=/api`,
`VITE_APP_VERSION=development`). The real `.env` is not committed.

## The `/api` proxy

In development, Vite proxies `/api` to the backend with `changeOrigin: false`, so
the origin the browser sees never changes. The target defaults to
`http://localhost:5006` and can be overridden with the Node environment variable
`BLACKWING_FRONTEND_PROXY_TARGET`.

In containers there is no proxy: the Caddy ingress owns the `/api` prefix. The
client code is identical in both cases, which is the whole point.

## i18next, and the no-literals rule

The interface is in Spanish, but it is translated from day one. `app/i18n/i18n.ts`
configures `es-ES` as the only supported language and as the fallback,
`defaultNS: 'platform'`, `escapeValue: false` and `returnNull: false`. Resources
are declared in `app/i18n/resources.ts`.

**No component may contain a visible literal string.** This is enforced twice,
and both nets are verified to fail when a literal is introduced:

1. The ESLint rule `react/jsx-no-literals` (`noStrings`, `ignoreProps`, tests
   excluded) rejects literal text in JSX.
2. `app/i18n/literals.test.tsx` renders the application tree and asserts that
   every visible string **and** every `aria-label`, `title`, `alt` and
   `placeholder` is a value declared in `resources.ts`, interpolation included.

The rule covers accessible names on purpose: a hard-coded English `aria-label` is
invisible on screen and still wrong for a screen-reader user in a Spanish
interface. That is also why `Spinner` takes a mandatory `label` rather than
defaulting to one, unlike its Segaris counterpart.

## Shell and navigation

`AppShell` is a persistent 264px sidebar (`--sidebar-w`, which Blackwing keeps
even though Segaris pruned it) with the brand mark and five entries — gallery,
upload, review, tags, settings — plus a 64px acrylic topbar whose eyebrow and
heading derive from the active entry, over the aurora body.

Only the gallery route exists in this phase; the other four entries render as
disabled buttons with a translated `title`, because inventing screens that belong
to phases 5-7 would be worse than showing them unavailable.

The content area does not scroll on itself: `.bw-body` carries the aurora and
clips it, and an inner `.bw-body__scroll` does the scrolling. Without that split
the aurora blobs (`inset: -25%`) overflow the page.

## Data fetching

TanStack Query, with the client created in `app/query/queryClient.ts`. Retries
are conservative (`1` for queries, none for mutations) and deliberately not yet
conditioned on the error shape: there is no API client layer until phase 2, and
the retry predicate should be narrowed when there is a real error type to
inspect.

## Tests

Vitest with jsdom, Testing Library, and `axe-core` for accessibility assertions.
`pnpm test` runs single-worker on purpose, matching Segaris.

Playwright is installed and `test:e2e` exists, but there are no end-to-end tests
yet; those arrive in phase 8.
