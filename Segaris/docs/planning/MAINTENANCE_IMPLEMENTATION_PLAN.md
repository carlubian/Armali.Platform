# Maintenance Implementation Plan

## Purpose

This plan delivers the initial Maintenance module defined in
`docs/requirements/MAINTENANCE_REQUIREMENTS.md`. It translates the accepted
functional decisions into dependency-ordered Waves with explicit backend,
frontend, migration, and test work.

The requirements document remains authoritative for behaviour. This plan owns
delivery order and technical scope.

## Delivery Principles

- Preserve Maintenance as a business module whose only cross-business-module
  dependency is a narrow, explicit reference to Assets.
- Reuse established Configuration, Attachments, privacy, REST, pagination,
  launcher-attention, and frontend conventions where their semantics match.
- Do not introduce recurrence, cost, labour, parts, service providers, an
  Assets-owned maintenance dashboard, or any other cross-module dependency.
- Keep the asset reference live and the visibility rule, the deletion guard, and
  the launcher-attention rule explicit in backend validation rather than inferred
  only by the frontend.
- Implement the Assets deletion guard by contract inversion so the dependency
  direction stays Maintenance to Assets.
- Keep each Wave independently testable and record deferrals in `ROADMAP.md`
  instead of hiding them in implementation notes.

## Fixed Technical Contracts

### Backend Module

Maintenance lives under `Segaris.Api.Modules.Maintenance` and owns the
maintenance task, the `MaintenanceType` catalogue, attachment authorization, the
launcher-attention contributor, and the implementation of the Assets deletion
reference contract. It consumes a narrow Assets read contract and the
Configuration presentation boundary. It does not depend on any other business
module.

Indicative resource routes are:

```text
GET    /api/maintenance/tasks
POST   /api/maintenance/tasks
GET    /api/maintenance/tasks/{taskId}
PUT    /api/maintenance/tasks/{taskId}
DELETE /api/maintenance/tasks/{taskId}

GET    /api/maintenance/tasks/{taskId}/attachments
POST   /api/maintenance/tasks/{taskId}/attachments
GET    /api/maintenance/tasks/{taskId}/attachments/{attachmentId}
DELETE /api/maintenance/tasks/{taskId}/attachments/{attachmentId}

GET    /api/maintenance/types
```

Administrative type routes follow the existing module-owned catalogue management
pattern exposed through Configuration.

All writes require antiforgery. Missing and inaccessible records share the
platform not-found behaviour so private data is not disclosed.

### Persistence

The model contains module-owned entities with provider-specific migrations:

- `MaintenanceTask`
- `MaintenanceType`

Tasks store the title, type reference, status, priority, optional due date,
system-managed completion date, optional notes, optional asset identifier,
visibility, and standard audit metadata. `MaintenanceType` stores a name and an
order.

Owned attachments are removed when their task is deleted. The initial model has
no cost, recurrence, or provider columns. The asset identifier is a stable
opaque reference and is not modelled as a foreign key to Assets entities; its
integrity is preserved by the deletion guard rather than by a database constraint
that would couple the modules.

Indexes must support task filters, deterministic sorting (due date with nulls
last, then identifier), the attention query, the asset-reference lookup used by
the deletion guard, and type reference migration.

### Frontend Route

Maintenance uses the protected lazy route `/maintenance`.

The initial UI presents a server-paginated table with URL-backed list state and
dialog state, following the Assets, Inventory, Travel, and Clothes pattern. One
practical route shape is:

```text
/maintenance
/maintenance?taskId=123
/maintenance?newTask=true
```

If the shared shell or router ergonomics suggest a slightly different parameter
layout, preserve the same behaviour: table state must survive dialog open and
close without a reload.

### Assets Integration

Maintenance consumes a narrow Assets read contract to validate an asset
reference, resolve its display name, and evaluate accessibility and visibility for
the visibility rule. Assets owns this contract.

Assets additionally defines a deletion reference contract that consumers
implement to report and reassign references when an asset is deleted. Maintenance
registers the implementation. Assets enumerates registered implementations during
deletion; it never queries Maintenance entities. This mirrors the existing
launcher-attention and Configuration reference-handler inversion patterns.

### Configuration Integration

Configuration presents the Maintenance catalogue alongside the other module-owned
catalogues. Maintenance owns `MaintenanceType` while exposing it through the
established Configuration presentation boundary.

Because a type is required on every task, a referenced value may only be
**replaced**; replacement re-points the affected tasks to the target value.

## Waves

### Wave 0: Contracts And Test Skeleton

Establish stable contracts before persistence or UI work begins.

Tasks:

1. Add the Maintenance module shell and registration after Assets.
2. Freeze task routes, the status and priority enums and their fixed values, DTOs,
   query contracts, stable error codes, the attachment owner kind (`MaintenanceTask`),
   and the launcher attention key.
3. Define Configuration-facing contracts for type reference handling without
   exposing Maintenance entities.
4. Define the Assets read contract and the Assets deletion reference contract that
   Maintenance will consume and implement, owned by Assets.
