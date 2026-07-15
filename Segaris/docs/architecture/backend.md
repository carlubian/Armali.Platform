# Backend Architecture

This document records the Phase 1 decisions for the  Segaris backend architecture.

## Architecture Style

The backend will be a modular monolith built with ASP.NET Core.

Segaris runs as one backend process, exposes one REST API, uses one relational database, and is deployed as one backend container. Internally, business capabilities are organized as explicit modules so the growing set of household domains does not collapse into one undifferentiated application layer.

A distributed microservice architecture is not justified for the initial product. Segaris has one household, a small number of users, one deployment target, and no independent scaling or availability requirements. Separate services would add networking, deployment, data-consistency, observability, and operational costs without a corresponding product benefit.

## Solution Shape

The initial backend should use a small number of projects and organize most business code by module rather than creating one project for every technical layer or domain.

An indicative structure is:

```text
src/backend/
|-- Segaris.Api/
|   |-- Platform/
|   |-- Modules/
|   |   |-- Identity/
|   |   |-- Configuration/
|   |   |-- Launcher/
|   |   |-- Capex/
|   |   |-- Opex/
|   |   |-- Inventory/
|   |   |-- Travel/
|   |   `-- ...
|   `-- Program.cs
|-- Segaris.Persistence/
|-- Segaris.Migrations.Postgres/
|-- Segaris.Migrations.Sqlite/
`-- Segaris.Shared/
```

The exact solution and project names will be selected during implementation planning. Additional projects should be introduced only when they provide an enforceable boundary or independently reusable capability, not to mirror conceptual layers mechanically.

`Segaris.Api` is the executable composition root and contains the REST host, module registration, middleware, authentication integration, health checks, and runtime configuration.

`Segaris.Persistence` owns the provider-neutral `SegarisDbContext`, persistence conventions, provider selection, and the model-contributor contract used by modules. It exists as a separate project so both migration assemblies and the API can depend on the context without circular project references. Module entities and mappings remain owned by their modules inside `Segaris.Api`.

`Segaris.Shared` is intentionally small. It may contain stable technical primitives and contracts that genuinely apply across modules. It must not become a general location for domain entities, miscellaneous helpers, or code that has no clear owner.

## Vertical Module Structure

Each domain module owns its behavior from the HTTP boundary to persistence. A module may contain:

- REST endpoints and transport contracts.
- Application use cases organized as commands or queries where that distinction is useful.
- Domain entities, value objects, rules, and policies.
- Request validation and authorization requirements.
- Entity Framework Core mappings and module-specific queries.
- Internal services and explicitly published contracts.
- Unit and integration tests for its behavior.

Code should be grouped around capabilities or use cases within a module rather than split globally into folders such as all controllers, all services, all repositories, and all validators.

The architecture does not require every operation to reproduce the same set of layers. Simple read operations may use focused EF Core projections, while behavior with meaningful invariants belongs in domain or application code. Abstractions should reflect actual complexity rather than a mandatory template.

## Module Boundaries

Modules own their internal types and persistence details.

- One module must not directly use another module's internal entities, `DbSet` properties, tables, or implementation services.
- Cross-module behavior uses a deliberately published interface, application service, or immutable contract owned by the providing module.
- Contracts expose the minimum information required and do not leak EF Core tracked entities.
- Circular module dependencies are prohibited. A shared concept must have one clear owner or be promoted to a genuinely shared platform primitive.
- Database foreign keys may preserve relational integrity across module-owned records when justified, but application code still respects the module boundary.

The backend initially uses direct in-process calls for cross-module collaboration. It does not require an internal message bus, distributed events, or a mediator library. Domain or integration events may be introduced later when they solve a concrete decoupling, consistency, or background-processing requirement.

## Persistence

The modular monolith uses one relational database and one application transaction boundary. This permits an application use case to update multiple module-owned records atomically when the documented behavior genuinely requires it.

Entity Framework Core remains the persistence abstraction described in `docs/architecture/data-and-storage.md`. Modules own their entity mappings and query logic while database registration and migration execution are composed centrally by the backend host.

The initial implementation should prefer a single application `DbContext` with module-owned configuration classes unless implementation experience demonstrates that separate contexts provide a useful enforceable boundary without complicating migrations and transactions. This choice will be confirmed with the detailed domain-organization decision.

Segaris will not add a generic repository or generic unit-of-work abstraction over EF Core. Module-specific repositories may be introduced when they express a meaningful domain collection or isolate non-trivial persistence behavior. Straightforward queries and updates may use the module's EF Core boundary directly.

## API Style

The frontend integration boundary is a versionable HTTP REST API under `/api/`.

Endpoints use ASP.NET Core Minimal APIs and are registered by each module through focused route-mapping extensions or equivalent module registration classes. Related endpoints are organized with `MapGroup` under a stable module or platform prefix.

Controllers and a global MVC controller hierarchy are not initially required. A controller may be introduced for a specific capability if it provides a demonstrated framework benefit that would otherwise require substantial custom Minimal API infrastructure. Mixing styles casually within one module should be avoided.

Indicative route groups include:

```text
/api/session
/api/admin/users
/api/capex/entries
/api/inventory/items
/api/travel/trips
/api/backup-jobs
```

Routes use lowercase plural resource names and stable identifiers. Internal C# type names, database table names, and implementation-layer terminology must not determine the public URL structure.

### HTTP Semantics

The API follows standard HTTP behavior:

- `GET /resources` returns a filtered or paginated collection.
- `GET /resources/{id}` returns one resource representation.
- `POST /resources` creates a resource and normally returns `201 Created` with its location.
- `PUT /resources/{id}` replaces the complete client-editable representation when complete replacement is useful and safe.
- `PATCH /resources/{id}` is used only for a clearly defined partial-update contract, not as the default update method.
- `DELETE /resources/{id}` performs the documented permanent deletion and normally returns `204 No Content`.

Operations that are not naturally CRUD use explicit subresources or action endpoints. They normally use `POST` when they initiate a state transition or asynchronous operation, for example `POST /api/backup-jobs` or `POST /api/admin/users/{id}/deactivate`. Action names should describe domain behavior rather than transport or implementation details.

All write operations define their idempotency expectations. The frontend and infrastructure must not automatically retry a non-idempotent request unless the endpoint provides a documented idempotency mechanism.

### JSON And Contracts

The API uses JSON with `camelCase` property names. Dates, timestamps, amounts, currencies, and identifiers follow the conventions in `docs/architecture/data-and-storage.md`.

Request and response contracts are explicit DTOs or immutable transport records. EF Core entities, tracked navigation properties, and internal domain objects are never serialized directly. Contracts expose only information the current user is authorized to access.

The initial implementation maintains C# and TypeScript transport types independently. OpenAPI-based frontend type generation may be adopted later if it reduces drift without coupling frontend behavior to generated service clients or exposing backend implementation models.

### Status Codes And Problem Details

Successful endpoints return the most specific standard status code. Error responses use ASP.NET Core `ProblemDetails` or `ValidationProblemDetails` with the `application/problem+json` media type where supported.

Segaris extends problem details with stable machine-readable fields:

- `code`: a stable application error code translated by the frontend.
- `traceId`: the request correlation identifier used for diagnosis.
- Structured field errors for validation failures when a stable request-field mapping exists.

The human-readable `title` and `detail` fields provide a safe fallback but are not the frontend's primary translation contract. Production responses never expose stack traces, exception types, SQL, secrets, private record contents, or other internal diagnostic details.

The baseline status mapping is:

- `400 Bad Request` for malformed requests, binding failures, and invalid transport values.
- `401 Unauthorized` when authentication is missing, invalid, or expired.
- `403 Forbidden` when the caller is authenticated but may know the resource exists and lacks the required platform capability.
- `404 Not Found` when a resource does not exist or its existence must be hidden by the privacy model.
- `409 Conflict` for state, uniqueness, concurrency, or active-operation conflicts.
- `422 Unprocessable Content` for a well-formed request that violates a documented domain rule and is not better represented by `409`.
- `500 Internal Server Error` for unexpected failures.
- `503 Service Unavailable` when a required backend dependency prevents the service from handling the request.

Authorization behavior must be consistent across modules. In particular, creator-only private records normally return `404` to other users so the API does not reveal their existence.

### Collections, Filtering, And Pagination

Collection endpoints use explicit resource-specific query parameters. Segaris does not initially expose OData, GraphQL, a generic filter expression language, or arbitrary client-selected database fields.

Large or potentially growing collections use page-based pagination with:

- `page`, starting at `1`.
- `pageSize`, with a documented default and backend-enforced maximum.
- Module-specific filter and sort parameters with allow-listed names and values.

The standard paginated response contains:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 25,
  "totalCount": 0
}
```

