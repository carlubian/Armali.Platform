# Backend Deployment And Operations Decisions

## Purpose

This document records the Wave 8 decisions for containerization, Compose
topology, ingress, persistence layout, and operational procedures. It resolves
the deployment-related items the implementation plan flagged as "required by
Wave 8" and complements `docs/architecture/deployment.md` with the concrete
choices made during implementation.

## Server Baseline

- **Operating system:** Ubuntu 24.04 LTS.
- **Hardware baseline:** x64 CPU with 6–8 cores, at least 8 GB RAM, and an SSD
  of at least 256 GB. This is comfortably sized for a single-household modular
  monolith with PostgreSQL, Caddy, the backend, and the frontend on one host.
- The server keeps a stable LAN address (static address or DHCP reservation) and
  a fixed published port so clients and any optional UniFi DNS record remain
  valid across deployments.

## Container Identity And Image

- **Non-root identity:** the backend container runs as a dedicated user and group
  with **UID:GID 5525:5525**. The value is deliberately high (no collision with
  default system users) and matches the default published HTTP port for
  memorability. Host directories bind-mounted into the backend must be owned by
  this identity.
- **Backend image:** a multi-stage build (`mcr.microsoft.com/dotnet/sdk:10.0`
  build stage, `mcr.microsoft.com/dotnet/aspnet:10.0` runtime stage),
  framework-dependent, listening on container port 8080. The image bundles the
  **PostgreSQL 17 client** (`pg_dump`) from the official PGDG repository so the
  administrative backup job can run; `curl` is included for the container health
  check.
- **PostgreSQL major version:** pinned to **17** for both the server image and the
  bundled client. Changing the major version is an explicit decision that must
  update both the `postgres:` image tag and the `postgresql-client-17` package in
  the backend Dockerfile together.
- The build context is the repository root because the projects depend on the
  repository-level `global.json`, `Directory.Build.props`, and
  `Directory.Packages.props` (central package management). A root `.dockerignore`
  keeps the context small and prevents local configuration or secrets from
  entering image layers.
- **Configuration in the image:** none. No `appsettings.json`, secret, or build
  argument carrying a secret is baked in. All runtime configuration arrives as
  environment variables (Portainer) following the `SEGARIS__` / `CONNECTIONSTRINGS__`
  hierarchy.

## Ingress (Caddy)

- Caddy is the only household-facing container. Routing is **baked into a
  dedicated `segaris-caddy` image** as config-as-code (the `Caddyfile`), so the
  production stack is self-contained and Portainer-deployable without host file
  bind mounts. The Caddyfile contains no secrets.
- Routing: `/api/*` is proxied to `backend:8080`; everything else is proxied to
  `frontend:80`. Health endpoints (`/health/live`, `/health/ready`) stay internal
  to the Compose network and are not exposed through Caddy.
- Caddy serves plain HTTP (`auto_https off`) on a trusted local network and its
  admin API is disabled. The published host port is the only runtime input,
  supplied through the Compose port mapping (`SEGARIS_HTTP_PORT`, default 5525).

## Frontend Placeholder

- A temporary `segaris-frontend-placeholder` image (a minimal static page served
  by Caddy) exists only to validate ingress routing and the smoke test before the
  real frontend exists. It is outside product scope. The Compose service is named
  `frontend`, so swapping in the real frontend image later requires no other
  change to the topology.

## Compose Topology

Three Compose files under `deploy/compose/`, all preserving the same service
names and network so local and production behavior match:

- **`docker-compose.yml`** — the production-oriented base. References images by
  tag (`SEGARIS_*_IMAGE`), never builds, and is the file Portainer deploys.
  Services: `postgres`, `backend`, `frontend`, `caddy`. Dependents are gated on
  health (`backend` waits for `postgres` healthy; `caddy` waits for `backend`
  healthy). The backend health check uses `/health/ready`.
- **`docker-compose.local.yml`** — a local override that adds `build:` sections so
  the images are built from source instead of pulled. Production never uses it.
- **`docker-compose.infra.yml`** — an infrastructure-only stack (PostgreSQL
  published to localhost, optional Seq behind the `seq` profile) for running the
  backend and tests natively.

`deploy/compose/.env.example` is the tracked catalogue of Compose variables; the
real `.env` is untracked.

## Persistence Layout

Production persistence uses the existing server convention under
`/data/volumes/<application>`. Segaris uses `/data/volumes/segaris`
(`SEGARIS_DATA_PATH`), with three bind-mounted subdirectories owned by
`5525:5525`:

```text
/data/volumes/segaris/
|-- attachments/            live user-uploaded files
|-- backups/                latest backup package and staging
`-- dataprotection-keys/    ASP.NET Core Data Protection key ring
```

The `dataprotection-keys/` directory is added in this wave so authentication
sessions survive backend container replacement. PostgreSQL data uses a
Docker-managed **named volume** (`postgres-data`), not a bind mount; the database
is never recovered by copying its files, only through the dump in the backup
package.

## Secret Injection And Rotation

- **Injection:** production secrets — the PostgreSQL password, the optional
  first-administrator bootstrap credentials, and the optional Seq API key — are
  supplied as **Portainer stack environment variables**. They are never committed,
  never placed in image layers, and never passed as command-line arguments.
- **Rotation:** secrets are rotated by updating the Portainer stack variables and
  redeploying. Rotating the PostgreSQL password also requires updating the role
  password in the database. The first-administrator bootstrap variables are set
  only for the initial deployment and then cleared, because the bootstrap is
  idempotent and acts only when no matching account exists.

## Restore And Recovery

- **Ownership:** the household administrator owns recovery. Segaris produces the
  latest valid backup package; an external service owns scheduling, off-server
  transfer, encryption, and retention.
- **Procedure:** `scripts/restore.sh` restores a `segaris-backup.tar` package into
  the running stack — it stops the backend and ingress, restores the database with
  `pg_restore --clean --if-exists`, mirrors the attachments tree, and restarts the
  services. It is destructive and requires an explicit `--confirm` flag. The full
  procedure is documented in `docs/operations/backup-and-restore.md`.
- **Verification frequency:** the restore procedure is rehearsed against a
  disposable target at least **quarterly** and after any change to the backup
  package format, persistence layout, or PostgreSQL major version.
- **Forward-only migrations:** database migrations are not automatically reversed.
  Rollback restores a known-good package and a compatible image rather than
  reverting a migration. See `docs/operations/rollback.md`.

## Smoke Test

`scripts/compose-smoke-test.sh` builds the full stack locally in an isolated
Compose project with ephemeral storage, waits for backend readiness, verifies
that Caddy serves the frontend at `/` and routes `/api/` to the backend, and
tears everything down. It is the Wave 8 acceptance gate for the runtime and is
intended to also run in CI in Wave 9.

## Naming Consistency Fix

Earlier deployment notes used mixed-case `Segaris_DATA_PATH` / `Segaris_HTTP_PORT`
and the path `/data/volumes/Segaris`. This wave standardizes on the uppercase
Compose variables `SEGARIS_DATA_PATH` / `SEGARIS_HTTP_PORT` and the lowercase
path `/data/volumes/segaris`, matching the rest of the documentation and the
implemented files. `docs/architecture/deployment.md` was corrected accordingly.
