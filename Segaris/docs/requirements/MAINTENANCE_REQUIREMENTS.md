# Maintenance Requirements

## Status

Phase 2 functional definition is complete. This document is the functional
source of truth for the initial Maintenance implementation plan.

## Purpose

Maintenance records repair and maintenance work the household needs to carry out
on its physical elements, and keeps the history of that work once it is done.

The initial module is intentionally simple. It is a task register: each task
carries a title, a maintenance type, a status, a priority, an optional due date,
optional notes, optional attachments, and an optional live link to an `Asset`. It
does not track cost, recurring or preventive schedules, labour, or parts.

Maintenance is the first business module that references another business module:
a task may optionally point to an Asset. The relationship is deliberately narrow
and follows the cross-module reference rules in
`docs/architecture/domain-organization.md`.

## Initial Scope

- Manage repair and maintenance work as a single `MaintenanceTask` entity.
- Describe each task with a title, a maintenance type, a status, a priority, an
  optional due date, an optional completion date, optional notes, and
  attachments.
- Optionally link a task to one `Asset` owned by the Assets module through a live
  reference, subject to a visibility rule.
- Keep completed and cancelled tasks as queryable history, not only open work.
- Organise tasks through a Maintenance-owned `MaintenanceType` catalogue presented
  through Configuration.
- Present tasks as a server-paginated table with search, filters, sorting, and a
  URL-aware popup editor.
- Surface launcher attention for overdue and imminently due open tasks.
- Introduce an Assets deletion guard so a referenced asset cannot be silently
  orphaned: deleting such an asset requires reassigning its tasks to another
  asset.

## Excluded Scope

The initial Maintenance implementation excludes:

- Recurring or preventive maintenance schedules that regenerate tasks
  automatically. Each task is a single, manually created item.
- Cost, price, labour, parts, invoices, or any link to Capex or Opex.
- Service providers as first-class entities with claims or contracts.
- A per-asset maintenance dashboard owned by the Assets module (Assets does not
  query Maintenance; only the deletion guard contract is shared).
- Integration with a future Analytics or Calendar module, although the due date
  is stored so a future calendar may read it.
- Spanish translations.

## Task Model

A maintenance task contains at least:

- A required title.
- A required `MaintenanceType`.
- A status.
- A priority.
- An optional due date (civil date).
- A system-managed completion date (civil date) set when the task is completed.
- Optional notes.
- An optional live reference to one `Asset`.
- Attachments.
- Visibility.

The initial model carries no cost, no recurrence, and no reference to any module
other than the optional Asset link.

## Task Status

Every task has one of these fixed statuses:

- `Pending`
- `InProgress`
- `Completed`
- `Cancelled`

The status is descriptive and manually controlled. It does not block editing,
linking, visibility changes, or deletion by itself, and it is not managed through
Configuration. It distinguishes work not yet started (`Pending`), work underway
(`InProgress`), finished work kept as history (`Completed`), and abandoned work
kept as history (`Cancelled`).

Only `Pending` and `InProgress` tasks participate in launcher attention.

When a task enters `Completed`, its completion date is set to the current civil
date in `Europe/Madrid`. When it leaves `Completed`, the completion date is
cleared. The completion date is system-managed and is not directly edited in the
initial release.

## Priority

Every task has one of these fixed priorities:

- `Low`
- `Medium`
- `High`

Priority is required and defaults to `Medium`. It is a fixed enum, not managed
through Configuration, and it is used for sorting and filtering.

## Dates

Tasks store two civil dates:

- `DueDate`: the optional date by which the work should be done. It has no
  artificial boundary and is the only date that drives launcher attention.
- `CompletedDate`: the system-managed date the task was marked `Completed`.

## Maintenance Type

Maintenance owns one module-specific catalogue:

- `MaintenanceType`

It is presented through Configuration and follows the established module-owned
catalogue behaviour:

- Administrator CRUD.
- Explicit ordering.
- Deletion-impact checks.
- Atomic replacement before deleting a referenced value.
- Privacy-neutral impact reporting.

Because a maintenance type is required on every task, a referenced value may only
be **replaced**, never cleared, following the Assets category pattern.
Replacement re-points the affected tasks to the target value.

### Initial Types

