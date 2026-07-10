# Blackwing Requirements

## Status

Phase 1 design. This document records the agreed high-level requirements for
Blackwing. It is the functional source of truth for later, more detailed
architecture and implementation planning. It intentionally avoids implementation
steps.

## Purpose

Blackwing is a private, multi-user **image gallery** application. Each user
uploads their own images, describes them with free-text tags, and later browses
and revisits them by tag. Every image and every tag is **private to its owner**:
nothing is shared with, or visible to, any other user.

Blackwing is a **standalone application** that lives in the Armali.Platform
monorepo as a sibling to Segaris and Belfalas. It is not a Segaris plugin or
module. It has its own backend, frontend, database, and deployment, and it
reuses Segaris's *patterns* (not its running instance).

## Application Form

- Standard web application with a split frontend / backend, hosted on an
  internal server.
- Primarily targeted at desktop computers with large displays. Mobile-first and
  fully responsive design are out of the initial scope.
- Designed for a **large and growing image corpus**: on the order of 25,000
  images at launch, growing by roughly 50 per week. The design must never depend
  on loading the whole corpus (or all its files) into memory.

## Users, Accounts and Roles

- Authentication uses **local username + password accounts**, following the
  Segaris identity pattern (session cookie, antiforgery protection, login rate
  limiting).
- Two roles:
  - **User** — the normal role. Uploads, tags, browses, edits and deletes their
    own images and tags.
  - **Admin** — **account management only**: create users and reset passwords.
    An admin has *no* access to any other user's images or tags. Administration
    is about accounts, not content.
- Identity is **replicated inside Blackwing**, isolated in its own module so it
  could later be swapped for a shared Armali SSO if that need ever arises. No
  shared SSO is built now.

## Core Concepts

### Image

An image is a single uploaded picture owned by one user. It carries:

- The stored picture and its generated derivatives (see Architecture).
- Intrinsic metadata: dimensions, content type, byte size.
- A **capture date** extracted from EXIF when available (used as the default
  ordering), with the upload time as a fallback.
- A **review state**: a newly uploaded image is *pending review* until the user
  has processed it. This is explicit and distinct from "has zero tags", so that
  "not looked at yet" is not confused with "reviewed and deliberately left
  untagged".

### Tag

A tag is a free-text label owned by one user. Tags are **not** curated by
admins; users create them implicitly as they type.

- Every tag has exactly one of **three fixed types**:
  - **Person** — people who appear in the image.
  - **Place** — where the image was taken / what location it depicts.
  - **Topic** — relevant subjects / themes.
- Tag values are free text. Within a single user and type, a tag value is
  unique (case/space-insensitive), so the same label is one reusable tag rather
  than many duplicates.
- An image may carry any number of tags, of any mix of the three types
  (many-to-many between images and tags).

## Functional Requirements

### Upload

- Users can upload images through the application. Both **single** and **bulk /
  multi-file** upload are supported; bulk upload is the intended way to bring in
  a large existing collection.
- On upload the application generates the required derivatives and extracts
  metadata (dimensions, capture date, orientation).
- Uploaded images land in the **pending review** state; tags are **not** applied
  during bulk upload.

### Review and Tagging

- A dedicated **review screen** lets the user process pending images **one at a
  time**, assigning tags and marking each as reviewed.
- When assigning a tag, the UI offers **autocomplete** over the user's existing
  tags, scoped to the current tag type, to encourage reuse of existing labels
  instead of creating near-duplicates.
- The user can create a new tag simply by typing a value that does not yet
  exist.

### Manage Existing Images

- Already-registered images can be **edited** (add, change or remove tags) and
  **deleted** (removing the image, its derivatives and its tag associations).
- Deleting an image must not delete tags that are still used by other images;
  tags that become unused may be cleaned up.

### Tag Migration / Merge

- A user can **migrate a tag**: replace every occurrence of tag A with tag B
  across all of their images, after which A no longer exists. This effectively
  merges A into B. It is always scoped to the acting user's own tags.

### Browsing

- Users navigate their gallery **by tag**: pick a tag (or combine tags) and see
  the matching images as a paginated gallery of thumbnails.
- Filtering supports combining multiple tags; the exact combination semantics
  (e.g. AND across types, OR within a type) are to be finalized in the
  architecture/UX phase.
- A dedicated view surfaces **pending-review** images.
- The default ordering is **capture date** (EXIF), falling back to upload time.
- From a gallery, the user opens an image to a **large detail view**.
- Galleries are always paginated and must remain responsive at full corpus size.

## Privacy

- All images and tags are strictly private to their owner.
- Images are **never** served as public static files. Every image and
  derivative is delivered through an **authorized endpoint** that verifies
  ownership before streaming the file.
- Ownership is enforced in the data layer as well: every query is scoped by the
  owning user (defense in depth, not only via storage layout).

## Scale Expectations

- ~25,000 images at launch; ~50 new images per week (~2,600 / year).
- Relational data stays small even at scale (images plus a many-to-many tag join
  of a few hundred thousand rows at most). The database is not the bottleneck.
- The performance focus is on image handling: precomputed thumbnails, paginated
  galleries, streamed (not in-memory) file serving, and aggressive HTTP caching
  of derivatives.

## Out of Scope (initial)

- Sharing, collaboration, or any cross-user visibility.
- Indexing a pre-existing on-disk folder: images enter **only** through the
  application's upload flow.
- Mobile-first / fully responsive UI.
- A shared Armali SSO service (kept as a future possibility, not built now).
- AI / automatic tagging, face recognition, and similar enrichment.
- Albums / hierarchies beyond tag-based navigation.
