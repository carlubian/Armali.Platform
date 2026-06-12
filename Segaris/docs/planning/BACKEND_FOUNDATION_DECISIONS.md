# Backend Foundation Decisions

## Status

This record closes Wave 0 of the backend implementation plan. It defines the conventions that Wave 1 and Wave 2 must use when scaffolding the backend.

## Runtime

- Target framework: `net10.0`.
- Supported runtime: .NET 10 LTS and ASP.NET Core 10.
- SDK selection: the repository-root `global.json` starts at `10.0.100`, permits later .NET 10 feature bands and patches through `latestFeature`, and rejects prerelease SDKs.
- Servicing policy: developer machines, CI, and container images use a currently supported .NET 10 patch. The repository does not pin applications to an obsolete patch merely because it was the original baseline.
- Upgrade policy: changing the target major version is an explicit architecture decision. Routine supported patches do not require an architecture decision.

The LTS line is preferred because Segaris is a small, long-lived household system and benefits more from a long support window than from annual framework upgrades.

## Solution And Project Names

The backend solution lives under `src/backend/Segaris.slnx`. Production assemblies and root namespaces use the `Segaris` prefix.

Initial production projects:

| Project | Assembly and root namespace | Responsibility |
| --- | --- | --- |
| `src/backend/Segaris.Api/Segaris.Api.csproj` | `Segaris.Api` | Executable ASP.NET Core composition root, modules, and platform implementations. |
| `src/backend/Segaris.Shared/Segaris.Shared.csproj` | `Segaris.Shared` | Deliberately small shared primitives and published contracts. |
| `src/backend/Segaris.Persistence/Segaris.Persistence.csproj` | `Segaris.Persistence` | Provider-neutral EF Core context, model-contributor contract, provider configuration, and persistence conventions. Added in Wave 2 to prevent circular dependencies between the API and migration assemblies. |
| `src/backend/Segaris.Migrations.Postgres/Segaris.Migrations.Postgres.csproj` | `Segaris.Migrations.Postgres` | PostgreSQL migration history. Added in Wave 2. |
| `src/backend/Segaris.Migrations.Sqlite/Segaris.Migrations.Sqlite.csproj` | `Segaris.Migrations.Sqlite` | SQLite migration history. Added in Wave 2. |

Initial test projects live under `tests/backend/`:

| Project | Responsibility |
| --- | --- |
| `Segaris.UnitTests` | Fast domain, application, validation, and policy tests. |
| `Segaris.ArchitectureTests` | Dependency direction, module boundaries, and shared-core governance. |
| `Segaris.Api.IntegrationTests` | HTTP application boundary through `WebApplicationFactory`. |
| `Segaris.Postgres.IntegrationTests` | Production-sensitive persistence behavior through Testcontainers. |
| `Segaris.Migrations.IntegrationTests` | Fresh creation, supported upgrades, and provider migration pairing. |

Business modules remain namespaces and folders inside `Segaris.Api` until a demonstrated build or dependency-boundary need justifies another assembly. A project is not created for every module or architectural layer.

## Backend Configuration Contract

### Sources And Precedence

The backend uses normal ASP.NET Core configuration. From lowest to highest precedence:

1. Framework defaults.
2. Optional `appsettings.json`.
3. Optional `appsettings.{Environment}.json`.
4. .NET user secrets in `Development` only.
5. Environment variables.
6. Command-line arguments.

`src/backend/appsettings.example.json` is the tracked catalogue of supported settings. Real `appsettings.json` and environment-specific local files are untracked. Production may mount a generated `appsettings.json`, but environment variables override it and are preferred for secrets.

Environment variables map nested keys with double underscores. Project-owned keys use the uppercase `SEGARIS__` prefix; the connection string uses the ASP.NET Core convention `CONNECTIONSTRINGS__SEGARIS`. Examples:

```text
SEGARIS__DATABASE__PROVIDER=Postgres
CONNECTIONSTRINGS__SEGARIS=Host=postgres;Database=segaris;Username=segaris;Password=...
SEGARIS__OBSERVABILITY__SEQ__APIKEY=...
```

Command-line overrides are intended for development and controlled operational commands, not for placing production secrets in process listings or shell history.

### Supported Schema

The following is the complete known backend configuration surface for the foundation. Each section is bound to typed options and validated at startup when its capability is enabled.

