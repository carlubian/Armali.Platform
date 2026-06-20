# Processes Implementation Plan

## Purpose

This plan delivers the initial Processes module defined in
`docs/requirements/PROCESSES_REQUIREMENTS.md`. It translates the accepted
functional decisions into dependency-ordered Waves with explicit backend,
frontend, migration, and test work.

The requirements document remains authoritative for behaviour. This plan owns
delivery order and technical scope.

## Delivery Principles

- Keep Processes an independent business module: it consumes only Identity,
  Attachments, the Configuration presentation boundary, and platform contracts,
  and it does not depend on or integrate with any other business module, including
  Projects.
- Reuse established Configuration, Attachments, privacy, REST, pagination,
  launcher-attention, and frontend conventions where their semantics match.
- Keep the sequential-execution invariants enforced in backend validation rather
  than inferred only by the frontend: the frontier rules (complete/skip the next
  pending step, undo only the most recently resolved step) and the contiguity
  invariant (resolved steps form a contiguous prefix).
- Derive the process status from its steps; never accept the derived status from
  the client. Persist only the manual terminal `Cancelled` override.
- Keep the step list editable at any time, preserving step state by identity and
  re-validating the contiguity invariant on every restructure.
- Keep each Wave independently testable and record deferrals in `ROADMAP.md`
  instead of hiding them in implementation notes.

## Fixed Technical Contracts

### Backend Module

Processes lives under `Segaris.Api.Modules.Processes` and owns the `Process` and
`Step` entities, the `ProcessCategory` catalogue, the derived-status and frontier
computation, the contiguity invariant, process attachment authorization, and the
launcher-attention contributor. It consumes the Configuration presentation
boundary and platform contracts. It does not depend on any other business module,
and no business module depends on Processes.

Indicative resource routes are:

```text
GET    /api/processes
POST   /api/processes
GET    /api/processes/{processId}
PUT    /api/processes/{processId}
DELETE /api/processes/{processId}

POST   /api/processes/{processId}/cancel
POST   /api/processes/{processId}/reopen

GET    /api/processes/{processId}/steps
PUT    /api/processes/{processId}/steps
POST   /api/processes/{processId}/steps/{stepId}/complete
POST   /api/processes/{processId}/steps/{stepId}/skip
POST   /api/processes/{processId}/steps/{stepId}/undo

GET    /api/processes/{processId}/attachments
POST   /api/processes/{processId}/attachments
GET    /api/processes/{processId}/attachments/{attachmentId}
DELETE /api/processes/{processId}/attachments/{attachmentId}

GET    /api/processes/categories
```

The step list `PUT` performs a full-collection restructure that preserves each
step's execution state by step identity; the `complete`, `skip`, and `undo`
actions advance or retract the frontier. `cancel` and `reopen` set and clear the
manual terminal override. Administrative category routes follow the existing
module-owned catalogue management pattern exposed through Configuration.

All writes require antiforgery. Missing and inaccessible records share the
platform not-found behaviour so private data is not disclosed.

### Persistence

The model contains module-owned entities with provider-specific migrations:

- `Process`
- `Step`
- `ProcessCategory`

`Process` stores the name, the category reference, the optional global due date,
optional notes, the `IsCancelled` override flag, visibility, and standard audit
metadata. The derived status is never persisted. `Step` stores the description,
the optional due date, optional notes, the `IsOptional` flag, the execution state
(`Pending`/`Completed`/`Skipped`), a `SortOrder`, the parent process reference,
and standard audit metadata. `ProcessCategory` stores a name and an order.

Owned steps and attachments are removed when their process is deleted. The
effective due date used for sorting and attention is computed as the global due
date when set, otherwise the next pending (frontier) step's due date.

Indexes must support process filters, deterministic sorting (effective due date
with nulls last, then identifier), the attention query (open processes plus their
frontier step due date), step reads ordered by `SortOrder`, and category
reference migration.

### Frontend Route

Processes uses the protected lazy route `/processes`.

