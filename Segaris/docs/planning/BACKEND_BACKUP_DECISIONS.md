# Backend Background Jobs And Backup Decisions

## Purpose

This document records the Wave 6 decisions for the shared persistent background-job
infrastructure and the administrative backup capability. It complements
`docs/architecture/backend.md` (Background Jobs) and `docs/architecture/data-and-storage.md`
(Backup Package Generation) with the concrete choices made during implementation.

## Background Jobs

- Jobs persist in a single `platform_background_jobs` table. PostgreSQL (and SQLite locally)
  is the source of truth for claiming, state transitions, progress, and recovery.
- The lifecycle states are `Queued`, `Running`, `Succeeded`, `Failed`, `CancellationRequested`,
  `Cancelled`, and `Interrupted`. Transitions are validated centrally by `JobStateMachine`.
- A single `BackgroundService` worker claims one job at a time through an atomic
  `Queued -> Running` update, runs the handler in its own dependency-injection scope, and
  records the terminal state. An in-process coordinator wakes the worker promptly on enqueue
  and signals cancellation to a handler running in the current process.
- Job types are declared through `JobTypeRegistration` (job-type code, optional exclusivity
  key, handler type) and run by a scoped `IJobHandler`. The metadata is separate from the
  handler so the API can enforce exclusivity at enqueue time without resolving the handler.
- **Single-run enforcement is portable.** While a job is active it holds its exclusivity key
  in `ActiveExclusivityKey`; the column is cleared on any terminal state. A plain unique index
  over that column rejects a second active job for the same key, because both PostgreSQL and
  SQLite allow multiple nulls in a unique index. The enqueue path also pre-checks and reports
  `409 Conflict` with the active job identifier.
- **Cancellation is cooperative.** A request sets a persisted flag and, for a running job,
  moves it to `CancellationRequested` and signals the in-process token. A queued job is moved
  directly to `Cancelled` by the worker before it is claimed.
- **Interrupted-job recovery.** Because only one backend instance runs, any job left in
  `Running` or `CancellationRequested` at startup is marked `Interrupted`; partial output is
  never reported as success. Handlers must use staging and atomic publication so an interrupted
  job is safe.
- **No automatic retry.** Failed and interrupted jobs are not retried automatically.
- **Retention.** Completed job records are kept indefinitely for the initial low-volume
  household deployment. No automatic cleanup is implemented yet; this can be revisited if the
  table grows.
- **Safety.** Serialized parameters are bounded and must not contain secrets. The record stores
  only safe diagnostic summaries (a stable failure code and a trace identifier); exceptions and
  stack traces stay in structured logs. Job status exposes lifecycle, progress, and a result
  reference, never parameters or private payloads.

## Backup Authorization And Retrieval

These items resolve the previously open "Backup API authorization" roadmap entry.

- **Authorization.** Backup endpoints live under `/api/backup-jobs` and require the `Admin`
  policy. State-changing requests (`POST /api/backup-jobs`, `POST /api/backup-jobs/{id}/cancel`)
  require antiforgery validation like every other cookie-authenticated write. `GET
  /api/backup-jobs/{id}` returns safe job status.
- **Conflict disclosure.** Only one backup may be queued or running at a time (exclusivity key
  `backup`). A conflicting start returns `409 Conflict` with the active job identifier, which is
  safe to disclose because the caller is already authorized to start the operation.
- **Unattended automation.** API keys remain out of scope for this foundation. The external
  household backup service authenticates as an `Admin` account through the existing cookie
  session flow (log in, obtain the antiforgery token, start the backup) rather than through a
  bespoke trigger mechanism.
- **Result retrieval is filesystem-based.** The API does not stream the package over HTTP. The
  job writes the completed package to the configured backups directory and reports its filename
  through job status; the external service reads the file directly from the mounted backups
  volume. This matches the deployment model and avoids streaming multi-gigabyte downloads.

## Backup Package

- **PostgreSQL only.** Backups are inherently PostgreSQL-oriented. Starting a backup while the
  active provider is not PostgreSQL is rejected with `422 Unprocessable Content`. On SQLite
  development databases the capability is intentionally unavailable.
- **Dump tool.** The dump is produced by the external `pg_dump` tool in custom format
  (`--format=custom --no-owner --no-privileges`). The password is passed through `PGPASSWORD`
  so it never appears on a command line or in logs. The production backend image must include
  the PostgreSQL client (finalized in Wave 8); the PostgreSQL backup integration test requires
  `pg_dump` on `PATH` and skips on a developer machine without it while remaining mandatory in CI.
- **Package format.** The package is a single uncompressed `.tar` file named
  `segaris-backup.tar`, containing `database.dump`, the live `attachments/` tree (excluding
  staging and trash), and `manifest.json`. A single archive makes atomic replacement a clean
  `File.Move(overwrite: true)` rename.
- **Manifest.** The camelCase `manifest.json` records the creation time, application version,
  schema version (the last applied migration), and the relative path, SHA-256 hash, and size of
  every packaged file. It contains no secrets and references attachments only by their UUID
  storage names.
- **Atomic replacement and recovery.** Generation runs in a hidden staging directory and the
  finished archive atomically replaces the previous package only after every artifact and the
  manifest succeed. Failure, cancellation, or interruption removes staging data and leaves the
  previous valid package untouched. Only the latest package is retained.
- **Operational boundary.** Segaris produces the latest valid package and nothing more. An
  external service owns scheduling, retrieval, off-server transfer, encryption, retention, and
  lifecycle, as already documented in `docs/architecture/data-and-storage.md`.

## Configuration

`Segaris:Storage:BackupsPath` selects the backups directory. It is required in Production and
defaults to a temporary environment-specific location when omitted outside Production.
Production Compose binds this path to the documented persistent backups directory in Wave 8.

## Testing Notes

- A Testing-only probe job type (`probe`) and `/api/platform/jobs` endpoints drive the generic
  lifecycle (success, failure, cancellation, claiming, single-run conflict) over SQLite without
  PostgreSQL. The probe handler is registered everywhere but is inert because only Testing maps
  its endpoints.
- The backup orchestration (packaging, manifest, atomic replacement, staging cleanup, and
  failure preserving the previous package) is covered on SQLite by substituting a controllable
  dump runner. The real `pg_dump` path is validated end to end against PostgreSQL through
  Testcontainers.
