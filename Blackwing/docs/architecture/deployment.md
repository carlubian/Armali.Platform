# Deployment

Blackwing ships as four containers orchestrated by Docker Compose. The same base
topology is used for full local execution and for production; only the image
tags and the source of the environment variables differ.

## Topology

```
        host:5526
            |
         [caddy]  ingress, the only published port
          /    \
   /api/*       everything else
      |              |
  [backend]      [frontend]      Caddy serving the built SPA
      |
  [postgres]                     plus the images volume
```

- `deploy/compose/docker-compose.yml` — the base stack, `name: blackwing`. It
  **never builds**; it references images by tag.
- `deploy/compose/docker-compose.local.yml` — adds `build:` sections so the
  local stack builds from source. Production never uses it.
- `deploy/compose/docker-compose.windows.yml` — replaces the host bind mounts
  with Docker-managed volumes. Docker Desktop on Windows does not reproduce the
  `UID:GID` ownership the non-root backend expects, so `compose-up.ps1` layers
  this file automatically when it runs on Windows.
- `deploy/compose/docker-compose.infra.yml` — `name: blackwing-infra`, only
  PostgreSQL (published on localhost) plus an optional Seq under the `seq`
  profile. For native development and tests.

## Ports and identity

| Thing | Value |
| --- | --- |
| Published host port (`BLACKWING_HTTP_PORT`) | `5526` |
| Backend port inside its container | `8080`, never published to the host |
| Frontend / ingress port inside their containers | `80` |
| Non-root user in the backend container | `blackwing`, `UID:GID 5526:5526` |
| Native development backend | `5006` HTTP, `7151` HTTPS |

`5526` is Blackwing's number in the monorepo: the same number serves as the
published HTTP port and as the UID/GID of the container user. Segaris uses
`5525`. A new application in the monorepo takes the next free number.

Caddy is the only ingress. The backend is reachable only over the internal
bridge network — it never publishes a port.

## Ingress routing

`deploy/caddy/Caddyfile` is baked into the ingress image as config-as-code, so
the production stack stays self-contained and Portainer-deployable with no host
bind mounts. It runs with `auto_https off` and `admin off`; TLS is out of scope
on a trusted local network.

Three routes, in order:

1. `/api/health/*` — the `/api` prefix is stripped and the request goes to the
   backend, which serves health outside the `/api` surface. Exposed on purpose:
   the stack must be verifiable from the published port.
2. `/api/*` — proxied to `backend:8080` untouched.
3. everything else — proxied to `frontend:80`, which serves the SPA with a
   single-page fallback.

## Volumes

| Volume | Container path | Contents |
| --- | --- | --- |
| `postgres-data` | `/var/lib/postgresql/data` | The database |
| `${BLACKWING_DATA_PATH}/images` | `/data/images` | Original images and their derivatives |
| `${BLACKWING_DATA_PATH}/dataprotection-keys` | `/data/dataprotection-keys` | ASP.NET Data Protection keys |
| `caddy-data`, `caddy-config` | | Ingress state |

`BLACKWING_DATA_PATH` defaults to `/data/volumes/blackwing`. On a Linux host the
subdirectories must exist and be owned by `5526:5526` before the stack starts.

**Backups of the database and of the image volume are out of scope for
Blackwing.** Both are persistent volumes backed up by a separate application.
That is why the backend runtime image does not bundle the PostgreSQL client.

## Variables Portainer must supply in production

Everything is documented in `deploy/compose/.env.example`. The ones that have no
usable default:

- `BLACKWING_POSTGRES_PASSWORD` — **required, secret, no default.** The stack
  refuses to start without it.
- `BLACKWING_BACKEND_IMAGE`, `BLACKWING_FRONTEND_IMAGE`,
  `BLACKWING_CADDY_IMAGE` — immutable commit-SHA tags from the private registry.
- `BLACKWING_DATA_PATH` — the host root of the persistent volumes.
- `BLACKWING_HTTP_PORT` — if `5526` is not the desired published port.
- `BLACKWING_SEQ_*` — only when log delivery to Seq is wanted; `BLACKWING_SEQ_APIKEY`
  is a secret.

The real `.env` is never committed. `scripts/compose-up.ps1` creates one from
the example with a local development password the first time it runs.

## Image publication

`.github/workflows/blackwing-publish-images.yml` publishes `blackwing-backend`,
`blackwing-frontend` and `blackwing-caddy` to the Azure Container Registry over
OIDC, tagged with the commit SHA, after the validation workflow succeeds on
`main`. It runs under the `blackwing-production-images` GitHub environment,
which is Blackwing's own — it does not share Segaris's.

The backend image builds with `Blackwing/` as its context, because the restore
needs `global.json`, `Directory.Build.props` and `Directory.Packages.props`.
