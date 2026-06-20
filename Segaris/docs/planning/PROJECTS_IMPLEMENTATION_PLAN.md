# Projects Implementation Plan

## Purpose

This plan delivers the initial Projects module defined in
`docs/requirements/PROJECTS_REQUIREMENTS.md`. It translates the accepted
functional decisions into dependency-ordered Waves with explicit backend,
frontend, migration, and test work.

The requirements document remains authoritative for behaviour. This plan owns
delivery order and technical scope.

## Delivery Principles

- Keep Projects an independent business module: it consumes only Identity,
  Attachments, the Configuration presentation boundary, and platform contracts,
  and it does not depend on or integrate with any other business module, including
  Processes.
- Reuse established Configuration, Attachments, privacy, REST, and frontend
  conventions where their semantics match.
- Keep the hierarchy invariants (mandatory parents, globally unique codes, the
  shared global number, the always-public programs and axes, leaf-only visibility)
  enforced in backend validation rather than inferred only by the frontend.
- Compute the unified identifier on demand from current ancestor state; never
  persist it.
- Keep the reassignment-on-delete behaviour for programs and axes atomic and
  internal to Projects.
- Keep each Wave independently testable and record deferrals in `ROADMAP.md`
  instead of hiding them in implementation notes.

## Fixed Technical Contracts

### Backend Module

Projects lives under `Segaris.Api.Modules.Projects` and owns the `Program`,
`Axis`, `Project`, `Activity`, and project-risk entities, the shared global number
allocator, the unified-identifier computation, project attachment authorization,
and the program/axis management exposed through Configuration. It does not depend
on any other business module, and the launcher attention contributor reports a
constant non-attention state.

Indicative resource routes are:

```text
GET    /api/projects/tree/programs
GET    /api/projects/tree/programs/{programId}/axes
GET    /api/projects/tree/axes/{axisId}/items

POST   /api/projects/projects
GET    /api/projects/projects/{projectId}
PUT    /api/projects/projects/{projectId}
DELETE /api/projects/projects/{projectId}

POST   /api/projects/activities
GET    /api/projects/activities/{activityId}
PUT    /api/projects/activities/{activityId}
DELETE /api/projects/activities/{activityId}

GET    /api/projects/projects/{projectId}/risks
POST   /api/projects/projects/{projectId}/risks
PUT    /api/projects/projects/{projectId}/risks/{riskId}
DELETE /api/projects/projects/{projectId}/risks/{riskId}

GET    /api/projects/projects/{projectId}/attachments
POST   /api/projects/projects/{projectId}/attachments
GET    /api/projects/projects/{projectId}/attachments/{attachmentId}
DELETE /api/projects/projects/{projectId}/attachments/{attachmentId}
```

Administrative program and axis routes (create, rename, recode, delete with
reassignment) follow the existing module-owned catalogue management pattern
exposed through Configuration. If shell or router ergonomics suggest a slightly
different parameter layout, preserve the same behaviour.

All writes require antiforgery. Missing and inaccessible records share the
platform not-found behaviour so private data is not disclosed.

### Persistence

The model contains module-owned entities with provider-specific migrations:

- `Program`
- `Axis`
- `Project`
- `Activity`
- `ProjectRisk`

`Program` and `Axis` store a name and a four-uppercase-letter code with a
globally unique index per kind; `Axis` references its `Program`. `Project` and
`Activity` store a name, a status, the allocated global number, visibility, the
parent axis reference, and standard audit metadata. `ProjectRisk` stores its
description, the three 1–5 integer factors, the computed score, and its parent
project reference.

Projects and activities share a single module-wide monotonic number allocator
that is independent of any single table's auto-increment, so the same sequence
covers both kinds and survives reparenting. The unified identifier is never
stored.

Owned risks and attachments are removed when their project is deleted. Indexes
must support tree reads (axes by program, items by axis), the global-uniqueness
constraints on codes and the item number, the visibility-filtered item reads, and
the child-existence checks used by the reassignment-on-delete flow.

### Frontend Route

Projects uses the protected lazy route `/projects`.

The initial UI presents the lazily expanded hierarchy tree with URL-backed dialog
state, following the established module patterns. One practical route shape is:

```text
/projects
/projects?projectId=123
/projects?activityId=456
/projects?newProject=axis-789
/projects?newActivity=axis-789
/projects?projectId=123&risks=true
```

Preserve the behaviour regardless of the exact parameter layout: tree expansion
and selection state must survive dialog open and close without a reload.

### Configuration Integration

Configuration presents `Program` and `Axis` management alongside the other
module-owned catalogues. Projects owns the entities, the code-uniqueness rules,
and the reassignment-on-delete logic while exposing management through the
established Configuration presentation boundary. Deleting a node with children
requires reassignment to a single compatible target; Configuration never queries
Projects' tables directly.

## Waves

### Wave 0: Contracts And Test Skeleton

