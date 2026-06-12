# Temporary frontend placeholder.
#
# This image exists ONLY to validate Caddy routing and the Compose smoke test
# before the real TypeScript frontend exists. It serves a single static page on
# port 80 and is outside product scope. Replace it with the real frontend image
# (built from src/frontend) once that exists; the Compose service name stays
# "frontend" so no other definition needs to change.
FROM caddy:2-alpine
COPY index.html /usr/share/caddy/index.html