Small bounded reference collections may return an unpaginated array when the module documents that the dataset is intentionally limited. Pagination must not be added mechanically when it harms a simple selection workflow, but endpoints must not permit unbounded reads of growing domain data.

Sorting is deterministic and includes a stable tie-breaker. Invalid filter, sort, page, or page-size values return a structured `400` response rather than being silently ignored.

The implemented shared defaults are page `1`, page size `25`, and maximum page size `100`. Modules allow-list their supported sort fields and always identify a stable tie-breaker, normally the resource identifier.

JSON API request bodies are limited to 1 MiB by default. An endpoint that legitimately requires a different bound must declare it explicitly through endpoint metadata; attachments will define their own security and size policy in Wave 5.

### OpenAPI

The backend generates an OpenAPI document through the supported ASP.NET Core OpenAPI tooling. Endpoint metadata documents request contracts, successful responses, problem responses, authentication requirements, and relevant operation summaries.

The OpenAPI document is validated during automated testing or CI so duplicate routes, missing schema metadata, and accidental contract changes are visible. Interactive API documentation may be enabled in development. Production exposure is disabled by default unless an operational need justifies protected access.

### API Versioning

The initial routes do not include a `/v1` prefix and the backend does not add an API-versioning package.

Frontend and backend are one household product, are normally released together, and initially have no independently supported external consumers. The API should still prefer additive and compatible contract evolution where practical.

