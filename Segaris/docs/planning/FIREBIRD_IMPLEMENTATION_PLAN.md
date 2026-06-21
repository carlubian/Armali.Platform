# Firebird Implementation Plan

## Purpose

This plan delivers the initial Firebird module defined in
`docs/requirements/FIREBIRD_REQUIREMENTS.md`. It translates the accepted functional
decisions into dependency-ordered Waves with explicit backend, frontend, migration,
and test work.

The requirements document remains authoritative for behaviour. This plan owns
delivery order and technical scope.

## Delivery Principles

- Keep Firebird an independent business module: it consumes only Identity,
  Attachments, the Configuration presentation boundary, and platform contracts, and
  it does not depend on or integrate with any other business module.
- Reuse established Configuration, Attachments, privacy, REST, pagination,
  launcher-attention, and frontend conventions where their semantics match.
- Keep the birthday a year-less month/day value end to end: store month and day,
  validate them as an all-or-nothing pair, sort in calendar order, and derive the
  next occurrence only for attention.
- Keep the avatar a single optional image owned by the person through the shared
  attachment storage, replaced atomically and cleaned up on person deletion.
- Inherit person visibility and authorization for usernames, interactions, and the
  avatar; never disclose private people through not-found behaviour.
- Keep each Wave independently testable and record deferrals in `ROADMAP.md` instead
  of hiding them in implementation notes.

## Fixed Technical Contracts

### Backend Module

Firebird lives under `Segaris.Api.Modules.Firebird` and owns the `Person`,
`Username`, and `Interaction` entities, the `PersonCategory` and `UsernamePlatform`
catalogues, the birthday calendar logic, the avatar attachment authorization, and
the launcher-attention contributor. It consumes the Configuration presentation
boundary and platform contracts. It does not depend on any other business module,
and no business module depends on Firebird.

The interface name is `Firebird`; the primary REST resource is `people`. Indicative
resource routes are:

```text
GET    /api/people
POST   /api/people
GET    /api/people/{personId}
PUT    /api/people/{personId}
DELETE /api/people/{personId}

GET    /api/people/{personId}/avatar
PUT    /api/people/{personId}/avatar
DELETE /api/people/{personId}/avatar

GET    /api/people/{personId}/usernames
POST   /api/people/{personId}/usernames
PUT    /api/people/{personId}/usernames/{usernameId}
DELETE /api/people/{personId}/usernames/{usernameId}

GET    /api/people/{personId}/interactions
POST   /api/people/{personId}/interactions
PUT    /api/people/{personId}/interactions/{interactionId}
DELETE /api/people/{personId}/interactions/{interactionId}

GET    /api/people/categories
GET    /api/people/platforms
```

The avatar `PUT` uploads or replaces the single image; `DELETE` clears it.
Administrative catalogue routes for `PersonCategory` and `UsernamePlatform` follow
the existing module-owned catalogue management pattern exposed through Configuration.

All writes require antiforgery. Missing and inaccessible records share the platform
not-found behaviour so private data is not disclosed.

### Persistence

The model contains module-owned entities with provider-specific migrations:

- `Person`
- `Username`
- `Interaction`
- `PersonCategory`
- `UsernamePlatform`

`Person` stores the name, the required category reference, the status, the optional
`BirthdayMonth` and `BirthdayDay` pair, optional notes, the optional avatar
attachment reference, visibility, and standard audit metadata. `Username` stores the
required platform reference, the handle value, optional notes, the parent person
reference, and standard metadata. `Interaction` stores the civil date, the
description, the parent person reference, and standard metadata. `PersonCategory` and
`UsernamePlatform` each store a name and an order.

Owned usernames, interactions, and the avatar are removed when their person is
deleted.

Indexes must support person filters (category, status, creator, visibility),
deterministic name sorting, birthday calendar sorting (month then day, nulls last,
then identifier), the attention query (accessible people with a birthday), username
and interaction reads by parent person (interactions ordered by date descending),
and catalogue reference migration.

