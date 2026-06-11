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

## Backend Implementation Status

Wave 1 of the backend foundation is complete. The repository now contains:

- The .NET 10 solution at `src/backend/Segaris.slnx`.
- The executable `Segaris.Api` composition root and deliberately small `Segaris.Shared` project.
- Explicit `Platform` and `Modules/Identity` registration points.
- Five backend test projects under `tests/backend`.
- Central build, analyzer, formatting, and package-version policies.
- Startup validation for the active database provider and connection string.
- A liveness endpoint at `/health/live`.
- Repeatable PowerShell commands under `scripts/`.

To run the backend locally:

1. Copy `src/backend/appsettings.example.json` to `src/backend/appsettings.json` and review its values.
2. Run `./scripts/backend-restore.ps1`.
3. Run `./scripts/backend-build.ps1` and `./scripts/backend-test.ps1`.
4. Run `./scripts/backend-run.ps1`.

Use `./scripts/backend-format.ps1 -Verify` to check repository formatting without changing files. Wave 2 will add EF Core, SQLite/PostgreSQL provider selection, migrations, and the development reset/seed command described in the foundation decisions.

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

