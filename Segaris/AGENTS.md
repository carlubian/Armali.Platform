# Agent Operating Guide

This repository contains Segaris, an internal household management application.
The project is now in active implementation. Recover context from the repository,
but load only the documentation relevant to the task at hand.

## Required Startup Routine

At the start of every new conversation or task:

1. Read this file first.
2. Read `README.md` for the current repository shape and development commands.
3. Inspect the code, tests, and configuration directly related to the task.
4. Read only the relevant documents under `docs/` when implementation context or
   decision rationale is needed.
5. Read `ROADMAP.md` only for planning, architecture, product decisions, or work
   that may resolve or introduce an open decision.
6. Before substantial edits, briefly summarize the relevant current state.

Do not read every planning document by default. In particular, completed
implementation plans and decision logs under `docs/planning/` are historical
context unless the task refers to them or the current code leaves a decision
unclear.

## Collaboration Language

The project owner communicates primarily in Spanish. Use Spanish for planning
conversations, questions, requirements, and summaries unless the user asks
otherwise.

Technical identifiers, filenames, code, and commit messages may remain in
English when that is more conventional.

## Current Development Model

- Segaris is implemented as a monorepo with separate backend and frontend
  applications.
- The backend is a .NET 10 ASP.NET Core modular monolith under `src/backend`.
- The frontend is a React and TypeScript application under `src/frontend`.
- Cross-application tests live under `tests`, deployment assets under `deploy`,
  and repeatable development commands under `scripts`.
- Architecture and accepted technical decisions live under
  `docs/architecture/`.
- Operational procedures live under `docs/operations/`.
- Planning and historical implementation decisions live under
  `docs/planning/` and should be consulted selectively.

## Documentation Rules

- Treat accepted documentation as the source of truth for product and
  architecture decisions.
- Treat the current code, configuration, and tests as the source of truth for
  implemented behavior.
- Update `README.md` when repository-wide setup, commands, or current development
  status changes.
- Update `ROADMAP.md` when an unresolved product or architecture decision is
  introduced, resolved, or explicitly deferred.
- Update focused documents under `docs/` when a lasting decision or operational
  procedure changes.
- Preserve completed plans and decision logs as historical context; do not keep
  them in the default startup path.
- Prefer small, focused documentation changes over duplicating implementation
  details across entry-point files.

## Task-Specific Documentation Routing

- Backend architecture or module boundaries: `docs/architecture/backend.md`,
  `domain-organization.md`, and `shared-core.md` as relevant.
- Persistence, attachments, or backup behavior:
  `docs/architecture/data-and-storage.md` and the applicable operational guide.
- Frontend architecture or user experience: `docs/architecture/frontend.md`,
  `user-experience.md`, and `design-system.md` as relevant.
- Deployment, CI, observability, or recovery:
  `docs/architecture/development-and-operations.md`,
  `docs/architecture/deployment.md`, and `docs/operations/` as relevant.
- Product planning or unresolved domain behavior: `ROADMAP.md` and the applicable
  requirements or planning document.

## Implementation Expectations

- Follow existing project structure, naming, analyzers, formatting, and test
  conventions.
- Keep changes scoped to the requested behavior and preserve unrelated work in a
  dirty worktree.
- Add or update tests in proportion to the behavioral risk.
- Use the scripts documented in `README.md` for repeatable restore, build, test,
  format, and local execution workflows.