### Frontend Route

Firebird uses the protected lazy route `/people`.

The initial UI presents a server-paginated avatar gallery with URL-backed list state
and dialog state, following the Clothes gallery pattern, plus dedicated username and
interaction popups following the Projects risk-table pattern. One practical route
shape is:

```text
/people
/people?personId=123
/people?newPerson=true
/people?personId=123&usernames=true
/people?personId=123&interactions=true
```

If the shared shell or router ergonomics suggest a slightly different parameter
layout, preserve the same behaviour: gallery state must survive dialog open and
close without a reload, and the username and interaction popups must be reachable
from the person editor without collapsing the person context.

The frontend ships English strings under a `firebird` i18n namespace prepared for
future translation; no Spanish translations are included in this version.

### Configuration Integration

Configuration presents the Firebird catalogues alongside the other module-owned
catalogues. Firebird owns `PersonCategory` and `UsernamePlatform` while exposing them
through the established Configuration presentation boundary.

Because both a category and a platform are required on their referencing entities, a
referenced value may only be **replaced**; replacement re-points the affected people
or usernames to the target value.

## Waves

### Wave 0: Contracts And Test Skeleton

Establish stable contracts before persistence or UI work begins.

Tasks:

1. Add the Firebird module shell and registration.
2. Freeze the person, avatar, username, and interaction routes; the status values
   (`Unknown`/`Active`/`Unavailable`/`Blocked`); the visibility values; the
   year-less birthday contract (month/day, all-or-nothing, calendar ordering,
   next-occurrence attention rule) as documented domain contracts; DTOs; query
   contracts; stable error codes; the attachment owner kind (`Person`); and the
   launcher attention key.
3. Define Configuration-facing contracts for `PersonCategory` and `UsernamePlatform`
   reference handling without exposing Firebird entities.
4. Define frontend API, validation-schema, route-state, and query-key skeletons, and
   the `firebird` i18n namespace.
5. Add architecture-test expectations: Firebird may consume Configuration,
   Attachments, Identity, and platform contracts but must not depend on Capex, Opex,
   Inventory, Travel, Assets, Maintenance, Clothes, Mood, Projects, or Processes, and
   no module depends on Firebird.
6. Add empty backend and frontend test fixtures shared by later Waves.

Tests:

- Unit tests for status and visibility values; route constants; query bounds; the
  birthday validation and calendar-ordering helpers; and error-code stability.
- Architecture tests for permitted dependencies and the Firebird non-dependency
  rules.

Exit criteria:

- Public contracts are explicit and test-covered, and later Waves do not need to
  invent new route, ownership, status, or birthday semantics.

### Wave 1: Domain, Persistence, And Catalogues

Implement the Firebird data model and module-owned catalogues on both providers.

Tasks:

1. Add `Person`, `Username`, `Interaction`, `PersonCategory`, and `UsernamePlatform`
   with the parent relationships.
2. Enforce the required category and platform relationships, bounded non-whitespace
   strings, known status and visibility values, the all-or-nothing month/day
   birthday with month 1–12 and a valid day (February allowing 29), the non-future
   interaction date, and standard audit metadata.
3. Implement the birthday domain logic as pure, unit-testable functions: validation,
   calendar ordering (month then day), and the next-occurrence computation for
   attention including the year wrap and the `02-29` non-leap-year observation.
4. Seed the accepted initial catalogue values once (`PersonCategory`: `Family`,
   `Friend`, `Colleague`, `Acquaintance`, `Other`; `UsernamePlatform`: `Email`,
   `Phone`, `Discord`, `Twitter`, `Instagram`, `Other`).
5. Implement module-owned catalogue reads plus administrator mutations through
   Configuration, including ordering and final-row protection, for both catalogues.
6. Add provider-specific migrations and model snapshots.
7. Add indexes for person filters, name sorting, birthday calendar sorting, the
   attention query, username and interaction reads by parent, and catalogue
   reference migration.

