# Blackwing Architecture

## Status

Phase 1 design. High-level architecture agreed for Blackwing. It records the
intended shape and the reasoning behind the main decisions; it does not define
implementation steps or milestones. See `REQUIREMENTS.md` for the functional
source of truth.

## Overview

Blackwing is a **standalone web application** in the Armali.Platform monorepo, a
sibling to Segaris and Belfalas. It reuses the proven Segaris technology stack
and identity patterns but runs as its own independently built and deployed app,
with its own database and storage volume.

## Technology Stack

- **Backend:** C# / ASP.NET, layered (API + Persistence + Shared, following the
  Segaris shape). Minimal-API endpoints grouped by concern.
- **Database:** **PostgreSQL** as the primary provider. Relational needs are
  real (many-to-many tags, prefix autocomplete, faceted filtering, tag merges)
  and concurrency is multi-user, so Postgres is the natural fit. A SQLite second
  provider may be added later if useful, but is not required for v1.
- **Frontend:** React 19 + Vite, aligned with Segaris (TanStack Query,
  React Router, React Hook Form + Zod, i18next).
- **Deployment:** Docker Compose with backend, frontend (served by Caddy), a
  Postgres container, and a persistent **image storage volume**. Registered in
  the monorepo CI alongside Segaris and Belfalas.

## Why the DB is not the bottleneck

At ~25,000 images and ~50/week growth, the relational footprint is tiny: the
images table stays in the tens of thousands, and the image↔tag join reaches at
most a few hundred thousand rows. The engineering attention goes to **image
storage and delivery**, not to query scale.

## Image Storage

Because images enter **only** through the application's upload flow, Blackwing
controls the on-disk layout end to end. Storage is **content-addressable within
each user**:

```
/data/blackwing/{userId}/{ab}/{cd}/{sha256}.orig          ← original bytes
/data/blackwing/{userId}/{ab}/{cd}/{sha256}.preview.webp  ← medium preview
/data/blackwing/{userId}/{ab}/{cd}/{sha256}.thumb.webp    ← grid thumbnail
```

- Files are addressed by the **SHA-256** of the original bytes; the first two
  byte-pairs shard the tree so no directory holds an unbounded number of files.
- **Per-user deduplication** falls out for free: re-uploading the same picture
  maps to the same hash and does not duplicate bytes. Deduplication is scoped
  per user to preserve isolation (two users uploading the same file keep
  independent copies).
- The database stores the `sha256` and metadata, **not** a fragile path, so the
  layout can evolve without rewriting records.
- Physical separation by `userId` is defense in depth; it is **not** the primary
  privacy boundary (see Serving).

### Derivatives and processing

On upload the backend, off the request's critical path where practical:

- Computes the SHA-256 and rejects an exact per-user duplicate.
- Reads dimensions, EXIF **capture date**, and EXIF **orientation** (derivatives
  are rotated upright).
- Generates two derivatives — a **thumbnail** for gallery grids and a **preview**
  for the detail view — normalized to a web-friendly format (e.g. WebP). The
  **original** is kept untouched and served only on explicit demand / download.
- The image-processing library choice (e.g. a permissively licensed option) is
  an implementation-phase decision; licensing will be checked at that point.

## Image Serving

Images are **never** exposed as public static files. Delivery goes through
authorized endpoints:

```
GET /api/images/{id}/thumb
GET /api/images/{id}/preview
GET /api/images/{id}/original
```

- Each request is authenticated and **verifies ownership** before returning
  anything.
- Files are **streamed** (sendfile / range requests), never buffered wholesale
  into application memory.
- Because the URL is keyed by an immutable content hash internally, responses
  use strong, long-lived caching (`Cache-Control: private, immutable` + ETag),
  so browsers avoid re-fetching thumbnails without any invalidation problem.

## Data Model (draft)

- **Image** — `Id`, `OwnerUserId`, `Sha256`, `ContentType`, `Width`, `Height`,
  `Bytes`, `CapturedAt?`, `UploadedAt`, `ReviewedAt?`.
  - `UNIQUE(OwnerUserId, Sha256)` — per-user dedup.
  - Index supporting default ordering by capture date within an owner.
  - `ReviewedAt IS NULL` distinguishes pending-review images from reviewed ones.
- **Tag** — `Id`, `OwnerUserId`, `Type` ∈ {Person, Place, Topic}, `Value`,
  `NormalizedValue`.
  - `UNIQUE(OwnerUserId, Type, NormalizedValue)` — one reusable tag per label.
  - Prefix index on `NormalizedValue` (scoped by owner + type) for autocomplete.
- **ImageTag** — `ImageId`, `TagId`; the many-to-many join.
  - Indexed both directions (images-of-a-tag and tags-of-an-image).

Operations expressed on this model:

- **Autocomplete:** prefix query on `Tag.NormalizedValue` filtered by owner and
  type.
- **Tag migration (A→B):** within the owner, repoint `ImageTag` rows from A to B
  (dropping rows that would duplicate), then delete the now-orphaned tag A. Done
  in a transaction.
- **Faceted browse:** filter images by joining `ImageTag`; combination semantics
  finalized in the UX phase.

Every read and write is **scoped by `OwnerUserId`**.

## Identity

- A self-contained identity module modeled on Segaris: ASP.NET Identity, session
  cookie, antiforgery on state-changing requests, login rate limiting, two roles
  (User, Admin).
- Kept isolated so it could later be replaced by a shared Armali SSO without
  touching gallery code.
- Admin capabilities are limited to **account management** (create user, reset
  password); admins never gain access to other users' content.

## Deployment

- Docker Compose stack: **backend**, **frontend** (Caddy), **Postgres**, and a
  persistent volume mounted at the image storage root.
- The database and the image volume are the two stateful assets; both need a
  backup strategy (details deferred to operations planning).
- Added to the monorepo CI (build/validate) next to the Segaris workflows.

## Performance Principles

- Never load the whole corpus, or all files, into memory.
- Precompute thumbnails and previews at upload time; never resize on read.
- Paginate galleries with **keyset pagination** (stable under insertion).
- Stream files and lean on HTTP caching for derivatives.
- Keep hot query paths (gallery, autocomplete) backed by appropriate indexes.

## Deferred / To Finalize

- Exact multi-tag filter semantics (AND across types vs OR within a type).
- Accepted upload formats, maximum file size, and re-encoding policy.
- Image-processing library selection (and its licensing).
- Whether derivative generation is synchronous or queued for large bulk uploads.
- Backup and retention strategy for the Postgres DB and the image volume.
- Whether a SQLite second provider is worth carrying.
