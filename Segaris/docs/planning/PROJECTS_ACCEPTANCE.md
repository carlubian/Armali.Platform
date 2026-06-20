# Projects Acceptance Record (Wave 8)

This document records the Wave 8 end-to-end, hardening, and acceptance pass for
the Projects module against `docs/requirements/PROJECTS_REQUIREMENTS.md` and the
exit criteria in `docs/planning/PROJECTS_IMPLEMENTATION_PLAN.md`.

## Verification Approach

Wave 8 was executed as a focused hardening and acceptance pass, matching the
Capex, Configuration, Opex, Inventory, Travel, Assets, and Maintenance
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
  `SEGARIS_E2E_USERNAME` / `SEGARIS_E2E_PASSWORD` credentials are present.
- The OpenAPI surface and database indexes/query shape were verified statically
  against the implemented endpoints and paired provider migrations.

## End-To-End Journey

`tests/frontend/e2e/projects.spec.ts` adds a single-user critical journey against
the full stack: sign in, create safe program/axis structure through
Configuration, open Projects from the deployed frontend, lazily expand the tree,
create a project and an activity under an axis, update the project's status, add
a high risk and result attachment, delete a non-empty axis through the
reassignment dialog, verify the project/activity identifiers are recomputed under
the target axis, and delete all disposable test data. It is skipped without
seeded administrator credentials, matching the other specs. The second-user
privacy journey is deferred (see Deferred Items).

## Static Verification Results

### HTTP / OpenAPI Surface

All frozen Projects routes are mapped under `/api/projects` with explicit Minimal
API metadata and DTO contracts; EF Core entities are not exposed. The group
requires authentication, write routes apply antiforgery, administrative
structure-management writes require the Admin policy, and missing or inaccessible
leaf records share the module not-found behaviour.

- **Tree**: `GET /tree/programs`, `GET /tree/programs/{programId}/axes`, and
  `GET /tree/axes/{axisId}/items` carry named OpenAPI operations, summaries,
  typed success responses, and problem responses for missing containers.
- **Projects**: `POST /projects`, `GET /projects/{projectId}`,
  `PUT /projects/{projectId}`, and `DELETE /projects/{projectId}` carry typed
  success responses and problem responses for validation, forbidden visibility
  changes, and not-found/privacy paths.
- **Activities**: `POST /activities`, `GET /activities/{activityId}`,
  `PUT /activities/{activityId}`, and `DELETE /activities/{activityId}` mirror
  the project lifecycle without risks or attachments.
- **Risks**: `GET /projects/{projectId}/risks`,
  `POST /projects/{projectId}/risks`,
  `GET /projects/{projectId}/risks/{riskId}`,
  `PUT /projects/{projectId}/risks/{riskId}`, and
  `DELETE /projects/{projectId}/risks/{riskId}` expose only DTOs and inherit
  project authorization.
- **Project attachments**: `GET /projects/{projectId}/attachments`,
  `POST /projects/{projectId}/attachments`,
  `GET /projects/{projectId}/attachments/{attachmentId}`, and
  `DELETE /projects/{projectId}/attachments/{attachmentId}` follow the shared
  attachment pattern, including upload size limits and project-owner
  authorization.
- **Program/axis management**: authenticated reads are exposed at `GET
/programs` and `GET /axes`; administrator create, update, deletion-impact,
  direct-delete, and reassign-and-delete routes are exposed for both structures
  with antiforgery on writes and privacy-neutral impact responses.

### Indexes And Query Shape

The Projects persistence indexes exist in both SQLite and PostgreSQL migrations
(`ProjectsDomainPersistence` and `ProjectsRisks`) and match the implemented query
shapes:

| Index                                                                                                                 | Query that uses it                                                   |
| --------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------- |
| `projects_programs (Code)` unique and `(Code, Id)`                                                                    | Program code uniqueness and tree/Configuration ordering              |
| `projects_axes (Code)` unique and `(ProgramId, Code, Id)`                                                             | Axis code uniqueness, axes-by-program reads, and reassignment checks |
| `projects_projects (Number)` unique and `projects_activities (Number)` unique                                         | Per-table uniqueness for the shared externally allocated number      |
| `projects_projects (AxisId, Number, Id)` / `projects_activities (AxisId, Number, Id)`                                 | Lazy items-by-axis tree reads ordered by number                      |
| `projects_projects (CreatedBy, Visibility, Id)` / `projects_activities (CreatedBy, Visibility, Id)` plus `Visibility` | Public/private accessibility filtering and creator checks            |
| `projects_risks (ProjectId, Id)` and `Score`                                                                          | Project risk table reads and risk-band summary calculation           |
| `projects_number_allocations` primary key sequence                                                                    | Shared monotonic number allocation across projects and activities    |

The tree and detail queries run as `IQueryable` projections translated to SQL;
the client loads only the expanded branch currently requested.

## Acceptance Criteria

Each criterion from `PROJECTS_REQUIREMENTS.md` and its primary covering evidence:

| #   | Criterion                                                                                                                                                                            | Status                | Primary evidence                                                                                                                                                                                                                         |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | --------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | The hierarchy `Program` -> `Axis` -> (`Project` or `Activity`) is enforced, parents are mandatory, and empty containers are valid                                                    | Met                   | `ProjectProgram`, `ProjectAxis`, `ProjectItem`, `ProjectsModelContributor`, provider migrations, `ProjectsDomainTests`, `ProjectsStructureEndpointTests`, `ProjectsItemEndpointTests`                                                    |
| 2   | `Program` and `Axis` carry only name/code, are always public, globally unique by four-letter code, and are managed through Configuration                                             | Met                   | `ProjectsStructureManagementService`, `ProjectsEndpoints`, `ProjectsStructureEndpointTests`, `ProjectsStructureSection`, `ConfigurationPage.test.tsx`, `projects.spec.ts`                                                                |
| 3   | `Project` and `Activity` carry name/status/global number/visibility; a project owns risks and attachments while an activity owns neither                                             | Met                   | `Project`, `Activity`, `ProjectRisk`, `ProjectsAttachments`, `ProjectsModelContributor`, `ProjectsContractTests`, `ProjectsRiskEndpointTests`, `ProjectsAttachmentEndpointTests`                                                         |
| 4   | Statuses `Planning`, `Active`, `Completed`, `OnHold`, and `Cancelled` are fixed, default to `Planning`, and block no operation by themselves                                         | Met                   | `ProjectsContractTests`, `ProjectsDomainTests`, `ProjectsItemEndpointTests`, `ProjectsPage`, `projects.spec.ts`                                                                                                                          |
| 5   | Projects and activities share one incremental number assigned at creation, stable on moves, and never reused                                                                         | Met                   | `ProjectNumberAllocator`, `ProjectNumberAllocation`, `ProjectsDomainTests`, `ProjectsItemEndpointTests`, `PostgresPersistenceTests` (`Postgres_persists_projects_hierarchy_and_concurrent_number_allocations`)                           |
| 6   | The unified identifier is computed on demand from current ancestor codes, number, and name, and is never persisted                                                                   | Met                   | `ProjectIdentifier`, `ProjectsReadService`, `ProjectsContractTests`, `ProjectsDomainTests`, `ProjectsItemEndpointTests`, `ProjectsPage.test.tsx`, `projects.spec.ts`                                                                     |
| 7   | Each project owns an editable risk table with computed score, low/medium/high bands, and summary                                                                                     | Met                   | `ProjectRisk`, `ProjectRiskScoring`, `ProjectRiskWriteService`, `ProjectsRiskEndpointTests`, `ProjectsPage.test.tsx`, `projects.spec.ts`                                                                                                 |
| 8   | Projects support multiple result attachments inheriting project visibility, with no primary image; activities have none                                                              | Met                   | `ProjectsAttachments`, `ProjectAttachments`, `ProjectsAttachmentEndpointTests`, `ProjectsPage`, `projects.spec.ts`                                                                                                                       |
| 9   | Visibility applies only to projects/activities, defaults to `Public`, follows public collaboration/private isolation, and only creators change it                                    | Met                   | `ProjectItemPolicies`, `ProjectItemWriteService`, `ProjectsItemEndpointTests`, `ProjectsRiskEndpointTests`, `ProjectsAttachmentEndpointTests`                                                                                            |
| 10  | Project deletion removes risks and attachments; non-empty program/axis deletion requires atomic reassignment to a compatible target and blocks without one                           | Met                   | `ProjectsStructureManagementService`, `ProjectItemWriteService`, `ProjectsStructureEndpointTests`, `ProjectsRiskEndpointTests`, `ProjectsAttachmentEndpointTests`, `projects.spec.ts`                                                    |
| 11  | The Projects page presents a lazily expanded tree with unified identifiers and URL-aware popups preserving tree state                                                                | Met                   | `ProjectsPage`, `projectsState`, `ProjectsPage.test.tsx`, `projects.spec.ts`                                                                                                                                                             |
| 12  | SQLite/PostgreSQL migrations, backend unit/integration/architecture tests, frontend component tests, and a representative Playwright journey verify behaviour and privacy boundaries | Met (single-user E2E) | `MigrationTests`, `PostgresPersistenceTests`, `ModuleBoundaryTests`, `ProjectsDomainTests`, `ProjectsContractTests`, Projects API suites, `contracts.test.ts`, `ProjectsPage.test.tsx`, `ConfigurationPage.test.tsx`, `projects.spec.ts` |

## Deferred Items

Recorded in `ROADMAP.md`:

- **Second-user Projects privacy E2E journey.** Public-collaboration and
  private-isolation behaviour is covered by API integration tests
  (`ProjectsItemEndpointTests`, `ProjectsRiskEndpointTests`,
  `ProjectsAttachmentEndpointTests`); the browser-level multi-session journey
  waits on multi-account Playwright infrastructure, matching the deferred
  patterns for earlier modules.
- **PostgreSQL representative-volume tree query-plan benchmark.** Index existence
  and database-level query shape are verified; a large-dataset `EXPLAIN ANALYZE`
  benchmark waits on a representative seeding/benchmark harness.
- **Future Projects scope.** Due dates, schedules, dependencies, effort, cost,
  cross-module references, Processes integration, richer risks, promotable
  activities, and tree search/filtering remain future versions.

## Documentation Outcomes

- `README.md`: no change; repository-wide commands and setup were unaffected by
  the Projects waves.
- `ROADMAP.md`: Projects implementation marked accepted; the intentional
  deferrals above recorded.
- `docs/planning/PROJECTS_IMPLEMENTATION_PLAN.md`: Wave 8 status updated to point
  at this record.