Formal API versioning is introduced only when Segaris must support multiple contract generations concurrently, an external consumer cannot upgrade with the main application, or a breaking change cannot be coordinated through one release. That decision must define the supported version lifetime and may use URL, header, or another standard strategy based on the concrete need.

## Authentication And Sessions

Segaris uses ASP.NET Core Identity as the user, password, role, lockout, security-stamp, and credential-hashing foundation. It does not use the scaffolded Identity Razor interface. The React SPA interacts with Segaris-owned Minimal API endpoints and transport contracts.

### Browser Authentication

The same-origin SPA authenticates through an ASP.NET Core authentication cookie.

- The cookie is `HttpOnly` and uses `SameSite=Strict`.
- JavaScript never reads or stores the authentication credential.
- Segaris does not place JWT access or refresh tokens in browser local storage, session storage, or normal frontend state.
- The initial session expires after 12 hours of inactivity and uses sliding expiration while the user remains active.
- There is no persistent "remember me" option initially.
- Explicit sign-out removes the browser credential.
- Password changes, account deactivation, and security-sensitive administrative changes invalidate existing sessions through ASP.NET Core Identity security-stamp validation or an equivalent central mechanism.

The cookie cannot initially use the `Secure` flag because the documented deployment serves Segaris over plain HTTP on a protected household network. This is an accepted limitation of the initial local-only deployment, not a general security recommendation. HTTPS and `Secure` cookies become mandatory before Segaris is exposed through remote access, an untrusted network, or infrastructure outside household control.

The authentication endpoints are:

```text
POST   /api/session
GET    /api/session
DELETE /api/session
POST   /api/session/password
```

Administrative user creation, activation, deactivation, and credential recovery use endpoints under `/api/admin/users`. Segaris has no public registration or email-based password-recovery workflow initially.

Login failures use a generic response that does not reveal whether a username exists, an account is inactive, or a password was incorrect. Authentication events are logged without passwords, credential material, or other secrets.

### Password Policy And Lockout

