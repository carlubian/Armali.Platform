#!/usr/bin/env bash
#
# Provision the Segaris persistent host directories on the production server.
#
# Creates the attachments, backups, and Data Protection key directories under
# the data path and gives them to the dedicated non-root backend identity
# (UID:GID 5525:5525). Run once on the Ubuntu host before the first deployment,
# and again only if the data path changes.
#
# Usage (as root):
#   sudo ./host-provision.sh [DATA_PATH]
#
# DATA_PATH defaults to /data/volumes/segaris and must match SEGARIS_DATA_PATH
# in the Compose configuration.

set -euo pipefail

DATA_PATH="${1:-/data/volumes/segaris}"
SEGARIS_UID=5525
SEGARIS_GID=5525

if [[ "${EUID}" -ne 0 ]]; then
  echo "This script must be run as root (use sudo)." >&2
  exit 1
fi

echo "Provisioning Segaris host storage under: ${DATA_PATH}"

for dir in attachments backups dataprotection-keys; do
  target="${DATA_PATH}/${dir}"
  mkdir -p "${target}"
  echo "  ensured ${target}"
done

# The backend container runs as 5525:5525 and is the only container that mounts
# these directories. Ownership must match so the non-root process can write.
chown -R "${SEGARIS_UID}:${SEGARIS_GID}" "${DATA_PATH}"
chmod -R 750 "${DATA_PATH}"

echo "Ownership set to ${SEGARIS_UID}:${SEGARIS_GID}, permissions 750."
echo "Done. PostgreSQL data uses a Docker-managed named volume and is not provisioned here."
