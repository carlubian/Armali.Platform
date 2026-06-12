# Rollback Runbook

Rollback returns Segaris to a previous known-good state. Because database
migrations are **forward-only** and are never reversed automatically, a rollback
is not simply re-deploying an older image — it must account for schema changes.

## Decision Tree

### Case 1: The new image did not apply any new migration

If the failed deployment introduced no schema change (or the backend never
reached the migration step), the database is still compatible with the previous
image.

1. In Portainer, re-deploy the stack with the **previous** immutable image tags.
2. Verify readiness:
   ```bash
   curl -fsS http://localhost:${SEGARIS_HTTP_PORT:-5525}/api/session
   ```

The persistent volumes and bind mounts are preserved across the re-deploy.

### Case 2: The new image applied a migration that must be undone

A forward-only migration cannot be reverted in place. Roll back by restoring the
last backup taken **before** the deployment, then re-deploying the compatible
image.

1. Confirm a pre-deployment `segaris-backup.tar` exists (risky or destructive
   migrations require generating one first; see the deployment runbook).
2. Restore it following `docs/operations/backup-and-restore.md`:
   ```bash
   sudo ./scripts/restore.sh --package /path/to/pre-deploy/segaris-backup.tar \
     --data-path /data/volumes/segaris --confirm
   ```
3. Re-deploy the stack with the previous image tags that match the restored
   schema.
4. Verify readiness and spot-check data and a sample attachment.

Data created between the pre-deployment backup and the rollback is lost; this is
the accepted trade-off for a single-instance, forward-only-migration household
system. Take a fresh backup immediately before any risky deployment to minimize
the window.

## Prevention

- Always deploy immutable commit-SHA image tags so "the previous image" is
  unambiguous.
- Generate a backup immediately before any deployment that includes a risky or
  destructive migration, and note it in the deployment record.
- Rehearse restore quarterly so the Case 2 path is a tested procedure.