The initial ordered type values are:

- `Repair`
- `Preventive`
- `Inspection`
- `Cleaning`
- `Installation`
- `Other`

The one-time initialization behaviour matches the established Configuration,
Assets, and Clothes catalogue pattern: values are initialized once and are not
reimposed after administrative changes.

## Asset Reference

A task may optionally reference exactly one `Asset`. The reference is **optional**
and **live**: Maintenance stores the stable asset identifier and resolves the
asset's display name through a narrow read contract published by Assets. No
snapshot is stored, because the deletion guard guarantees the reference always
points to an existing asset.

### Visibility Rule

The reference is constrained by visibility so a public, collaboratively editable
task never exposes a private asset:

- A `Public` task may reference only `Public` assets.
- A `Private` task may reference any asset its creator can access, that is, the
  creator's own `Private` assets and any `Public` asset.

The rule is enforced by the backend on creation, on update, and whenever a task's
visibility or asset link changes, regardless of the client. A task whose
visibility would become `Public` while it references a `Private` asset is
rejected.

### Resolution And Privacy

Because linking already enforces accessibility, resolving the asset name for a
task the user can see never discloses an asset the user could not otherwise
access. When a referenced asset cannot be resolved for the current user, the
client shows a neutral placeholder rather than asset details.

### Deletion Guard And Reassignment

Introducing this reference changes the Assets deletion behaviour. An asset that is
referenced by one or more maintenance tasks cannot be deleted without resolving
those references.

- The guard is implemented by **contract inversion**: Assets defines a deletion
  reference contract and Maintenance implements it. The dependency direction
  stays Maintenance to Assets; Assets never queries Maintenance entities.
- Deleting a referenced asset requires **reassigning all** of its tasks to a
  single target asset, regardless of task status. Reassignment never clears the
  reference. Migrating the whole history (including `Completed` and `Cancelled`
  tasks) is correct because deleting and reassigning an asset represents adjusting
  the asset's complete identity, for example correcting a wrong entity or merging
  duplicates. The normal end-of-life flow does not delete: it sets the old asset
  to `Retired` and creates a new one.
- The target asset must satisfy the visibility rule for every affected task. A
  `Public` target asset satisfies all tasks; if any affected task is `Public`, the
  target must be `Public`.
- If no compatible target asset exists, the deletion is **blocked** and the user
  is told to reassign or delete the affected tasks manually first. The reference
  is never left null.
- Reassignment is atomic within the deletion transaction and rolls back on any
  failure.
- Impact reporting is privacy-neutral: it reports how many tasks must be
  reassigned without disclosing private tasks owned by other users. Reassignment
  re-points every affected task atomically regardless of ownership.

## Attachments

- Tasks may contain multiple attachments using the shared platform attachment
  policies and authorization model (for example before/after photos or quotes).
- Maintenance attachments have no primary image and no thumbnail column; the task
  table is text-based.
- Any user who may access the task may view, add, and remove attachments.
- Attachments inherit the visibility and authorization of their owning task.

## Visibility And Authorization

Every task uses the platform-standard visibility values:

- `Public`
- `Private`

New tasks default to `Public`.

These rules apply:

- A user can view and edit their own tasks and public tasks.
- A private task remains creator-only, including from administrators.
- Public collaboration follows the standard Segaris rule: any authenticated user
  may edit a public task.
- Only the creator may change a task's visibility, and changing visibility must
  keep the asset link valid under the visibility rule.

These constraints are enforced by the backend regardless of the client.

## Deletion

Deletion of a task is physical, immediate, and irreversible. A task can always be
deleted; no module references maintenance tasks, so there is no guard on task
deletion. Deleting a task removes its owned attachments.

Deletion of a referenced asset is governed by the deletion guard and
reassignment described under **Asset Reference**.

## Validation

- Title is required, trimmed, not whitespace-only, and at most 200 characters.
- Maintenance type reference is required and valid.
- Status, priority, and visibility are known values.
- Description/notes are optional and at most 4,000 characters.
- `DueDate` is an optional civil date with no artificial boundary.
- `CompletedDate` is system-managed and not accepted from the client.
- The asset reference is optional, must point to an existing accessible asset, and
  must satisfy the visibility rule.

## Creation Defaults

