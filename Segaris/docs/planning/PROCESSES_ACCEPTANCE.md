# Processes Acceptance Record (Wave 8)

This document records the Wave 8 end-to-end, hardening, and acceptance pass for
the Processes module against `docs/requirements/PROCESSES_REQUIREMENTS.md` and
the exit criteria in `docs/planning/PROCESSES_IMPLEMENTATION_PLAN.md`.

## Verification Approach

Wave 8 was executed as a focused hardening and acceptance pass, matching the
Capex, Configuration, Opex, Inventory, Travel, Assets, Maintenance, and Projects
precedents:

- Functional behaviour is covered by the automated suites delivered in Waves 0-7
  and gated on every pull request through the required CI checks (`Segaris
Backend`, `Segaris PostgreSQL`, `Segaris Compose`; see
  `docs/planning/BACKEND_CI_DECISIONS.md`).
- The fast local suites are expected to remain green through the repository
  scripts: backend format verification, build, unit, API integration,
  architecture, provider migration coverage, frontend format, lint, type-check,
  unit, production build, and Playwright.
- The representative Playwright journey added below is compiled with the
  frontend test suite and runs against the Compose stack when seeded
  `SEGARIS_E2E_USERNAME` / `SEGARIS_E2E_PASSWORD` administrator credentials are
  present.
- The OpenAPI surface and database indexes/query shape were verified statically
  against the implemented endpoints and paired provider migrations.

## End-To-End Journey

`tests/frontend/e2e/processes.spec.ts` adds a single-user critical journey
against the full stack: sign in, create safe process categories through
Configuration, open Processes from the deployed frontend, exercise and clear a
table filter, create a process with a category, global due date, notes, and
attachment, add steps, complete, skip, and undo in frontier order, cancel and
reopen the process, delete a referenced category through replacement in
Configuration, verify the table reflects the replacement, and delete all
disposable process and category data. It is skipped without seeded administrator
credentials, matching the other specs. The second-user privacy journey is
deferred (see Deferred Items).

## Static Verification Results

### HTTP / OpenAPI Surface

All frozen Processes routes are mapped under `/api/processes` with explicit
Minimal API metadata and DTO contracts; EF Core entities are not exposed. The
group requires authentication, write routes apply antiforgery, administrative
category writes require the Admin policy, and missing or inaccessible records
share the module not-found behaviour.

- **Processes**: `GET /api/processes`, `POST /api/processes`,
  `GET /api/processes/{processId}`, `PUT /api/processes/{processId}`,
  `DELETE /api/processes/{processId}`, `POST /api/processes/{processId}/cancel`,
  and `POST /api/processes/{processId}/reopen` carry named OpenAPI operations,
  summaries, typed success responses, and problem responses for validation,
  forbidden visibility changes, not-found/privacy, and conflict paths.
- **Steps**: `GET /api/processes/{processId}/steps`,
  `PUT /api/processes/{processId}/steps`,
  `POST /api/processes/{processId}/steps/{stepId}/complete`,
  `POST /api/processes/{processId}/steps/{stepId}/skip`, and
  `POST /api/processes/{processId}/steps/{stepId}/undo` expose DTOs, preserve
  state by step identity on restructure, and surface frontier/contiguity problem
  responses without exposing private process details.
- **Process attachments**: `GET /api/processes/{processId}/attachments`,
  `POST /api/processes/{processId}/attachments`,
  `GET /api/processes/{processId}/attachments/{attachmentId}`, and
  `DELETE /api/processes/{processId}/attachments/{attachmentId}` follow the
  shared attachment pattern, including upload size limits and owner authorization
  through `Process`.
- **Categories**: `GET /api/processes/categories` is an authenticated read.
  Administrator category management routes for create, update, move,
  deletion-impact, direct delete, and replace-and-delete use the established
  Configuration module-owned catalog boundary with antiforgery on writes.

### Indexes And Query Shape

The Processes persistence indexes exist in both SQLite and PostgreSQL migrations
(`ProcessesDomainPersistence`) and match the implemented query shapes:

| Index                                             | Query that uses it                                                    |
| ------------------------------------------------- | --------------------------------------------------------------------- |
| `processes_categories (NormalizedName)` unique    | Category name uniqueness                                              |
| `processes_categories (SortOrder)`                | Default category ordering                                             |
| `processes_processes (CategoryId)`                | Exact category filter and category reference migration                |
| `processes_processes (CreatedBy, Visibility, Id)` | Visibility/accessibility filter and creator filter                    |
| `processes_processes (DueDate, Id)`               | Process due-date ordering and effective-date default ordering support |
| `processes_processes (IsCancelled, DueDate)`      | Launcher attention over open processes with global due dates          |
| `processes_processes (Visibility)`                | Exact visibility filter                                               |
| `processes_steps (ProcessId, SortOrder, Id)`      | Ordered step-list reads and process-detail projections                |
| `processes_steps (DueDate)`                       | Frontier step due-date attention and effective-date projections       |

List filtering, sorting, pagination, progress/status projection, and effective
due-date projection run as `IQueryable` queries translated to SQL; the client
never loads the full result set. Partial search is an intentional
database-backed `LIKE` scan across process name and notes.

## Acceptance Criteria

Each criterion from `PROCESSES_REQUIREMENTS.md` and its primary covering
evidence:

