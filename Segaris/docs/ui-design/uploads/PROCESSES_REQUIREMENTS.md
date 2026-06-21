# Processes Requirements

## Status

Phase 2 functional definition is complete. This document is the functional
source of truth for the initial Processes implementation plan.

## Purpose

Processes manages sequential procedures the household must carry out step by step
in order, by a target date — bureaucratic, legal, or administrative procedures
such as renewing a document, completing a public-administration application, or
following a multi-stage legal procedure.

A `Process` is a container of ordered `Step`s that are completed strictly in
sequence. The module tracks how far a procedure has advanced, what the next
pending step is, and when work is overdue or imminently due.

The initial module is intentionally focused. It does not track cost, effort,
service providers, or any cross-module reference. Processes is an independent
business module; it does not integrate with Projects or any other module in this
version.

## Initial Scope

- Manage sequential procedures as a `Process` root entity that owns an ordered
  list of `Step` children.
- Describe each process with a name, a required `ProcessCategory`, an optional
  global due date, optional notes, attachments, and visibility.
- Describe each step with a required description, an optional due date, optional
  notes, an optional/skippable flag, and an execution state.
- Enforce strict sequential execution: only the next pending step (the
  "frontier") can be completed or skipped, and only the most recent resolved step
  can be undone.
- Derive the process status from its steps, with a manual terminal `Cancelled`
  override for abandoned or denied procedures.
- Allow restructuring the step list at any time, including while the process is in
  progress, subject to a contiguity invariant.
- Organise processes through a Processes-owned `ProcessCategory` catalogue
  presented through Configuration.
- Present processes as a server-paginated table with search, filters, sorting,
  and a URL-aware popup editor, with a dedicated step-timeline popup.
- Surface launcher attention for overdue and imminently due open processes, driven
  by the global due date and the next pending step's due date.

## Excluded Scope

The initial Processes implementation excludes:

- Cost, price, effort, labour, or any link to Capex or Opex.
- Service providers as first-class entities.
- Any cross-module reference (Projects, Assets, Maintenance, and others) or any
  integration with the Projects module.
- Per-step attachments (attachments live at the process level only).
- A system-managed per-step completion date.
- Branching, conditional, or parallel steps; execution is a single strict
  sequence with optional skippable steps.
- Recurring or templated processes that regenerate steps automatically.
- Integration with a future Analytics or Calendar module, although civil dates are
  stored so a future calendar may read them.
- Spanish translations.

## Process Model

A process contains at least:

- A required name.
- A required `ProcessCategory`.
- An optional global due date (civil date).
- Optional notes.
- An ordered list of zero or more steps.
- Attachments.
- Visibility.
- Standard ownership and audit metadata.

A process may be created empty (no steps) and have steps added later. An empty
container is a valid, if unproductive, state.

## Step Model

A step belongs to exactly one process and contains:

- A required description.
- An optional due date (civil date).
- Optional notes.
- An `optional` flag marking the step as skippable.
- An execution state: `Pending`, `Completed`, or `Skipped`.
- A `SortOrder` defining its position in the sequence.

Only a step whose `optional` flag is set may enter the `Skipped` state. A required
step can only be `Pending` or `Completed`. Steps carry no attachments and no
system-managed completion date in the initial release.

## Sequential Execution

Steps are executed in a single strict sequence using a frontier model:

- The **frontier** is the first step that is neither `Completed` nor `Skipped`.
- Only the frontier step may be **completed**, and only an optional frontier step
  may be **skipped**.
- Only the most recently resolved step (the last `Completed` or `Skipped` step
  before the frontier) may be **undone**, returning it to `Pending`.
- At all times, the resolved steps (`Completed` or `Skipped`) form a **contiguous
  prefix** from the start of the sequence.

These rules are enforced by the backend regardless of the client.

## Process Status

The process status is **derived** from its steps, except for one manual terminal
override:

- `NotStarted`: no step is resolved (the frontier is the first step, or there are
  no steps).
- `InProgress`: at least one step is resolved but not every required step is yet
  `Completed`.
- `Completed`: every required step is `Completed` and every optional step is
  `Completed` or `Skipped` (the frontier has reached the end of a non-empty
  sequence).
- `Cancelled`: a manual terminal override for a denied or abandoned procedure.

