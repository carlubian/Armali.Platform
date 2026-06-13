# Frontend Foundation Decisions

## Purpose

This document resolves the frontend foundation choices required by Wave 0 of
`FRONTEND_CORE_IMPLEMENTATION_PLAN.md`. Wave 2 must apply these conventions when
it creates `src/frontend`; changes to them require an explicit documentation
update rather than an incidental scaffold default.

## Runtime And Package Management

- Node.js `24.16.0` is the repository frontend runtime. Node 24 is the current
  LTS line and the exact version is pinned in `.node-version` and `.nvmrc`.
- pnpm `11.6.0` is the package manager. The exact version is pinned through the
  root `package.json` `packageManager` and `engines` fields.
- Corepack is the preferred package-manager launcher. Contributors may install
  the pinned pnpm release by another method, but committed lockfiles and CI use
  pnpm only.
- `pnpm-lock.yaml` is committed once Wave 2 introduces frontend dependencies.
  Dependency installation in CI and container builds uses
  `pnpm install --frozen-lockfile`.
- Dependency versions are saved exactly. Automated dependency updates may be
  introduced later, but the application must not rely on unreviewed range
  resolution.
- Frontend dependencies remain owned by `src/frontend/package.json`. The root
  package file establishes the repository toolchain and may later expose
  orchestration scripts; it does not merge backend and frontend build systems.

## Static Analysis And Formatting

Wave 2 will configure these tools:

- ESLint using flat configuration and type-aware TypeScript rules where they
  provide useful application checks.
- `typescript-eslint`, `eslint-plugin-react`,
  `eslint-plugin-react-hooks`, and `eslint-plugin-jsx-a11y`.
- Prettier for frontend TypeScript, TSX, JavaScript, JSON, CSS, and Markdown.
- TypeScript's compiler in no-emit mode as a separate type-check command.

ESLint owns correctness, React, hooks, and accessibility rules. Prettier owns
layout. Formatting rules are not duplicated in ESLint, and
`eslint-config-prettier` disables conflicting lint rules. Generated output,
coverage, Playwright artifacts, and build output are ignored by both tools.

The frontend package scripts will expose at least:

- `dev`, `build`, and `preview`.
- `lint` and `lint:fix`.
- `format` and `format:check`.
- `typecheck`.
- `test`, `test:watch`, and `test:coverage`.
- `test:e2e`.

Repository PowerShell wrappers added in Wave 2 will call these package scripts
and will not duplicate their implementation.

## Test Tooling

- Vitest runs unit and component tests in a `jsdom` environment.
- React Testing Library and `@testing-library/user-event` test observable
  behavior and user interaction. `@testing-library/jest-dom` supplies DOM
  assertions.
- Unit and component tests are colocated with production code as
  `*.test.ts` or `*.test.tsx`. Shared setup and test helpers live under
  `src/test/` and must not be imported by production modules.
- Playwright runs browser journeys in Chromium. Full-stack end-to-end specs live
  under `tests/frontend/e2e/` because they validate the deployed frontend,
  backend, Caddy, and PostgreSQL boundary rather than one frontend module.
- Playwright traces, screenshots, videos, reports, and authentication state are
  generated artifacts and are never committed unless a future visual-baseline
  decision explicitly says otherwise.
- Broad snapshots are not part of the default strategy. Tests assert behavior,
  accessibility, state transitions, and stable rendered outcomes.

## Development API Integration

The Vite development server proxies `/api` to the native backend at
`http://localhost:5004`, matching the backend HTTP launch profile.

The browser always requests relative `/api/...` URLs. Vite performs the proxying
only during local development; the production Caddy ingress routes the same path
to the backend. This preserves one browser origin for session and antiforgery
cookies, remains compatible with `SameSite=Strict`, and does not add a CORS
policy.

The proxy target may be overridden for a developer process with
`SEGARIS_FRONTEND_PROXY_TARGET`. This is a Vite configuration input read by
Node.js, not a `VITE_*` variable, and is never included in the browser bundle.
It defaults to `http://localhost:5004` and must be an absolute HTTP or HTTPS
origin. This override is development-only and is not part of production
application configuration.

## Frontend Environment Contract

Wave 2 will add `src/frontend/.env.example` with exactly this initial public
contract:

```dotenv
VITE_API_BASE_URL=/api
VITE_APP_VERSION=development
```

- `VITE_API_BASE_URL` is the relative API path prefix. The initial application
  requires `/api`; absolute cross-origin URLs are rejected so local and deployed
  cookie behavior stays consistent.
