# Projects Requirements

## Status

Phase 2 functional definition is complete. This document is the functional
source of truth for the initial Projects implementation plan.

## Purpose

Projects organises personal work into a navigable tree. The household groups its
initiatives under top-level `Program` containers, subdivides each program into
`Axis` containers, and places concrete work inside an axis as either a `Project`
or an `Activity`.

A `Project` is the rich unit of work: it carries a status, an own risk table, and
result files. An `Activity` is a lightweight unit that carries only a name and a
status. Both live at the same level inside an axis and share a single global
numbering scheme so any item can be referenced unambiguously.

The initial module is a structured register and tree browser. It does not track
schedules, due dates, dependencies, effort, cost, or any cross-module reference.
Projects is an independent business module; it does not integrate with Processes
in this version.

## Initial Scope

- Organise work in a four-level tree: `Program` → `Axis` → (`Project` or
  `Activity`).
- Own `Program` and `Axis` as module-owned structural nodes managed through
  Configuration, each carrying only a name and a four-letter uppercase code.
- Manage `Project` and `Activity` from the Projects page through a URL-aware popup
  editor, each carrying a name, a status, a global incremental number, and
  visibility.
- Assign every project and activity a globally unique incremental number and
  present it through a computed unified identifier.
- Give each project an own risk table with a computed risk score and a
  low/medium/high banding, plus a risk-band summary.
- Let each project own result files through the shared platform attachment model.
- Present the hierarchy as a lazily expanded tree.
- Require, when deleting a non-empty `Program` or `Axis`, that its children be
  reassigned to a compatible target node before deletion.

## Excluded Scope

The initial Projects implementation excludes:

- Due dates, schedules, milestones, dependencies, effort, or progress percentages.
- Cost, budget, or any link to Capex or Opex.
- Any cross-module reference (Assets, Maintenance, Inventory, Travel, and others)
  or any integration with the Processes module.
- Sub-activities or any nesting below a project or activity.
- Attachments or risks on activities, programs, or axes.
- Configurable project/activity statuses; the status set is a fixed enum.
- Search, filtering, or sorting controls over the tree beyond its natural ordering.
- Launcher attention for the module.
- Spanish translations.

## Hierarchy Model

The module owns four entities arranged as a strict containment tree:

- `Program`: the top-level container. Has a name and a code.
- `Axis`: contained by exactly one `Program`. Has a name and a code.
- `Project`: contained by exactly one `Axis`. Rich work item.
- `Activity`: contained by exactly one `Axis`. Lightweight work item.

Containment rules:

- Every `Axis` belongs to a `Program`; every `Project` and `Activity` belongs to
  an `Axis`.
- `Project` and `Activity` sit at the same level inside an axis.
- Any container may be empty. A `Program` with no axes and an `Axis` with no
  projects or activities are valid, if unproductive, states.

## Program And Axis

`Program` and `Axis` are structural nodes owned by Projects and managed through
the Configuration experience, following the established module-owned catalogue
presentation pattern (administrator CRUD, deletion-impact handling, atomic
reassignment before deletion).

Each carries only:

- A required name.
- A required code of exactly four uppercase letters (`[A-Z]{4}`).
- An identifier and standard persistence metadata.

Codes are **globally unique**: no two programs may share a code, and no two axes
may share a code. Names and codes are mutable after creation.

`Program` and `Axis` are always public; they have no visibility of their own and
are visible to every authenticated user. Default ordering for presentation is by
code ascending.

## Project And Activity

A `Project` contains:

- A required name.
- A required status.
- A globally unique incremental number (system-assigned).
- An own risk table.
- Result-file attachments.
- Visibility.
- Standard ownership and audit metadata.

An `Activity` contains:

- A required name.
- A required status.
- A globally unique incremental number (system-assigned).
- Visibility.
- Standard ownership and audit metadata.

An activity has no risks and no attachments; that is the only structural
difference from a project. Both are reparentable to another axis.

## Status

Every project and activity has one of these fixed statuses:

- `Planning`
- `Active`
- `Completed`
- `OnHold`
- `Cancelled`

The status is descriptive and manually controlled. It is a fixed enum, not
managed through Configuration, and it blocks no operation by itself in the initial
release. New items default to `Planning`.