ASP.NET Core Identity performs password hashing using its supported password hasher and upgrade behavior. Application code must not implement its own password hashing algorithm.

The initial policy requires a minimum length of 12 characters. It does not require arbitrary combinations of uppercase letters, lowercase letters, digits, and symbols. Administrators set a permanent, immediately usable password when creating an account or recovering credentials; users may change their own password but are not forced to replace it on first use. This was resolved in Wave 4 and is recorded in `docs/planning/BACKEND_IDENTITY_DECISIONS.md`.

Five failed authentication attempts trigger a 15-minute lockout. Lockout and failure responses remain generic to the client. Administrators may restore account access through the documented administrative workflow.

Administrator-initiated credential recovery resets the target account's password and invalidates its active sessions through security-stamp revalidation. The first administrator is bootstrapped idempotently from configuration or environment variables, and no credential is committed to the repository.

### Cross-Site Request Forgery

Cookie-authenticated state-changing requests require antiforgery validation in addition to `SameSite=Strict`.

- The SPA obtains an antiforgery token through a same-origin bootstrap or dedicated endpoint.
- The browser receives the antiforgery cookie required by the ASP.NET Core token pair.
- The SPA sends the request token through a documented custom header.
- The backend validates the token for cookie-authenticated `POST`, `PUT`, `PATCH`, and `DELETE` requests.
- Safe read operations must remain free of side effects and do not require antiforgery validation.

The production API does not initially enable cross-origin credentialed browser access. CORS remains disabled or restricted to explicitly configured development origins. CORS is not used as a substitute for authentication, authorization, or antiforgery protection.

### Data Protection Keys

ASP.NET Core Data Protection keys must persist outside the disposable backend container and be shared across successive deployments of the single backend instance. Losing or replacing the key ring would invalidate otherwise valid authentication cookies and other protected application tokens.

The production Compose topology mounts a dedicated persistent location for the key ring. The key directory is accessible only to the backend container user and is included in operational recovery planning. Key encryption at rest, rotation, and the exact host or named-volume path are finalized with secrets management and deployment provisioning.

### User API Keys

Trusted non-browser clients acting as a specific Segaris user authenticate with a user-bound API key. The scheme is implemented in the Identity module, which owns keys because a key is user-bound state and Identity already owns the user, its roles, and its security stamp.

An API key is presented through the `Authorization` header and never through the browser authentication cookie. It produces the same application user identity and enters the same authorization policies as an interactive session, so role and creator-only privacy rules remain centralized and unchanged.

API keys follow these constraints:

- Each key is bound to one active user and cannot grant permissions that user does not have.
- A complete secret is returned only once when the key is created.
- The database stores a cryptographic hash or verifier, never the usable secret.
- Stored metadata includes a name, creation time, optional expiration, last-use time, and revocation state.
- A key may have no automatic expiration, but it is always individually revocable.
- Key use does not require antiforgery validation because it does not rely on ambient browser cookies.
- Deactivating the user or revoking the key immediately prevents future authenticated use.
- Even an administrator's key cannot access another user's creator-only private records.

#### Implemented Scheme

- Keys live in `identity_api_keys`. The record stores the owning user, name, key identifier, secret verifier, the owner's security stamp at issue time, creation time, and the optional expiration, last-use, and revocation times.
- The token format is `segaris_<keyId>_<secret>`. Both segments are lowercase hexadecimal, because base64url contains the underscore that separates them. The `keyId` is the lookup index; the `secret` is 256 bits of cryptographic randomness.
- Only a SHA-256 digest of the secret is persisted. A password hasher is deliberately not used: it defends against dictionary attacks that do not apply to a uniformly random secret, and would add a key derivation to every authenticated request. This is the one place where application code selects a hash directly, and it does not contradict the password-hashing rule above, which governs user passwords.
- The scheme name is `Segaris.ApiKey`, presented as `Authorization: Bearer segaris_...`. A policy scheme selects the key handler when an `Authorization` header is present and the cookie handler otherwise, so cookie behaviour is unchanged.
- The handler builds its principal with the same claims factory the cookie sign-in uses, so `ICurrentUser`, `VisibilityPolicy`, and every module's authorization policy apply to a key with no module changes.
- Keys carry the owner's security stamp. Deactivation, password changes, and administrative credential recovery rotate that stamp and therefore invalidate the user's keys through the same central mechanism that invalidates their sessions.
- Antiforgery validation is bypassed for the key scheme only. The bypass is keyed on the scheme that authenticated the request, never on the endpoint, so cookie-authenticated writes keep antiforgery validation unconditionally.
- Expired, revoked, unknown, and malformed tokens all produce the same generic `401`, consistent with the login-failure position.
- Last use is recorded at five-minute granularity so a burst of agent calls does not turn every read into a write.
- Self-service management lives under `/api/session/profile/api-keys` (`POST` to create, `GET` to list, `DELETE` to revoke). Creation is self-service only: no user, including an administrator, can mint a key bound to another user.

