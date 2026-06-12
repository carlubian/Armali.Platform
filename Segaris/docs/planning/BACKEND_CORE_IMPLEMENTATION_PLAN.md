# Backend And Core Implementation Plan

## Purpose

This plan translates the Phase 1 architecture into an executable technical backlog for the Segaris backend, automated tests, and deployment assets.

It is a foundation plan, not a Phase 3 product-version plan. It deliberately avoids detailed business-module behavior until the Phase 2 functional requirements exist. Frontend implementation and visual design are outside this plan, except for contracts or infrastructure required to validate the complete runtime topology.

## Target Outcome

At the end of this plan, the repository should contain a deployable backend foundation that:

- Runs as an ASP.NET Core modular monolith.
- Provides authentication, authorization, privacy enforcement, API conventions, persistent jobs, attachments, backups, health checks, and structured logging.
- Supports SQLite for local development and PostgreSQL for production and integration tests.
- Builds and runs through native development commands and Docker Compose.
- Has repeatable unit, integration, migration, container, and operational tests.
- Publishes an immutable backend image through GitHub Actions.
- Is ready for business modules to be added without revisiting the main platform boundaries.

## Scope Boundaries

Included:

- Backend solution and project scaffolding.
- Shared technical primitives and platform contracts.
- Identity and administrative user-management foundation.
- Database, migrations, attachments, jobs, backups, logging, diagnostics, and health checks.
- Backend unit and integration test projects.
- Migration and deployment smoke tests.
- Dockerfiles, Compose definitions, Caddy configuration, scripts, and GitHub Actions.
- Architecture enforcement and developer documentation.

Excluded:

- Frontend application and visual component implementation.
- Detailed Capex, Opex, Inventory, Travel, and other business-module behavior.
- Future API-key authentication.
- External OCR, AI, calendar, or other provider integrations.
- Public internet exposure, TLS, Kubernetes, multi-instance execution, and high availability.
- User-facing import, export, and portability features beyond the administrative backup package.

## Decisions Required Before Or During Implementation

The following open architecture items must be resolved at the indicated point. They should not silently acquire implementation defaults.

| Decision | Required by |
| --- | --- |
| Supported .NET and ASP.NET Core version | Resolved in Wave 0 |
| Exact solution and project names | Resolved in Wave 0 |
| Configuration keys and environment-variable hierarchy | Resolved in Wave 0 |
| Development seed and database-reset workflow | Resolved in Wave 0 |
| Temporary-password and forced-change behavior | Resolved in Wave 4 |
| Production secret injection and rotation | Wave 8 |
| Backend container UID/GID and host-directory provisioning | Wave 8 |
| Attachment size, type, validation, and malware policy | Wave 5 |
| Backup API authorization details | Wave 6 |
| Restore procedure and verification frequency | Wave 8 |
| Ubuntu and server hardware baseline | Wave 8 |
| Exact required GitHub checks and branch protection | Wave 9 |

## Delivery Strategy

Work is divided into dependency-ordered waves. A wave may be split into multiple pull requests, but its exit criteria should pass before work that depends on it is considered complete.

### Wave 0: Resolve Foundation Blockers

Status: **Completed**. Decisions and conventions are recorded in `docs/planning/BACKEND_FOUNDATION_DECISIONS.md`; SDK selection is enforced by the repository-root `global.json`.

Tasks:

1. Select the supported .NET SDK/runtime and pin it with `global.json` if appropriate.
2. Confirm solution, assembly, root namespace, and test-project names.
3. Define the complete backend configuration schema, configuration precedence, and environment-variable mapping.
4. Record decisions for local database reset/seed behavior.
5. Add unresolved security and operational choices to `ROADMAP.md` if implementation analysis discovers additional gaps.

Deliverables:

- Updated architecture documents and roadmap entries.
- A short implementation decision record for naming, runtime, and configuration conventions.

Exit criteria:

- Wave 1 can scaffold the repository without inventing undocumented conventions.

### Wave 1: Repository And Backend Skeleton

Status: **Completed on 2026-06-11**. The solution skeleton, module composition points, validated database configuration, health endpoint, five test projects, repository policies, and developer scripts are implemented. Persistence-specific behavior remains in Wave 2.

Tasks:

1. Create the monorepo directories: `src/backend`, `tests`, `deploy/compose`, and `scripts`.
2. Create the backend solution with the executable API project and the deliberately small shared project.
3. Create backend unit, API integration, PostgreSQL integration, and migration test projects.
4. Configure nullable reference types, implicit usings, analyzers, deterministic builds, and repository formatting.
5. Add central package/version management if it reduces maintenance without obscuring project ownership.
6. Implement module registration conventions and add empty `Platform` and `Modules/Identity` composition points.
7. Add `appsettings.example.json`, ignore real local configuration, and validate required typed options at startup.
8. Add repeatable restore, build, format, test, and local-run scripts.

