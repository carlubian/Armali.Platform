# Phase Protocol

This document describes how to run the planning process across conversations with limited context.

## Principle

The files in this repository are the memory of the project. A conversation may discover, debate, or decide something, but it only becomes durable once it is written to the appropriate markdown file.

## Conversation Startup

Every new planning conversation should begin by reading:

1. `AGENTS.md`
2. `README.md`
3. `ROADMAP.md`
4. Relevant files under `docs/`

The agent should then provide a short Spanish summary of:

- Current phase.
- Known decisions.
- Open decisions relevant to the requested topic.
- Proposed next step.

## Phase 1 Workflow

Use Phase 1 to discuss architecture and structure.

Recommended loop:

1. Pick one architecture category from `ROADMAP.md`.
2. Ask focused questions in Spanish.
3. Offer concrete options with tradeoffs.
4. Record decisions and rationale.
5. Update `ROADMAP.md`.
6. Add or update files under `docs/architecture/` when a topic becomes substantial.

Phase 1 is complete when:

- The main technology stack is chosen.
- The high-level architecture is documented.
- Core shared concepts are identified.
- The remaining questions are mostly functional rather than structural.

## Phase 2 Workflow

Use Phase 2 to define application behavior in detail.

Recommended loop:

1. Select one open functional topic from `ROADMAP.md`.
2. Ask detailed questions about real workflows and edge cases.
3. Suggest defaults or patterns when helpful.
4. Write requirements under `docs/requirements/`.
5. Mark the corresponding roadmap item as resolved, deferred, or split into new questions.

A requirements document should usually include:

- Purpose.
- Users and roles involved.
- Core entities.
- Workflows.
- Rules and constraints.
- Edge cases.
- Open questions.
- Acceptance criteria.

## Phase 3 Workflow

Use Phase 3 to convert requirements into implementation versions.

Recommended loop:

1. Review all resolved requirements.
2. Identify dependency order.
3. Define small vertical versions.
4. Create one file per version under `docs/versions/`.
5. Keep each version independently understandable.

A version document should usually include:

- Goal.
- User value.
- Scope.
- Non-scope.
- Required decisions already made.
- Implementation notes.
- Data model impact.
- Testing and acceptance criteria.
- Follow-up work.

## Roadmap Maintenance

When updating `ROADMAP.md`:

- Add new open questions as soon as they appear.
- Change `Open` to `Resolved` only after the decision is documented.
- Use `Deferred` when a decision is intentionally postponed.
- Keep short notes pointing to the relevant document when possible.

## Suggested Documentation Locations

```text
docs/
|-- architecture/
|   |-- stack.md
|   |-- domain-structure.md
|   `-- data-and-storage.md
|-- requirements/
|   |-- purchases.md
|   |-- expenses.md
|   |-- inventory.md
|   `-- travel.md
|-- versions/
|   |-- v0-foundation.md
|   |-- v1-first-domain.md
|   `-- v2-expansion.md
`-- planning/
    `-- PHASE_PROTOCOL.md
```

