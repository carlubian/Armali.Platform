# Segaris

Segaris is an internal household management web application for purchases,
expenses, inventory, travel, assets, maintenance, and other household records.

The repository is in active implementation. It is a monorepo with an ASP.NET
Core backend, a React frontend, shared deployment assets, and focused technical
documentation.

## Repository Shape

```text
.
|-- src/
|   |-- backend/       .NET 10 ASP.NET Core modular monolith
|   `-- frontend/      React and TypeScript SPA
|-- tests/             Backend, frontend, integration, and end-to-end tests
|-- deploy/            Compose and deployment assets
|-- scripts/           Repeatable development and operational commands
|-- docs/
|   |-- architecture/  Accepted system and application design
|   |-- operations/    Deployment, backup, restore, and rollback runbooks
|   |-- planning/      Plans and decision records, including historical ones
|   `-- ui-design/     Design-system assets and screen references
|-- AGENTS.md          Agent workflow and documentation routing
`-- ROADMAP.md         Open and resolved product or architecture decisions
```

## Current State

The backend foundation is implemented as a .NET 10 modular monolith with:

- ASP.NET Core Minimal APIs and a single Entity Framework Core context.
- SQLite for local development and PostgreSQL for production.
- Cookie-based Identity authentication, antiforgery, and administrative user
  management.
- Attachments, persistent background jobs, administrative backups, health
  checks, structured logging, and diagnostics.
- Provider-specific migrations, integration tests, container images, Compose
  definitions, and GitHub Actions validation/publication workflows.

Frontend implementation is maintained independently under `src/frontend` and
integrates with the backend through the same-origin `/api` boundary.

For current behavior, inspect the relevant code and tests. Completed plans under
`docs/planning/` remain available for historical rationale but are not required
reading for ordinary development tasks.

## Backend Development

Create `src/backend/appsettings.json` from
`src/backend/appsettings.example.json`, then use the repository scripts:

```powershell
./scripts/backend-restore.ps1
./scripts/backend-build.ps1
./scripts/backend-test.ps1
./scripts/backend-run.ps1
```

Useful supporting commands:

```powershell
./scripts/backend-format.ps1 -Verify
./scripts/backend-reset.ps1 -Confirm
./scripts/backend-seed.ps1
./scripts/foundation-acceptance.ps1
```

PostgreSQL integration tests require Docker. To run supporting infrastructure
while developing the backend natively:

```powershell
./scripts/infra-up.ps1
./scripts/infra-down.ps1
```

Add `-Seq` to `infra-up.ps1` when the local Seq UI is needed.

## Frontend Development

The frontend is a Vite + React + TypeScript single-page application under
`src/frontend`, managed with pnpm 11.x (usually provisioned through Corepack;
the exact pnpm patch/minor version is intentionally not pinned). Create
`src/frontend/.env` from `src/frontend/.env.example`, then use the repository
scripts:

```powershell
./scripts/frontend-restore.ps1
./scripts/frontend-build.ps1
./scripts/frontend-test.ps1
./scripts/frontend-run.ps1
```

Useful supporting commands:

```powershell
./scripts/frontend-lint.ps1
./scripts/frontend-format.ps1 -Verify
./scripts/frontend-test.ps1 -E2E
```

`frontend-run.ps1` starts the Vite development server, which proxies `/api` to
the backend (default `http://localhost:5004`, overridable with
`SEGARIS_FRONTEND_PROXY_TARGET`) so session and antiforgery cookies stay
same-origin. Run the backend with `./scripts/backend-run.ps1` alongside it.

End-to-end tests require the Playwright browsers, installed once with
`corepack pnpm -C src/frontend exec playwright install chromium`.

## Containerized Stack

Run the complete local stack and open `http://localhost:5525/`:

```powershell
./scripts/compose-up.ps1
./scripts/compose-down.ps1
```

The stack contains PostgreSQL, backend, frontend, and Caddy ingress. Production
deployment and recovery procedures are documented under `docs/operations/`.

## Documentation Guide

Read documentation according to the task rather than as a complete startup set:

- `docs/architecture/backend.md` for backend composition and module structure.
- `docs/architecture/frontend.md` for frontend structure and state ownership.
- `docs/architecture/data-and-storage.md` for persistence and file storage.
- `docs/architecture/deployment.md` for runtime and container topology.
- `docs/architecture/development-and-operations.md` for testing, CI, and
  observability.
- `docs/operations/` for deployment, backup/restore, and rollback procedures.
- `ROADMAP.md` for planning work and unresolved product or architecture choices.
- `docs/planning/` only when a task needs the rationale or scope recorded in a
  specific plan or decision log.

See `AGENTS.md` for the agent startup routine and task-specific documentation
routing.