The initial UI presents a server-paginated table with URL-backed list state and
dialog state, following the Maintenance, Assets, Inventory, Travel, and Clothes
pattern, plus a dedicated step-timeline popup following the Projects risk-table
pattern. One practical route shape is:

```text
/processes
/processes?processId=123
/processes?newProcess=true
/processes?processId=123&steps=true
```

If the shared shell or router ergonomics suggest a slightly different parameter
layout, preserve the same behaviour: table state must survive dialog open and
close without a reload.

### Configuration Integration

Configuration presents the Processes catalogue alongside the other module-owned
catalogues. Processes owns `ProcessCategory` while exposing it through the
established Configuration presentation boundary.

Because a category is required on every process, a referenced value may only be
**replaced**; replacement re-points the affected processes to the target value.

## Waves

### Wave 0: Contracts And Test Skeleton

Establish stable contracts before persistence or UI work begins.

Tasks:

1. Add the Processes module shell and registration.
2. Freeze process and step routes; the derived status values
   (`NotStarted`/`InProgress`/`Completed`) and the `Cancelled` override; the step
   execution states (`Pending`/`Completed`/`Skipped`); the visibility values; the
   frontier and contiguity rules as a documented domain contract; DTOs; query
   contracts; stable error codes; the attachment owner kind (`Process`); and the
   launcher attention key.
3. Define Configuration-facing contracts for `ProcessCategory` reference handling
   without exposing Processes entities.
4. Define frontend API, validation-schema, route-state, and query-key skeletons.
5. Add architecture-test expectations: Processes may consume Configuration,
   Attachments, Identity, and platform contracts but must not depend on Capex,
   Opex, Inventory, Travel, Assets, Maintenance, Clothes, Mood, or Projects, and no
   module depends on Processes.
6. Add empty backend and frontend test fixtures shared by later Waves.

Tests:

- Unit tests for status, override, and execution-state values; route constants;
  query bounds; and error-code stability.
- Architecture tests for permitted dependencies and the Processes non-dependency
  rules.

Exit criteria:

- Public contracts are explicit and test-covered, and later Waves do not need to
  invent new route, ownership, status, or execution semantics.

### Wave 1: Domain, Persistence, And Catalogue

Implement the Processes data model and module-owned catalogue on both providers.

Tasks:

1. Add `Process`, `Step`, and `ProcessCategory` with the parent relationship.
2. Enforce the required category relationship, bounded non-whitespace strings,
   known visibility and execution-state values, the `IsOptional` and `IsCancelled`
   flags, the optional civil dates, and standard audit metadata.
3. Implement the derived-status, frontier, and contiguity-invariant domain logic
   as pure, unit-testable functions over a step list.
4. Seed the accepted initial category values once
   (`Administrative`, `Legal`, `Tax`, `Health`, `Education`, `Vehicle`,
   `Housing`, `Other`).
5. Implement module-owned category reads plus administrator mutations through
   Configuration, including ordering and final-row protection.
6. Add provider-specific migrations and model snapshots.
7. Add indexes for process filters, sorting, the attention query, step reads by
   `SortOrder`, and category reference migration.

Tests:

- Domain tests for derived status across empty/partial/complete step lists, the
  frontier computation with optional and skipped steps, and the contiguity
  invariant including rejected arrangements.
- SQLite and PostgreSQL migration tests, including upgrades and idempotent
  catalogue initialization.
- Integration tests for category ordering, final-row protection, and administrator
  authorization.

Exit criteria:

- Both providers persist the complete model, the status/frontier/contiguity logic
  is correct and isolated, and the category catalogue is configurable.

### Wave 2: Process Read And Mutation APIs

Deliver the process-level backend contract, excluding step execution and
attachments.

Tasks:

1. Implement the paginated process table query and the process detail query,
   projecting the derived status, the step progress (resolved over total), and the
   effective due date.
2. Implement partial search across name and notes.
3. Implement exact filters for category, status, creator, and visibility, and
   deterministic sorting with the default ordering (effective due date ascending
   with nulls last, then identifier ascending).
