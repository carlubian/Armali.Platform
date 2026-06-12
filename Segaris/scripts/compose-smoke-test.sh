#!/usr/bin/env bash
#
# Build and start the full Segaris container stack locally, verify that Caddy
# routes correctly and the backend reaches readiness, then tear everything down.
#
# This is the Wave 8 smoke test. It is safe to run repeatedly; it uses an
# isolated Compose project and ephemeral host storage in a temporary directory,
# and always cleans up.
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

export SEGARIS_HTTP_PORT="${HTTP_PORT}"
export SEGARIS_DATA_PATH="${WORK_DIR}"
export SEGARIS_POSTGRES_PASSWORD="smoke-test-only"
export SEGARIS_BOOTSTRAP_USERNAME=""
export SEGARIS_BOOTSTRAP_PASSWORD=""

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
if ! curl -fsS "${base}/" | grep -q "Segaris frontend placeholder"; then
  echo "Frontend placeholder was not served through Caddy." >&2
  exit 1
fi
echo "Frontend routing OK."

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

echo ""
echo "Smoke test passed."
