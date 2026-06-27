FROM node:24-alpine AS build
WORKDIR /app

RUN corepack enable
COPY package.json pnpm-lock.yaml pnpm-workspace.yaml ./
COPY src/frontend/package.json src/frontend/
RUN pnpm install --frozen-lockfile

COPY src/frontend src/frontend
RUN pnpm --dir src/frontend build

FROM caddy:2-alpine AS runtime
COPY src/frontend/Caddyfile /etc/caddy/Caddyfile
COPY --from=build /app/src/frontend/dist /usr/share/caddy
EXPOSE 80