Permission granularity below the user level, management screens, rotation, and rate limits remain outside this scope. A future per-key scope belongs in the key record rather than in the callers.

### Model Context Protocol

Segaris hosts its Model Context Protocol surface inside the same `Segaris.Api` process rather than running a separate service. The MCP host is platform infrastructure under `Platform/Mcp` and exposes Streamable HTTP at `/mcp` only when `Segaris:Mcp:Enabled` is true. The endpoint is disabled by default.

MCP clients authenticate with the same user-bound API key scheme described above. Browser cookies are not accepted for `/mcp`, CORS remains disabled, and antiforgery is not involved because API keys do not rely on ambient browser credentials. Once authenticated, MCP tools execute as the owning Segaris user and must reuse the same authorization, privacy, and module-boundary rules as REST endpoints.

The initial MCP surface contains only an identity probe tool used to validate endpoint mapping, API-key authentication, current-user resolution, and SDK transport behavior. Domain tools are added by module-owned contributors and must keep stable names, explicit descriptions, input schemas, read/write classification, idempotency classification, and focused tests.

## Background Jobs

Segaris provides a small shared background-job infrastructure inside the backend process. It is built with ASP.NET Core `BackgroundService` and persists job state in PostgreSQL.

The infrastructure supports multiple job types without knowing their domain behavior. Modules register their own typed job definitions and handlers while the shared platform owns queuing, claiming, status transitions, progress, cancellation requests, and diagnostic metadata.

Segaris does not initially require Hangfire, Quartz, a message broker, a separate worker container, or a distributed scheduler. The single backend instance processes jobs sequentially unless measured requirements later justify bounded concurrency.

### Job Lifecycle

An API use case creates a persistent job record in the `Queued` state and returns its identifier. A hosted worker claims queued jobs, creates a dependency-injection scope, resolves the registered handler, and executes it outside the initiating HTTP request.

The common lifecycle states are:

- `Queued`: accepted and waiting for execution.
- `Running`: claimed by the current backend process.
- `Succeeded`: completed and any result was published successfully.
- `Failed`: ended with a handled or unexpected failure.
- `CancellationRequested`: a user or system request has asked a cooperative handler to stop.
- `Cancelled`: stopped at a safe cancellation boundary.
- `Interrupted`: execution was lost because the process stopped or restarted before recording completion.

State transitions are validated centrally. Modules may expose a smaller user-facing vocabulary where some internal states do not need separate presentation.

### Persistent Job Record

The common job record contains at least:

- Job identifier and stable job-type code.
- Current lifecycle state.
- Creation, start, completion, and last-update timestamps.
- Initiating user when the job was user-triggered.
- Optional progress value and safe progress message code.
- Safe completion result metadata or a reference to a separately owned result.
- Stable failure code and correlation or trace identifier.
- Cancellation-request metadata.
- Minimal serialized parameters required by the handler.

Serialized parameters use a versioned, typed contract per job type. They must not contain passwords, usable API keys, session cookies, connection strings, complete private documents, or other secrets. Large input and output files live in module-owned staging or persistent storage and are referenced by controlled identifiers or relative paths.

The database record stores safe diagnostic summaries, not serialized exceptions or stack traces. Detailed failures remain in structured application logs.