Establish stable contracts before persistence or UI work begins.

Tasks:

1. Add the Projects module shell and registration.
2. Freeze tree, project, activity, risk, and attachment routes; the status enum and
   its fixed values; the visibility values; the risk-band thresholds (`>= 100`
   high, `>= 60` medium, else low); the unified-identifier format; DTOs; query
   contracts; stable error codes; and the attachment owner kind (`Project`).
3. Define Configuration-facing contracts for program/axis management and
   reassignment without exposing Projects entities.
4. Define the launcher attention key with a constant non-attention contributor.
5. Define frontend API, validation-schema, route-state, and query-key skeletons.
6. Add architecture-test expectations: Projects may consume Configuration,
   Attachments, Identity, and platform contracts but must not depend on Capex,
   Opex, Inventory, Travel, Assets, Maintenance, Clothes, Mood, or Processes, and
   no module depends on Projects.
7. Add empty backend and frontend test fixtures shared by later Waves.

Tests:

- Unit tests for status and visibility values, the risk-band thresholds, the
  unified-identifier formatter, route constants, and error-code stability.
- Architecture tests for permitted dependencies and the Projects non-dependency
  rules.

Exit criteria:

- Public contracts are explicit and test-covered, and later Waves do not need to
  invent new route, ownership, numbering, or identifier semantics.

### Wave 1: Hierarchy Domain And Persistence

Implement the structural model and numbering on both providers.

Tasks:

1. Add `Program`, `Axis`, `Project`, and `Activity` with the containment
   references.
2. Enforce bounded, non-whitespace names; the four-uppercase-letter code format;
   globally unique program and axis codes; known status and visibility values; and
   standard audit and ownership metadata.
3. Implement the shared global number allocator and assign a number to every new
   project and activity, guaranteeing uniqueness across both kinds and stability
   across reparenting.
4. Implement the on-demand unified-identifier computation from current ancestor
   codes, the six-digit number, and the name.
5. Add provider-specific migrations and model snapshots.
6. Add indexes for tree reads, code and number uniqueness, and child-existence
   checks.

Tests:

- Domain tests for code validation, status and visibility values, number
  allocation and stability, and unified-identifier formatting including renamed or
  reparented ancestors.
- SQLite and PostgreSQL migration tests, including upgrades and concurrent number
  allocation.

Exit criteria:

- Both providers persist the complete hierarchy with stable global numbers and a
  correct computed identifier.

### Wave 2: Program And Axis Management Through Configuration

Deliver program/axis CRUD and the reassignment-on-delete behaviour.

Tasks:

1. Implement administrator CRUD for `Program` and `Axis` through the Configuration
   presentation boundary, including rename and recode with global-uniqueness
   enforcement.
2. Implement deletion of empty programs and axes.
3. Implement deletion of a non-empty program or axis through atomic reassignment of
   all children to a single compatible target node, rolling back on any failure.
4. Block deletion when no compatible target exists and report impact (the number of
   children to reassign) without disclosing private items of other users.
5. Provide program/axis reads ordered by code ascending for Configuration and the
   tree.

Tests:

- Integration tests for code uniqueness and format, rename/recode, empty deletion,
  reassignment of mixed projects and activities (and of axes between programs), the
  no-target block, rollback, and privacy-neutral impact reporting.
- Administrator-authorization tests for the management endpoints.

Exit criteria:

- Programs and axes can be fully managed, and a non-empty container can be deleted
  only through a safe, atomic reassignment or is blocked.

### Wave 3: Project And Activity Tree And Mutation APIs

Deliver the tree reads and the project/activity lifecycle, excluding risks and
attachments.

Tasks:

1. Implement the lazy tree reads: programs, axes by program, and projects and
   activities by axis, each projecting the unified identifier and ordered by code
   or number ascending.
2. Apply visibility filtering so private items are returned only to their creator
   while programs and axes stay public.
3. Implement create, update, and delete for projects and activities with full
   validation, the documented creation defaults, and reparenting to another axis.
4. Enforce known status and visibility, creator-only visibility change, and the
   standard public-collaboration and private-isolation authorization.
5. Wire project deletion to remove its owned risks and attachments (cleanup
   completed in later Waves) and activity deletion as a simple physical delete.

Tests:

- API integration tests for tree reads, visibility filtering, defaults, required
  fields, reparenting with number stability, identifier recomputation, and
  not-found privacy behaviour.
- Two-user privacy tests for public collaboration and private isolation, including
  an axis that appears empty because its only children are another user's private
  items.

Exit criteria:

- Users can browse the tree and fully manage accessible projects and activities
  through the backend with correct validation and privacy.

### Wave 4: Project Risks

Add the per-project risk table and its scoring.

Tasks:

1. Add `ProjectRisk` CRUD scoped to a project, validating the description and the
   three 1–5 factors.
2. Compute and store the `score = probability × impact × mitigation` on create and
   update, rejecting any client-supplied score.
