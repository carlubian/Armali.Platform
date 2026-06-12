# Runtime And Deployment

This document records the Phase 1 decisions for the initial Segaris Platform runtime and deployment model.

## Hosting Target

Segaris Platform will run on a local household server, expected to use Ubuntu.

The server is part of the household infrastructure rather than a public cloud environment. The application remains online-only relative to this server: clients require network access to it and do not maintain an offline replica.

The server baseline is Ubuntu 24.04 LTS on an x64 CPU with 6–8 cores, at least 8 GB RAM, and an SSD of at least 256 GB. See `docs/planning/BACKEND_DEPLOYMENT_DECISIONS.md`.

## Containerization

Docker is the preferred deployment mechanism.

The frontend and backend will be built as separate images and run as separate containers:

- The frontend container serves the compiled TypeScript web application as static assets.
- The backend container runs the ASP.NET Core REST API and owns application and domain behavior.

The containers may be released together, but they remain independently buildable. Their interface is the documented REST API.

The backend image is a multi-stage build on the .NET 10 SDK and ASP.NET Core runtime images, runs as a dedicated non-root identity (UID:GID 5525:5525), listens on container port 8080, and bundles the PostgreSQL 17 client (`pg_dump`) for the administrative backup job. Concrete container, Compose, ingress, and operational decisions are recorded in `docs/planning/BACKEND_DEPLOYMENT_DECISIONS.md`. The implemented assets live under `src/backend/Segaris.Api/Dockerfile`, `deploy/caddy/`, `deploy/frontend-placeholder/`, and `deploy/compose/`.

Docker Compose is the expected initial orchestration mechanism because the system targets one local server and does not currently need cluster orchestration. This is an architectural default rather than a final implementation commitment.

## Persistence

Containers are disposable and must not be the sole location of persistent state.

Components that require persistence will use Docker-managed volumes or explicit host-mounted directories. Likely persistent data includes:

- Primary database files or database-managed storage.
- User attachments and imported documents, unless an external object store is selected.
- ASP.NET Core Data Protection keys required to preserve authentication sessions across backend container replacement.
- Operational data needed for backup or recovery.

Application images, temporary files, caches, and generated frontend assets should remain replaceable and should not require backup unless a later decision identifies a specific need.

The exact storage layout must be decided together with the primary database and attachment-storage choices.

## Host Storage Layout

Production persistence follows the existing server convention under `/data/volumes/<application>`. Segaris uses this default layout:

```text
/data/volumes/segaris/
|-- attachments/
|-- backups/
`-- dataprotection-keys/
```

- `attachments/` is bind-mounted into the backend container and contains live user-uploaded files.
- `backups/` is bind-mounted into the backend container as the staging and output location for the latest completed backup package. It is also the integration point from which the external household backup service copies the package off-server.
- `dataprotection-keys/` is bind-mounted into the backend container and holds the ASP.NET Core Data Protection key ring so authentication sessions survive backend container replacement.

The root host path is configurable through the Compose environment variable `SEGARIS_DATA_PATH`, which defaults to `/data/volumes/segaris`. Compose definitions derive the bind mounts from that variable rather than repeating absolute host paths:

```yaml
volumes:
  - "${SEGARIS_DATA_PATH:-/data/volumes/segaris}/attachments:/data/attachments"
  - "${SEGARIS_DATA_PATH:-/data/volumes/segaris}/backups:/data/backups"
  - "${SEGARIS_DATA_PATH:-/data/volumes/segaris}/dataprotection-keys:/data/dataprotection-keys"
```

These directories must be owned by the backend container identity (UID:GID 5525:5525) before the stack starts; `scripts/host-provision.sh` creates and chowns them.

PostgreSQL data uses a Docker-managed named volume rather than a bind mount. Database files must not be inspected, copied, or modified directly from the host as an application backup mechanism; database recovery uses the PostgreSQL dump included in Segaris's backup package.

The backend is the only application container with access to the attachment and backup bind mounts. Caddy and the frontend have no access to persistent application data. Host directories must be created with ownership and permissions matching the non-root backend container user before the production stack starts.

## Deployment Principles

- Images should be immutable and versioned.
- Configuration must be supplied at runtime rather than baked into images.
- Secrets must not be committed to the repository or included in images.
- Container upgrades must preserve compatible persistent volumes.
- The single backend instance applies pending database migrations before accepting traffic; failed migration aborts startup.
- Backup and restore procedures must cover every persistent volume and be tested periodically.
- Backend health must be observable so the frontend or ingress layer can distinguish an unavailable service from valid empty data.

## Initial Topology

The expected topology is:

```text
Household browser
        |
        v
