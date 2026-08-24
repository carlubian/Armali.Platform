# Blackwing backend image.
#
# Build context is the Blackwing directory because the projects rely on the
# application-level global.json, Directory.Build.props, and
# Directory.Packages.props (central package management). Build from Blackwing/,
# for example:
#
#   docker build -f src/backend/Blackwing.Api/Dockerfile -t blackwing-backend:local .
#
# Unlike Segaris, the runtime image does not bundle the PostgreSQL client:
# backups of the database and of the image volume are handled by a separate
# application and are out of scope here. Only curl is installed, for the
# container health check.

# ---------------------------------------------------------------------------
# Build stage
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Restore against the central package and build policy files first so the
# restore layer is cached unless a project file or version actually changes.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/backend/Blackwing.Api/Blackwing.Api.csproj src/backend/Blackwing.Api/
COPY src/backend/Blackwing.Persistence/Blackwing.Persistence.csproj src/backend/Blackwing.Persistence/
COPY src/backend/Blackwing.Shared/Blackwing.Shared.csproj src/backend/Blackwing.Shared/
RUN dotnet restore src/backend/Blackwing.Api/Blackwing.Api.csproj

# Copy the rest of the backend source and publish a framework-dependent build.
COPY src/backend/ src/backend/
RUN dotnet publish src/backend/Blackwing.Api/Blackwing.Api.csproj \
    -c Release \
    --no-restore \
    -o /app/publish

# ---------------------------------------------------------------------------
# Runtime stage
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# curl is the only extra package: the container health check uses it.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Dedicated non-root identity. The UID/GID match the host directory ownership
# documented for the images and Data Protection key bind mounts, and the HTTP
# port published by the ingress.
RUN groupadd --gid 5526 blackwing \
    && useradd --uid 5526 --gid 5526 --no-create-home --shell /usr/sbin/nologin blackwing \
    && install -d -o 5526 -g 5526 \
        /data/images \
        /data/dataprotection-keys

WORKDIR /app
COPY --from=build /app/publish ./

# Kestrel listens on 8080 inside the container. The host never publishes this
# port directly; Caddy is the only ingress.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

USER 5526:5526

# Process-level health check. Orchestration (Compose/Portainer) gates dependents
# on /health/ready instead, which additionally covers the database and storage.
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -fsS http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "Blackwing.Api.dll"]