Tests:

- Domain tests for birthday validation (valid, partial, out-of-range, `02-29`),
  calendar ordering with nulls last, and next-occurrence computation across the year
  wrap and non-leap-year `02-29`.
- SQLite and PostgreSQL migration tests, including upgrades and idempotent catalogue
  initialization.
- Integration tests for catalogue ordering, final-row protection, and administrator
  authorization for both catalogues.

Exit criteria:

- Both providers persist the complete model, the birthday logic is correct and
  isolated, and both catalogues are configurable.

### Wave 2: Person Read And Mutation APIs

Deliver the person-level backend contract, excluding sub-entities and the avatar.

Tasks:

1. Implement the paginated gallery query and the person detail query, projecting the
   category, status, birthday, and avatar presence.
2. Implement partial search across name (and notes where practical).
3. Implement exact filters for category, status, creator, and visibility, and
   deterministic sorting for name and birthday (birthday in January-to-December
   calendar order with nulls last, then identifier), with name ascending as the
   default.
4. Implement create, update, and delete for people with full validation, the
   documented creation defaults, and ownership/audit metadata. A person may be
   created with no birthday, avatar, usernames, or interactions.
5. Enforce category validity, visibility transitions, creator-only visibility change,
   and standard public-collaboration and private-isolation authorization.

Tests:

- API integration tests for pagination, filters, search, sorting (including the
  birthday calendar ordering and nulls-last behaviour), defaults, required fields,
  visibility isolation, and not-found privacy behaviour.
- Two-user privacy tests for public collaboration and private isolation.

Exit criteria:

- Users can browse and fully manage accessible people through the backend, with
  correct validation and privacy, without sub-entities or the avatar.

### Wave 3: Usernames, Interactions, And Avatar

Deliver the person's sub-entities and the single avatar image.

Tasks:

1. Implement username listing and create/update/delete with platform validation,
   bounded handle and notes, and repeated-platform support.
2. Implement interaction listing ordered by date descending and create/update/delete
   with non-future date validation and a bounded description.
3. Implement the avatar upload/replace/delete routes using the shared attachment
   policies, accepting only image content and storing a single image with cleanup of
   the replaced or deleted file.
4. Inherit person visibility and authorization for usernames, interactions, and the
   avatar, and remove all of them when the person is deleted.

Tests:

- API integration tests for username CRUD including repeated platforms and
  validation failures; interaction CRUD including descending ordering and the
  non-future-date rule; avatar upload, replacement, download, deletion, content-type
  rejection, and filesystem cleanup on person deletion.
- Two-user tests confirming sub-entity and avatar actions follow the person's
  public-collaboration and private-isolation rules.

Exit criteria:

- People own usernames, a chronological interaction log, and a single avatar that all
  respect person visibility and are cleaned up on deletion.

### Wave 4: Launcher Attention

Deliver the upcoming-birthday launcher attention.

Tasks:

1. Implement the Firebird attention contributor: attention is true when the current
   user can access at least one person whose next birthday occurrence is within the
   inclusive window from today to today plus seven natural days in `Europe/Madrid`,
   wrapping across the year boundary, with `02-29` observed on `03-01` in non-leap
   years.
2. Reuse the established launcher attention aggregation and accessibility rules so
   only accessible people count and private people are never disclosed.
3. Expose the boolean attention state through the launcher contract.

Tests:

- Integration tests for the attention window boundaries (today, today plus 7,
  just-past, and far-future birthdays), the year-wrap case, the `02-29`
  non-leap-year case, missing-birthday exclusion, and accessibility filtering across
  two users.

Exit criteria:

- Firebird attention is true exactly under the documented condition and never leaks
  private data.

### Wave 5: Firebird Gallery And Person Editor

Build the user-facing avatar gallery and the person editor dialog.

Tasks:

1. Add the lazy `/people` route, module error boundary, the `firebird` translation
   namespace, and a launcher card wired to the attention state.