A new task starts with:

- Status `Pending`.
- Priority `Medium`.
- Visibility `Public`.
- The first available maintenance type by `SortOrder`, then `Id`.
- No due date and no completion date.
- No notes.
- No asset reference.
- No attachments.

## Module Entry And Navigation

Opening Maintenance takes the user directly to the tasks table. Maintenance does
not have an initial overview or dashboard.

Creating, viewing, and editing tasks happens in a popup dialog over the table,
following the established Segaris URL-aware dialog pattern, so table state
survives dialog open and close without a reload.

## Maintenance View

The primary view is a server-paginated table. It includes at least these columns:

- Title.
- Type.
- Status.
- Priority.
- Asset (resolved name or neutral placeholder).
- Due date.
- Visibility.

The default ordering is due date ascending with tasks without a due date last,
then identifier ascending.

The table supports:

- Partial search across title and notes.
- Exact filters for type, status, priority, asset, creator, and visibility.
- User-controlled sorting and bounded pagination following platform conventions.

Search, key filters, sort, page, and page size should be URL-backed where
practical.

## Attention

The Maintenance launcher card requires attention when at least one accessible
task satisfies all of these conditions:

- Status is `Pending` or `InProgress`.
- `DueDate` is set.
- `DueDate` is already in the past or falls within the inclusive window from
  today to today plus 7 natural days in `Europe/Madrid`.

`Completed` and `Cancelled` tasks and tasks without a due date never activate
attention. Only accessible tasks count for the current user.

The launcher exposes only the platform-standard boolean attention state.

## Acceptance Criteria

The initial Maintenance definition is satisfied when:

1. Authenticated users can create, query, edit, and irreversibly delete visible
   tasks with the documented fields, defaults, validation, and privacy rules.
2. A task is a single entity with a status, a priority, a required maintenance
   type, optional dates, optional notes, and at most one optional Asset link.
3. Task statuses `Pending`, `InProgress`, `Completed`, and `Cancelled` are
   available and descriptive, blocking no operation by themselves, with
   completion date set on `Completed` and cleared otherwise.
4. Priority `Low`, `Medium`, and `High` is required, defaults to `Medium`, and is
   available for sorting and filtering.
5. Completed and cancelled tasks remain queryable history.
6. The optional Asset reference is live, resolves the asset name only for
   accessible assets, and obeys the visibility rule that a public task may
   reference only public assets.
7. Deleting an asset referenced by tasks requires atomic reassignment of all those
   tasks (any status) to a compatible target asset, never clears the reference,
   blocks when no compatible target exists, and reports impact without disclosing
   private tasks of other users.
8. The Assets deletion guard is implemented by contract inversion so Maintenance
   depends on Assets and Assets never depends on Maintenance.
9. Public collaboration and private isolation follow the Segaris visibility
   baseline for tasks, and only the creator changes visibility.
10. Tasks support multiple attachments inheriting task visibility, with no primary
    image.
11. Maintenance-owned `MaintenanceType` is initialized once and managed through
    Configuration with CRUD and reorder, and deleting a referenced type requires
    replacement, never clearing, atomically and without disclosing private tasks.
12. The tasks table presents the documented columns with the documented search,
    filters, sorting, and bounded pagination, and the editor is a URL-aware popup
    that preserves table state.
13. Maintenance attention is true exactly when the current user can access at
    least one `Pending` or `InProgress` task whose due date is in the past or
    within the next 7 days in `Europe/Madrid`.
14. SQLite and PostgreSQL migrations, backend unit/integration/architecture tests,
    frontend component tests, and a representative Playwright journey verify the
    supported behaviour and privacy boundaries.

## Deferred Decisions

- Whether Maintenance should later support recurring or preventive schedules that
  regenerate tasks automatically.
- Whether tasks should record cost, labour, parts, or a link to a Capex or Opex
  entry.
- Whether service providers should become first-class references, optionally
  reusing the shared Supplier catalogue.
- Whether the Assets module should present a per-asset maintenance history by
  consuming a Maintenance read contract.
- Whether a user-editable completion date and a richer activity timeline are
  needed beyond the system-managed completion date.
- Whether future Analytics or a shared Calendar will consume Maintenance read
  contracts or due-date projections.
