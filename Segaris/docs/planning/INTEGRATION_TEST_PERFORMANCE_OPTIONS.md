# API Integration Test Performance — Options

## Scope

The `Segaris.Api.IntegrationTests` suite spins up a full API host per test, so its runtime
grows with both the number of tests and the per-host startup cost, and the latter grows with
each module. This document records the measured cost breakdown, what has already been applied,
and the remaining options to evaluate later.

## Measured cost breakdown (2026-06-21)

Each test creates a fresh `WebApplicationFactory<Program>` against a file-backed SQLite
database. Measured per-host startup cost on a development machine:

| Path | Cost per host |
| --- | --- |
| Cold start (no template: full migrate + every module seeder) | ~2140 ms |
| Warm start (migrated + seeded template copied in) | ~1651 ms |
| Migrate + seed slice removed by the template | ~489 ms (~23%) |

Key finding: the **dominant cost is building the host** (the dependency-injection container,
the EF Core model, Identity/Data Protection, and the background `JobWorker`), not migrate+seed.
About 498 hosts are built for ~623 executed cases.

## Applied

### Option 1 — Template database by file copy (done)

`tests/backend/Segaris.Api.IntegrationTests/Infrastructure/SqliteTemplateDatabase.cs` builds a
migrated and seeded SQLite database once per process and hands each test host a file copy. The
host's startup still runs migrate (now a no-op) and the seeders (idempotent, so they skip), so
the expensive schema creation and seed inserts are paid once instead of ~498 times.

- Saves ~23% of per-host startup and, more importantly, keeps the migration-replay cost flat —
  that cost otherwise grows forever as migrations accumulate, independent of new modules.
- Templates are cached per Identity bootstrap credential pair (currently `founder`,
  `job-admin`, `attachment-admin`).
- The copy is skipped when the destination file already exists, so the restart-style tests
  (`DataProtectionRestartTests`, `JobRecoveryTests`) that reuse a prior host's database keep
  working.
- Preserves the existing per-test database file, so xUnit parallelism and the `JobWorker` are
  unchanged. Full suite stays green (623/623).

### CI sharding (done)

`scripts/backend-test-shard.ps1` partitions the integration test classes round-robin across N
shards (discovered via `dotnet test --list-tests`, so a new module's classes are sharded
automatically with no list to maintain). The `backend-integration` matrix job in
`.github/workflows/segaris-validation.yml` runs the shards on parallel runners, multiplying the
available cores and keeping wall-clock flat as modules are added. See
`docs/planning/BACKEND_CI_DECISIONS.md` for the branch-protection implications (each matrix leg
is its own required check).

Trade-off: each shard restores and builds independently, so runner-minute usage rises even
though wall-clock falls. Mitigations to consider: share a single build via upload/download
artifacts, or balance shards by recorded duration instead of class count.

## Remaining options to evaluate

### Option A — EF Core compiled model

Generate a compiled model (`dotnet ef dbcontext optimize`) and register it with `UseModel`.
EF model building is part of the ~1651 ms host build and grows with entity count, so a compiled
model attacks the dominant cost across **every** host without the isolation complexity of a
shared host. Lower risk than Option B; good effort-to-payoff. To evaluate: measure the
model-build share of host startup, then the delta with `UseModel`.

### Option B — Shared host across tests (the structural win)

Build the host once and reuse the DI container and EF model across tests instead of per test.
This removes most of the ~1651 ms fixed cost, which is the largest remaining lever. Costs:

- Test isolation must be handled without a fresh host: a per-test database file resolved at
  request time (e.g. an `AsyncLocal` connection-string resolver) so parallelism is preserved,
  combined with the template copy from Option 1.
- The host's startup migrate/seed must be disabled (the template already carries both).
- The background `JobWorker` runs outside any test's async context, so it would not observe a
  per-test `AsyncLocal` connection. It must be disabled in the shared host and exercised
  separately through a dedicated per-test host. This is the main reason Option B was deferred.

### Option C — Split into per-module test projects

Move each module's integration tests into its own test project. This makes sharding a matrix
over projects (no class partitioning) and lets unrelated modules build and test independently,
but it is a larger refactor and changes the project layout. Evaluate only if Options A/B prove
insufficient.

## Recommended sequence

1. Option 1 + CI sharding — done.
2. Option A (compiled model) — cheap, broad, low risk. Do next if more headroom is needed.
3. Option B (shared host) — the biggest win, only if A is not enough, accepting the `JobWorker`
   and isolation work.
4. Option C — last resort / if the project layout is being reworked anyway.

## GitHub Actions limits (context)

The hard limit is 6 hours per job and 35 days per workflow run, so the suite is not near a hard
cap; the concern is feedback-loop time, runner-minute budget, and the per-job `timeout-minutes`
(currently 25 for each integration shard). Sharding addresses the wall-clock and timeout
headroom directly.
