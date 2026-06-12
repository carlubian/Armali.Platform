# Segaris

Segaris is planned as an internal household management application. Its purpose is to centralize everyday household operations such as purchases, expenses, inventory, travel, and related planning or tracking workflows.

This repository is currently in the planning stage. The main objective is to preserve enough written context that future agents can continue the project across separate conversations without depending on chat history.

## Current Phase

Current phase: **Phase 1 - Architecture, Structure, And Core**.

In this phase, the focus is the application foundation:

- Architecture and system boundaries.
- Main components and modules.
- Framework and technology choices.
- Cross-cutting concerns such as authentication, data storage, observability, background jobs, and deployment.
- A living roadmap of decisions still pending.

This phase should not go deep into detailed business functionality yet. Specific domains like shopping, expenses, inventory, and travel will be defined during Phase 2.

## Product Shape Decisions

The initial product shape is now defined:

- A standard web application hosted on an internal household server.
- A separate C# / ASP.NET Core backend exposing a REST API.
- A separate TypeScript frontend optimized for desktop computers and large displays.
- Chromium-based browsers as the primary supported platform.
- One household with a small number of distinct `User` and `Admin` accounts.
- Public household entities and creator-only private entities.
- Online-only operation with explicit handling for unavailable services and expired sessions.
- Spain as the initial regional context, with internationalization built into the frontend architecture.
- Deployment to a local Ubuntu server using separate Docker images for the frontend and backend.
- Persistent data stored outside the containers through Docker volumes where required.
- Relational persistence through Entity Framework Core, using SQLite for local development and PostgreSQL for production.

See [`docs/architecture/product-shape.md`](docs/architecture/product-shape.md) for the decisions, rationale, constraints, and explicit non-goals.
See [`docs/architecture/deployment.md`](docs/architecture/deployment.md) for the initial runtime and containerization decisions.
See [`docs/architecture/data-and-storage.md`](docs/architecture/data-and-storage.md) for database-provider and migration decisions.
See [`docs/architecture/user-experience.md`](docs/architecture/user-experience.md) for the launcher-based navigation and immersive module experience.
See [`docs/architecture/frontend.md`](docs/architecture/frontend.md) for the selected React, TypeScript, and Vite SPA foundation and remaining frontend decisions.
See [`docs/architecture/backend.md`](docs/architecture/backend.md) for the ASP.NET Core modular-monolith structure and backend module boundaries.
See [`docs/architecture/domain-organization.md`](docs/architecture/domain-organization.md) for module ownership, dependency direction, and cross-domain reference rules.
See [`docs/architecture/shared-core.md`](docs/architecture/shared-core.md) for the deliberately minimal set of shared primitives and explicit exclusions.
See [`docs/architecture/integrations.md`](docs/architecture/integrations.md) for provider adapters, resilience boundaries, webhook handling, and external-data privacy requirements.
See [`docs/architecture/development-and-operations.md`](docs/architecture/development-and-operations.md) for repository organization and the evolving development and delivery model.
See [`docs/planning/BACKEND_CORE_IMPLEMENTATION_PLAN.md`](docs/planning/BACKEND_CORE_IMPLEMENTATION_PLAN.md) for the dependency-ordered implementation plan for the backend, tests, and deployment foundation.
See [`docs/planning/BACKEND_FOUNDATION_DECISIONS.md`](docs/planning/BACKEND_FOUNDATION_DECISIONS.md) for the completed Wave 0 decisions covering .NET 10, project naming, backend configuration, and local database reset/seed conventions.
See [`docs/planning/BACKEND_MODULE_CONVENTIONS.md`](docs/planning/BACKEND_MODULE_CONVENTIONS.md) for the Wave 3 implementation path that new backend modules must follow.
See [`docs/planning/BACKEND_IDENTITY_DECISIONS.md`](docs/planning/BACKEND_IDENTITY_DECISIONS.md) for the Wave 4 identity, session, antiforgery, administrative-user, and credential-lifecycle decisions.
See [`docs/planning/BACKEND_ATTACHMENT_DECISIONS.md`](docs/planning/BACKEND_ATTACHMENT_DECISIONS.md) for the Wave 5 attachment security, storage, ownership, and recovery decisions.
See [`docs/planning/BACKEND_BACKUP_DECISIONS.md`](docs/planning/BACKEND_BACKUP_DECISIONS.md) for the Wave 6 persistent background-job infrastructure, backup authorization, package format, and recovery decisions.
See [`docs/planning/BACKEND_OBSERVABILITY_DECISIONS.md`](docs/planning/BACKEND_OBSERVABILITY_DECISIONS.md) for the Wave 7 logging, Seq, correlation, health, diagnostics, rate-limit, and redaction decisions.
See [`docs/planning/BACKEND_DEPLOYMENT_DECISIONS.md`](docs/planning/BACKEND_DEPLOYMENT_DECISIONS.md) for the Wave 8 server baseline, container identity/image, Caddy ingress, Compose topology, persistence layout, secret injection, and recovery decisions.
See [`docs/planning/BACKEND_CI_DECISIONS.md`](docs/planning/BACKEND_CI_DECISIONS.md) for the Wave 9 required checks, branch protection, Azure OIDC image publication, and foundation-acceptance decisions.
See [`docs/operations/`](docs/operations/) for the deployment, backup/restore, and rollback runbooks.

