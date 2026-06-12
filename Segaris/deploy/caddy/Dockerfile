# Segaris ingress (Caddy) image.
#
# The routing configuration is baked into the image as config-as-code so the
# production stack stays self-contained and Portainer-deployable without any
# host file bind mounts. The Caddyfile contains no secrets. Only the published
# host port is supplied at runtime, through the Compose port mapping.
FROM caddy:2-alpine
COPY Caddyfile /etc/caddy/Caddyfile
RUN caddy validate --config /etc/caddy/Caddyfile --adapter caddyfile