3. Compute the risk-band summary (counts of low, medium, and high risks) and expose
   it on the project projection.
4. Inherit project visibility and authorization for risks, and remove risks when
   the project is deleted.

Tests:

- API integration tests for risk CRUD, score computation, band classification at
  the `60` and `100` boundaries, the band summary counts, visibility inheritance,
  and deletion cleanup.

Exit criteria:

- A project owns a correct, access-controlled risk table with a computed score,
  banding, and summary.

### Wave 5: Project Attachments

Deliver project result attachments without a primary image.

Tasks:

1. Add project attachment listing, upload, download, and delete routes using the
   shared attachment policies.
2. Inherit project visibility and authorization for attachments.
3. Clean up attachments on project deletion.

Tests:

- Attachment tests for round-trip behaviour, authorization, validation failures,
  and filesystem cleanup on project deletion.

Exit criteria:

- Projects support multiple result attachments that respect project visibility and
  are cleaned up on deletion, and activities have none.

### Wave 6: Projects Frontend Tree

Build the user-facing Projects experience.

Tasks:

1. Add the lazy `/projects` route, module error boundary, translation namespace,
   and a launcher card wired to the constant non-attention state.
2. Build the lazily expanded tree: expand a program to load its axes and an axis to
   load its projects and activities, presenting every item by its unified
   identifier and reflecting each project's risk-band summary.
3. Build the project and activity dialogs with React Hook Form and Zod, covering
   name, status, visibility guards, and (for projects) the entry into the risk
   table and attachments.
4. Build the risk-table popup with risk CRUD, the live computed score, the band
   colouring, and the band summary indicator.
5. Wire deletion confirmation, reparenting where exposed, and tree/query
   invalidation, preserving tree expansion and selection state across dialogs.

Tests:

- Frontend API and component tests for lazy expansion, identifier rendering,
  project/activity validation, the risk popup with score and band computation,
  attachment flows, privacy-safe errors, and dialog state preservation.
- Accessibility tests for dialog focus, keyboard operation, and error association.

Exit criteria:

- Users can browse the tree and complete the full project/activity and risk
  workflow without page reloads while preserving tree state.

### Wave 7: Configuration Frontend For Programs And Axes

Surface program/axis management and the reassignment dialog.

Tasks:

1. Add the Projects section to the Configuration UI for `Program` and `Axis`,
   including create, rename, recode with uniqueness feedback, and ordering by code.
2. Build the deletion flow that, for a non-empty node, presents the reassignment
   dialog with a privacy-neutral impact summary, requires a compatible target node,
   and surfaces the blocked state when none exists.
3. Invalidate the relevant Projects and Configuration caches after structural
   mutations.

Tests:

- Component tests for program/axis CRUD, code-uniqueness feedback, the reassignment
  dialog including the blocked state, and cache invalidation.

Exit criteria:

- Administrators can manage programs and axes and can delete a non-empty container
  only through a safe, privacy-neutral reassignment, entirely through the UI.

### Wave 8: End-To-End, Hardening, And Acceptance

Validate the implemented behaviour across both providers and the deployed
frontend/backend boundary.

Tasks:

1. Run backend format, build, unit, API, PostgreSQL, migration, and architecture
   suites through repository scripts.
2. Run frontend format, lint, type-check, unit, build, and Playwright suites.
3. Add a representative Playwright journey covering login, opening Projects,
   expanding the tree, creating a project and an activity under an axis, editing a
   project's status and risks, verifying the band summary, adding an attachment,
   and managing a program/axis with a reassignment in Configuration.
4. Review OpenAPI for the tree, project, activity, risk, attachment, and
   program/axis routes.
5. Verify keyboard behaviour, dialog scrolling, lazy expansion, and narrow desktop
   widths.
6. Map every criterion in `docs/requirements/PROJECTS_REQUIREMENTS.md` to covering
   code and tests in a Projects acceptance record.
7. Update `ROADMAP.md` to mark Projects as implemented and accepted and to record
   only intentional remaining deferrals.

Exit criteria:

- All relevant repository scripts pass, both providers are covered, the Compose
  journey succeeds, and every accepted Projects requirement is implemented or
  explicitly deferred.

## Suggested Pull Request Boundaries

1. Projects contracts, hierarchy persistence, and numbering (Waves 0-1).
2. Program/axis management with reassignment, and project/activity tree and
   mutation APIs (Waves 2-3).
3. Risks and attachments (Waves 4-5).
4. Projects tree frontend and Configuration frontend (Waves 6-7).
5. End-to-end, hardening, and acceptance (Wave 8).

## Plan Completion Criteria

The plan is complete when all Waves meet their exit criteria and the Projects
requirements document describes implemented behaviour rather than only functional
intent.

Due dates and scheduling, cost, cross-module references, Processes integration, a
richer risk model, promotable activities, and tree search remain separate future
planning topics.