The derived status is computed by the backend and is never accepted from the
client. `Cancelled` takes precedence over the derived value, is set and cleared
explicitly by the user, removes the process from launcher attention, and is kept
as queryable history. Clearing `Cancelled` returns the process to its derived
status. An empty process is `NotStarted` until it is given steps or cancelled.

## Step List Editing

The step list may be **restructured at any time**, including while the process is
in progress: steps may be added, removed, reordered, renamed, and have their due
date, notes, or optional flag changed.

Restructuring preserves each step's execution state by step identity and is
re-validated by the backend against the contiguity invariant:

- The resolved (`Completed`/`Skipped`) steps must remain a contiguous prefix at
  the front of the sequence.
- A new or `Pending` step may not be inserted inside the resolved prefix; it must
  be placed at or after the frontier.
- Reordering within the resolved prefix (all resolved) or within the pending tail
  is allowed.
- Removing a resolved step shrinks the prefix and advances no state.
- A restructure that would leave a resolved step after a `Pending` step is
  rejected.

## Process Category

Processes owns one module-specific catalogue:

- `ProcessCategory`

It is presented through Configuration and follows the established module-owned
catalogue behaviour:

- Administrator CRUD.
- Explicit ordering.
- Deletion-impact checks.
- Atomic replacement before deleting a referenced value.
- Privacy-neutral impact reporting.

Because a category is required on every process, a referenced value may only be
**replaced**, never cleared, following the Maintenance and Assets category
pattern. Replacement re-points the affected processes to the target value.

### Initial Categories

The initial ordered category values are:

- `Administrative`
- `Legal`
- `Tax`
- `Health`
- `Education`
- `Vehicle`
- `Housing`
- `Other`

The one-time initialization behaviour matches the established Configuration,
Maintenance, Assets, and Clothes catalogue pattern: values are initialized once
and are not reimposed after administrative changes.

## Attachments

- A process may contain multiple attachments using the shared platform attachment
  policies and authorization model (for example forms, receipts, or official
  letters).
- Process attachments have no primary image and no thumbnail column; the module is
  text- and table-based.
- Any user who may access the process may view, add, and remove attachments.
- Attachments inherit the visibility and authorization of their owning process.
- Steps have no attachments of their own.

## Visibility And Authorization

Every process uses the platform-standard visibility values:

- `Public`
- `Private`

New processes default to `Public`.

These rules apply:

- A user can view and edit their own processes and public processes.
- A private process remains creator-only, including from administrators.
- Public collaboration follows the standard Segaris rule: any authenticated user
  may edit a public process, including completing, skipping, and undoing steps and
  restructuring the step list.
- Only the creator may change a process's visibility.

Steps and attachments inherit the visibility and authorization of their owning
process. These constraints are enforced by the backend regardless of the client.

## Deletion

Deletion of a process is physical, immediate, and irreversible. A process can
always be deleted; no module references processes, so there is no guard on process
deletion. Deleting a process removes its steps and its owned attachments.

Individual steps are deleted as part of step-list restructuring, subject to the
contiguity invariant.

## Validation

- Name is required, trimmed, not whitespace-only, and at most 200 characters.
- `ProcessCategory` reference is required and valid.
- The global due date is an optional civil date with no artificial boundary.
- Process notes are optional and at most 4,000 characters.
- Visibility is a known value.
- Step description is required, trimmed, not whitespace-only, and at most 500
  characters.
- Step due date is an optional civil date with no artificial boundary.
- Step notes are optional and at most 1,000 characters.
- The optional flag and execution state are known values; `Skipped` is valid only
  for an optional step.
- Execution-state transitions obey the frontier rules; the step list obeys the
  contiguity invariant.
- The derived status is system-computed and not accepted from the client; only the
  `Cancelled` override is set or cleared by the client.

## Creation Defaults

A new process starts with:

- Visibility `Public`.
- The first available process category by `SortOrder`, then `Id`.
- No global due date.
- No notes.
- No steps (status `NotStarted`).
- No attachments.

A new step starts with:

- State `Pending`.
- Optional flag unset (required step).
- Appended at the end of the pending tail.
- No due date and no notes.

## Module Entry And Navigation

Opening Processes takes the user directly to the processes table. Processes does
not have an initial overview or dashboard.