Ingress / reverse proxy (to be selected)
        |-- serves or routes to frontend container
        `-- routes /api requests to backend container
                                      |
                                      |-- PostgreSQL container in production
                                      `-- attachment storage (to be selected)
```

Only the ingress component is exposed to the household network. Backend, database, and supporting services communicate through an internal Docker network unless an exceptional operational task requires temporary direct access.

## Ingress And Network Access

Caddy will provide the initial ingress layer as a dedicated container. It is used for HTTP routing only; Segaris does not require Caddy to manage public domains or TLS certificates.

The routing model is:

- Requests under `/api/` are proxied to the backend container.
- All other requests are routed to the frontend container.
- The backend and PostgreSQL containers do not publish ports to the household network.
- PostgreSQL administration, when required, is performed from the server or through a temporary explicitly configured access path.

This gives browsers one same-origin application address and avoids exposing or configuring a separate backend URL on client devices.

Segaris is served over plain HTTP because it is an internal household application deployed on a protected local network. HTTPS, certificate generation, and certificate trust distribution are outside the initial deployment scope. This decision must be reconsidered before exposing Segaris to an untrusted network, allowing remote access, or moving authentication traffic across infrastructure that is not fully controlled by the household.

The deployment publishes Caddy on one fixed host port. The default is `5525`, configurable through the Compose environment variable `SEGARIS_HTTP_PORT`:

```yaml
ports:
  - "${SEGARIS_HTTP_PORT:-5525}:80"
```

The variable changes only the host port. Caddy continues to listen on port `80` inside its container, and internal service ports remain private implementation details of the Compose network. The Caddy routing configuration is baked into a dedicated `segaris-caddy` image as config-as-code so the production stack needs no host file bind mount; only the published port is supplied at runtime.

Local containerized execution may use the default or override `SEGARIS_HTTP_PORT` through the Compose environment. Portainer supplies the same variable as stack configuration, allowing the production port to change without editing the Compose definition. The repository must include the default in the relevant example configuration and must not require a real environment file to be committed.

The household server must retain a stable LAN address through infrastructure configuration such as a static address or DHCP reservation. Successive deployments should preserve the configured published port so clients and any UniFi DNS record can continue to point to the same address.

Segaris itself does not manage local DNS. It can be accessed directly through the server IP and port, or through a custom DNS record managed by the household network infrastructure.

## Compose And Portainer

Docker Compose is the source format for both local containerized execution and production deployment. Production stacks must remain compatible with Portainer's Compose-based stack deployment and must not depend on orchestration features unavailable there.

Environment-specific Compose configuration may select images, host ports, volume paths, and runtime settings, but local and production deployments preserve the same service topology and container-to-container names. Portainer-specific secrets or configuration are supplied outside the repository rather than embedded in the Compose files.

Production frontend and backend images are pulled from a private Azure Container Registry. Portainer owns its existing read credentials for that registry. Deployments select immutable commit-SHA image tags and are initiated manually from Portainer; publishing an image does not automatically modify the running stack.

## Explicit Non-Goals

The initial deployment does not require:

- Kubernetes or another cluster orchestrator.
- Multi-node high availability.
- Public internet hosting.
- Independent scaling of frontend and backend replicas.
- Cloud-managed persistence as a prerequisite.

## Resolved Decisions

The deployment decisions previously open here were resolved in Wave 8 and are
recorded in `docs/planning/BACKEND_DEPLOYMENT_DECISIONS.md`:

- Server baseline: Ubuntu 24.04 LTS, x64 6–8 cores, ≥8 GB RAM, ≥256 GB SSD.
- Caddy routing and health checks: `/api/*` to the backend, all other traffic to
  the frontend; baked into the `segaris-caddy` image; health gating through
  container health checks and `/health/ready`.
- Backend container identity and provisioning: UID:GID 5525:5525, with
  `scripts/host-provision.sh` creating and chowning the host directories.
- Restore procedure and verification: `scripts/restore.sh` and
  `docs/operations/backup-and-restore.md`, rehearsed at least quarterly.
- Secret injection and rotation: Portainer stack environment variables, rotated
  by updating the stack and re-deploying.
