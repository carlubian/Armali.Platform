# Firebird Requirements

## Status

Phase 2 functional definition is complete. This document is the functional
source of truth for the initial Firebird implementation plan.

## Purpose

Firebird is a personal register of known people. The household records the people
it knows, the identities those people use across services, and a chronological
log of interactions with them. The module also surfaces upcoming birthdays so the
household is reminded of them in time.

`Firebird` is the interface name of the module. Its primary REST resource is
`people` (`/api/people`), chosen for code clarity.

The initial module is a structured contact register. It does not manage messaging,
schedules, free-form reminders, relationships between people, or any cross-module
reference. Firebird is an independent business module; it does not integrate with
any other business module in this version.

## Initial Scope

- Maintain a `Person` register with a name, a required category, a fixed status, a
  day/month birthday, free-text notes, an optional avatar image, and visibility.
- Own the `PersonCategory` and `UsernamePlatform` catalogues, managed through
  Configuration with replace-only deletion.
- Let each person own a list of `Username` sub-entities (a platform plus a handle
  value) and a chronological list of `Interaction` sub-entities (a date plus a
  description).
- Present the register as a server-paginated avatar gallery with search, category
  and status filters, and name/birthday sorting.
- Manage usernames and interactions through dedicated URL-aware popups from the
  person editor.
- Raise a launcher attention signal when an accessible person has a birthday within
  the next seven natural days.

## Excluded Scope

The initial Firebird implementation excludes:

- A standalone reminder entity or any free-form, non-birthday reminders.
- Birthday years; only the day and month are stored and shown.
- Recurring-event modelling beyond the derived next-birthday occurrence.
- Username URLs, a preferred/primary username flag, and per-username verification.
- Interaction types, categories, or attachments.
- Multiple person attachments; a person carries at most one avatar image and no
  other files.
- Relationships, links, or groupings between people.
- Any cross-module reference or Entity Link integration (Maintenance, Projects, and
  others).
- Analytics or Calendar integration.
- Spanish translations; the module ships English strings under an i18n namespace
  prepared for future translation.

## Person

A `Person` contains:

- A required name.
- A required `PersonCategory` reference.
- A required status.
- An optional day/month birthday.
- Optional free-text notes.
- An optional avatar image.
- Zero or more usernames.
- Zero or more interactions.
- Visibility.
- Standard ownership and audit metadata.

## Status

Every person has one of these fixed statuses:

- `Unknown`
- `Active`
- `Unavailable`
- `Blocked`

The status is descriptive and manually controlled. It is a fixed enum, not managed
through Configuration, and it blocks no operation by itself. New people default to
`Unknown`.

## Birthday

The birthday is optional and stores only a **month and day**, never a year. When
present, both the month and the day are set together; a partial birthday is not a
valid state.

- The month is an integer in the inclusive range 1–12.
- The day is an integer valid for that month, ignoring the year. February allows up
  to day 29, so `02-29` is a storable birthday.
- The UI presents the birthday as day and month only.

For ordering and attention the birthday is interpreted as a recurring calendar
occurrence:

- **Sorting** by birthday uses pure calendar order from January to December (month
  ascending, then day ascending), independent of the current date, with people who
  have no birthday sorted last and `id` as the final tie-breaker.
- **Attention** uses the next upcoming occurrence relative to today in
  `Europe/Madrid` (see `Attention`). A `02-29` birthday is observed on `03-01` in
  non-leap years for attention purposes.

## Avatar

- A person carries at most one optional avatar image, stored through the shared
  platform attachment storage with the owner kind `Person`.
- Only image content types are accepted, under the shared attachment size and
  policy bounds.
- Uploading a new avatar replaces any existing one; the previous file is cleaned up.
- The avatar is shown on the gallery card; a person without an avatar shows a
  placeholder.
- A person has no other attachments.

## Username

Each person owns zero or more usernames. A `Username` contains:

- A required `UsernamePlatform` reference.
- A required handle value, trimmed and non-whitespace.
- Optional free-text notes.
- An identifier and standard persistence metadata.

A person may hold several usernames, including more than one on the same platform.
There is no URL and no preferred/primary flag. Usernames inherit the visibility and
authorization of their owning person, and they are removed when the person is
deleted.

## Interaction

Each person owns zero or more interactions forming a chronological log. An
`Interaction` contains:

- A required interaction date (a civil date; the past and today are allowed, and it
  defaults to today on creation).
