# Deployment Runbook

This runbook covers running the full Segaris container stack locally and
deploying it to the production server through Portainer. Decisions behind these
steps are recorded in `docs/planning/BACKEND_DEPLOYMENT_DECISIONS.md`.

## Topology

| Service | Image | Role | Host port |
| --- | --- | --- | --- |
| `caddy` | `segaris-caddy` | Ingress; routes `/api/*` to backend, rest to frontend | `SEGARIS_HTTP_PORT` (default 5525) |
| `frontend` | `segaris-frontend-placeholder` (temporary) | Static web app | internal only |
| `backend` | `segaris-backend` | ASP.NET Core REST API | internal only (8080) |
| `postgres` | `postgres:17` | Database | internal only |

Only Caddy publishes a port. Backend and PostgreSQL stay on the internal Compose
network.

## Local Full Stack

For validating the complete runtime on a developer machine.

1. Ensure Docker Desktop (or Docker Engine with the Compose plugin) is running.
2. From the repository root:
   ```powershell
   ./scripts/compose-up.ps1
   ```
   The first run creates `deploy/compose/.env` from the example with a local
   development password. Builds all images and starts the stack.
3. Open `http://localhost:5525/`. The placeholder page confirms frontend routing;
   `http://localhost:5525/api/session` confirms backend routing.
4. Tear down with `./scripts/compose-down.ps1` (add `-Volumes` to drop data).

The infrastructure-only stack (`./scripts/infra-up.ps1`) is the alternative when
running the backend natively; see the project README.

## Smoke Test

`scripts/compose-smoke-test.sh` performs a full build-up-verify-teardown cycle in
an isolated project with ephemeral storage. Run it before publishing images or
deploying:

```bash
./scripts/compose-smoke-test.sh
```

It fails if the backend does not reach readiness or if Caddy routing is wrong.

## Production Deployment (Portainer)

### Image publication setup

Create a GitHub environment named `segaris-production-images` and add the variables
`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `ACR_NAME`, and
`ACR_LOGIN_SERVER`. The Azure application or managed identity must have a federated
credential for this repository's environment subject and only the `AcrPush` role
on the target registry. Do not create a client secret for this workflow.

This environment is configured for `olyssia.azurecr.io` using Azure OIDC; no
GitHub secret is required for registry publication.

Every trusted commit on `main` publishes `segaris-backend`, `segaris-frontend`,
and `segaris-caddy` with the exact Git commit SHA as the tag. Publication never
updates Portainer automatically.

### One-time host preparation

1. Provision the persistent directories with the correct ownership (UID:GID
   5525:5525):
   ```bash
   sudo ./scripts/host-provision.sh /data/volumes/segaris
   ```
2. Confirm the server has a stable LAN address and that the chosen
   `SEGARIS_HTTP_PORT` is free and reachable from household clients.

### Stack deployment

1. Ensure the backend, Caddy, and frontend images for the target commit SHA have
   been published to the private Azure Container Registry (Wave 9 CI).
2. In Portainer, create or update the Segaris stack from `deploy/compose/docker-compose.yml`.
3. Supply the stack environment variables (never committed). At minimum:
   - `SEGARIS_HTTP_PORT` — published host port (default 5525).
   - `SEGARIS_DATA_PATH` — `/data/volumes/segaris`.
   - `SEGARIS_POSTGRES_PASSWORD` — strong, unique database password.
   - `SEGARIS_BACKEND_IMAGE`, `SEGARIS_CADDY_IMAGE`, `SEGARIS_FRONTEND_IMAGE` —
     immutable commit-SHA image tags from the registry.
   - For the very first deployment only: `SEGARIS_BOOTSTRAP_USERNAME` and
     `SEGARIS_BOOTSTRAP_PASSWORD` to create the first administrator. Clear them
     afterwards.
   - Optional: `SEGARIS_SEQ_ENABLED`, `SEGARIS_SEQ_SERVERURL`, `SEGARIS_SEQ_APIKEY`.
4. Deploy. The backend applies pending migrations before accepting traffic; if a
   migration fails the container does not become healthy and Caddy does not route
   to it.
5. Verify readiness from the server:
   ```bash
   curl -fsS http://localhost:${SEGARIS_HTTP_PORT:-5525}/api/session
   ```
   and open the application in a browser.

### Upgrades

- Publishing a new image does not change a running stack. Re-deploy the stack with
  the new immutable image tags from Portainer.
- Upgrades preserve the `postgres-data` named volume and the host bind mounts.
- Database migrations are forward-only. Before a risky or destructive migration,
  generate a backup first (see the backup runbook) and note it in the deployment.

## Secret Rotation

Rotate a secret by updating the Portainer stack variable and re-deploying.
Rotating `SEGARIS_POSTGRES_PASSWORD` additionally requires updating the role
password inside PostgreSQL (`ALTER ROLE segaris WITH PASSWORD '…';`).
