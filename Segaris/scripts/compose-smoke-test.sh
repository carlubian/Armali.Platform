#!/usr/bin/env bash
#
# Build and start the full Segaris container stack locally, verify that Caddy
# routes correctly, the backend reaches readiness, and the authenticated
# application flow (sign-in, self-service profile, administrative user list)
# works end to end through the ingress, then tear everything down.
#
# It is safe to run repeatedly; it uses an isolated Compose project and ephemeral
# host storage in a temporary directory, seeds a disposable bootstrap
# administrator, and always cleans up.
#
# Usage:
#   ./compose-smoke-test.sh
#
# Requirements: Docker with the Compose plugin.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_DIR="${SCRIPT_DIR}/../deploy/compose"
PROJECT="segaris-smoke"
HTTP_PORT="${SEGARIS_HTTP_PORT:-15525}"

WORK_DIR="$(mktemp -d)"
mkdir -p "${WORK_DIR}/attachments" "${WORK_DIR}/backups" "${WORK_DIR}/dataprotection-keys"
# The backend runs as the fixed non-root identity 5525:5525. These directories
# contain disposable smoke-test data, so allow that container identity to write
# without requiring privileged ownership changes on the GitHub runner.
chmod 0777 "${WORK_DIR}/attachments" "${WORK_DIR}/backups" "${WORK_DIR}/dataprotection-keys"

# Seed a disposable first administrator so the functional flow below can sign in
# and exercise the profile and administrative endpoints. The bootstrap is
# idempotent and only acts on the empty smoke database.
SMOKE_ADMIN_USERNAME="smoke-admin"
SMOKE_ADMIN_PASSWORD="SmokeAdmin123!"

export SEGARIS_HTTP_PORT="${HTTP_PORT}"
export SEGARIS_DATA_PATH="${WORK_DIR}"
export SEGARIS_POSTGRES_PASSWORD="smoke-test-only"
export SEGARIS_BOOTSTRAP_USERNAME="${SMOKE_ADMIN_USERNAME}"
export SEGARIS_BOOTSTRAP_PASSWORD="${SMOKE_ADMIN_PASSWORD}"

compose() {
  docker compose -p "${PROJECT}" \
    -f "${COMPOSE_DIR}/docker-compose.yml" \
    -f "${COMPOSE_DIR}/docker-compose.local.yml" "$@"
}

cleanup() {
  echo "--- Tearing down ---"
  compose down --volumes --remove-orphans || true
  rm -rf "${WORK_DIR}"
}
trap cleanup EXIT

echo "--- Building and starting stack (project: ${PROJECT}, port: ${HTTP_PORT}) ---"
if ! compose up --build -d; then
  echo "Compose failed while starting the stack." >&2
  compose ps || true
  compose logs backend || true
  exit 1
fi

echo "--- Waiting for the backend to become healthy ---"
deadline=$(( $(date +%s) + 240 ))
until [[ "$(compose ps backend --format '{{.Health}}' 2>/dev/null)" == "healthy" ]]; do
  if [[ "$(date +%s)" -ge "${deadline}" ]]; then
    echo "Backend did not become healthy in time." >&2
    compose logs backend || true
    exit 1
  fi
  sleep 5
done
echo "Backend is healthy."

base="http://localhost:${HTTP_PORT}"

echo "--- Checking frontend routing ( / ) ---"
if ! curl -fsS "${base}/" | grep -q '<title>Segaris</title>'; then
  echo "The Segaris frontend was not served through Caddy." >&2
  exit 1
fi
echo "Frontend routing OK."

echo "--- Checking frontend SPA fallback ---"
if ! curl -fsS "${base}/login" | grep -q '<title>Segaris</title>'; then
  echo "The frontend did not serve index.html for a client-side route." >&2
  exit 1
fi
echo "Frontend SPA fallback OK."

echo "--- Checking backend routing ( /api/session ) ---"
# An unauthenticated current-session request must reach the backend. Any HTTP
# response (401 expected) proves Caddy routed /api/ to the backend rather than
# the frontend. A routing failure would return the frontend's 200 HTML instead.
api_status="$(curl -s -o /dev/null -w '%{http_code}' "${base}/api/session")"
echo "GET /api/session -> HTTP ${api_status}"
case "${api_status}" in
  200|401|403) echo "Backend routing OK." ;;
  *) echo "Unexpected status from /api/session; backend routing may be broken." >&2; exit 1 ;;
esac

echo "--- Checking the authenticated application flow (login, profile, admin) ---"
COOKIES="${WORK_DIR}/cookies.txt"

# 1. Obtain an antiforgery token (and its cookie) for the state-changing sign-in.
csrf="$(curl -fsS -c "${COOKIES}" -b "${COOKIES}" "${base}/api/session/antiforgery" \
  | sed -n 's/.*"csrfToken":"\([^"]*\)".*/\1/p')"
if [[ -z "${csrf}" ]]; then
  echo "Could not obtain an antiforgery token from /api/session/antiforgery." >&2
  exit 1
fi

# 2. Sign in as the seeded administrator. The administrator is created during
#    backend startup, so allow a few attempts before giving up.
login_status=""
for _ in 1 2 3 4 5 6; do
  login_status="$(curl -s -o /dev/null -w '%{http_code}' \
    -c "${COOKIES}" -b "${COOKIES}" \
    -H 'Content-Type: application/json' \
    -H "X-CSRF-TOKEN: ${csrf}" \
    -X POST "${base}/api/session" \
    --data "{\"userName\":\"${SMOKE_ADMIN_USERNAME}\",\"password\":\"${SMOKE_ADMIN_PASSWORD}\"}")"
  [[ "${login_status}" == "204" ]] && break
  sleep 5
done
if [[ "${login_status}" != "204" ]]; then
  echo "Sign-in as the seeded administrator failed (HTTP ${login_status})." >&2
  exit 1
fi
echo "Sign-in OK."

# 3. The self-service profile must be reachable for the authenticated session.
profile_status="$(curl -s -o /dev/null -w '%{http_code}' -b "${COOKIES}" "${base}/api/session/profile")"
if [[ "${profile_status}" != "200" ]]; then
  echo "GET /api/session/profile returned HTTP ${profile_status} (expected 200)." >&2
  exit 1
fi
echo "Profile OK."

# 4. The administrative user list must be reachable for the administrator role.
admin_status="$(curl -s -o /dev/null -w '%{http_code}' -b "${COOKIES}" "${base}/api/admin/users")"
if [[ "${admin_status}" != "200" ]]; then
  echo "GET /api/admin/users returned HTTP ${admin_status} (expected 200)." >&2
  exit 1
fi
echo "Administrative user management OK."

echo ""
echo "Smoke test passed."