## Backend Implementation Status

Waves 1 through 8 of the backend foundation are complete. Wave 9 is implemented
locally; its GitHub environment, Azure OIDC federation, and `main` branch
protection are active. The first controlled publication run remains pending. The
repository now contains:

- The .NET 10 solution at `src/backend/Segaris.slnx`.
- The executable `Segaris.Api` composition root and deliberately small `Segaris.Shared` project.
- Explicit `Platform` and `Modules/Identity` registration points.
- Five backend test projects under `tests/backend`.
- Central build, analyzer, formatting, and package-version policies.
- Startup validation for the active database provider and connection string.
- A liveness endpoint at `/health/live`.
- A single `SegarisDbContext` with module-owned model contributors.
- Runtime SQLite/PostgreSQL selection and provider-specific migration assemblies.
- Automatic startup migrations that fail startup before HTTP traffic is accepted.
- Development-only, explicitly confirmed database reset and idempotent seed commands.
- SQLite migration tests and PostgreSQL compatibility coverage through Testcontainers.
- Shared identity, visibility, UTC time, metadata, ISO currency, pagination, sorting, and error-code primitives.
- A claims-backed current-user adapter and creator-only privacy policy that administrators do not bypass.
- Standard `/api` route groups, camel-case JSON, bounded request bodies, cancellation propagation, and explicit DTO probes.
- Centralized ProblemDetails responses with stable error codes and trace identifiers.
- Bounded page-based pagination, allow-listed deterministic sorting, and a stable tie-breaker convention.
- OpenAPI 3.1 generation in Development and Testing, with Scalar interactive documentation only in Development.
- Architecture tests that enforce shared-core dependency and excluded-abstraction rules.
- ASP.NET Core Identity integrated into the shared context with `int` keys, a 12-character password policy, and five-attempt/15-minute lockout.
- A hardened same-origin session cookie, antiforgery for cookie-authenticated writes, and filesystem-persisted Data Protection keys.
- `/api/session` endpoints (login, current session, logout, password change) and administrative user management under `/api/admin/users`.
- Configuration-driven first-administrator bootstrap and session invalidation on deactivation and credential recovery.
- Narrow attachment contracts with a 25 MiB positive allow-list, content validation, UUID filesystem names, relational metadata, and owner-bound access.
- Compensating create/delete operations, reconciliation diagnostics for missing and orphaned files, and attachment-storage readiness at `/health/ready`.
- Paired SQLite/PostgreSQL attachment migrations with upgrade coverage from the previous Identity schema.
- A persistent background-job foundation with a single `platform_background_jobs` table, central state-machine validation, a single-instance `BackgroundService` worker, atomic claiming, portable single-run exclusivity, cooperative cancellation, and interrupted-job recovery at startup.
- An administrative backup capability under `/api/backup-jobs` that generates a single `segaris-backup.tar` package (PostgreSQL `pg_dump`, live attachments, and a hashed manifest) in staging and atomically replaces the previous package; the package is read from the backups volume rather than downloaded through the API, and backups require the PostgreSQL provider.
- Paired SQLite/PostgreSQL background-job migrations with upgrade coverage from the previous attachment schema.
- Compact structured Serilog events on `stdout`/`stderr`, category-specific levels, and optional bounded best-effort Seq delivery that never affects readiness or core operation.
- Request correlation through `X-Trace-ID`, ProblemDetails, request completion events, and accepted frontend diagnostic responses.
- `/health/ready` coverage for database connectivity, pending migrations, and writable attachment storage, while `/health/live` remains process-only.
- A protected `/api/diagnostics/frontend` endpoint with a fixed schema, antiforgery, validated payload/rate limits, and known-secret redaction, plus a separate login rate limit.
- A multi-stage backend Dockerfile running as the non-root identity `5525:5525` and bundling the PostgreSQL 17 client for backups, a temporary frontend placeholder image, and a `segaris-caddy` ingress image with baked-in `/api/*` and frontend routing.
- Compose definitions for production/Portainer (`docker-compose.yml`), local builds (`docker-compose.local.yml`), and infrastructure-only native development (`docker-compose.infra.yml`), with PostgreSQL on a named volume and attachments, backups, and Data Protection keys on bind mounts under `SEGARIS_DATA_PATH`, published through Caddy on `SEGARIS_HTTP_PORT` (default 5525).
- Host provisioning, Compose smoke-test, and restore scripts, plus deployment, backup/restore, and rollback runbooks under `docs/operations/`, with production secrets injected as Portainer stack environment variables.
- Secret-free pull-request validation with required `Segaris Backend`, `Segaris PostgreSQL`, and `Segaris Compose` checks, plus trusted main-branch publication of immutable backend, frontend, and Caddy images to ACR through Azure OIDC.
- A complete local foundation gate at `scripts/foundation-acceptance.ps1` that mirrors the required CI boundaries.
- Repeatable PowerShell commands under `scripts/`.