### Job Handlers

Each job type has one handler registered by its owning module. A handler receives its typed parameters, a scoped service provider through normal dependency injection, a cancellation token, and a restricted mechanism for updating progress or safe result metadata.

Handlers must define:

- Whether the operation is idempotent.
- Whether and when cancellation is supported.
- Its concurrency or mutual-exclusion policy.
- Its cleanup behavior for partial output.
- Whether an interrupted job may be retried automatically, manually, or not at all.

The infrastructure does not automatically retry failed or interrupted jobs by default. A job type may opt into bounded retry only when duplicate execution and partial effects are understood and controlled.

### Claiming And Concurrency

PostgreSQL is the source of truth for job claiming. The worker uses an atomic database transition so a queued job cannot be claimed twice, even if the deployment topology changes later.

The initial worker executes one job at a time. Job types may additionally declare a named exclusivity key, allowing policies such as only one backup job being queued or active at once. A conflicting start request returns `409 Conflict` with the active job identifier where disclosure is authorized.

Concurrency may be increased later through a configured bounded limit, but only after job isolation, database load, filesystem access, and resource consumption have been measured. Unbounded task creation is prohibited.

### Cancellation And Shutdown

Cancellation is cooperative. A cancellation endpoint records the request and signals the active handler when it runs in the current process. Handlers check cancellation between safe steps and clean staging output before entering `Cancelled`.

A job may reject cancellation before execution or after a documented point of no return. Requesting cancellation does not imply that an external command or database operation can always stop immediately.

During graceful backend shutdown, the worker stops claiming new jobs and signals cancellation to the running handler. If the job does not complete within the configured shutdown window, or the process terminates unexpectedly, startup recovery marks the stale `Running` record as `Interrupted`.

Interrupted jobs are not assumed to have rolled back. Their handlers must use staging, atomic publication, transactions, or compensating cleanup so partially produced output is never presented as a successful result.

### Scheduling

The background-job infrastructure executes accepted work but does not initially provide calendar scheduling or recurring triggers. External household automation remains responsible for calling the administrative backup endpoint on its schedule.

If future functional requirements need recurring in-application rules, their scheduling semantics will be designed separately. They may enqueue jobs through this infrastructure, but a scheduler must not be added merely because the execution mechanism exists.

### Backup Integration

Backup generation is the first expected job type and follows the general lifecycle rather than implementing a separate queue.

- The backup module registers a backup job handler.
- Only one backup job may be queued or running at a time.
- Database dump, attachment collection, hashing, and manifest generation occur in staging.
- The completed package atomically replaces the previous latest package only after every required artifact succeeds.
- Failure, cancellation, or interruption removes or quarantines staging data and leaves the previous valid package untouched.
- The backup job exposes safe progress phases but never includes database credentials, attachment contents, or private filenames in its public status.

## Entity Framework Core Organization

Segaris uses one application `SegarisDbContext` for the modular monolith.

One context matches the single relational database and keeps startup migration, cross-module transactions, testing, and operational recovery understandable. Module boundaries are enforced through code ownership and published contracts rather than by creating several contexts over the same database or pretending modules are independently deployed services.

### Module Ownership

Each module owns its persisted entities, EF Core mappings, query implementations, and persistence-specific services. An indicative module layout is:

```text
Modules/
`-- Inventory/
    |-- Domain/
    |-- Application/
    |-- Endpoints/
    `-- Persistence/
        |-- ItemConfiguration.cs
        `-- InventoryQueries.cs