## Numbering And Unified Identifier

Projects and activities share a single, module-wide incremental number sequence.
The number is allocated once at creation, is globally unique across both projects
and activities, and never changes — including when an item is moved to another
axis — and is never reused after deletion.

Users never see a bare number. Every place that identifies a project or activity
(tree, tables, selectors) shows the **unified identifier**:

```text
PPPPAAAA-123456 nnnn
```

where `PPPP` is the parent program code, `AAAA` is the parent axis code, `123456`
is the item number padded to six digits with leading zeros, and `nnnn` is the
item name.

Because program codes, axis codes, the item's axis, and the item's name are all
mutable, the unified identifier is **computed on demand** from the current state
of the item and its ancestors. It is never persisted as a property of the project
or activity. Moving an item or renaming an ancestor changes the displayed
identifier while the stored number is preserved.

## Risks

Each project owns its own risk table. The table is hidden from the main view and
is opened on demand as a popup from the project.

A risk contains:

- A required description (the risk's only free text).
- A `probability` integer in the range 1–5.
- An `impact` integer in the range 1–5.
- A `mitigation` integer in the range 1–5.
- A system-computed `score`.

The `score` is the product `probability × impact × mitigation` (range 1–125). It
is computed by the backend and never accepted from the client. The score
determines a band:

- `score >= 100`: high risk (red).
- `score >= 60` and `score < 100`: medium risk (yellow).
- `score < 60`: low risk (green).

The project surfaces a **risk-band summary**: a count of low, medium, and high
risks presented as pills or a small bar indicator. The summary is available on the
project so it can be shown without opening the full risk table.

Risks inherit the visibility and authorization of their owning project. Deleting a
project deletes its risks.

## Attachments

- A project may contain multiple result-file attachments using the shared platform
  attachment policies and authorization model.
- Project attachments have no primary image and no thumbnail column; the module is
  text- and tree-based.
- Any user who may access the project may view, add, and remove its attachments.
- Attachments inherit the visibility and authorization of their owning project.
- Activities, programs, and axes have no attachments.

## Visibility And Authorization

Visibility applies only to `Project` and `Activity`. `Program` and `Axis` are
always public.

Projects and activities use the platform-standard visibility values:

- `Public`
- `Private`

New projects and activities default to `Public`. The standard Segaris baseline
applies:

- A user can view and edit their own items and public items.
- A private item remains creator-only, including from administrators.
- Any authenticated user may edit a public item.
- Only the creator may change an item's visibility.

These constraints are enforced by the backend regardless of the client. Because
visibility lives on the leaf items, an axis may appear empty to a user when its
only children are other users' private items.

## Deletion And Reassignment

- Deleting a `Project` is physical and irreversible and also removes its risks and
  its owned attachments.
- Deleting an `Activity` is physical and irreversible.
- Deleting a `Program` or an `Axis` that has children is only allowed after
  **reassigning all** of its children to a single compatible target node:
  - an `Axis`'s projects and activities move to another `Axis`;
  - a `Program`'s axes move to another `Program`.
- Reassignment moves every child to one target node and is atomic within the
  deletion transaction, rolling back on any failure. Reassigned items keep their
  own number; their unified identifier is recomputed under the new ancestor.
- When no compatible target node exists (for example, deleting the only program
  while it still contains axes), the deletion is **blocked** and the user is told
  to create a target or empty the node first.
- An empty `Program` or `Axis` may be deleted directly.

## Validation

- Program and axis name is required, trimmed, not whitespace-only, and at most 200
  characters.
- Program and axis code is exactly four uppercase ASCII letters and is globally
  unique within its kind.
- Project and activity name is required, trimmed, not whitespace-only, and at most
  200 characters.
- Status and visibility are known values.
- The parent axis of a project or activity, and the parent program of an axis,
  must exist.
- Risk description is required, trimmed, not whitespace-only, and at most 1,000
  characters.
- Risk probability, impact, and mitigation are integers in the inclusive range
  1–5.
- Risk score is system-computed and is not accepted from the client.
- The item number is system-assigned and is not accepted from the client.

## Creation Defaults

A new project starts with:

- Status `Planning`.
- Visibility `Public`.
- A newly allocated global number.
- No risks.
- No attachments.

A new activity starts with:

- Status `Planning`.
- Visibility `Public`.
- A newly allocated global number.

## Module Entry And Navigation

Opening Projects shows the hierarchy as a lazily expanded tree. The user expands a
`Program` to load its axes, and an `Axis` to load its projects and activities.
There is no separate overview or dashboard, and there are no search, filter, or
sort controls beyond the natural ordering (programs and axes by code ascending,
projects and activities by number ascending).

In the tree, `Program` and `Axis` are read-only structure: their creation,
renaming, recoding, deletion, and reassignment happen in Configuration. Projects
and activities are created, viewed, edited, and deleted from the Projects page
through the established Segaris URL-aware popup pattern, so tree state survives
dialog open and close without a reload. Each project's risk table opens as a
further popup.

Every project and activity is presented by its unified identifier.

## Configuration Integration

Configuration presents `Program` and `Axis` management alongside the other
module-owned catalogues. Projects owns the `Program` and `Axis` entities, the code
uniqueness rules, and the reassignment-on-delete behaviour, while exposing
management through the established Configuration presentation boundary.

Because programs and axes are required containers, deleting a node that has
children requires reassignment to a compatible target; the children are never
orphaned and are never silently deleted.

## Attention

The Projects launcher card never requests attention in the initial release. The
launcher exposes only the platform-standard boolean attention state, which is
always false for this module.

## Acceptance Criteria

The initial Projects definition is satisfied when:

1. The hierarchy `Program` → `Axis` → (`Project` or `Activity`) is enforced, every
   axis has a program and every project/activity has an axis, and empty programs
   and axes are valid.
2. `Program` and `Axis` carry only a name and a globally unique four-uppercase-letter
   code, are always public, and are managed through Configuration.
3. `Project` and `Activity` carry a required name, a fixed status, a global number,
   and visibility; a project additionally owns risks and result attachments while
   an activity owns neither.
4. Project and activity statuses `Planning`, `Active`, `Completed`, `OnHold`, and
   `Cancelled` are available, descriptive, default to `Planning`, and block no
   operation by themselves.
5. Projects and activities share one global incremental number that is assigned at
   creation, never changes when an item is moved, and is never reused.
6. The unified identifier `PPPPAAAA-123456 nnnn` is computed on demand from current
   ancestor codes, the six-digit number, and the name, and is never persisted.
7. Each project owns a risk table whose risks carry a description and 1–5
   probability, impact, and mitigation, with a system-computed `score = p × i × m`
   and bands high (`>= 100`), medium (`>= 60`), and low (`< 60`), surfaced as a
   risk-band summary and editable through a popup.
8. Projects support multiple result attachments that inherit project visibility and
   have no primary image, and activities have none.
9. Visibility applies only to projects and activities, follows the Segaris
   public-collaboration and private-isolation baseline, defaults to `Public`, and
   only the creator changes it; programs and axes are always public.
10. Deleting a project removes its risks and attachments; deleting a non-empty
    program or axis requires atomic reassignment of all children to a single
    compatible target and is blocked when none exists; empty containers delete
    directly.
11. The Projects page presents the lazily expanded tree with items shown by their
    unified identifier, with program/axis structure read-only and project/activity
    editing through URL-aware popups that preserve tree state.
12. SQLite and PostgreSQL migrations, backend unit/integration/architecture tests,
    frontend component tests, and a representative Playwright journey verify the
    supported behaviour and privacy boundaries.

## Deferred Decisions

- Whether projects or activities should gain due dates, schedules, milestones,
  dependencies, effort, or progress, and whether that would introduce launcher
  attention or calendar projections.
- Whether Projects and Processes should share a task model or integrate through
  references.
- Whether projects should reference other modules (for example Assets,
  Maintenance, or financial entries) or publish read contracts to Analytics.
- Whether activities should later be promotable to projects or gain their own
  risks or attachments.
- Whether the risk model should grow beyond the numeric probability/impact/mitigation
  scoring (for example qualitative categories, owners, or mitigation actions as
  text).
- Whether the tree should gain search, filtering, or cross-axis views as the data
  grows.