To run the backend locally:

1. Copy `src/backend/appsettings.example.json` to `src/backend/appsettings.json` and review its values, including storage, diagnostics, and optional Seq settings. To create the first administrator, set `Segaris:Identity:Bootstrap:UserName` and `:Password` (preferably through user secrets or environment variables); leave them empty to seed only the platform roles. Generating a backup additionally requires the PostgreSQL provider and the `pg_dump` client tool on `PATH`.
2. Run `./scripts/backend-restore.ps1`.
3. Run `./scripts/backend-build.ps1` and `./scripts/backend-test.ps1`.
4. Run `./scripts/backend-run.ps1`.

Use `./scripts/backend-format.ps1 -Verify` to check repository formatting without changing files. Use `./scripts/backend-reset.ps1 -Confirm` to recreate the configured Development database and seed it, or `./scripts/backend-seed.ps1` to run only the idempotent seed phase. PostgreSQL integration tests require Docker locally and are mandatory in CI.

Use `./scripts/foundation-acceptance.ps1` to run the complete backend-foundation acceptance gate locally, including all backend suites and the disposable Compose smoke test.

To run the supporting services in containers while developing the backend natively, run `./scripts/infra-up.ps1` (add `-Seq` for the Seq UI) and point the backend at `Host=localhost;Port=5432;Database=segaris;Username=segaris;Password=segaris`; stop them with `./scripts/infra-down.ps1`.

To run the complete stack (PostgreSQL, backend, frontend placeholder, and Caddy ingress) in containers, run `./scripts/compose-up.ps1` and open `http://localhost:5525/`; stop it with `./scripts/compose-down.ps1`. The Bash smoke test `./scripts/compose-smoke-test.sh` builds the stack, verifies readiness and Caddy routing, and tears it down. See [`docs/operations/deployment.md`](docs/operations/deployment.md) for production deployment through Portainer.

## Planning Phases

### Phase 1: Architecture, Structure, And Core

Define the technical and organizational foundation of the application.

Outputs:

- General project README.
- Roadmap of pending decisions grouped by category.
- Architecture notes as needed.

### Phase 2: Functional Definition

Work through every pending roadmap item and define the application behavior in detail.

Outputs:

- Requirements documents under `docs/requirements/`.
- Updated roadmap with resolved, deferred, and newly discovered decisions.

### Phase 3: Incremental Version Planning

Organize the future implementation into incremental versions that are atomic, understandable, and useful.

Outputs:

- Version documents under `docs/versions/`.
- Acceptance criteria and implementation context for each version.

## Repository Map

```text
.
|-- AGENTS.md
|   Agent instructions and continuity rules.
|-- README.md
|   General project overview and current phase.
|-- ROADMAP.md
|   Living list of open planning decisions.
`-- docs/
    |-- architecture/
    |   Architecture notes, diagrams, and technical decisions.
    |-- requirements/
    |   Functional requirements created during Phase 2.
    |-- versions/
    |   Incremental implementation plans created during Phase 3.
    `-- planning/
        Planning process, phase protocol, and continuity notes.
```

## Continuity Rule

Before starting work in a new conversation, read:

1. `AGENTS.md`
2. `README.md`
3. `ROADMAP.md`
4. Any relevant file under `docs/`

Then summarize the current state and continue from the documented source of truth.

## Initial Domain Candidates

These are known candidate domains, not final feature definitions:

- Purchases and shopping lists.
- Expenses, recurring payments, and household finances.
- Inventory of food, supplies, documents, assets, or equipment.
- Trips, travel planning, bookings, itineraries, and budgets.
- Household tasks, routines, vendors, warranties, and maintenance.

Detailed domain behavior will be defined in Phase 2.