Tests:

- Solution restore and build from a clean checkout.
- Configuration validation tests for missing, malformed, and valid settings.
- Architecture test proving the API host is the composition root and shared code has no dependency on modules.

Exit criteria:

- A contributor can clone the repository, create local configuration from the example, build the solution, and start an empty healthy API.

### Wave 2: Persistence Foundation And Provider Compatibility

Status: **Completed on 2026-06-12**. The implementation adds `Segaris.Persistence`, one modular `SegarisDbContext`, SQLite and PostgreSQL migration assemblies, startup migration execution, provider compatibility fixtures, development reset/seed commands, and automated migration coverage. The SQLite, PostgreSQL Testcontainers, migration, API, unit, and architecture suites pass. PostgreSQL coverage remains mandatory in CI. There is no previous supported schema fixture for this first migration baseline; upgrade-fixture coverage begins with the next schema release.

Tasks:

1. Implement `SegarisDbContext` as the single application context.
2. Define module-owned EF Core configuration discovery and registration.
3. Configure SQLite and PostgreSQL provider selection through validated configuration.
4. Add separate SQLite and PostgreSQL migration assemblies and design-time factories.
5. Implement startup migration execution before readiness and HTTP traffic are enabled.
6. Establish table naming, key generation, UTC timestamp, date-only, decimal precision, and currency-code conventions.
7. Implement transaction and query helpers only where the documented conventions require them; do not add generic repositories.
8. Add the agreed development reset and seed workflow.

Tests:

- Fresh database creation for SQLite and PostgreSQL.
- PostgreSQL migration upgrade from the previous supported schema fixture.
- Provider-compatibility tests for mappings, constraints, dates, decimals, and representative queries.
- Startup failure when migrations fail.

Exit criteria:

- Both providers can create the same logical model, while PostgreSQL remains the production compatibility authority.

### Wave 3: Shared Core And API Conventions

Status: **Completed on 2026-06-12**. The implementation adds the deliberately small shared primitives, claims-backed current-user access, creator-only visibility policy, standard module route groups, camel-case JSON, bounded request bodies, centralized ProblemDetails, validated pagination and deterministic sorting, OpenAPI 3.1, development-only Scalar documentation, and focused unit, integration, and architecture coverage. The implementation path for future modules is recorded in `docs/planning/BACKEND_MODULE_CONVENTIONS.md`.

Tasks:

1. Implement stable primitives such as `UserId`, ISO currency code validation, UTC clock abstraction, creation/modification metadata, visibility values, and pagination contracts.
2. Implement current-user context and centralized public/private record authorization policies.
3. Add API route-group and module-registration conventions.
4. Configure camel-case JSON, explicit DTO handling, bounded request sizes, and cancellation propagation.
5. Implement centralized `ProblemDetails` mapping with stable error codes and trace identifiers.
6. Implement pagination validation, deterministic sorting rules, and maximum page-size enforcement.
7. Add OpenAPI generation and development-only interactive documentation.
8. Add architecture tests that prevent business entities and excluded generic abstractions from entering `Segaris.Shared`.

Tests:

- Primitive and policy unit tests.
- Error/status mapping tests, including privacy-preserving `404` behavior.
- Invalid pagination and binding tests.
- OpenAPI schema validation and duplicate-route detection.
- Dependency-direction and shared-core governance tests.

Exit criteria:

- New modules have one documented path for registration, API contracts, errors, pagination, current-user access, and visibility enforcement.

### Wave 4: Identity, Sessions, And Administrative Users

Status: **Completed on 2026-06-12**. ASP.NET Core Identity is integrated into the single `SegarisDbContext` through a module-owned model contributor (the persistence project stays free of Identity dependencies), with `int` keys aligned to `UserId`. The implementation adds the configured password policy, lockout, security-stamp revalidation, the hardened same-origin session cookie, antiforgery for cookie-authenticated writes, filesystem-persisted Data Protection keys, the `/api/session` endpoints with password change, administrative user management under `/api/admin/users`, configuration-driven first-administrator bootstrap, and session invalidation on deactivation and credential recovery. SQLite, PostgreSQL, migration, API, unit, and architecture suites pass; PostgreSQL coverage includes an Identity lifecycle test. Decisions are recorded in `docs/planning/BACKEND_IDENTITY_DECISIONS.md`.

Tasks:

1. Integrate ASP.NET Core Identity into the shared database context.
2. Configure password hashing, 12-character minimum, five-attempt lockout, 15-minute lockout duration, roles, and security-stamp validation.
3. Configure the same-origin authentication cookie with `HttpOnly`, `SameSite=Strict`, 12-hour sliding inactivity expiry, and the documented non-secure local-network limitation.
4. Persist Data Protection keys outside the disposable backend container.
5. Implement antiforgery token issuance and validation for cookie-authenticated writes.
6. Implement `POST`, `GET`, and `DELETE /api/session` plus password-change behavior.
7. Implement administrative user listing, creation, activation, deactivation, and approved credential-recovery behavior under `/api/admin/users`.
8. Bootstrap the first administrator through an explicit, repeatable, non-public mechanism.
9. Ensure deactivation and security-sensitive changes invalidate active sessions.