4. Implement create, update, and delete for processes with full validation, the
   documented creation defaults, and ownership/audit metadata. A process may be
   created with zero steps.
5. Implement the `cancel` and `reopen` actions for the terminal `Cancelled`
   override, ensuring the derived status is never accepted from the client.
6. Enforce category validity, visibility transitions, creator-only visibility
   change, and standard public-collaboration and private-isolation authorization.

Tests:

- API integration tests for pagination, filters, search, sorting (including the
  effective-due-date ordering and nulls-last behaviour), defaults, required
  fields, the cancel/reopen lifecycle, derived-status projection, visibility
  isolation, and not-found privacy behaviour.
- Two-user privacy tests for public collaboration and private isolation.

Exit criteria:

- Users can browse and fully manage accessible processes and their terminal
  override through the backend, with correct validation and privacy, without step
  execution or attachments.

### Wave 3: Step List Management And Sequential Execution

Deliver the heart of the module: the editable step list and the strict frontier
execution.

Tasks:

1. Implement the step list read ordered by `SortOrder`.
2. Implement the full-collection step restructure (`PUT`): add, remove, reorder,
   rename, and change due date, notes, and the optional flag, preserving each
   step's execution state by step identity.
3. Re-validate the contiguity invariant on every restructure: resolved steps must
   remain a contiguous prefix; a new or pending step may not be inserted inside the
   resolved prefix; reject any arrangement that would leave a resolved step after a
   pending step.
4. Implement the `complete`, `skip`, and `undo` frontier actions: only the
   frontier step may be completed, only an optional frontier step may be skipped,
   and only the most recently resolved step may be undone.
5. Recompute and expose the derived status, the step progress, and the next
   pending (frontier) step on every change.
6. Inherit process visibility and authorization for steps, and remove steps when
   the process is deleted.

Tests:

- API integration tests for restructuring (add/remove/reorder/rename/date/optional)
  with state preservation, every contiguity-invariant rejection path, the
  complete/skip/undo frontier actions and their rejection paths (non-frontier
  completion, skipping a required step, undoing a non-latest step), and the
  derived-status transitions to `Completed` with and without optional/skipped
  steps.
- Two-user tests confirming step actions follow the process's public-collaboration
  and private-isolation rules.

Exit criteria:

- The step list can be restructured and executed strictly in order under the
  frontier and contiguity rules, and the derived status reflects step state
  exactly.

### Wave 4: Attachments

Deliver process attachments without a primary image.

Tasks:

1. Add process attachment listing, upload, download, and delete routes using the
   shared attachment policies.
2. Inherit process visibility and authorization for attachments.
3. Clean up attachments on process deletion.

Tests:

- Attachment tests for round-trip behaviour, authorization, validation failures,
  and filesystem cleanup on process deletion.

Exit criteria:

- Processes support multiple attachments that respect process visibility and are
  cleaned up on deletion, and steps have none.

### Wave 5: Launcher Attention

Deliver the overdue-and-upcoming launcher attention.

Tasks:

1. Implement the Processes attention contributor: attention is true when the
   current user can access at least one open process (`NotStarted` or
   `InProgress`, never `Completed` or `Cancelled`) whose global due date, or whose
   next pending (frontier) step's due date, is in the past or within the inclusive
   window from today to today plus 7 natural days in `Europe/Madrid`.
2. Reuse the established launcher attention aggregation and accessibility rules so
   only accessible processes count and private processes are never disclosed.
3. Expose the boolean attention state through the launcher contract.

Tests:

- Integration tests for the attention window boundaries (overdue, today, today
  plus 7, and far-future dates) across both the global due date and the frontier
  step due date, `Completed`/`Cancelled` and no-effective-date exclusion, and
  accessibility filtering across two users.

Exit criteria:

- Processes attention is true exactly under the documented condition and never
  leaks private data.

### Wave 6: Processes Frontend Table

Build the user-facing process table and the process editor dialog.

Tasks:

1. Add the lazy `/processes` route, module error boundary, translation namespace,
   and a launcher card wired to the attention state.
