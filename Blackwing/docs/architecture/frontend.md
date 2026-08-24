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
    api/                HTTP client, error shapes, one file per API area
    config/env.ts       Typed, validated access to import.meta.env
    i18n/               i18next setup, resources, formatters
    query/queryClient.ts  TanStack Query client and its defaults
    routing/AppRouter.tsx React Router route table and guards
    session/            Session context: who is signed in, and expiry
  assets/fonts/         The four self-hosted .woff2 faces
  components/
    feedback/            Loading, service-unavailable and not-found screens
    shell/AppShell       Sidebar + topbar + aurora body
    ui/                  Ported design-system primitives
  modules/
    admin/               Account administration (Admin only)
    auth/                The login screen
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

The topbar also carries the signed-in account and a sign-out action, and the
navigation gains an account-administration entry that is **hidden**, not
disabled, for anyone who is not an `Admin`. The distinction is deliberate: a
disabled entry advertises that the area exists.

The content area does not scroll on itself: `.bw-body` carries the aurora and
clips it, and an inner `.bw-body__scroll` does the scrolling. Without that split
the aurora blobs (`inset: -25%`) overflow the page.

## The HTTP client, and CSRF

`app/api/` is the only place that talks to the backend. Nothing else builds a
URL or calls `fetch`.

- **`errors.ts`** — `ApiError`, `ApiErrorKind` and the `ProblemDetails` shape the
  backend returns. A failed call always surfaces as an `ApiError`, so screens
  branch on a kind, never on a status number scattered through components.
- **`client.ts`** — the transport. Three parts of it are load-bearing and should
  not be simplified away:
  - a request timeout driven by `AbortSignal`, so a hung backend fails the query
    instead of hanging the screen;
  - a cached CSRF token, fetched from `GET /api/session/antiforgery` and sent as
    `X-CSRF-TOKEN` on **every** mutation;
  - `suppressSessionExpired`, so a 401 from the login call is a form error rather
    than a global session expiry.
- **`session.ts`** — `getSession`, `signIn`, `signOut`. `signIn` calls
  `resetCsrfToken()` after authenticating: the cached token was bound to the
  anonymous identity, and without the reset the next mutation fails with a 400
  that looks like nothing.
- **`adminUsers.ts`** — `list`, `create`, `resetPassword`, `activate`,
  `deactivate`.

Paths are composed from `appConfig.apiBaseUrl` rather than hardcoding `/api`, so
the validated environment layer stays the single source of that value.

Any request that comes back 401 outside the login flow raises a
`blackwing:session-expired` event; the session context listens for it and moves
the whole application to the unauthenticated state at once.

## Session and route guards

`app/session/SessionContext.tsx` holds the only answer to "who is signed in".
It exposes a status of `loading`, `authenticated`, `unauthenticated` or
`unavailable`, the session itself, plus `refresh` and `signOut`. There is no
profile query and no language switch: the interface is Spanish only.

`AppRouter` puts every screen behind that status:

- `/login` is the **only public route**.
- `loading` renders the loading screen, `unavailable` the service-unavailable
  screen with a retry, and `unauthenticated` redirects to `/login`.
- `/admin/users` additionally requires the `Admin` role. A `User` who reaches it
  by typing the URL gets the **not-found** screen, not an access-denied one: the
  answer must not confirm that the area exists. It is the same reasoning the
  backend applies when it answers 404 for another account's resource.

Client-side guards are convenience, not security. Every one of them has a
server-side counterpart; see `identity.md`.

## Data fetching

TanStack Query, with the client created in `app/query/queryClient.ts`. Retries
are conservative: `1` for queries, none for mutations. Every call goes through
`app/api/client.ts`, so a retry predicate can now be narrowed against `ApiError`
whenever a case appears that actually needs it.

## Tests

Vitest with jsdom, Testing Library, and `axe-core` for accessibility assertions.
`pnpm test` runs single-worker on purpose, matching Segaris.

Playwright is installed and `test:e2e` exists, but there are no end-to-end tests
yet; those arrive in phase 8.