Tests:

- Login, logout, expiry, sliding session, lockout, password change, and security-stamp invalidation.
- Generic login failures that do not reveal account existence or state.
- Antiforgery rejection and success paths.
- Role-policy tests and creator-only privacy tests involving administrators.
- Restart test proving persisted Data Protection keys preserve valid sessions.

Exit criteria:

- Browser-session authentication and user administration work end to end without exposing credentials or private records.

### Wave 5: Attachments And Required Storage

Status: **Completed on 2026-06-12**. The implementation adds the documented 25 MiB positive allow-list, extension/media-type/content validation, narrow shared attachment contracts, UUID filesystem storage with relational metadata, owner-bound lookup, compensating create/delete behavior, reconciliation diagnostics, attachment-storage readiness, paired SQLite/PostgreSQL migrations, and Testing-only HTTP probes. SQLite and PostgreSQL upgrade coverage starts from the Wave 4 Identity schema; all backend suites pass. Decisions are recorded in `docs/planning/BACKEND_ATTACHMENT_DECISIONS.md`.

Tasks:

1. Finalize and document attachment limits and validation policy.
2. Implement narrow attachment contracts in the shared platform layer and concrete storage in the owning platform module.
3. Store metadata and ownership in the database while writing UUID-named files beneath module-specific directories.
4. Prevent path traversal, client-controlled storage paths, unsafe filenames, unbounded streams, and unauthorized access.
5. Implement create/delete compensating operations and orphan-reconciliation diagnostics.
6. Add attachment-storage checks to readiness without turning optional integrations into dependencies.

Tests:

- Upload, metadata, authorization, download, and deletion paths.
- Size, content-type, filename, path traversal, interrupted write, duplicate, and missing-file cases.
- Compensation behavior for database and filesystem failures.
- Readiness behavior for writable, missing, and inaccessible storage.

Exit criteria:

- Modules can safely attach files through a narrow contract, and database/filesystem inconsistencies are detectable and recoverable.

### Wave 6: Persistent Background Jobs And Backups

Tasks:

1. Implement the persistent job record, lifecycle states, progress, cancellation request, diagnostics, and timestamps.
2. Implement typed job registration, handler resolution, sequential claiming, graceful shutdown, and interrupted-job recovery for one backend instance.
3. Define retention or cleanup behavior for completed job records.
4. Implement the administrative backup job with single-run exclusion.
5. Generate a staged package containing a PostgreSQL dump, attachments, and a manifest, then atomically replace the previous valid package.
6. Expose authenticated endpoints to start a backup and query job/result status.
7. Ensure secrets, private payloads, and usable credentials never enter job parameters or normal logs.

Tests:

- State-transition, claim, cancellation, restart, failure, and graceful-shutdown tests.
- Concurrent backup conflict and single-run enforcement.
- Manifest and package-content validation.
- Failure tests proving the previous valid package is preserved.
- Integration test using a disposable PostgreSQL database and temporary attachment tree.

Exit criteria:

- A long-running operation survives request completion and process restart, and administrators can generate one valid latest backup package safely.

### Wave 7: Observability, Diagnostics, And Runtime Safety

Tasks:

1. Configure structured logging to `stdout`/`stderr` with category-specific levels.
2. Add optional best-effort Seq delivery with validated configuration and bounded failure behavior.
3. Add request correlation and propagate the trace identifier into problem responses and operational events.
4. Implement `/health/live` and `/health/ready`, including migrations, database connectivity, and attachment storage in readiness.
5. Implement the protected, rate-limited, fixed-schema frontend diagnostics endpoint without requiring a frontend client.
6. Add rate limiting and request-size controls to authentication, diagnostics, webhook placeholders only when implemented, and other abuse-sensitive endpoints.
7. Review all logs for secret and sensitive-data leakage.

Tests:

- Health transitions during startup, dependency failure, and recovery.
- Seq unavailability never blocks startup, readiness, requests, or jobs.
- Correlation identifier consistency.
- Diagnostics authentication, schema, rate, and payload limits.
- Automated checks or focused tests for log redaction of known secret fields.

Exit criteria:

- Operators can diagnose startup, requests, migrations, storage, and jobs through container logs and health endpoints even when Seq is unavailable.

### Wave 8: Containers, Compose, Caddy, And Operations

Tasks:

1. Create a production-oriented multi-stage backend Dockerfile running as the selected non-root UID/GID.
2. Add a temporary placeholder frontend container or static response only if required to validate Caddy routing before the real frontend exists; keep it out of product scope.
3. Create Compose definitions or profiles for infrastructure-only development, full local container execution, and Portainer-compatible production deployment.
4. Configure PostgreSQL named-volume persistence and bind mounts for attachments, backups, Data Protection keys, and backend configuration.
5. Implement Caddy routing for `/api/` and frontend traffic, plus container health checks and startup dependencies.
6. Provide tracked environment/configuration examples including the documented default HTTP port and data path.
7. Add host provisioning, permissions, deployment, backup generation, restore, rollback, and smoke-test scripts or runbooks.
8. Validate immutable image-tag configuration for production stacks.

Tests:

- Docker image build and non-root execution.
- Full Compose startup from empty storage.
- Caddy routing, API reachability, readiness gating, restart, and persistence tests.
- Backup generation and documented restore rehearsal.
- Rollback rehearsal that accounts for forward-only database migrations.

Exit criteria:

- The complete non-visual runtime can be started locally and deployed through Portainer with persistent state, health reporting, and documented recovery steps.

### Wave 9: CI, Publication, And Foundation Acceptance

Tasks:

1. Add GitHub Actions workflows for restore, formatting, build, unit tests, API integration tests, PostgreSQL integration tests, migration tests, and Compose smoke tests.
2. Keep pull-request workflows free of production and registry secrets.
3. Add trusted main-branch image publication to the private Azure Container Registry.
4. Tag images with the immutable Git commit SHA and optionally a human-readable release tag.
5. Use Azure OIDC federation where practical; otherwise document narrowly scoped push credentials and rotation.
6. Define required checks and branch-protection expectations.
7. Add a foundation acceptance script that runs the same critical validations locally.
8. Update architecture and operational documentation to match the implemented commands, paths, configuration, and constraints.

Tests:

- Workflow syntax and least-privilege permissions review.
- Clean pull-request validation without secret access.
- Image publication dry run or controlled trusted-branch test.
- Final clean-machine/clean-storage acceptance rehearsal.

Exit criteria:

- Every change receives automated backend and deployment validation, and trusted main-branch commits can produce traceable immutable images.

## Recommended Pull Request Breakdown

The following pull-request sequence keeps changes reviewable while preserving useful checkpoints:

1. Runtime decisions, solution skeleton, configuration, and developer commands.
2. EF Core providers, migrations, conventions, and migration tests.
3. Shared core, API conventions, OpenAPI, and architecture tests.
4. Identity, sessions, antiforgery, roles, and administrative users.
5. Attachment storage and readiness integration.
6. Persistent jobs and backup generation.
7. Logging, Seq adapter, diagnostics, health checks, and rate limits.
8. Dockerfiles, Compose, Caddy, persistence, and operational runbooks.
9. GitHub Actions, ACR publication, and final foundation acceptance.

Each pull request should update configuration examples and documentation in the same change whenever it introduces or changes a supported setting or command.

## Cross-Cutting Quality Gates

These conditions apply to every wave:

- Backend authorization, including creator-only privacy, is enforced in server-side queries and commands.
- No EF Core entity or provider DTO is serialized as an API contract.
- No module reads another module's internal `DbSet`, table, entity, or implementation service.
- No secret enters source control, frontend configuration, image layers, logs, job payloads, or API responses.
- All I/O supports cancellation and has explicit size or time bounds where applicable.
- PostgreSQL tests cover production-sensitive persistence behavior.
- New settings are added to tracked examples and documentation.
- Unexpected failures return safe problem details with a trace identifier.
- Optional services such as Seq never affect readiness or core operation.
- Architecture documents are corrected when implementation proves an assumption invalid.

## Foundation Completion Criteria

The backend and core foundation is complete when all of the following are true:

1. A clean checkout can build and run through documented native commands.
2. SQLite development and PostgreSQL production configurations both work, with provider-specific migration histories.
3. Authentication, roles, antiforgery, privacy, and session invalidation are covered by integration tests.
4. Persistent jobs, attachments, and backup generation survive the documented failure and restart scenarios.
5. Liveness and readiness accurately represent the backend and required dependencies.
6. The Compose stack starts from empty storage, preserves data across recreation, and routes through Caddy.
7. Restore and rollback procedures have been rehearsed, not merely written.
8. Pull requests run the agreed validation suites and main-branch workflows publish immutable backend images.
9. A sample non-business module or architecture fixture proves module registration, persistence ownership, API conventions, and dependency rules without prematurely implementing a real household domain.
10. Documentation matches the actual repository structure, commands, configuration, and operational behavior.

## Follow-Up

After Phase 2 defines the first business module, Phase 3 should create a small vertical version plan that uses this foundation. That version should add real user value without expanding the shared core merely because two domain concepts have similar names or shapes.
