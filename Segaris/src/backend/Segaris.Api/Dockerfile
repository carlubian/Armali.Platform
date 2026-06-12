# Segaris backend image.
#
# Build context is the repository root because the projects rely on the
# repository-level global.json, Directory.Build.props, and Directory.Packages.props
# (central package management). Build from the repository root, for example:
#
#   docker build -f src/backend/Segaris.Api/Dockerfile -t segaris-backend:local .
#
# The runtime image bundles the PostgreSQL client so the administrative backup job
# can run pg_dump. The client major version is pinned to match the production
# PostgreSQL server (17); changing the server major requires changing it here too.

# ---------------------------------------------------------------------------
# Build stage
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Restore against the central package and build policy files first so the
# restore layer is cached unless a project file or version actually changes.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/backend/Segaris.Api/Segaris.Api.csproj src/backend/Segaris.Api/
COPY src/backend/Segaris.Persistence/Segaris.Persistence.csproj src/backend/Segaris.Persistence/
COPY src/backend/Segaris.Migrations.Postgres/Segaris.Migrations.Postgres.csproj src/backend/Segaris.Migrations.Postgres/
COPY src/backend/Segaris.Migrations.Sqlite/Segaris.Migrations.Sqlite.csproj src/backend/Segaris.Migrations.Sqlite/
COPY src/backend/Segaris.Shared/Segaris.Shared.csproj src/backend/Segaris.Shared/
RUN dotnet restore src/backend/Segaris.Api/Segaris.Api.csproj

# Copy the rest of the backend source and publish a framework-dependent build.
COPY src/backend/ src/backend/
RUN dotnet publish src/backend/Segaris.Api/Segaris.Api.csproj \
    -c Release \
    --no-restore \
    -o /app/publish

# ---------------------------------------------------------------------------
# Runtime stage
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# Install the PostgreSQL 17 client (pg_dump) from the official PGDG repository,
# plus curl for the container health check. ca-certificates is required for the
# HTTPS apt source. gnupg is only needed while adding the repository key.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl ca-certificates gnupg \
    && install -d /usr/share/postgresql-common/pgdg \
    && curl -fsSL https://www.postgresql.org/media/keys/ACCC4CF8.asc \
        -o /usr/share/postgresql-common/pgdg/apt.postgresql.org.asc \
    && . /etc/os-release \
    && echo "deb [signed-by=/usr/share/postgresql-common/pgdg/apt.postgresql.org.asc] https://apt.postgresql.org/pub/repos/apt ${VERSION_CODENAME}-pgdg main" \
        > /etc/apt/sources.list.d/pgdg.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends postgresql-client-17 \
    && apt-get purge -y --auto-remove gnupg \
    && rm -rf /var/lib/apt/lists/*

# Dedicated non-root identity. The UID/GID match the host directory ownership
# documented for the attachments, backups, and Data Protection key bind mounts.
RUN groupadd --gid 5525 segaris \
    && useradd --uid 5525 --gid 5525 --no-create-home --shell /usr/sbin/nologin segaris

WORKDIR /app
COPY --from=build /app/publish ./

# Kestrel listens on 8080 inside the container. The host never publishes this
# port directly; Caddy is the only ingress.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

USER 5525:5525

# Process-level health check. Orchestration (Compose/Portainer) gates dependents
# on /health/ready instead, which additionally covers the database and storage.
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -fsS http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "Segaris.Api.dll"]
