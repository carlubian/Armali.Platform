# Images: storage, processing and serving

How an uploaded photo becomes bytes on a volume, a row in PostgreSQL and three
endpoints. The privacy rules behind it are in [`identity.md`](identity.md); the
entities are in [`backend.md`](backend.md).

## Content-addressed storage

The volume root is `Blackwing:Storage:ImagesPath` (`/data/images` in the
container). Inside it:

```
{ownerUserId}/{hash[0..2]}/{hash[2..4]}/{hash}                 the original, untouched
{ownerUserId}/{hash[0..2]}/{hash[2..4]}/{hash}.thumb.webp      small derivative
{ownerUserId}/{hash[0..2]}/{hash[2..4]}/{hash}.preview.webp    large derivative
.staging/{stagingId}.upload                                     uploads in progress
```

`hash` is the SHA-256 of the original content in lowercase hexadecimal. The
database stores the hash and the metadata and **never a path**, so the physical
layout can evolve behind `IImageBlobStore` without touching a row.

Why addressed by content:

- An exact duplicate is detected from the hash alone, before any decoding.
- The name of a file can never collide with another, and two uploads of the same
  bytes by the same account can only mean one thing.
- If the bytes change it is a different image with a different identifier, which
  is what makes the strong ETag below correct by construction.

Why **per owner**: the milestone chose to duplicate bytes between accounts rather
than share them. Two accounts uploading the same photo get two rows and two copies
on disk. That costs disk and buys the property that no operation on one account's
content can ever touch another's. A fan-out of two levels of two hexadecimal
characters keeps any directory far below tens of thousands of entries at the
corpus size this application is built for.

`IImageBlobStore` (in `Blackwing.Shared`) is the contract and
`FileSystemImageBlobStore` (in `Blackwing.Api`) the implementation. Only the
implementation knows the layout above. The path calculation is a pure static method
so it is tested without touching a disk.

### The original is never transformed

The original is written byte for byte as uploaded: no recompression, no EXIF
stripping, no rotation. A download is exactly what was uploaded, and the EXIF block,
orientation included, is still in it. Everything that has to be *interpreted*
happens in the derivatives and in the stored dimensions.

### Nothing is ever held whole in memory

`StageAsync` copies the request body to `.staging` in 64 KB chunks, hashing as it
goes, and **gives up the moment the limit is crossed**: a 413 is raised and the rest
of the upload is never read. That is a constraint of the whole milestone, not an
optimisation. The upload endpoint reads the multipart body part by part for the same
reason, instead of letting the framework buffer it to a temporary file first.

Writes are tidy about failure. A derivative is written to a temporary name and
moved into place; if promoting an image fails midway, whatever it already wrote is
removed before the error propagates; if the database insert fails after the files
are down, the files are removed too. The single exception is the duplicate race
described below.

## Ingestion

`ImageIngestionService.IngestAsync` is the whole path, and the one entry point the
phase 4 background worker will call:

1. Validate the file name and the declared content type against the extension.
2. Stage the body, hashing it. Over the limit: 413.
3. Check the leading bytes against the format the extension promises. A `.jpg` that
   is really a PNG: 400.
4. Look for an existing row with the same hash. The global filter has already
   narrowed the query to the current account, so no owner predicate is written.
   Found: discard the staged file and report a duplicate (409 from the endpoint).
5. Read the capture date from EXIF and produce the derivatives.
6. Promote the original and the derivatives into the volume.
7. Insert the row, `ReviewState = Pending`. The contract takes no owner; the context
   stamps it.

**Trust sits in the bytes, not in the name.** Only JPEG, PNG and WebP are accepted,
recognised by their leading bytes. A name or a declared content type that
contradicts them is refused. HEIC/HEIF is out of scope for the milestone.

**Two simultaneous uploads of the same file** cannot create two rows: the unique
index `IX_images_owner_content_hash` is the arbiter, and the loser gets a 409. The
loser deliberately does **not** delete the files it wrote, because they are
content-addressed and therefore the very ones the winner's row points at.