```

Entity mappings use focused `IEntityTypeConfiguration<TEntity>` classes located with their owning module. `SegarisDbContext.OnModelCreating` applies the registered module configurations through explicit registration or assembly scanning with a controlled convention.

The context should not expose a growing public `DbSet` property for every entity as the primary navigation API. Module-internal persistence code may use `Set<TEntity>()` through the scoped context. This reduces accidental discovery of other modules' entities but is not considered a security boundary.

One module must not query, attach, update, or include another module's internal entity types. Cross-module reads and operations use the published application contracts described by the module-boundary rules. Architecture tests should verify namespace and assembly dependency rules where they can be enforced automatically.

### Table Naming

Database tables use stable lowercase `snake_case` names prefixed by their owning module or platform area, for example:

```text
identity_users
inventory_items
travel_trips
platform_background_jobs
```

Segaris does not use PostgreSQL schemas to represent module boundaries because SQLite does not support schemas and must share the same conceptual model. Prefixes prevent common names from colliding and keep ownership visible in both providers.

Column, key, index, constraint, and join-table names follow one documented `snake_case` convention. Database object names must be explicit or produced through one tested naming convention so migrations do not churn when C# implementation names are refactored.

### Context Lifetime And Transactions

`SegarisDbContext` uses the normal scoped lifetime. One application use case normally performs one coordinated `SaveChangesAsync` call after validation and domain behavior complete.

The context is not shared across parallel tasks and must not escape its dependency-injection scope. Background job handlers create their own scope and context rather than retaining one from the request that queued the job.

EF Core's implicit transaction for one `SaveChanges` operation is sufficient for normal use cases. Explicit transactions are used only when a use case requires multiple saves or coordinates persistence steps that must commit atomically. A cross-module use case may use the common transaction, but this does not grant general access to the participating modules' internal entities.

Transactions cannot include the attachment filesystem or external services. Those operations use staging and compensating behavior as defined by their owning architecture.

### Query Conventions

- Read-only queries use `AsNoTracking` by default.
- Collection and detail queries project directly to explicit response or application read models when practical.
- Queries avoid loading full aggregate graphs when only a subset of fields is required.
- Lazy-loading proxies are not enabled.
- Change-tracking proxies and dynamic proxy requirements are not part of the initial architecture.
- Cancellation tokens flow from HTTP requests or background jobs into asynchronous EF Core operations.
- Module-specific repositories are added only when they express a meaningful domain collection or isolate non-trivial persistence behavior.
- Provider-specific SQL or EF functions remain isolated within the persistence boundary and require PostgreSQL integration coverage.

### Provider-Specific Migrations

PostgreSQL and SQLite use separate migration assemblies generated from the same `SegarisDbContext` model:

```text
Segaris.Migrations.Postgres
Segaris.Migrations.Sqlite
```

The active provider configuration selects the matching migrations assembly. A design-time factory or equivalent tooling configuration creates the context for each provider without requiring production secrets or starting the complete web application.

Each logical schema change has a corresponding migration in both providers. Paired migrations use correlated names and describe the same application intent even when their generated operations differ. A provider-specific no-op or manual operation requires an explanatory migration comment or deployment note.

PostgreSQL remains the production reference. SQLite exists for local convenience and must not constrain production correctness, but its migration history must remain usable for supported local development databases.

Automated verification covers:

- Applying the complete migration history to an empty PostgreSQL database.
- Applying the complete migration history to an empty SQLite database.
- Upgrading the previous supported PostgreSQL schema to the current schema.
- Upgrading the previous supported SQLite schema when local database preservation is supported for that release.
- Comparing the resulting EF Core model intent and detecting missing provider migration pairs.
- Exercising provider-sensitive queries and decimal, date, constraint, and index behavior against PostgreSQL.

The backend applies only the migration set that matches its configured provider. It never attempts to infer provider compatibility by executing migrations generated for the other provider.

## Dependency Direction

The ASP.NET Core host composes modules and infrastructure. Domain behavior does not depend on HTTP, container hosting, logging sinks, or concrete external-service clients.

Infrastructure concerns implement interfaces owned by the module or application behavior that consumes them. Dependency injection is used for explicit runtime substitution and testing, not as a reason to add interfaces for every class.

## Explicit Non-Goals

The initial backend does not require:

- Microservices or independently deployed domain services.
- A distributed message broker.
- Event sourcing or CQRS infrastructure.
- A mediator library for every request.
- Generic repositories over Entity Framework Core.
- A separate project for every module or architectural layer.
- A plugin system that loads unknown modules dynamically.

## Open Decisions

- Define the detailed domain ownership and dependency map between modules.
- Confirm the shared core model and which concepts have a platform-level owner.
