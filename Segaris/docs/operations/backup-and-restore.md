# Backup And Restore Runbook

Decisions behind this procedure are in `docs/planning/BACKEND_BACKUP_DECISIONS.md`
(package format) and `docs/planning/BACKEND_DEPLOYMENT_DECISIONS.md` (recovery
ownership and verification frequency).

## What Segaris Produces

Segaris generates a single latest backup package, `segaris-backup.tar`, in the
backups directory (`/data/volumes/segaris/backups`). It contains:

- `database.dump` — PostgreSQL custom-format dump (`pg_restore`).
- `attachments/` — live attachment files named by UUID.
- `manifest.json` — creation time, application and schema versions, and the
  relative path, SHA-256 hash, and size of every packaged file.

Backups require the PostgreSQL provider. An external household service owns
scheduling, off-server transfer, encryption, and retention; Segaris only produces
and atomically replaces the latest package.

## Generating A Backup

A backup runs as a background job started through the authenticated admin API.

1. Authenticate as an administrator and obtain the antiforgery token (the same
   flow any cookie-authenticated write uses).
2. `POST /api/backup-jobs`. A `409 Conflict` with the active job id means a backup
   is already running (only one at a time).
3. Poll `GET /api/backup-jobs/{id}` until it reports a terminal state.
4. On success the package is at `/data/volumes/segaris/backups/segaris-backup.tar`.
   The external service copies it off-server.

Generation runs in a hidden staging directory and replaces the previous package
atomically only after every artifact and the manifest succeed; a failure leaves
the previous valid package untouched.

## Restoring

Restoring is **destructive**: it overwrites the live database and attachments
with the package contents.

### Prerequisites

- The backup package is available on the host.
- The Compose stack is running and `deploy/compose/.env` is present with the
  correct database credentials.

### Procedure

```bash
sudo ./scripts/restore.sh \
  --package /path/to/segaris-backup.tar \
  --data-path /data/volumes/segaris \
  --confirm
```

The script:

1. Extracts the package to a temporary directory.
2. Stops the backend and Caddy to prevent concurrent writes.
3. Restores the database with `pg_restore --clean --if-exists --no-owner
   --no-privileges` into the running PostgreSQL container.
4. Mirrors the attachments tree into `<data-path>/attachments` and restores
   ownership to `5525:5525`.
5. Restarts the backend and Caddy.

### Verification

```bash
curl -fsS http://localhost:${SEGARIS_HTTP_PORT:-5525}/api/session
```

Then log in and confirm that application data is present and that a sample
attachment opens correctly.

## Restore Rehearsal

Rehearse the restore against a disposable target (a separate Compose project or a
scratch host) at least **quarterly**, and additionally after any change to the
backup package format, the persistence layout, or the PostgreSQL major version.
Record the rehearsal outcome so recovery is a tested procedure, not just a written
one.
