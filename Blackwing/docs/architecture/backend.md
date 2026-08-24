# Backend architecture

The backend is a .NET 10 ASP.NET Core application built with Minimal APIs. It
lives in `src/backend` and is described by `Blackwing.slnx` (the `.slnx` format,
not `.sln`).

## Layers and the dependency rule

| Project | Responsibility |
| --- | --- |
| `Blackwing.Api` | HTTP surface, composition root, configuration, observability, health |
| `Blackwing.Persistence` | `BlackwingDbContext`, its DI registration, and later the entities and migrations |
| `Blackwing.Shared` | Types and contracts shared by the other two |

The dependency direction is fixed:

```
Api -> Persistence -> Shared
```

Never the other way round, and `Shared` references nothing. This is not a
convention to be remembered: `Blackwing.ArchitectureTests` asserts it by
reflection over the built assemblies, and also asserts that no public type of
`Blackwing.Persistence` exposes a type from `Blackwing.Api`. Inverting a
reference breaks the build in CI.

Blackwing is deliberately **not** a modular monolith. Segaris carries a module
composition layer (`ISegarisModule`, `SegarisModules`) because it hosts twenty
domains; Blackwing's surface is small enough that the machinery would cost more
than it saves.

PostgreSQL is the only database provider, in every environment. There are no
per-provider migration projects: migrations live inside `Blackwing.Persistence`.

There is one seam, and it is not a module system: `Blackwing.Persistence` must
never reference `Microsoft.AspNetCore.Identity`. It assembles its model from
`IBlackwingModelContributor` implementations instead, so the identity module can
be pulled out whole the day an Armali sign-on service arrives. An architecture
test asserts it. See `identity.md`.

## Identity and the ownership perimeter

Authentication, roles and — more importantly — the guarantee that an account's
content never leaves it are documented in full in
[`identity.md`](identity.md). Two facts belong here because they constrain
everything else the backend does:

- Every entity of user content implements `IOwnedByUser`, and
  `BlackwingDbContext` applies a **global query filter** plus **owner stamping on
  insert**. Endpoints do not write owner predicates, so forgetting one is not a
  failure mode.
- Reading another account's resource answers **404, never 403**. A 403 would
  confirm that the identifier exists.

Adding a content entity without `IOwnedByUser` is a security defect, not a style
slip, and `Blackwing.ArchitectureTests` fails on it.

## Migrations

Migrations live in `Blackwing.Persistence/Migrations` — the `DbContext`
assembly, which is the default — but they are generated with **`Blackwing.Api`
as the startup project**, because that is the only place where the identity model
contributor is registered:

```
dotnet ef migrations add <Name> --project src/backend/Blackwing.Persistence --startup-project src/backend/Blackwing.Api --output-dir Migrations
```

`scripts/backend-migrations-add.ps1 -Name <Name>` wraps exactly that command.

`Microsoft.EntityFrameworkCore.Design` is referenced by **both** projects, with
`PrivateAssets=all`: `dotnet ef` refuses to run when the startup project does not
reference it, and the private assets keep design-time tooling from leaking into
anything that references `Blackwing.Api`.

There is **no `IDesignTimeDbContextFactory`** on purpose. One would force
`Blackwing.Persistence` to know about the identity module and would break the
very isolation described above. The cost is remembering the startup project,
which the script removes.

Migrations are applied **automatically at startup**
(`MigrateBlackwingDatabaseAsync`, right after `app.Build()` and before the
pipeline), followed by the idempotent identity seed. A fresh deployment against
an empty database is operable with no manual step, and a restart changes nothing.
Startup fails loudly, with a `LogCritical`, if migration fails: a backend serving
requests against a half-migrated schema is worse than one that will not start.

## Typed configuration and startup validation

Configuration follows the options pattern, one options type and one validator
per area, under `Blackwing.Api/Configuration`:

- `DatabaseOptions` — the connection string is mandatory; the provider is fixed
  to PostgreSQL.
- `StorageOptions` — `ImagesPath` (the root of the image volume) and
  `DataProtectionKeysPath`.
- `ObservabilityOptions` — the `Seq` subsection: `Enabled`, `ServerUrl`,
  `ApiKey`, `MinimumLevel`.

Everything is registered through `AddBlackwingConfiguration(...)` and validated
with `ValidateOnStart`. **An invalid configuration fails at startup, not on the
first request.** No secret option has a default value in code.

The configuration section is `Blackwing` in `appsettings`, which maps to
`Blackwing__…` environment variables; Compose supplies them that way.

## Secrets

Nothing secret is committed. `src/backend/appsettings.json` is git-ignored and
only exists locally; the shape of the file is documented by the versioned
`src/backend/appsettings.example.json`. In containers every value arrives as an
environment variable, and in production Portainer supplies them as stack
variables.

## Observability

Serilog, configured by `AddBlackwingLogging`:

- Structured console output, always.
- A Seq sink only when `Blackwing:Observability:Seq:Enabled` is `true`, so log
  delivery is a deployment decision and never a hard dependency.
- `RequestCorrelationMiddleware` establishes a correlation identifier per
  request, and `UseSerilogRequestLogging` enriches the request summary with
  `TraceId`.

## Health endpoints

Two endpoints, whose names are contractual — the container `HEALTHCHECK`, the
Compose health checks and the ingress all reference them:

- `GET /health/live` — the process answers. No external dependencies, so a
  database outage never restarts the container.
- `GET /health/ready` — PostgreSQL is reachable and the images directory is
  writable. Implemented as health checks tagged `ready` and filtered by that
  tag.

`GET /` redirects to `/health/live`.

The distinction matters operationally: with PostgreSQL stopped, `/health/live`
still returns 200 while `/health/ready` returns 503, so orchestration holds
traffic back without killing the process.

## API surface conventions

Product endpoints are mapped under `/api`. The health endpoints sit outside that
prefix, and the ingress strips `/api` for them specifically (see
`deployment.md`), so the stack can be probed from the published port.

OpenAPI is served in Development and Testing; the Scalar reference UI only in
Development.

## Tests

Three projects under `tests/backend`:

- `Blackwing.UnitTests` — fast, in-process. Currently covers the options
  validators.
- `Blackwing.Api.IntegrationTests` — `WebApplicationFactory` over the real
  application, against a real PostgreSQL started with Testcontainers. Requires
  Docker.
- `Blackwing.ArchitectureTests` — reflection over the built assemblies, no extra
  libraries.

Do not run the integration suite in full locally; CI runs it sharded. See
`AGENTS.md`.
