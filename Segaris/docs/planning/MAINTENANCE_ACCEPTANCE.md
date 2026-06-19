# Maintenance Acceptance Record (Wave 9)

This document records the Wave 9 end-to-end, hardening, and acceptance pass for
the Maintenance module against `docs/requirements/MAINTENANCE_REQUIREMENTS.md`
and the exit criteria in
`docs/planning/MAINTENANCE_IMPLEMENTATION_PLAN.md`.

## Verification Approach

Wave 9 was executed as a focused hardening and acceptance pass, matching the
Capex, Configuration, Opex, Inventory, Travel, and Assets precedents:

- Functional behaviour is covered by the automated suites delivered in Waves 0-8
  and gated on every pull request through the required CI checks
  (`Segaris Backend`, `Segaris PostgreSQL`, `Segaris Compose`; see
  `docs/planning/BACKEND_CI_DECISIONS.md`).
- The fast local suites are expected to remain green through the repository
  scripts: backend format verification, build, unit, API integration,
  architecture, provider migration coverage, frontend format, lint, type-check,
  unit, production build, and Playwright.
- The representative Playwright journey added below is compiled with the
  frontend test suite and runs against the Compose stack when seeded
  `SEGARIS_E2E_USERNAME` / `SEGARIS_E2E_PASSWORD` credentials are present.
- The OpenAPI surface and database indexes/query shape were verified statically
  against the implemented endpoints and paired provider migrations.

## End-To-End Journey

`tests/frontend/e2e/maintenance.spec.ts` adds a single-user critical journey
against the full stack: sign in, create two safe Assets, open Maintenance from
the launcher, exercise and clear a table filter, create a task with type,
priority, due date, and an Asset link, complete it, verify filtering, delete the
referenced Asset through the reassignment dialog, confirm the completed task now
points at the replacement Asset, and delete all safe test data. It is skipped
without seeded credentials, matching the other specs. The second-user privacy
journey is deferred (see Deferred Items).

## Static Verification Results

### HTTP / OpenAPI Surface

All frozen Maintenance routes are mapped under `/api/maintenance` with explicit
Minimal API metadata and DTO contracts; EF Core entities are not exposed. The
group requires authentication, write routes apply antiforgery, and private or
missing tasks share the module not-found behaviour.

- **Tasks**: `GET /tasks`, `GET /tasks/{taskId}`, `POST /tasks`,
  `PUT /tasks/{taskId}`, and `DELETE /tasks/{taskId}` carry named OpenAPI
  operations, summaries, typed success responses, and problem responses for
  validation, forbidden visibility changes, not-found/privacy, and conflict
  paths.
- **Task attachments**: `GET /tasks/{taskId}/attachments`,
  `POST /tasks/{taskId}/attachments`,
  `GET /tasks/{taskId}/attachments/{attachmentId}`, and
  `DELETE /tasks/{taskId}/attachments/{attachmentId}` follow the shared
  attachment pattern, including upload size limits and owner authorization
  through `MaintenanceTask`.
- **Types**: `GET /types` is an authenticated read. Administrator type
  management routes for create, update, move, deletion-impact, direct delete,
  and replace-and-delete use the established Configuration module-owned catalog
  boundary with antiforgery on writes.
- **Assets deletion integration**: `GET /api/assets/items/{assetId}/deletion-impact`
  and `POST /api/assets/items/{assetId}/reassign-and-delete` expose the
  privacy-neutral impact and atomic reassignment/deletion flow while Assets
  depends only on its published deletion-reference contract.

### Indexes And Query Shape

The Maintenance persistence indexes exist in both SQLite and PostgreSQL
migrations (`MaintenanceDomainPersistence`) and match the implemented query
shapes:

| Index                                           | Query that uses it                                                    |
| ----------------------------------------------- | --------------------------------------------------------------------- |
| `maintenance_tasks (DueDate, Id)`               | Default deterministic ordering: due date asc with nulls last, then id |
| `maintenance_tasks (CreatedBy, Visibility, Id)` | Visibility/accessibility filter and creator filter                    |
| `maintenance_tasks (Status, DueDate)`           | Launcher attention over open tasks with due dates                     |
| `maintenance_tasks (MaintenanceTypeId)`         | Exact type filter and type reference migration                        |
| `maintenance_tasks (AssetId)`                   | Exact asset filter and Assets deletion guard lookup                   |
| `maintenance_tasks (Priority)` / `(Visibility)` | Exact filters                                                         |
| `maintenance_types (NormalizedName)` unique     | Type name uniqueness                                                  |
| `maintenance_types (SortOrder)`                 | Default catalog ordering                                              |

List filtering, sorting, pagination, and partial search run as `IQueryable`
queries translated to SQL; the client never loads the full result set. Partial
search is an intentional database-backed `LIKE` scan across title and notes.

## Acceptance Criteria

Each criterion from `MAINTENANCE_REQUIREMENTS.md` and its primary covering
evidence:

