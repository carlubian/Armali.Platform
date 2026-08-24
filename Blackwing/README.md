# Blackwing

Blackwing is a private, multi-user photo library. Each user uploads their own
images, tags them with three tag types — person, place and topic — and browses
their gallery filtering by those tags, combined with AND. Images and tags are
private and unique per account.

Blackwing is one application inside the `Armali.Platform` monorepo, a sibling of
Segaris and Belfalas. The Git repository is one folder above this one; the
GitHub workflows live at the repository root, everything else lives here.

## Repository shape

```
Blackwing/
  deploy/        Caddy ingress and Docker Compose stacks
  docs/          Architecture documentation
  scripts/       Repeatable PowerShell development commands
  src/backend/   .NET 10 solution: Blackwing.Api, .Persistence, .Shared
  src/frontend/  React 19 + Vite + TypeScript client
  tests/backend/ Unit, integration and architecture tests
```

## Prerequisites

- .NET SDK `10.0.100` (pinned in `global.json`)
- Node `24.16.0` (pinned in `.node-version`) and pnpm 11 via Corepack
- Docker, for Compose and for the Testcontainers-based integration tests

## Development commands

All scripts live in `scripts/` and are run with PowerShell.

| Command | What it does |
| --- | --- |
| `./scripts/backend-restore.ps1` | Restores the backend solution |
| `./scripts/backend-build.ps1` | Builds the backend without restoring |
| `./scripts/backend-test.ps1` | Runs the backend test suite |
| `./scripts/backend-format.ps1` | Formats the backend solution |
| `./scripts/backend-run.ps1` | Runs `Blackwing.Api` natively |
| `./scripts/frontend-restore.ps1` | Installs frontend dependencies |
| `./scripts/frontend-lint.ps1` | Lints the frontend |
| `./scripts/frontend-build.ps1` | Type-checks and builds the frontend |
| `./scripts/frontend-test.ps1` | Runs the frontend test suite |
| `./scripts/frontend-format.ps1` | Formats the frontend |
| `./scripts/frontend-run.ps1` | Runs the Vite dev server |
| `./scripts/compose-up.ps1` | Builds and starts the full local stack |
| `./scripts/compose-down.ps1` | Stops the local stack |
| `./scripts/infra-up.ps1` | Starts PostgreSQL (and optionally Seq) only |
| `./scripts/infra-down.ps1` | Stops the infrastructure stack |

Do not run the full integration suite locally; see `AGENTS.md`.

## Local development

Two supported modes:

**Native.** `./scripts/infra-up.ps1` for PostgreSQL, then
`./scripts/backend-run.ps1` (HTTP on `5006`) and `./scripts/frontend-run.ps1`.
The Vite dev server proxies `/api` to the backend, so the browser always
requests relative URLs.

**Full stack in Compose.** `./scripts/compose-up.ps1` builds and starts backend,
frontend, PostgreSQL and the Caddy ingress. The application is served at
<http://localhost:5526/>; the backend is reachable only through Caddy, under
`/api`.

## Configuration and secrets

No secrets are committed. `src/backend/appsettings.json` is ignored; the shape of
the configuration is documented in `src/backend/appsettings.example.json`.
Compose reads `deploy/compose/.env`, which is also ignored;
`deploy/compose/.env.example` documents every variable.

Every `VITE_*` variable is embedded in the client bundle and is therefore
public. Never put a secret there.

## Health endpoints

- `GET /health/live` — the process is up. No external dependencies.
- `GET /health/ready` — PostgreSQL is reachable and the images directory is
  writable.

## Documentation

- `docs/architecture/backend.md`
- `docs/architecture/frontend.md`
- `docs/architecture/design-system.md`
- `docs/architecture/deployment.md`