5. Define frontend API, validation-schema, route-state, and query-key skeletons.
6. Add architecture-test expectations: Maintenance may consume Configuration,
   Assets, and platform contracts but must not depend on Capex, Opex, Inventory,
   Travel, Clothes, or Mood, and Assets must not depend on Maintenance.
7. Add empty backend and frontend test fixtures shared by later Waves.

Tests:

- Unit tests for enum values, defaults, route constants, query bounds, and
  error-code stability.
- Architecture tests for permitted dependencies, the Assets-to-Maintenance
  non-dependency, and published contracts.

Exit criteria:

- Public contracts are explicit and test-covered, and later Waves do not need to
  invent new route, ownership, or cross-module semantics.

### Wave 1: Domain, Persistence, And Catalogue

Implement the Maintenance data model and module-owned catalogue on both
providers.

Tasks:

1. Add `MaintenanceTask` and `MaintenanceType`.
2. Enforce the required type relationship, bounded strings, known status,
   priority, and visibility values, the optional due date, the system-managed
   completion date transition, and standard audit metadata.
3. Seed the accepted initial type values once.
4. Implement module-owned type reads plus administrator mutations through
   Configuration, including ordering and final-row protection.
5. Add provider-specific migrations and model snapshots.
6. Add indexes for task filters, sorting, the attention query, the asset-reference
   lookup, and type reference migration.

Tests:

- Domain tests for status, priority, and visibility values and the completion-date
  transition.
- SQLite and PostgreSQL migration tests, including upgrades and idempotent
  catalogue initialization.
- Integration tests for type ordering, final-row protection, and administrator
  authorization.

Exit criteria:

- Both providers persist the complete model and expose a configurable type
  catalogue.

### Wave 2: Task Read And Mutation APIs

Deliver the core task backend contract, excluding the asset link.

Tasks:

1. Implement the paginated task table query and the task detail query.
2. Implement partial search across title and notes.
3. Implement exact filters for type, status, priority, creator, and visibility,
   and deterministic sorting with the default ordering (due date ascending with
   nulls last, then identifier ascending).
4. Implement create, update, and delete for tasks with full validation, the
   documented creation defaults, and the completion-date lifecycle.
5. Enforce type validity, visibility transitions, creator-only visibility change,
   and standard public-collaboration and private-isolation authorization.

Tests:

- API integration tests for pagination, filters, search, sorting, defaults,
  required fields, completion-date transitions, visibility isolation, and
  not-found privacy behaviour.
- Two-user privacy tests for public collaboration and private isolation.

Exit criteria:

- Users can browse and fully manage accessible tasks through the backend with
  correct validation and privacy, without the asset link.

### Wave 3: Asset Reference

Add the optional live link to Assets and its visibility rule.

Tasks:

1. Consume the Assets read contract to validate the referenced asset, resolve its
   display name, and evaluate accessibility for the current user.
2. Accept, store, and clear the optional asset identifier on create and update.
3. Enforce the visibility rule on create, update, visibility change, and asset
   change: a public task may reference only public assets; a private task may
   reference any asset its creator can access.
4. Surface the resolved asset name in the table and detail projections, with a
   neutral placeholder when the asset is not resolvable for the viewer.
5. Add the asset filter to the table query.

Tests:

- API integration tests for linking, unlinking, the visibility rule and its
  rejection paths, name resolution, the neutral placeholder, and the asset
  filter.
- Two-user tests confirming a public task cannot expose a private asset.

Exit criteria:

- Tasks can be linked to accessible assets under the visibility rule, and the link
  never discloses a private asset.

### Wave 4: Assets Deletion Guard And Reassignment

Integrate Maintenance into Assets deletion safely through contract inversion.

Tasks:

1. Implement the Assets deletion reference contract in Maintenance: report the
   number of tasks referencing an asset and reassign all of them to a target
   asset within the deletion transaction.
2. Implement target validation so the target satisfies the visibility rule for
   every affected task, and block deletion when no compatible target exists.
3. Wire Assets deletion to enumerate registered reference contracts, evaluate
   impact, perform reassignment atomically, and roll back on any failure.
4. Provide privacy-neutral impact reporting that never discloses private tasks of
   other users.
5. Invalidate affected Maintenance and Assets frontend queries after a successful
   reassignment.

Tests:

- Cross-module integration tests for reassignment of mixed-status and
  mixed-ownership tasks, target visibility validation, the no-compatible-target
  block, rollback, and privacy-neutral impact reporting.
- SQLite and PostgreSQL coverage for atomic deletion-and-reassignment behaviour.
- Architecture tests confirming Assets gains no dependency on Maintenance.

Exit criteria:

- An asset referenced by tasks cannot be silently orphaned, deletion reassigns all
  references atomically or is blocked, and the dependency direction is preserved.

### Wave 5: Attachments

Deliver task attachments without a primary image.

Tasks:

1. Add task attachment listing, upload, download, and delete routes using the
   shared attachment policies.
2. Inherit task visibility and authorization for attachments.
3. Clean up attachments on task deletion.

Tests:

- Attachment tests for round-trip behaviour, authorization, validation failures,
  and filesystem cleanup on task deletion.

Exit criteria:

