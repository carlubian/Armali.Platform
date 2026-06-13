FROM node:24.16.0-alpine AS build

WORKDIR /app

RUN corepack enable

COPY package.json pnpm-lock.yaml pnpm-workspace.yaml ./
RUN pnpm install --frozen-lockfile

COPY . .

ARG VITE_APP_VERSION=local
ENV VITE_API_BASE_URL=/api
ENV VITE_APP_VERSION=${VITE_APP_VERSION}

RUN pnpm run build

# Runtime stage: Caddy serves the compiled static assets with single-page
# fallback. Caddy is used across the stack (the ingress is Caddy too), so the
# frontend keeps a single web-server technology and a config-as-code Caddyfile.
FROM caddy:2-alpine AS runtime

COPY Caddyfile /etc/caddy/Caddyfile
RUN caddy validate --config /etc/caddy/Caddyfile --adapter caddyfile

COPY --from=build /app/dist /srv

EXPOSE 80

HEALTHCHECK --interval=15s --timeout=5s --retries=5 --start-period=10s \
  CMD wget --quiet --tries=1 --spider http://127.0.0.1/ || exit 1
