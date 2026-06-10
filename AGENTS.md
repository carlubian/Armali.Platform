# Agent Operating Guide

This repository is the planning and implementation workspace for Armali Platform: an internal household management application.

The project is intentionally planned in phases. Future agents must recover context from the files in this repository, not from prior chat history.

## Required Startup Routine

At the start of every new conversation or task:

1. Read this file first.
2. Read `README.md` for the project overview and current phase.
3. Read `ROADMAP.md` for open decisions and planning backlog.
4. Read any phase-specific documents under `docs/` that are relevant to the task.
5. Summarize the current state before making substantial edits.

## Collaboration Language

The project owner communicates primarily in Spanish. Use Spanish for planning conversations, questions, requirements, and summaries unless the user asks otherwise.

Technical identifiers, filenames, code, and commit messages may remain in English when that is more conventional.

## Project Intent

Armali Platform will be an internal management application for a household. It is expected to cover domains such as:

- Purchases and shopping.
- Expenses and household finance tracking.
- Inventory and supplies.
- Trips and travel planning.
- Other operational household records that may be discovered during planning.

The early planning goal is not to define every feature immediately. The goal is to define the architecture, structure, core concepts, and a repeatable documentation process that lets later conversations continue without losing state.

## Phase Model

The project is organized into three planning phases before implementation.

### Phase 1: Architecture, Structure, And Core

Purpose:

- Discuss the application architecture.
- Decide the main components and frameworks.
- Define the documentation structure.
- Identify open decisions, grouped by category.

Expected outputs:

- `README.md`: general project overview for future agents.
- `ROADMAP.md`: living list of pending decisions.
- Supporting architecture notes under `docs/architecture/` when needed.

Avoid:

- Over-defining specific business features.
- Starting implementation work unless explicitly requested.

### Phase 2: Functional Definition

Purpose:

- Work through the pending decisions in `ROADMAP.md` one by one.
- Ask detailed questions about each area.
- Propose options and tradeoffs where useful.
- Convert decisions into functional requirements and domain documents.

Expected outputs:

- One or more documents under `docs/requirements/`.
- Updates to `ROADMAP.md` marking decisions as resolved, deferred, or newly discovered.

Avoid:

- Treating a feature as settled without documenting the decision and rationale.
- Removing open questions unless they are answered or explicitly deferred.

### Phase 3: Incremental Version Planning

Purpose:

- Convert the defined requirements into implementable versions.
- Keep each version atomic, testable, and useful on its own.
- Prepare markdown files that can be given directly to implementation agents.

Expected outputs:

- Version documents under `docs/versions/`.
- Each version document should include scope, non-scope, acceptance criteria, dependencies, and implementation notes.

Avoid:

- Creating oversized versions that cannot be implemented independently.
- Hiding unresolved requirements inside version plans.

## Documentation Rules

- Treat documentation as the source of truth.
- Update `README.md` when the project overview, architecture, or current phase changes.
- Update `ROADMAP.md` whenever a new unresolved decision appears.
- Add rationale for important decisions, not just the final answer.
- Prefer small, focused markdown files over one enormous document.
- Keep pending decisions visible until they are explicitly resolved or deferred.

## Suggested Directory Structure

```text
.
|-- AGENTS.md
|-- README.md
|-- ROADMAP.md
`-- docs/
    |-- architecture/
    |-- requirements/
    |-- versions/
    `-- planning/
```

## Current Status

Current phase: Phase 1.

The repository currently contains the planning scaffold. The next useful step is to discuss and document the architecture, structure, core modules, and framework choices for the application.

