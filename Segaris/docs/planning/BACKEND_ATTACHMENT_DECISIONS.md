# Backend Attachment Decisions

## Purpose

This document records the Wave 5 attachment security, storage, ownership, and recovery decisions.

## Security Policy

- Each attachment is limited to 25 MiB. The HTTP request and copied stream are both bounded.
- Permitted images are JPEG, PNG, and WebP.
- Permitted documents are PDF, DOCX, XLSX, PPTX, ODT, ODS, and ODP.
- Permitted text formats are TXT, CSV, Markdown, XML, JSON, YAML, and YML.
- Executables, scripts, legacy Office binary formats, and generic archives are not accepted.
- The extension, declared media type, and actual content must agree. Binary formats use magic bytes or required package entries. JSON and XML are parsed; XML prohibits DTD processing and external resolution. Other text formats must be valid text without null bytes.
- The original filename is normalized, length-bounded, stored only as metadata, and never used to construct the physical path.
- Physical files use generated UUID names below a normalized module directory.
- Malware scanning is not included in the initial trusted-household deployment. The allow-list and structural validation reduce exposure but do not establish that a file is malware-free. A scanner may be inserted at the validation boundary before broader or remote exposure.

## Ownership And Authorization

The attachment service stores an explicit module, entity type, and entity identifier. These values identify the owning record but do not grant access. The owning module must authorize access to its record before calling the attachment service and must pass the same owner reference for metadata, download, and deletion. A mismatched owner returns no attachment, preserving private-record non-disclosure.

The platform does not expose a production generic attachment API. Wave 5 includes Testing-only probe endpoints so the complete storage and HTTP behavior can be exercised without inventing a business domain before Phase 2.

## Consistency And Recovery

- Creation writes and validates a staging file, atomically moves it to its UUID path, then inserts metadata. A database failure deletes the moved file.
- Deletion first moves the live file to a trash area, then deletes metadata. A database failure restores the file. A final trash-cleanup failure is logged and detected by reconciliation.
- Reconciliation reports database records with missing files, unreferenced live files, interrupted staging files, and residual trash files. It does not delete evidence automatically.
- Readiness creates and removes a small probe file, so a missing directory is created when possible and an inaccessible or read-only location reports unhealthy.

## Configuration

`Segaris:Storage:AttachmentsPath` selects the storage root. It is required in Production and defaults to a temporary environment-specific location when omitted outside Production. Production Compose will bind this path to the documented persistent attachments directory in Wave 8.