- `VITE_APP_VERSION` is a non-secret release identifier included in safe
  diagnostics. Local development uses `development`; CI may replace it with the
  immutable Git commit SHA during the production build.
- All `VITE_*` values are public build-time data because Vite embeds them in
  static assets. Passwords, keys, connection strings, private service URLs,
  registry credentials, and other secrets are prohibited.
- `src/frontend/.env` and variant local files are ignored. The example file is
  committed. Production values are supplied during the image build and cannot
  be changed at container runtime without rebuilding the static assets.
- Access to `import.meta.env` is centralized in `src/app/config/`; feature and
  module code consumes a validated typed configuration object.

No additional frontend environment variable is introduced until code has a
concrete need for it and its public, build-time semantics are documented.

## Source Tree Conventions

Wave 2 will create this baseline structure:

```text
src/frontend/
|-- public/
|-- src/
|   |-- app/                 # bootstrap, providers, router, configuration
|   |-- assets/              # imported images and self-hosted fonts
|   |-- components/
|   |   |-- ui/              # Project Armali primitives
|   |   `-- shared/          # composed cross-feature UI
|   |-- lib/                 # API, i18n, formatting, and narrow utilities
|   |-- modules/
|   |   |-- platform/        # auth, launcher, profile, and user management
|   |   `-- <domain>/        # future immersive business modules
|   |-- styles/              # tokens, reset, and global styles
|   `-- test/                # Vitest setup, fixtures, and render helpers
`-- package.json

tests/frontend/e2e/           # full-stack Playwright journeys
```

Conventions:

- `app/` is the composition root. Domain and platform modules do not import
  application bootstrap internals.
- `components/ui/` contains generic typed design-system primitives with no
  Segaris business knowledge. `components/shared/` contains reusable platform
  compositions.
- Each module owns its routes, screens, components, queries, forms, schemas,
  types, and translation resources. Internal folders are added only when their
  contents justify them; empty architectural folders are avoided.
- Cross-module imports use only an explicit public entry point (`index.ts`) or a
  shared contract. A module must not reach into another module's internal path.
- Shared code is promoted to `components/shared/` or `lib/` only after it has a
  genuine cross-module responsibility. `lib/` must not become a miscellaneous
  dumping ground.
- Path aliases use `@/` for `src/`. Relative imports remain preferred within a
  small local folder; the alias is for stable cross-area imports.
- Production filenames use kebab-case except React component files, which use
  PascalCase. Hooks use `use-*.ts`, and tests mirror the file they cover.
- Top-level immersive module route components are lazy-loaded. Platform startup,
  login, launcher, shared shell, and global failure screens remain eager.

## Deferred Decisions

Wave 0 does not select exact versions for application libraries or lint/test
plugins. Wave 2 will select mutually compatible current versions, record them
exactly in `src/frontend/package.json`, and lock them in `pnpm-lock.yaml`.

Frontend container serving, final CI check names, and end-to-end placement in CI
remain Wave 9 decisions as already recorded in the implementation plan. No new
open roadmap item was discovered during Wave 0.

## Rationale

The selected foundation follows the established SPA, same-origin cookie, and
independent frontend-build decisions. Exact runtime and package-manager pins make
the scaffold reproducible, while deferring application dependency versions to
Wave 2 avoids creating a partial frontend package before the scaffold exists.

## Wave 2 Implementation Notes

Wave 2 applied these conventions while scaffolding `src/frontend` and resolved
the following points that were left open or proved inexact above:

- pnpm 10+ reads project-wide settings from `pnpm-workspace.yaml`, not from
  `.npmrc` (which it now treats as registry/auth configuration). The
  exact-pinning and pinned-engine policies are therefore enforced in
  `src/frontend/pnpm-workspace.yaml` via `saveExact: true` and
  `engineStrict: true`. The repository-root `.npmrc` is not read for a project
  rooted at `src/frontend`.
- ESLint is pinned to the `9.x` line rather than `10.x`. `eslint-plugin-react`
  does not yet support ESLint 10's rule context API; ESLint 9 is the current
  line the React, hooks, accessibility, and `typescript-eslint` plugins target,
  and flat configuration is its default. Revisit when the React plugin ships
  ESLint 10 support.
- The selected, exactly pinned versions are recorded in
  `src/frontend/package.json` and locked in `src/frontend/pnpm-lock.yaml`. The
  scaffold runs on React 19, Vite 8, TypeScript 6, Vitest 4, and Playwright 1.6.
- The development proxy override `SEGARIS_FRONTEND_PROXY_TARGET` is validated in
  `vite.config.ts` (absolute HTTP/HTTPS origin only) and defaults to
  `http://localhost:5004`.
