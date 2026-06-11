# Data And Storage

This document records the Phase 1 decisions for relational persistence, database providers, schema migrations, attachments, backups, shared data conventions, search, and audit metadata.

## Persistence Technology

The backend will use Entity Framework Core as its primary relational persistence abstraction.

The expected providers are:

- SQLite for local development, where minimal setup and fast project startup are valuable.
- PostgreSQL for production, running as a separate container with persistent storage.

The active provider and connection string will be selected through runtime configuration, normally environment variables. Changing environments should not require changes to application or domain code.

## Provider Portability

Entity Framework Core reduces direct coupling to a database provider, but it does not make SQLite and PostgreSQL behavior identical.

Relevant differences include:

- Supported column types, precision, collations, and case sensitivity.
- Date, time, decimal, JSON, and generated-value behavior.
- Constraint and index capabilities.
- Transaction, locking, concurrency, and schema-alteration behavior.
- SQL generated for advanced queries or provider-specific functions.

Application code should prefer provider-neutral EF Core features unless a provider-specific capability has a documented benefit. Provider-specific behavior must remain behind the persistence boundary rather than leaking into domain logic.

PostgreSQL is the production source of truth. Automated integration tests must run against PostgreSQL even when most local development uses SQLite. SQLite passing locally is not sufficient evidence that a migration or query is production-compatible.

## Configuration

The backend configuration will identify at least:

- Database provider, such as `Sqlite` or `Postgres`.
- Provider-specific connection string.

Secrets and production connection strings must be injected at runtime and must not be stored in source control or container images.

## Migrations

Schema changes will be managed with Entity Framework Core migrations. One application `SegarisDbContext` composes mappings owned by the individual backend modules.

Because EF Core migrations can contain provider-specific metadata or SQL, PostgreSQL and SQLite use separate migration assemblies generated from the same model. Every logical schema change has a corresponding migration in both provider histories, with PostgreSQL remaining the production reference. See `docs/architecture/backend.md` for context ownership, naming, query, transaction, and migration-pair conventions.

The execution policy is:

- Local development: the backend applies pending migrations automatically at startup.
- Automated tests: test setup may create or migrate disposable databases explicitly.
- Production: the backend applies pending migrations automatically during startup, before opening the HTTP server or reporting itself healthy.

This policy is appropriate for the initial deployment because only one backend instance may run at a time. The deployment process is responsible for stopping or replacing the previous instance without overlapping two migration-capable backend processes.

Production startup migration must follow these safeguards:

- Exactly one process is responsible for applying migrations.
- The API does not accept traffic until migration succeeds.
- Migration failure stops the backend instead of continuing with an unknown schema.
- Migration logs are retained and visible to the operator.
- Health checks report failure until migration and normal application startup complete.
- A current database backup exists before any migration classified as risky or destructive.
- Every risky or destructive migration includes a deployment note describing compatibility and recovery.

Automatic migration is a deployment mechanism, not a rollback strategy. EF Core schema rollback may lose data and must not be treated as the default recovery path.

## Migration Compatibility

Normal releases should prefer additive, forward-compatible schema changes so that application and schema transitions remain predictable. Examples include adding nullable columns, adding new tables, or creating indexes whose construction is acceptable during startup.

Potentially destructive or long-running changes include dropping or renaming columns, narrowing types, rewriting large tables, changing required constraints, and transforming existing data. These changes may still be initiated automatically by the backend, but the release must first ensure:

- A verified backup or snapshot is available.
- The expected migration duration and startup downtime are understood.
- Existing data has been validated or transformed safely.
- Restoring the previous application version will not require an older incompatible schema.

When a release cannot satisfy those conditions, the schema change must be split across compatible releases instead of relying on a single destructive migration.

## Development Fidelity

SQLite is optimized for developer convenience, not exact production parity. Development workflows must therefore include an easy way to run PostgreSQL locally, most likely through Docker Compose, when working on:

- Migrations and schema changes.
- Complex queries.
- Concurrency behavior.
- Indexes, constraints, JSON, full-text search, or provider-specific types.
- Performance-sensitive data access.

## File And Attachment Storage

User-uploaded files will be stored on the local server filesystem. The backend container will access this storage through a persistent Docker volume. Files must never depend on the writable layer of the container image.

In production, this storage is a bind mount rooted at `${Segaris_DATA_PATH}/attachments`, where `Segaris_DATA_PATH` defaults to `/data/volumes/Segaris`. See `docs/architecture/deployment.md` for the host layout and access boundaries.

The initial directory layout is organized by owning module, followed by a UUID-based physical filename:

```text
/{module}/{file-uuid}.{extension}
```

The physical path must not contain the original filename or reproduce the full entity hierarchy. PostgreSQL stores the attachment metadata and relationship to its owning entity, including at least:

- Original filename.
- Physical UUID and relative storage path.
- Media type.
- File size.
- Owning module and entity reference.
- Creator and creation timestamp.

The UUID is generated before the file is written and is independent from the integer database identifier used by the attachment record.