- Tasks support multiple attachments that respect task visibility and are cleaned
  up on deletion.

### Wave 6: Launcher Attention

Deliver the overdue-and-upcoming launcher attention.

Tasks:

1. Implement the Maintenance attention contributor: attention is true when the
   current user can access at least one `Pending` or `InProgress` task whose
   `DueDate` is set and is in the past or within the inclusive window from today
   to today plus 7 natural days in `Europe/Madrid`.
2. Reuse the established launcher attention aggregation and accessibility rules so
   only accessible tasks count and private tasks are never disclosed.
3. Expose the boolean attention state through the launcher contract.

Tests:

- Integration tests for the attention window boundaries (overdue, today, today
  plus 7, and far-future dates), `Completed`/`Cancelled` and no-due-date
  exclusion, and accessibility filtering across two users.

Exit criteria:

- Maintenance attention is true exactly under the documented condition and never
  leaks private data.

### Wave 7: Maintenance Frontend Table

Build the user-facing maintenance experience.

Tasks:

1. Add the lazy `/maintenance` route, module error boundary, translation
   namespace, and a launcher card wired to the attention state.
2. Build the server-paginated table with URL-backed search, filters, sorting, and
   bounded pagination, including the resolved-asset column with its neutral
   placeholder.
3. Build the task dialog with React Hook Form and Zod, covering title, type,
   status, priority, due date, notes, the asset picker constrained by the
   visibility rule, visibility guards, and task attachments.
4. Wire deletion confirmation and list invalidation.

Tests:

- Frontend API and component tests for route state, filters, sorting, pagination,
  task validation, the visibility-constrained asset picker, privacy-safe errors,
  and attachment flows.
- Accessibility tests for dialog focus, keyboard operation, and error
  association.

Exit criteria:

- Users can complete the full Maintenance workflow without page reloads while
  preserving table state.

### Wave 8: Configuration And Assets Frontend Integration

Surface the Maintenance catalogue and the asset-deletion reassignment dialog.

Tasks:

1. Add the Maintenance section to the Configuration UI for `MaintenanceType`,
   including reorder controls and the replacement dialog with a privacy-neutral
   impact summary.
2. Extend the Assets deletion flow in the Assets frontend to present the
   maintenance-task reassignment dialog: show the privacy-neutral impact, require
   a compatible target asset, and surface the blocked state when none exists.
3. Invalidate the relevant Maintenance, Assets, and Configuration caches after
   structural mutations.

Tests:

- Component tests for type CRUD, reorder, the replacement dialog, the asset
  reassignment dialog including the blocked state, and cache invalidation.

Exit criteria:

- Administrators can manage the Maintenance type catalogue, and users can delete a
  referenced asset only through a safe, privacy-neutral reassignment, entirely
  through the UI.

### Wave 9: End-To-End, Hardening, And Acceptance

> **Status: complete.** The hardening and acceptance pass is recorded in
> `docs/planning/MAINTENANCE_ACCEPTANCE.md`, which maps every Maintenance
> requirement acceptance criterion to covering code and tests. The representative
> Playwright journey lives in `tests/frontend/e2e/maintenance.spec.ts`.

Validate the implemented behaviour across both providers and the deployed
frontend/backend boundary.

Tasks:

1. Run backend format, build, unit, API, PostgreSQL, migration, and architecture
   suites through repository scripts.
2. Run frontend format, lint, type-check, unit, build, and Playwright suites.
3. Add a representative Playwright journey covering login, opening Maintenance,
   creating a task with a type, priority, and due date, linking it to an asset,
   completing it, filtering the table, and deleting safe test data; plus deleting
   a referenced asset through the reassignment dialog.
4. Review OpenAPI for Maintenance task, type, and attachment routes and the Assets
   deletion changes.
5. Verify keyboard behaviour, dialog scrolling, filtered table invalidation, and
   narrow desktop widths.
6. Map every criterion in `docs/requirements/MAINTENANCE_REQUIREMENTS.md` to
   covering code and tests in a Maintenance acceptance record.
7. Update `ROADMAP.md` to mark Maintenance as implemented and accepted and to
   record only intentional remaining deferrals.

Exit criteria:

- All relevant repository scripts pass, both providers are covered, the Compose
  journey succeeds, and every accepted Maintenance requirement is implemented or
  explicitly deferred.

## Suggested Pull Request Boundaries

1. Maintenance contracts, persistence, and module-owned catalogue (Waves 0-1).
2. Task read and mutation APIs (Wave 2).
3. Asset reference and Assets deletion guard (Waves 3-4).
4. Attachments and launcher attention (Waves 5-6).
5. Maintenance table, Configuration, and Assets frontend integration (Waves 7-8).
6. End-to-end, hardening, and acceptance (Wave 9).

## Plan Completion Criteria

The plan is complete when all Waves meet their exit criteria and the Maintenance
requirements document describes implemented behaviour rather than only functional
intent.

Recurring and preventive schedules, cost and parts, service providers, an
Assets-owned maintenance history, a user-editable completion date and activity
timeline, and Analytics or Calendar integration remain separate future planning
topics.