2. Build the server-paginated table with URL-backed search, filters, sorting, and
   bounded pagination, including the derived-status badge, the step-progress
   column, and the effective-due-date column.
3. Build the process dialog with React Hook Form and Zod, covering name, category,
   global due date, notes, visibility guards, the cancel/reopen action, and the
   entry into the step timeline and attachments.
4. Wire deletion confirmation and list invalidation, preserving table state across
   dialogs.

Tests:

- Frontend API and component tests for route state, filters, sorting, pagination,
  process validation, the cancel/reopen control, derived-status and progress
  rendering, privacy-safe errors, and attachment flows.
- Accessibility tests for dialog focus, keyboard operation, and error association.

Exit criteria:

- Users can complete the full process-level workflow without page reloads while
  preserving table state.

### Wave 7: Step Timeline And Configuration Frontend

Surface the step timeline popup and the Processes catalogue.

Tasks:

1. Build the step-timeline popup: list steps in order with their due dates and
   state, present the complete/skip/undo frontier actions with the next-step
   affordance, and present the restructure controls (add/remove/reorder/rename/date/optional)
   under the contiguity invariant with privacy-safe error feedback.
2. Reflect the derived status and step progress live as steps change, and preserve
   timeline and table state across the popup.
3. Add the Processes section to the Configuration UI for `ProcessCategory`,
   including reorder controls and the replacement dialog with a privacy-neutral
   impact summary.
4. Invalidate the relevant Processes and Configuration caches after structural
   mutations.

Tests:

- Component tests for the timeline frontier actions and their disabled states, the
  restructure controls and contiguity-invariant feedback, live status/progress
  updates, category CRUD, reorder, the replacement dialog, and cache invalidation.
- Accessibility tests for the timeline popup focus, keyboard operation, and error
  association.

Exit criteria:

- Users can execute and restructure steps entirely through the UI, and
  administrators can manage the category catalogue with safe, privacy-neutral
  replacement.

### Wave 8: End-To-End, Hardening, And Acceptance

Validate the implemented behaviour across both providers and the deployed
frontend/backend boundary.

Tasks:

1. Run backend format, build, unit, API, PostgreSQL, migration, and architecture
   suites through repository scripts.
2. Run frontend format, lint, type-check, unit, build, and Playwright suites.
3. Add a representative Playwright journey covering login, opening Processes,
   creating a process with a category and a global due date, adding steps,
   completing and skipping steps in order, undoing a step, cancelling and
   reopening a process, filtering the table, adding an attachment, deleting safe
   test data, and managing the category catalogue with a replacement in
   Configuration.
4. Review OpenAPI for the process, step, attachment, and category routes.
5. Verify keyboard behaviour, dialog and timeline scrolling, filtered table
   invalidation, and narrow desktop widths.
6. Map every criterion in `docs/requirements/PROCESSES_REQUIREMENTS.md` to covering
   code and tests in a Processes acceptance record.
7. Update `ROADMAP.md` to mark Processes as implemented and accepted and to record
   only intentional remaining deferrals.

Exit criteria:

- All relevant repository scripts pass, both providers are covered, the Compose
  journey succeeds, and every accepted Processes requirement is implemented or
  explicitly deferred.

## Suggested Pull Request Boundaries

1. Processes contracts, persistence, and module-owned catalogue (Waves 0-1).
2. Process read and mutation APIs (Wave 2).
3. Step list management and sequential execution (Wave 3).
4. Attachments and launcher attention (Waves 4-5).
5. Processes table, step timeline, and Configuration frontend (Waves 6-7).
6. End-to-end, hardening, and acceptance (Wave 8).

## Plan Completion Criteria

The plan is complete when all Waves meet their exit criteria and the Processes
requirements document describes implemented behaviour rather than only functional
intent.

Branching or parallel steps, recurring or templated processes, per-step
attachments or completion dates or assignees, cost and effort, Projects
integration, and Analytics or Calendar integration remain separate future planning
topics.
