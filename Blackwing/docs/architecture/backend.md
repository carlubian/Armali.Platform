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
per-provider migration projects: migrations will live inside
`Blackwing.Persistence`.

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
