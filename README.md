# Armali Platform

Armali Platform is planned as an internal household management application. Its purpose is to centralize everyday household operations such as purchases, expenses, inventory, travel, and related planning or tracking workflows.

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