| #   | Criterion                                                                                                                                                                                      | Status                | Primary evidence                                                                                                                                                                                                                              |
| --- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Authenticated users can create, query, edit, and irreversibly delete visible processes with documented fields, defaults, validation, and privacy rules                                         | Met                   | `Process`, `ProcessWriteService`, `ProcessReadService`, `ProcessEndpointTests`, `ProcessesDomainTests`, `ProcessesPage.test.tsx`, `processes.spec.ts`                                                                                         |
| 2   | A process root owns required name/category, optional global due date, optional notes, attachments, visibility, and an ordered list of zero or more steps                                       | Met                   | `Process`, `Step`, `ProcessesModelContributor`, provider migrations, `ProcessesDomainTests`, `ProcessEndpointTests`, `ProcessStepEndpointTests`                                                                                               |
| 3   | A step carries required description, optional due date, optional notes, optional/skippable flag, and `Pending`/`Completed`/`Skipped` state, with `Skipped` valid only for optional steps       | Met                   | `Step`, `StepExecutionState`, `ProcessExecution`, `ProcessesContractTests`, `ProcessesDomainTests`, `ProcessStepEndpointTests`, `ProcessStepsDialog`                                                                                          |
| 4   | Strict sequential execution is enforced: only the frontier step may complete/skip, only the latest resolved step may be undone, and resolved steps form a contiguous prefix                    | Met                   | `ProcessExecution`, `ProcessWriteService`, `ProcessesDomainTests`, `ProcessStepEndpointTests`, `ProcessStepsDialog`, `processes.spec.ts`                                                                                                      |
| 5   | Status is derived as `NotStarted`, `InProgress`, or `Completed`, is never accepted from the client, and reversible `Cancelled` override takes precedence                                       | Met                   | `ProcessExecution`, `ProcessesContractTests`, `ProcessesDomainTests`, `ProcessEndpointTests`, `ProcessesPage.test.tsx`, `processes.spec.ts`                                                                                                   |
| 6   | Step list restructuring is allowed at any time, preserves step state by identity, and rejects contiguity violations                                                                            | Met                   | `Step`, `ProcessWriteService`, `ProcessesDomainTests`, `ProcessStepEndpointTests`, `ProcessesPage.test.tsx`, `ProcessStepsDialog`                                                                                                             |
| 7   | Processes support multiple attachments inheriting process visibility, with no primary image, and steps have none                                                                               | Met                   | `ProcessesAttachments`, `ProcessAttachmentTests`, `ProcessAttachments.tsx`, `StagedProcessAttachments.tsx`, `ProcessesPage.test.tsx`, `processes.spec.ts`                                                                                     |
| 8   | Public collaboration and private isolation follow the platform baseline for processes, steps, and attachments; only the creator changes visibility                                             | Met                   | `ProcessPolicies`, `ProcessWriteService`, `ProcessEndpointTests`, `ProcessStepEndpointTests`, `ProcessAttachmentTests`, `ProcessAttentionTests`                                                                                               |
| 9   | `ProcessCategory` is initialized once and managed through Configuration with CRUD, reorder, and required-reference replacement without disclosing private process details                      | Met                   | `ProcessesSeeder`, `ProcessCategoryManagementService`, `ProcessCategoryEndpointTests`, `ConfigurationPage.test.tsx`, `catalogs.ts`, `processes.spec.ts`                                                                                       |
| 10  | Processes table presents documented columns with search, filters, sorting, bounded pagination, URL-aware editor state, and dedicated step-timeline popup                                       | Met                   | `ProcessListQuery`, `ProcessReadService`, `ProcessesPage`, `ProcessesTable`, `ProcessesFilters`, `processesState`, `contracts.test.ts`, `ProcessesPage.test.tsx`, `processes.spec.ts`                                                         |
| 11  | Processes attention is true exactly for accessible open processes whose global due date or frontier step due date is overdue or within 7 natural days in `Europe/Madrid`                       | Met                   | `ProcessesAttentionContributor`, `ProcessAttentionTests`, launcher integration                                                                                                                                                                |
| 12  | SQLite/PostgreSQL migrations, backend unit/integration/architecture tests, frontend component tests, and a representative Playwright journey verify supported behaviour and privacy boundaries | Met (single-user E2E) | `MigrationTests`, `PostgresPersistenceTests`, `ModuleBoundaryTests`, `ProcessesDomainTests`, `ProcessesContractTests`, Processes API suites, `contracts.test.ts`, `ProcessesPage.test.tsx`, `ConfigurationPage.test.tsx`, `processes.spec.ts` |

## Deferred Items

Recorded in `ROADMAP.md`:

- **Second-user Processes privacy E2E journey.** Public-collaboration and
  private-isolation behaviour is covered by API integration tests
  (`ProcessEndpointTests`, `ProcessStepEndpointTests`, `ProcessAttachmentTests`,
  `ProcessAttentionTests`); the browser-level multi-session journey waits on
  multi-account Playwright infrastructure, matching the deferred patterns for
  earlier modules.
- **PostgreSQL representative-volume query-plan benchmark.** Index existence and
  database-level query shape are verified; a large-dataset `EXPLAIN ANALYZE`
  benchmark waits on a representative seeding/benchmark harness.
- **Future Processes scope.** Branching/parallel steps, recurring or templated
  processes, per-step attachments/completion dates/assignees, cost/effort,
  Projects integration, and Analytics/Calendar integration remain future
  versions.

## Documentation Outcomes

- `README.md`: no change; repository-wide commands and setup were unaffected by
  the Processes waves.
- `ROADMAP.md`: Processes implementation marked accepted; the intentional
  deferrals above recorded.
- `docs/planning/PROCESSES_IMPLEMENTATION_PLAN.md`: Wave 8 status updated to
  point at this record.