| #   | Criterion                                                                                                                                                                                                  | Status                | Primary evidence                                                                                                                                                                              |
| --- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Authenticated users can create, query, edit, and irreversibly delete visible tasks with documented fields, defaults, validation, and privacy rules                                                         | Met                   | `MaintenanceTaskWave2Tests`, `MaintenanceTaskWave3Tests`, `MaintenanceDomainTests`, `MaintenancePage.test.tsx`, `maintenance.spec.ts`                                                         |
| 2   | A task is a single entity with status, priority, required type, optional dates, optional notes, and at most one optional Asset link                                                                        | Met                   | `MaintenanceTask`, `MaintenanceModelContributor`, provider migrations, `MaintenanceDomainTests`, `MaintenanceContractTests`                                                                   |
| 3   | Statuses `Pending`, `InProgress`, `Completed`, and `Cancelled` are available and descriptive, blocking no operation by themselves, with completion date managed on `Completed`                             | Met                   | `MaintenanceContractTests`, `MaintenanceDomainTests`, `MaintenanceTaskWave2Tests` (`Detail_update_and_delete_manage_the_complete_task_lifecycle`)                                             |
| 4   | Priority `Low`, `Medium`, and `High` is required, defaults to `Medium`, and is available for sorting and filtering                                                                                         | Met                   | `MaintenanceContractTests`, `MaintenanceTaskWave2Tests`, `MaintenanceTaskReadService`, `MaintenanceFilters`, `MaintenanceTable`                                                               |
| 5   | Completed and cancelled tasks remain queryable history                                                                                                                                                     | Met                   | `MaintenanceTaskWave2Tests` (status filters), `MaintenanceAssetDeletionReferenceTests` (completed task reassignment), `maintenance.spec.ts`                                                   |
| 6   | The optional Asset reference is live, resolves accessible names, and obeys the public/private visibility rule                                                                                              | Met                   | `IAssetReferenceReader`, `MaintenanceTaskReadService`, `MaintenanceTaskWriteService`, `MaintenanceTaskWave3Tests`, `MaintenancePage.test.tsx`                                                 |
| 7   | Deleting a referenced asset requires atomic reassignment of all referencing tasks to a compatible target, never clears references, blocks when incompatible, and reports impact without private disclosure | Met                   | `MaintenanceAssetDeletionReferenceHandler`, `AssetsEndpoints`, `MaintenanceAssetDeletionReferenceTests`, `AssetsPage.test.tsx`, `maintenance.spec.ts`                                         |
| 8   | The Assets deletion guard is implemented by contract inversion so Maintenance depends on Assets and Assets never depends on Maintenance                                                                    | Met                   | `IAssetDeletionReferenceHandler`, `MaintenanceAssetDeletionReferenceHandler`, `SegarisModules`, `ModuleBoundaryTests`                                                                         |
| 9   | Public collaboration and private isolation follow the platform visibility baseline, and only the creator changes visibility                                                                                | Met                   | `MaintenanceTaskPolicies`, `MaintenanceTaskWriteService`, `MaintenanceTaskWave2Tests`, `MaintenanceTaskWave3Tests`                                                                            |
| 10  | Tasks support multiple attachments inheriting task visibility, with no primary image                                                                                                                       | Met                   | `MaintenanceAttachments`, `MaintenanceTaskAttachmentTests`, `MaintenanceAttachments.tsx`, `StagedMaintenanceAttachments.tsx`, `MaintenancePage.test.tsx`                                      |
| 11  | `MaintenanceType` is initialized once and managed through Configuration with CRUD, reorder, and replace-before-delete semantics                                                                            | Met                   | `MaintenanceSeeder`, `MaintenanceTypeManagementService`, `MaintenanceCatalogEndpointTests`, `ConfigurationPage.test.tsx`, `catalogs.ts`                                                       |
| 12  | The tasks table presents documented columns with search, filters, sorting, bounded pagination, and URL-aware editor preserving table state                                                                 | Met                   | `MaintenanceTaskReadService`, `MaintenancePage`, `MaintenanceFilters`, `MaintenanceTable`, `maintenanceState`, `MaintenancePage.test.tsx`, `contracts.test.ts`, `maintenance.spec.ts`         |
| 13  | Maintenance attention is true exactly for accessible `Pending`/`InProgress` tasks overdue or due within 7 days in `Europe/Madrid`                                                                          | Met                   | `MaintenanceAttentionContributor`, `MaintenanceAttentionTests`, launcher integration                                                                                                          |
| 14  | SQLite and PostgreSQL migrations, backend unit/integration/architecture tests, frontend component tests, and a representative Playwright journey verify behaviour and privacy boundaries                   | Met (single-user E2E) | `MigrationTests`, `PostgresPersistenceTests`, `ModuleBoundaryTests`, `MaintenanceDomainTests`, Maintenance API suites, `contracts.test.ts`, `MaintenancePage.test.tsx`, `maintenance.spec.ts` |

## Deferred Items

Recorded in `ROADMAP.md`:

- **Second-user Maintenance privacy E2E journey.** Public-collaboration and
  private-isolation behaviour is covered by API integration tests
  (`MaintenanceTaskWave2Tests`, `MaintenanceTaskWave3Tests`,
  `MaintenanceTaskAttachmentTests`, `MaintenanceAttentionTests`); the
  browser-level multi-session journey waits on multi-account Playwright
  infrastructure, matching the deferred Capex, Configuration, Opex, Inventory,
  Travel, and Assets patterns.
- **PostgreSQL representative-volume query-plan benchmark.** Index existence and
  database-level query shape are verified; a large-dataset `EXPLAIN ANALYZE`
  benchmark waits on a representative seeding/benchmark harness.
- **Future Maintenance scope.** Recurring/preventive schedules, cost/labour/parts,
  service providers, an Assets-owned maintenance history, user-editable
  completion date/activity timeline, and Analytics/Calendar integration remain
  future versions.

## Documentation Outcomes

- `README.md`: no change; repository-wide commands and setup were unaffected by
  the Maintenance waves.
- `ROADMAP.md`: Maintenance implementation marked accepted; the intentional
  deferrals above recorded.
- `docs/planning/MAINTENANCE_IMPLEMENTATION_PLAN.md`: Wave 9 status updated to
  point at this record.