Attachments are deleted immediately when the attachment itself or its owning entity is permanently deleted. Both the database record and physical file must be removed. PostgreSQL and the filesystem cannot participate in one atomic transaction, so the persistence layer must use compensating operations:

- A failed filesystem operation prevents the database operation from being treated as successfully completed.
- Partial failures are logged with enough information for diagnosis and recovery.
- A maintenance operation should be able to detect missing files and unreferenced physical files.

External backup retention may preserve an older copy after deletion, but the live Segaris storage does not provide a recycle bin or indefinite attachment retention.

## Backup Package Generation

Segaris will expose an administrative API for generating a restorable package of the current application data. A separate household infrastructure service will call this API during a period in which no user activity is expected and will copy the completed package to a data lake or equivalent backup destination.

The package contains:

- A restorable PostgreSQL dump.
- All live attachment files.
- A manifest containing the creation time, Segaris application version, database schema version, and file hashes.

The package does not contain secrets or general operational configuration.

Backup generation uses the shared persistent background-job infrastructure described in `docs/architecture/backend.md`:

- The start endpoint returns a job identifier.
- Job state and failure details can be queried.
- Only one backup job may run at a time.
- A concurrent start request is rejected with a conflict response that identifies the active job.
- Job lifecycle and safe progress metadata persist in PostgreSQL and survive normal API requests and frontend disconnection.
- A backend restart marks an unfinished running backup as interrupted; it is not automatically treated as successful or resumed without an explicit safe retry policy.
- Only the latest completed package is retained in the configured output location.

The job writes to a temporary staging location and replaces the previous completed package only after every required artifact and the manifest have been produced successfully. A failed job must not expose a partial package as the latest valid backup.

The application remains available while the job runs. A PostgreSQL dump can provide a consistent database snapshot, but PostgreSQL and the attachment filesystem do not share a global snapshot. The initial operational guarantee therefore relies on running the job during an agreed period without application writes. The job should record or detect overlapping writes where practical so the operator can reject a suspect package rather than assuming cross-storage atomicity.

Segaris's operational responsibility ends after exposing the administrative API and producing the latest valid package. An external household service is responsible for:

- Calling the API on its own schedule.
- Waiting for generation to complete and retrieving or copying the resulting package.
- Uploading it to external storage.
- Applying encryption when required.
- Managing retention, versioning, expiration, and deletion of stored copies.
- Monitoring transfer and storage failures.

Segaris does not contain a backup scheduler, external-storage credentials, retention configuration, or lifecycle-management logic. These concerns must not be added to the application unless the architecture decision is explicitly revisited.

The latest package and its temporary staging data live under the production bind mount `${Segaris_DATA_PATH}/backups`, defaulting to `/data/volumes/Segaris/backups`. The exact filenames and staging subdirectories will be defined during implementation planning.

## Shared Data Conventions

Unless a module documents a justified exception, persisted entities follow these conventions:

- Primary and foreign keys use auto-incrementing integer identifiers.
- Physical attachment names use UUIDs and do not change the database identifier convention.
- Technical instants are stored in UTC. PostgreSQL uses `timestamp with time zone`; the application converts values for display in `Europe/Madrid`.
- Civil dates without a time or instant, such as birthdays, use a date-only type.
- Monetary amounts use fixed-precision decimal values together with an ISO 4217 currency code.
- PostgreSQL `numeric` is the production representation for monetary values. Queries and calculations involving decimals must be tested against both PostgreSQL and SQLite because SQLite does not provide identical decimal semantics.
- Concurrent updates use last-write-wins behavior. General-purpose concurrency tokens are not required for the initial system.
- Physical deletion is the default. A module must define whether deletion is rejected, cascaded, or disassociated when external references make the default unsafe.

Shared creation and modification metadata consists of:

- `CreatedAt` and `CreatedBy`.
- `UpdatedAt` and `UpdatedBy`.

There is no general soft-deletion convention, historical audit table, entity revision history, or undo mechanism. Once an operation is confirmed and completed, it is irreversible in the live application.

## Search Strategy

The initial application has no global search index and no dedicated search service. Search is owned by each domain module and implemented through its relational queries.

The common baseline is:

- Exact matching for enumerations and classification fields, such as category or status.
- Partial matching for the primary entity name where useful.
- Module-specific selection of searchable fields and available filters.

Full-text ranking, stemming, fuzzy matching, and cross-module search are outside the initial architecture. A module may propose additional search behavior later when its functional requirements justify it.

## Audit And History

General audit needs are limited to the creation and last-modification metadata stored on each entity. The application does not maintain a general audit-event table or retain previous field values.

Modules with legal, financial, or operational requirements beyond this baseline must define them explicitly during functional planning rather than inheriting an unbounded global history mechanism.

## Open Decisions

- Choose the exact environment-variable names and configuration hierarchy.
- Define the concrete container UID/GID and provisioning procedure for attachment and backup directories.
- Define the backup administrative API authorization details.
- Define package restoration and restore verification.
- Define user-facing data import, export, and portability requirements.
- Define development seed data and database reset workflows.
- Define attachment size limits, permitted file types, content validation, and malware-scanning requirements.