## Libraries and licences

| Job | Library | Licence |
| --- | --- | --- |
| Decode, orient, resize, encode WebP | SkiaSharp (+ `SkiaSharp.NativeAssets.Linux.NoDependencies`) | MIT, with BSD-3 for Skia itself |
| Read EXIF | MetadataExtractor | Apache 2.0 |

Both are permissive and carry no revenue threshold. ImageSharp was rejected for
exactly that threshold, and Magick.NET for roughly 40 MB of native binaries. The
`NoDependencies` native assets keep the runtime image free of `libfontconfig`,
which Blackwing never needs because it never draws text; the `Dockerfile` needed no
new `apt-get` line.

Both are **confined to `Blackwing.Api`**: `SkiaImageProcessor` is the only class
that knows SkiaSharp, and `ImageMetadataReader` the only one that knows
MetadataExtractor. An architecture test fails if `Blackwing.Shared` or
`Blackwing.Persistence` ever reference either.

Skia's memory is native, so the garbage collector cannot see it. Every codec,
bitmap, image and data object is in a `using`; a leak would show up weeks later as a
container that grows until the kernel kills it. As a guard against decompression
bombs, an image of more than 300 megapixels is refused as undecodable.

## Derivatives

| Variant | Longest edge | WebP quality |
| --- | --- | --- |
| `thumb` | 400 px | 75 |
| `preview` | 1600 px | 82 |

Configurable through `Blackwing:Images`. **Nothing is scaled up**: an original
smaller than the target is encoded at its native size. Where the codec can decode
at a reduced size natively (JPEG at 1/2, 1/4 or 1/8) it does, which bounds the peak
of native memory on large originals.

### EXIF orientation and the stored dimensions

EXIF orientation is applied to the derivatives and to the dimensions stored in
`images`, and **never** to the original. A portrait photo taken with the sensor
turned, stored as 4000 x 3000 with orientation 6, is recorded as 3000 x 4000 and its
derivatives are upright. All eight orientations are covered by a test that tracks a
marker through each transformation.

EXIF carries no time zone, so the capture date is read as **UTC** and stored as such.
That is deterministic, and ordering is the only thing the date is used for. A file
without a usable date has `CapturedAt = null`, and `SortedAt` falls back to the
upload time.

## Serving

`GET /api/images/{id}/thumb`, `/preview` and `/original`. Image files are **never**
served as static files: every byte goes through `IImageBlobStore` after the row has
been found for the current account.

- The row is looked up first, and that lookup is the ownership check. Another
  account's image is not in the result: **404, never 403**.
- A row whose file is missing is **503**, not 404.
- `Cache-Control: private, immutable, max-age=31536000`, plus a **strong ETag**
  derived from the hash and the variant. `If-None-Match` is answered with 304 and
  no body.
- `original` also supports range requests and is offered as a download under its
  original file name.
- The response is a stream, never a buffered body.

The session cookie would normally defeat that cache policy. See *Sessions* in
[`identity.md`](identity.md): these routes opt out of the cookie renewal that
overwrites `Cache-Control`, while still validating the session on every request.

## Deleting

The row goes first, and its `image_tags` with it through the cascade; the files go
second. In the other order, a failure would leave a live row pointing at a file that
no longer exists, which is worse than a file nobody points at. If deleting the files
fails after the row is gone, it is logged and the request still succeeds.

## What this deliberately does not do

- **There is no reconciliation of orphan files** on the volume, no trash and no
  undelete. An orphan can only appear after a crash between the files and the row,
  or after a failed file deletion, and costs disk and nothing else.
- **The volume is persistent data that another application backs up**, together
  with PostgreSQL. Backups are outside the scope of this milestone.
- The batch endpoint, the persisted queue and the background worker are phase 4.
  `POST /api/images` is synchronous and takes one file; phase 4 wraps this path, it
  does not rewrite it.