- A required description (the interaction's only free text), trimmed and
  non-whitespace.
- An identifier and standard persistence metadata.

Interactions are listed in descending date order, with `id` as the tie-breaker so
the most recent entries appear first. There is no interaction type, category, or
attachment. Interactions inherit the visibility and authorization of their owning
person, and they are removed when the person is deleted.

## Visibility And Authorization

People use the platform-standard visibility values:

- `Public`
- `Private`

New people default to `Public`. The standard Segaris baseline applies:

- A user can view and edit their own people and public people.
- A private person remains creator-only, including from administrators.
- Any authenticated user may edit a public person.
- Only the creator may change a person's visibility.

Usernames, interactions, and the avatar inherit the visibility and authorization of
their owning person. These constraints are enforced by the backend regardless of
the client. Missing and inaccessible people share the platform not-found behaviour
so private data is not disclosed.

## Catalogues And Configuration Integration

Firebird owns two catalogues, both presented alongside the other module-owned
catalogues through the established Configuration presentation boundary:

- `PersonCategory`: a required name and an order. Because every person requires a
  category, a referenced value may only be **replaced**; replacement re-points the
  affected people to the target value.
- `UsernamePlatform`: a required name and an order. Because every username requires
  a platform, a referenced value may only be **replaced**; replacement re-points the
  affected usernames to the target value.

Administrator CRUD, ordering, final-row protection, and the replacement dialog with
a privacy-neutral impact summary follow the established module-owned catalogue
pattern.

Accepted initial catalogue values, seeded once:

- `PersonCategory`: `Family`, `Friend`, `Colleague`, `Acquaintance`, `Other`.
- `UsernamePlatform`: `Email`, `Phone`, `Discord`, `Twitter`, `Instagram`, `Other`.

## Attention

The Firebird launcher card requests attention when the current user can access at
least one person whose birthday falls within the inclusive window from today to
today plus seven natural days in `Europe/Madrid`.

- Only upcoming birthdays count; a birthday earlier in the window's start day is not
  considered past, but birthdays that already occurred earlier in the year are not
  retroactively flagged — the comparison uses the next upcoming calendar occurrence.
- The window wraps across the year boundary, so on 28 December a birthday on
  3 January is within range.
- People without a birthday, and other users' private people, never contribute to
  attention.
- A `02-29` birthday is observed on `03-01` in non-leap years for this computation.

The launcher exposes only the platform-standard boolean attention state.

## Module Entry And Navigation

Opening Firebird shows a server-paginated **avatar gallery** of accessible people.
Each card shows the avatar (or a placeholder), the name, the category, the status,
and the birthday when present.

- Search matches the person name (and notes where practical).
- Filters cover category and status.
- Sorting covers name and birthday, with the documented birthday calendar ordering.
- People are created, viewed, edited, and deleted through the established Segaris
  URL-aware popup pattern, so gallery state survives dialog open and close without a
  reload.
- A person's usernames and interactions are each managed through a further dedicated
  popup reached from the person editor, following the Projects risk-table pattern.

Indicative frontend route shapes:

```text
/people
/people?personId=123
/people?newPerson=true
/people?personId=123&usernames=true
/people?personId=123&interactions=true
```

## Validation

- Person name is required, trimmed, not whitespace-only, and at most 200 characters.
- The person category reference is required and must exist.
- Person status and visibility are known values.
- The birthday is either absent or a complete, valid month/day pair (month 1–12, day
  valid for the month with February allowing 29).
- Person notes are optional and at most 2,000 characters.
- The avatar, when present, is a single image within the shared attachment policy
  bounds.
- Username platform reference is required and must exist; the handle value is
  required, trimmed, not whitespace-only, and at most 200 characters; username notes
  are optional and at most 1,000 characters.
- Interaction date is required and is not in the future; the description is required,
  trimmed, not whitespace-only, and at most 2,000 characters.
- Catalogue names are required, trimmed, not whitespace-only, and at most 200
  characters.

## Creation Defaults

A new person starts with:

- Status `Unknown`.
- Visibility `Public`.
- No birthday.
- No avatar.
- No usernames.
- No interactions.

A new interaction defaults its date to today. A new username has no notes by
default.

## Acceptance Criteria

The initial Firebird definition is satisfied when:

1. A `Person` carries a required name, a required `PersonCategory`, a fixed status, an
   optional day/month birthday, optional notes, an optional single avatar, and
   visibility, with standard ownership and audit metadata.
2. The statuses `Unknown`, `Active`, `Unavailable`, and `Blocked` are available,
   descriptive, default to `Unknown`, and block no operation by themselves.
3. The birthday stores only month and day, is all-or-nothing, accepts `02-29`, sorts
   in January-to-December calendar order independent of the current date, and is
   shown as day and month only.
4. A person carries at most one avatar image accepted as image content, replaced on
   re-upload with cleanup, shown on the gallery card, and has no other attachments.
5. A person owns zero or more usernames, each with a required `UsernamePlatform` and
   a required handle value plus optional notes, allowing repeated platforms, with no
   URL or primary flag.
6. A person owns zero or more interactions, each with a required non-future date and
   a required description, listed in descending date order, with no type or
   attachment.
7. Firebird owns the `PersonCategory` and `UsernamePlatform` catalogues through
   Configuration, both required and replace-only, seeded with the accepted initial
   values.
8. Visibility follows the Segaris public-collaboration and private-isolation
   baseline, defaults to `Public`, is changed only by the creator, and is inherited
   by usernames, interactions, and the avatar; inaccessible people return the
   standard not-found behaviour.
9. The launcher attention is true exactly when the current user can access a person
   whose next birthday occurrence is within today through today plus seven natural
   days in `Europe/Madrid`, wrapping across the year boundary, never counting
   missing birthdays or other users' private people.
10. Firebird opens on a server-paginated avatar gallery with name search, category
    and status filters, and name/birthday sorting, with person editing and the
    username and interaction popups using URL-aware dialogs that preserve gallery
    state.
11. SQLite and PostgreSQL migrations, backend unit/integration/architecture tests,
    frontend component tests, and a representative Playwright journey verify the
    supported behaviour and privacy boundaries.

## Deferred Decisions

- Whether Firebird should gain a standalone reminder entity or free-form,
  non-birthday reminders, and whether birthdays should later store a year.
- Whether interactions should gain a configurable type, attachments, or richer
  structure.
- Whether usernames should gain a URL, a preferred/primary flag, or verification.
- Whether people should support multiple attachments beyond a single avatar.
- Whether people should be referenceable from other modules through Entity Link, or
  gain relationships, groupings, or links between people.
- Whether Firebird should publish read contracts to Analytics or project birthdays
  into a Calendar module.
