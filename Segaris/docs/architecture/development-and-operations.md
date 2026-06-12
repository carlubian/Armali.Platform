# Development And Operations

This document records the Phase 1 decisions for repository organization, development workflows, testing, observability, and delivery operations.

## Repository Model

Segaris Platform will use a single monorepo containing the frontend, backend, tests, deployment definitions, documentation, and operational scripts.

The initial structure is:

```text
.
|-- src/
|   |-- backend/
|   `-- frontend/
|-- tests/
|-- deploy/
|   `-- compose/
|-- docs/
`-- scripts/
```

Backend solution, assembly, namespace, and test-project names are defined in `docs/planning/BACKEND_FOUNDATION_DECISIONS.md`. Frontend package names remain to be selected with frontend implementation planning.

## Rationale

The application is developed and released as one household system even though the frontend and backend remain independently buildable and run in separate containers. Keeping both applications and their supporting assets in one repository provides:

- One place for architecture, requirements, version plans, and implementation.
- Coordinated changes to the REST API and its frontend consumers.
- Shared visibility of tests, Docker Compose definitions, migrations, and operational scripts.
- Simpler versioning and release traceability for a small project maintained as one product.

The monorepo does not imply that the frontend and backend share runtime code or deployment artifacts. Each application keeps its own dependencies, build process, tests, and Docker image.

## Repository Boundaries

- `src/backend/` owns the ASP.NET Core REST API and backend solution projects.
- `src/frontend/` owns the TypeScript web application and frontend packages.
- `tests/` contains tests that do not naturally belong beside one application, especially cross-application and end-to-end tests. Framework-specific unit tests may remain near their source when that ecosystem benefits from colocation.
- `deploy/compose/` contains Docker Compose definitions and deployment-specific supporting files.
- `docs/` remains the source of truth for architecture, requirements, and version planning.
- `scripts/` contains repeatable development and operational commands that are useful across applications.

Shared libraries should only be introduced when they contain a real reusable contract or capability. The monorepo must not create direct source-level coupling between the TypeScript frontend and C# backend; their integration boundary remains the documented REST API.

## Local Development Model

During normal development, the frontend and backend run natively so that each ecosystem can use its standard watch, debugging, and hot-reload tools. PostgreSQL and any other supporting infrastructure run through Docker Compose.

A separate Compose definition or profile must also support running the complete containerized system locally. This provides a production-like validation path for images, networking, health checks, persistence, and runtime configuration without making containers mandatory for every edit cycle.

The containerized topology uses the same Compose-compatible service model in local development and production. Production is deployed as a Portainer stack from Compose definitions stored in the repository. Caddy is the only service that publishes a household-facing port and routes HTTP requests to the frontend or backend; TLS and application-managed DNS are not part of the initial system. The published port is controlled by `Segaris_HTTP_PORT` and defaults to `5525`.

Production bind mounts are rooted at `Segaris_DATA_PATH`, defaulting to `/data/volumes/Segaris` to follow the server's existing volume convention. PostgreSQL uses a Docker-managed named volume, while attachments and generated backup packages use explicit bind mounts beneath that root.

Backup scheduling and lifecycle management are external operational concerns. Segaris only exposes the administrative backup-generation API and produces the latest valid package; a separate service calls the API, transfers the package to external storage, and owns encryption, retention, and cleanup.

## Application Configuration

Configuration files follow the native convention of each application:

- The ASP.NET Core backend uses `appsettings.json`.
- The TypeScript frontend uses `.env`.

The real local files are excluded from Git. The repository contains corresponding example files that document every supported setting and provide non-sensitive placeholder values:

```text
src/backend/appsettings.example.json
src/frontend/.env.example
```

Developers create their untracked local configuration from these examples. Whenever a supported setting is added, renamed, or removed, its example file and relevant documentation must change in the same commit.

The backend configuration file may contain local credentials and connection strings. Production uses the same ASP.NET Core configuration model, but the final mechanism for mounting or generating its `appsettings.json` and rotating secrets remains an operational decision.

Frontend configuration is public by nature: values included in the compiled application can be inspected by anyone using the browser. The frontend `.env` must therefore contain only non-secret settings such as the backend base URL, feature flags, or display configuration. Credentials, private API keys, and service tokens belong exclusively in the backend or deployment environment.

Environment-specific generated files and framework conventions may be introduced later if the selected frontend framework requires them, but they must preserve this division: tracked examples, untracked real configuration, and no frontend secrets.

The backend targets .NET 10 LTS. The repository-root `global.json` establishes the minimum .NET 10 SDK feature band, rolls forward only within .NET 10, and excludes prerelease SDKs. Backend configuration sources, precedence, environment-variable mapping, typed sections, and secret rules are defined in `docs/planning/BACKEND_FOUNDATION_DECISIONS.md`.

Local database reset and seed are explicit development commands rather than startup behavior. Reset is restricted to the `Development` environment and guarded against production-like PostgreSQL targets. Seed operations are deterministic and idempotent; tests continue to own disposable data independently.

## Testing Strategy

Testing is divided by scope so developers receive fast feedback locally while production-sensitive behavior is still verified against representative infrastructure.

### Backend

- Unit tests use xUnit and cover domain logic, application services, validation, authorization decisions, and other behavior that does not require infrastructure.
- Integration tests use xUnit with ASP.NET Core `WebApplicationFactory` where the HTTP application boundary is relevant.
- Persistence integration tests run against a real disposable PostgreSQL instance provided through Testcontainers. SQLite tests may provide faster local feedback but are never the sole evidence for production query or migration compatibility.

### Frontend

- Unit and component tests use Vitest and Testing Library.
- Tests focus on user-observable behavior, state transitions, validation, and API interaction boundaries rather than internal implementation details.
- Broad snapshot testing is avoided. Targeted snapshots may be used only when they provide stable and meaningful coverage.

### End-To-End And Visual Validation

- Playwright runs end-to-end tests against the complete containerized application, including Caddy, frontend, backend, and PostgreSQL.
- End-to-end coverage focuses on critical user journeys and integration boundaries rather than duplicating every unit-level case.
- Playwright screenshots or visual comparisons may protect a small set of critical screens. Large, fragile visual snapshot suites are outside the initial strategy.

### Database Migrations

Automated migration tests use PostgreSQL and verify at least:

- Creation of a fresh database by applying the full migration history.
- Upgrade from the previous supported release schema to the current schema.
- Any data transformation or provider-specific migration behavior introduced by a release.

### Test Data And Coverage

Test data must be small, deterministic, and owned by the test or suite that creates it. Tests must not depend on execution order or shared mutable household data.

There is no initial repository-wide percentage coverage target. Coverage is judged by behavior and risk: important domain rules, authorization boundaries, persistence behavior, critical user journeys, and bug fixes require tests. Every bug fix should add a regression test when the failure can be reproduced automatically.

### Execution Tiers

- Fast unit and component tests run routinely during development and on every pull request.
- Backend integration and PostgreSQL migration tests run in GitHub Actions for pull requests.
- End-to-end tests run in GitHub Actions against a disposable Compose stack. Their exact trigger and whether they become required merge checks will be decided with the CI workflow.
- Developers can run every CI suite locally using documented repository commands before opening or updating a pull request.

GitHub Actions will report suite status on pull requests so contributors can assess application stability before merging. Workflow structure, required checks, image builds, release triggers, and deployment automation remain part of the open CI/CD decision.

## Observability

Segaris uses lightweight structured logging and health checks suitable for a single household server. Observability infrastructure is optional and must never become a runtime dependency for normal application behavior.

### Structured Logging

- Backend logs are structured events and are always written to `stdout` and `stderr` for Docker and Portainer visibility.
- The backend uses Serilog with compact JSON console events and supports optional delivery through the Serilog Seq sink to an existing Seq server on the household network. Delivery uses bounded buffering and batches.
- Seq delivery is best-effort. Network failures, timeouts, authentication failures, or Seq downtime must not fail requests, delay application startup indefinitely, make health checks unhealthy, or stop background operations.
- Local container logs remain available when Seq is unavailable. Docker or Portainer owns their rotation and retention policy.
- Production defaults to the `Information` level, with category-specific overrides supplied through backend configuration.

The backend Seq integration is configured through `appsettings.json` and represented in `appsettings.example.json`. Configuration includes an enable switch, server URL, optional API key, and minimum level. The API key is a backend secret and must never be exposed to the frontend.

### Frontend Diagnostics

The browser does not connect directly to Seq. Direct browser delivery would expose Seq configuration or credentials and would give untrusted client input a path into the logging service.

Instead, the frontend may submit selected diagnostic events to a protected backend endpoint. The backend validates and limits these events, enriches them with server-controlled context, and records them through the same logging pipeline used for backend events. This allows them to reach `stdout` and Seq when configured.

Frontend reporting is limited to actionable diagnostics such as uncaught errors, failed application initialization, and explicitly handled critical failures. It must not forward routine browser console output. The endpoint must apply authentication, payload-size limits, rate limiting, and a fixed event schema to prevent log injection or accidental data collection.

### Sensitive Information

Logs must not contain passwords, session cookies, authorization headers, API keys, connection strings, full request or response bodies, attachment contents, or sensitive private-record values. User and entity identifiers may be logged only when needed for diagnosis and must remain structured fields rather than being embedded into free-form messages.

### Correlation

Each backend request has a correlation identifier based on the ASP.NET Core trace identifier or standard distributed-tracing context. The identifier is returned to the frontend in error responses where useful and included in related backend and forwarded frontend diagnostic events.

### Health Checks

- `/health/live` reports whether the backend process is running. It does not check Seq or other optional external services.
- `/health/ready` reports whether startup and migrations completed and whether required dependencies such as PostgreSQL and attachment storage are available.
- Seq status is excluded from readiness because logging delivery is non-blocking.
- Caddy and Compose use backend health information to avoid routing traffic before the backend is ready and to expose meaningful container status.

### Initial Scope

Segaris does not initially require Prometheus, Grafana, distributed tracing infrastructure, or a separate application-performance monitoring service. Seq provides centralized searchable events when available; Docker or Portainer logs and health endpoints remain the operational fallback.

Administrative and background operations, including backup generation and migrations, emit structured start, completion, failure, and duration events. Operational logs do not replace functional audit history, which remains outside the shared data model.

## Continuous Integration And Delivery

GitHub Actions provides validation and image publication. Production deployment remains an explicit Portainer operation so publishing a commit does not automatically update the household server.

### Pull Request Validation

Pull request workflows run the repository validation suites without publishing images or deploying:

- `Segaris Backend`: restore, formatting verification, build, unit tests, architecture
  tests, and API integration tests.
- `Segaris PostgreSQL`: production-provider integration tests and provider-specific
  migration tests through disposable Testcontainers databases.
- `Segaris Compose`: a clean build of the complete stack plus readiness and Caddy routing
  validation through `scripts/compose-smoke-test.sh`.

All three jobs are required checks for `main`. The workflow has only read access to
repository contents and receives no production or registry credentials. Frontend
unit/component and Playwright suites will join these gates when the real frontend
replaces the temporary placeholder.

### Main Branch Publication

After a change reaches the main branch, GitHub Actions repeats the three validation
jobs for that exact commit. Only a successful main-branch validation triggers the
workflow that builds and publishes independent backend, temporary frontend, and
Caddy container images to the household's private Azure Container Registry.

Each image is tagged with the immutable Git commit SHA. Releases may add a human-readable version tag pointing to the same image, but deployment definitions must not rely exclusively on a mutable `latest` tag. Frontend and backend images from the same workflow run form one logical Segaris release and should normally be deployed together.

### Registry Authentication

GitHub Actions authenticates through Azure workload identity federation and OIDC.
The federated identity is scoped to this repository's `segaris-production-images`
environment and receives only `AcrPush` on the target registry. The environment
stores identifiers and registry names as variables; no Azure client secret or
registry password is stored in GitHub.

Portainer already owns separate credentials with pull access to the private Azure Container Registry. GitHub Actions does not transmit deployment credentials to Portainer, and Portainer does not require GitHub repository secrets.

### Production Deployment

Production deployment is manual from Portainer:

- Select the immutable image tags for one logical release.
- Update or redeploy the Compose-based stack.
- Allow the backend to apply pending migrations before reporting readiness.
- Confirm container health and the basic application flow after deployment.

The publication workflow does not call Portainer or automatically redeploy the stack. Automated deployment may be reconsidered after the manual process is stable and recovery procedures have been exercised.

### Rollback

Previous images remain available in Azure Container Registry according to its external retention policy. Application rollback consists of selecting the previous frontend and backend image tags in Portainer and redeploying the stack.

A container rollback does not automatically reverse database migrations. Normal releases therefore prefer additive, forward-compatible migrations. A release containing a destructive or backward-incompatible migration requires a current backup and explicit deployment and recovery notes before production deployment.

### Workflow Security

- Pull request workflows from untrusted contexts must not receive registry publishing secrets.
- Image publication occurs only from trusted branch or release events after validation succeeds.
- GitHub Actions permissions are declared explicitly and kept to the minimum required.
- Production `appsettings.json`, Portainer stack variables, Seq credentials, database credentials, and other runtime secrets are not needed to build images and are not stored in the container registry.

The concrete workflow, required-check, branch-ruleset, and environment-variable
contract is recorded in `docs/planning/BACKEND_CI_DECISIONS.md`.

## Open Decisions

- Define the exact local startup commands and Docker Compose profiles.
- Define production generation or mounting of backend configuration and secret rotation.
- Define repository-wide formatting, linting, and validation commands.
- Decide whether REST contracts generate frontend types or are maintained independently.
