# Blackwing

Blackwing is Armali's private multi-user image gallery. Phase 1 provides the .NET API, React client, PostgreSQL Compose stack and validation commands.

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
