# Blackwing

Blackwing is Armali's private multi-user image gallery: a .NET API, React client and
PostgreSQL Compose stack where each user uploads, tags and browses strictly private
images. See `docs/` for the requirements, architecture and roadmap.

## Development

Copy `src/backend/Blackwing.Api/appsettings.example.json` to `appsettings.json`, then run `./scripts/backend-restore.ps1`, `./scripts/backend-build.ps1` and `dotnet run --project src/backend/Blackwing.Api`.

In a second terminal run `./scripts/frontend-restore.ps1` then `corepack pnpm --dir src/frontend dev`. Vite proxies `/api` to `http://localhost:5054`; override with `BLACKWING_FRONTEND_PROXY_TARGET`.

### Database

The API applies EF Core migrations automatically on startup, so a fresh Postgres volume is provisioned without manual steps. Migrations live in `src/backend/Blackwing.Persistence/Migrations`. To add one after changing the model:

```
dotnet ef migrations add <Name> --project src/backend/Blackwing.Persistence
```

## Full stack

Copy `deploy/compose/.env.example` to `.env`, set strong values for `BLACKWING_POSTGRES_PASSWORD` and `BLACKWING_INITIAL_ADMIN_PASSWORD`, then run `./scripts/compose-up.ps1`. Open `http://localhost:5055`.

The API exposes `/health/live` and `/health/ready`. Persistent Docker volumes are `postgres-data` and `image-data`; the latter holds the content-addressed, per-user private image store.

## Operations

Operational runbooks live under `docs/operations/`:

- [despliegue.md](docs/operations/despliegue.md) — reproducible internal deploy via Compose and CI.
- [volumenes-y-backup.md](docs/operations/volumenes-y-backup.md) — the persistent volumes an external backup must protect, and the restore procedure.
- [observabilidad.md](docs/operations/observabilidad.md) — request correlation (`X-Trace-ID`), health probes, and ingestion-queue metrics (`GET /api/ops/ingestion`, admin only).
- [seguridad.md](docs/operations/seguridad.md) — security posture review.
- [pruebas-de-carga.md](docs/operations/pruebas-de-carga.md) — load-test methodology and latency targets.

Phase acceptance is tracked in [docs/FASE7_ACEPTACION.md](docs/FASE7_ACEPTACION.md).