Creating, viewing, and editing a process's own fields happens in a popup dialog
over the table, following the established Segaris URL-aware dialog pattern, so
table state survives dialog open and close without a reload. A process's step
timeline opens as a further popup, where steps are listed in order with their due
dates and the complete, skip, undo, and restructure actions.

## Processes View

The primary view is a server-paginated table. It includes at least these columns:

- Name.
- Category.
- Status (derived badge, or `Cancelled`).
- Progress (resolved steps over total steps, for example `3/7`).
- Effective due date (the global due date, or the next pending step's due date when
  no global date is set).
- Visibility.

The default ordering is effective due date ascending with processes without an
effective date last, then identifier ascending.

The table supports:

- Partial search across name and notes.
- Exact filters for category, status, creator, and visibility.
- User-controlled sorting and bounded pagination following platform conventions.

Search, key filters, sort, page, and page size should be URL-backed where
practical.

## Attention

The Processes launcher card requires attention when at least one accessible
process is **open** — its status is `NotStarted` or `InProgress`, never
`Completed` or `Cancelled` — and satisfies at least one of these conditions:

- The global due date is set and is already in the past or falls within the
  inclusive window from today to today plus 7 natural days in `Europe/Madrid`.
- The next pending step (the frontier) has a due date that is already in the past
  or falls within that same window.

`Completed` and `Cancelled` processes never activate attention. Processes with no
effective due date never activate attention. Only accessible processes count for
the current user.

The launcher exposes only the platform-standard boolean attention state.

## Acceptance Criteria

The initial Processes definition is satisfied when:

1. Authenticated users can create, query, edit, and irreversibly delete visible
   processes with the documented fields, defaults, validation, and privacy rules.
2. A process is a root entity owning a required name, a required category, an
   optional global due date, optional notes, attachments, visibility, and an
   ordered list of zero or more steps.
3. A step carries a required description, an optional due date, optional notes, an
   optional/skippable flag, and an execution state of `Pending`, `Completed`, or
   `Skipped`, where `Skipped` is valid only for an optional step.
4. Strict sequential execution is enforced: only the frontier step may be
   completed, only an optional frontier step may be skipped, only the most recently
   resolved step may be undone, and resolved steps always form a contiguous prefix.
5. The process status is derived as `NotStarted`, `InProgress`, or `Completed` from
   its steps, is never accepted from the client, and a manual terminal `Cancelled`
   override can be set and cleared and takes precedence over the derived value.
6. The step list can be restructured at any time, including while in progress,
   preserving step state by identity and rejecting any restructure that violates
   the contiguity invariant.
7. Processes support multiple attachments that inherit process visibility and have
   no primary image, and steps have none.
8. Public collaboration and private isolation follow the Segaris visibility
   baseline for processes, steps and attachments inherit process visibility, and
   only the creator changes visibility.
9. Processes-owned `ProcessCategory` is initialized once and managed through
   Configuration with CRUD and reorder, and deleting a referenced category requires
   replacement, never clearing, atomically and without disclosing private processes
   of other users.
10. The processes table presents the documented columns — including derived status
    and step progress — with the documented search, filters, sorting, and bounded
    pagination, and the editor is a URL-aware popup that preserves table state, with
    a dedicated step-timeline popup.
11. Processes attention is true exactly when the current user can access at least
    one open process whose global due date or next pending step's due date is in the
    past or within the next 7 days in `Europe/Madrid`.
12. SQLite and PostgreSQL migrations, backend unit/integration/architecture tests,
    frontend component tests, and a representative Playwright journey verify the
    supported behaviour and privacy boundaries.

## Deferred Decisions

- Whether processes should support branching, conditional, or parallel steps
  beyond a single strict sequence with optional skippable steps.
- Whether recurring or templated processes should regenerate steps automatically
  from a reusable definition.
- Whether steps should gain their own attachments, a system-managed completion
  date, or an assignee.
- Whether processes should record cost, effort, or a link to a Capex or Opex entry.
- Whether Processes and Projects should share a task model or integrate through
  references.
- Whether future Analytics or a shared Calendar will consume Processes read
  contracts or due-date projections.
- Whether attention should consider any pending step with a due date in the window
  rather than only the frontier step.
