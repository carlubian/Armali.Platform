#!/usr/bin/env bash
#
# Restore Segaris from a backup package (segaris-backup.tar) into the running
# Compose stack. This is a DESTRUCTIVE operation: it replaces the current
# database contents and attachment files with the contents of the package.
#
# The package (produced by the administrative backup job) contains:
#   - database.dump   PostgreSQL custom-format dump (pg_restore)
#   - attachments/    live attachment files named by UUID
#   - manifest.json   creation time, versions, and per-file SHA-256 hashes
#
# Usage:
#   ./restore.sh --package /path/to/segaris-backup.tar --confirm [--data-path DIR] [--project NAME]
#
# Requirements: Docker with the Compose plugin, run from a host that can reach
# the stack. The Compose environment (.env) must be present in deploy/compose.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_DIR="${SCRIPT_DIR}/../deploy/compose"
PROJECT="segaris"
DATA_PATH="${SEGARIS_DATA_PATH:-/data/volumes/segaris}"
PACKAGE=""
CONFIRM="no"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --package) PACKAGE="$2"; shift 2 ;;
    --data-path) DATA_PATH="$2"; shift 2 ;;
    --project) PROJECT="$2"; shift 2 ;;
    --confirm) CONFIRM="yes"; shift ;;
    *) echo "Unknown argument: $1" >&2; exit 1 ;;
  esac
done

if [[ -z "${PACKAGE}" || ! -f "${PACKAGE}" ]]; then
  echo "Provide an existing backup package with --package <file>." >&2
  exit 1
fi
if [[ "${CONFIRM}" != "yes" ]]; then
  echo "Refusing to restore without --confirm. This OVERWRITES the live database and attachments." >&2
  exit 1
fi

DB_NAME="${SEGARIS_POSTGRES_DB:-segaris}"
DB_USER="${SEGARIS_POSTGRES_USER:-segaris}"

compose() {
  docker compose -p "${PROJECT}" --env-file "${COMPOSE_DIR}/.env" \
    -f "${COMPOSE_DIR}/docker-compose.yml" "$@"
}

WORK_DIR="$(mktemp -d)"
cleanup() { rm -rf "${WORK_DIR}"; }
trap cleanup EXIT

echo "--- Extracting package ---"
tar -xf "${PACKAGE}" -C "${WORK_DIR}"
if [[ ! -f "${WORK_DIR}/database.dump" ]]; then
  echo "Package does not contain database.dump." >&2
  exit 1
fi

echo "--- Stopping backend and ingress to prevent concurrent writes ---"
compose stop backend caddy

echo "--- Restoring database ${DB_NAME} ---"
# --clean --if-exists drops existing objects before recreating them; the dump was
# taken with --no-owner --no-privileges so it restores cleanly as the app user.
cat "${WORK_DIR}/database.dump" | compose exec -T postgres \
  pg_restore --clean --if-exists --no-owner --no-privileges \
  --username "${DB_USER}" --dbname "${DB_NAME}"

echo "--- Restoring attachments into ${DATA_PATH}/attachments ---"
if [[ -d "${WORK_DIR}/attachments" ]]; then
  mkdir -p "${DATA_PATH}/attachments"
  # Mirror the package's attachment tree, removing files not present in the backup.
  rsync -a --delete "${WORK_DIR}/attachments/" "${DATA_PATH}/attachments/"
  chown -R 5525:5525 "${DATA_PATH}/attachments" 2>/dev/null || \
    echo "  (could not chown attachments; run as root if ownership is wrong)"
else
  echo "  package had no attachments/ directory; skipping."
fi

echo "--- Starting backend and ingress ---"
compose start backend caddy

echo ""
echo "Restore complete. Verify readiness:"
echo "  curl -fsS http://localhost:\${SEGARIS_HTTP_PORT:-5525}/api/session"
echo "Then confirm application data and a sample attachment open correctly."