| Key | Required when | Purpose |
| --- | --- | --- |
| `Segaris:Database:Provider` | Always | `Sqlite` or `Postgres`; defaults to `Sqlite` only in `Development`. |
| `ConnectionStrings:Segaris` | Always | Active provider connection string. |
| `Segaris:Storage:AttachmentsPath` | Attachments are enabled | Persistent attachment root. |
| `Segaris:Storage:BackupsPath` | Backup generation is enabled | Persistent latest-package and staging root. |
| `Segaris:Storage:DataProtectionKeysPath` | Required in Production; optional in Development | Persistent ASP.NET Core Data Protection key ring. When set, keys are written there; in Production startup fails if it is missing. |
| `Segaris:Identity:Bootstrap:UserName` | Optional | First-administrator user name applied idempotently at startup when no matching account exists. |
| `Segaris:Identity:Bootstrap:Password` | Bootstrap user name is set | First-administrator password; a secret, supplied only through user secrets or environment/mounted configuration. |
| `Segaris:Jobs:WorkerEnabled` | Optional | Enables the persistent background worker; defaults to `true`. |
| `Segaris:Jobs:PollIntervalSeconds` | Worker enabled | Bounded delay between claim attempts. |
| `Segaris:Jobs:ShutdownGracePeriodSeconds` | Worker enabled | Bounded graceful-shutdown allowance. |
| `Segaris:Backups:PostgresDumpExecutable` | PostgreSQL backup enabled | Path or executable name for `pg_dump`. |
| `Segaris:Observability:Seq:Enabled` | Optional | Enables best-effort Seq delivery; defaults to `false`. |
| `Segaris:Observability:Seq:ServerUrl` | Seq enabled | Seq endpoint. |
| `Segaris:Observability:Seq:ApiKey` | Optional | Seq API key; secret source only outside disposable development. |
| `Segaris:Observability:Seq:MinimumLevel` | Seq enabled | Minimum level sent to Seq. |
| `Segaris:Diagnostics:MaxBodyBytes` | Diagnostics endpoint enabled | Maximum accepted diagnostic payload. |
| `Segaris:Diagnostics:PermitLimit` | Diagnostics endpoint enabled | Requests allowed in one fixed window. |
| `Segaris:Diagnostics:WindowSeconds` | Diagnostics endpoint enabled | Fixed rate-limit window. |

Standard ASP.NET Core keys remain supported where the framework owns the behavior, including `ASPNETCORE_ENVIRONMENT`, `ASPNETCORE_URLS`, `AllowedHosts`, and `Logging`. Project code must not create aliases for them.

Settings introduced by later capabilities must follow this hierarchy, update `appsettings.example.json` and this record in the same change, and include typed validation tests. Unknown configuration keys are tolerated because framework and logging providers add their own keys; unknown properties inside a bound `Segaris` options section should be rejected where practical.

### Secret Rules

- Passwords, connection-string credentials, Seq API keys, and future provider credentials never receive usable values in tracked files.
- Secrets use user secrets for local development or environment/mounted production configuration.
- Secret values must not be accepted through frontend configuration, normal logs, API responses, job parameters, or image build arguments.
- The first-administrator bootstrap was resolved in Wave 4: `Segaris:Identity:Bootstrap:UserName` and `:Password` create the first administrator idempotently at startup. The password is a secret supplied only through user secrets or environment/mounted configuration, never committed. See `docs/planning/BACKEND_IDENTITY_DECISIONS.md`.

## Development Database Reset And Seed

Database reset is an explicit developer operation. The API never drops or recreates a database during normal startup.

- `scripts/backend-reset.ps1` will wrap a command mode exposed by `Segaris.Api` and require an explicit confirmation flag.
- The command refuses to run unless the ASP.NET Core environment is `Development`.
- SQLite reset deletes only the configured database file after resolving and displaying its absolute path, then applies migrations.
- PostgreSQL reset is opt-in and accepts only a configured local/Testcontainers database. It drops and recreates that database, then applies migrations. Production-like hosts and databases are rejected unless future operational tooling defines a separate protected path.
- `database seed` is independently runnable and idempotent. `database reset` invokes it after migrations unless `--no-seed` is supplied.
- Foundation seed data is deterministic and limited to platform-owned records required for operation, such as Identity roles after Wave 4. Business demonstration data belongs to module-specific development seeders and is not inserted by default.
- Development users and credentials are not defined until Wave 4 resolves administrator bootstrap and temporary-password behavior. No fixed password is committed to the repository.

Automated tests own their data and continue to create disposable databases; they do not call the developer reset script.

Wave 2 implements these commands as `scripts/backend-reset.ps1 -Confirm [-NoSeed]` and `scripts/backend-seed.ps1`. The seed pipeline is intentionally empty until a platform capability, beginning with Identity roles in Wave 4, owns deterministic foundation data.

## Deferred Decisions Preserved

Wave 0 does not silently settle later security or operational choices. Production secret injection and rotation, administrator bootstrap and temporary credentials, attachment validation limits, backup authorization, restore rehearsal, container UID/GID, and exact CI checks remain visible in the implementation plan and roadmap until their owning wave resolves them.