2. Build the server-paginated avatar gallery with URL-backed search, category and
   status filters, name/birthday sorting, and bounded pagination, including the
   avatar (or placeholder), name, category, status, and birthday on each card.
3. Build the person dialog with React Hook Form and Zod, covering name, category,
   status, the day/month birthday control, notes, visibility guards, and the avatar
   upload/replace/clear control, plus entry into the username and interaction popups.
4. Wire deletion confirmation and list invalidation, preserving gallery state across
   dialogs.

Tests:

- Frontend API and component tests for route state, filters, sorting, pagination,
  person validation, the birthday control, avatar upload/replace/clear, privacy-safe
  errors, and gallery rendering.
- Accessibility tests for dialog focus, keyboard operation, and error association.

Exit criteria:

- Users can complete the full person-level workflow without page reloads while
  preserving gallery state.

### Wave 6: Sub-Entity Popups And Configuration Frontend

Surface the username and interaction popups and the Firebird catalogues.

Tasks:

1. Build the usernames popup: list usernames with platform and value, and add, edit,
   and remove them with platform selection and privacy-safe error feedback.
2. Build the interactions popup: list interactions in descending date order, and add,
   edit, and remove them with date and description fields.
3. Add the Firebird sections to the Configuration UI for `PersonCategory` and
   `UsernamePlatform`, including reorder controls and the replacement dialog with a
   privacy-neutral impact summary.
4. Invalidate the relevant Firebird and Configuration caches after mutations.

Tests:

- Component tests for the username and interaction CRUD flows and their validation,
  descending interaction ordering, category and platform CRUD, reorder, the
  replacement dialog, and cache invalidation.
- Accessibility tests for the popup focus, keyboard operation, and error association.

Exit criteria:

- Users can manage usernames and interactions entirely through the UI, and
  administrators can manage both catalogues with safe, privacy-neutral replacement.

### Wave 7: End-To-End, Hardening, And Acceptance

Validate the implemented behaviour across both providers and the deployed
frontend/backend boundary.

Tasks:

1. Run backend format, build, unit, API, PostgreSQL, migration, and architecture
   suites through repository scripts.
2. Run frontend format, lint, type-check, unit, build, and Playwright suites.
3. Add a representative Playwright journey covering login, opening Firebird, creating
   a person with a category, a status, and a day/month birthday, uploading an avatar,
   adding usernames and interactions, filtering and sorting the gallery, deleting safe
   test data, and managing both catalogues with a replacement in Configuration.
4. Review OpenAPI for the person, avatar, username, interaction, and catalogue routes.
5. Verify keyboard behaviour, dialog and popup scrolling, filtered gallery
   invalidation, and narrow desktop widths.
6. Map every criterion in `docs/requirements/FIREBIRD_REQUIREMENTS.md` to covering
   code and tests in a Firebird acceptance record.
7. Update `ROADMAP.md` to mark Firebird as implemented and accepted and to record only
   intentional remaining deferrals.

Exit criteria:

- All relevant repository scripts pass, both providers are covered, the Compose
  journey succeeds, and every accepted Firebird requirement is implemented or
  explicitly deferred.

## Suggested Pull Request Boundaries

1. Firebird contracts, persistence, and module-owned catalogues (Waves 0-1).
2. Person read and mutation APIs (Wave 2).
3. Usernames, interactions, and avatar (Wave 3).
4. Launcher attention (Wave 4).
5. Firebird gallery, person editor, sub-entity popups, and Configuration frontend
   (Waves 5-6).
6. End-to-end, hardening, and acceptance (Wave 7).

## Plan Completion Criteria

The initial implementation plan is complete through Wave 7 when the Firebird
requirements document describes implemented behaviour rather than only functional
intent.

A standalone reminder entity, free-form reminders, birthday years, recurring events,
interaction types or attachments, additional person attachments, username URLs or
primary flags, relationships or links between people, Entity Link integration, and
Analytics or Calendar integration remain separate future planning topics.
