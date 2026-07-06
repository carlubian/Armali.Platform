# Games Requirements

## Status

Phase 2 functional definition is complete. This document is the functional
source of truth for the initial Games implementation plan.

## Purpose

Games records the household's progress through video games, board games,
tabletop campaigns, and similar entertainment. A `Game` is an administrator
managed catalogue entry, while a `Playthrough` is the user-owned run or save file
where progress is tracked.

The initial module is intentionally focused on structured progress tracking. It
does not model achievements, time played, ratings, screenshots, external game
stores, calendar events, or integrations with other Segaris modules.

## Initial Scope

- Manage a `Game` catalogue through Configuration, with a required name and a
  fixed platform enum.
- Manage `Playthrough` records as the module's primary user-facing entity, each
  linked to one `Game`.
- Track each playthrough's required name, required start month and year, manual
  status, free-text tags, visibility, and derived progress.
- Let each playthrough own ordered `Section` records used to group progress by
  theme.
- Let each section own `Goal` records that can be quickly marked complete or
  incomplete.
- Present playthroughs as server-paginated cards with search, filters, sorting,
  URL-aware editing, and a dedicated progress page per playthrough.

## Excluded Scope

The initial Games implementation excludes:

- End dates, completion dates, time played, sessions, achievements, ratings, or
  review text.
- Attachments, screenshots, cover images, or external metadata import.
- Game store, publisher, genre, release date, or franchise metadata.
- Configurable platforms or a platform catalogue.
- Nested sections or sub-goals.
- Reordering goals after creation.
- Progress history, audit history beyond standard metadata, undo, or soft delete.
- Launcher attention, Analytics integration, or Calendar integration.
- Spanish translations; the module ships English strings under an i18n namespace
  prepared for future translation.

## Game Catalogue

`Game` is a module-owned catalogue presented through Configuration. Only
administrators can create, edit, reorder, or delete games through the management
surface. Authenticated users can read games through the purpose-specific APIs
needed by Games forms and filters.

A game contains:

- A required name.
- A required platform.
- An integer `SortOrder`.
- Standard catalogue audit metadata.

The fixed platform values are:

- `PC`
- `Console`
- `Mobile`
- `BoardGame`
- `TabletopRpg`
- `Other`

The platform set is not configurable in the initial release.

Game names are trimmed, required, not whitespace-only, at most 200 characters,
and unique case-insensitively within the catalogue. The same game name on a
different platform is still treated as a duplicate in the initial release; if
the household needs separate editions, the name should distinguish them.

There are no seeded games. The catalogue may be empty, although a playthrough
cannot be created until at least one game exists.

Deleting an unreferenced game is immediate after confirmation. Deleting a game
referenced by playthroughs requires replacing it with another game in the same
transaction, following the established module-owned catalogue replacement
pattern. Impact reporting is privacy-neutral and never discloses another user's
private playthroughs.

## Playthrough

A `Playthrough` represents one run, campaign, save file, or comparable tracked
instance of a game. A playthrough contains:

- A required name.
- A required `Game` reference.
- A required start year.
- A required start month.
- A manual status.
- A free-text tag list.
- Visibility.
- Standard ownership and audit metadata.

The linked game is required. If an administrator replaces a referenced game
through Configuration, existing playthroughs point at the replacement game and
retain all progress data.

### Start Month And Year

The start date is stored as two integers, not as a synthetic date:

- `StartYear`
- `StartMonth`

Both are required. `StartMonth` is an integer from 1 to 12. `StartYear` must be a
valid positive year accepted by the backend validation. The UI presents the value
as a month and year according to the current interface locale and household
regional rules.

The initial release does not record an end date or completion date.

### Status

Every playthrough has one of these fixed manual statuses:

- `Planning`
- `Active`
- `Completed`

The status is descriptive and manually controlled. It is a fixed enum, not
managed through Configuration. It does not imply any automatic changes or
validation against the goals: a playthrough may be marked `Completed` even when
some goals remain incomplete, and completing all goals does not change the
status.

New playthroughs default to `Planning`.

### Tags

Tags are free-text labels stored on the playthrough. They are not backed by a
catalogue and are not shared as first-class records.

On save, tags are normalized by trimming whitespace, removing empty values, and
deduplicating within the playthrough case-insensitively while preserving the
capitalization of the kept value. Tags are used for filtering and card display.

## Sections

A `Section` belongs to exactly one playthrough and groups goals by theme, such as
reputations, missions, houses, or collections. A section contains:

- A required name.
- A required highlight color.
- A `SortOrder` within its playthrough.
- Standard ownership and audit metadata.

A playthrough may contain zero or more sections. No default section is created
when a playthrough is created.

Section names are trimmed, required, not whitespace-only, at most 200 characters,
and unique case-insensitively within the same playthrough. The same section name
may be reused in different playthroughs.

Sections are shown in a one-level list on the left side of the playthrough
progress page. The main progress view does not expose drag-and-drop or inline
reordering. Section ordering is managed through a dedicated popup or management
mode.

### Section Color Palette

Section colors come from a fixed enum. The persisted value is the color token,
not a raw CSS or hex value.

Accepted values:

- `Blue`
- `Green`
- `Amber`
- `Red`
- `Purple`
- `Pink`
- `Teal`
- `Indigo`
- `Slate`
- `Orange`

The frontend maps these tokens to design-system-aware presentation styles.

## Goals

A `Goal` belongs to exactly one section. A goal contains:

- Required free-text content.
- A completion flag.
- A creation-order position.
- Standard ownership and audit metadata.

Goal text is trimmed, required, not whitespace-only, and at most 500 characters.
New goals default to incomplete.

Goals keep their creation order permanently in the initial release. Users cannot
reorder goals, and marking a goal complete or incomplete never changes its
position in the list. Completing and uncompleting goals should be a quick inline
action, such as a checkbox.

Deleting a goal is physical, immediate, and irreversible after confirmation.
There is no goal history.

## Derived Progress

Goals are the source of truth for progress. Section and playthrough progress are
computed on demand from current goals and are never persisted.

For a section:

- Total goals is the number of goals in that section.
- Completed goals is the number of goals whose completion flag is true.
- Progress is derived from completed goals over total goals.

For a playthrough:

- Total goals is the sum of goals across all of its sections.
- Completed goals is the sum of completed goals across all of its sections.
- Progress is derived from completed goals over total goals.

A section or playthrough with no goals exposes zero completed goals and zero
total goals. The UI may show this as an empty or neutral progress state rather
than a numeric percentage.

## Visibility And Authorization

Playthroughs use the platform-standard visibility values:

- `Public`
- `Private`

New playthroughs default to `Public`. The standard Segaris baseline applies:

- A user can view and edit their own playthroughs and public playthroughs.
- A private playthrough remains creator-only, including from administrators.
- Any authenticated user may edit a public playthrough, including its sections
  and goals.
- Only the creator may change a playthrough's visibility.

Sections and goals inherit the visibility and authorization of their owning
playthrough. A user who may access a playthrough may access and manage all of its
sections and goals. A user who may not access a playthrough cannot access any of
its children, and the backend returns the platform not-found behaviour so private
playthroughs are not disclosed.

Games are administrative catalogue records and are visible to authenticated
users through read APIs regardless of playthrough ownership.

## Attention

Games contributes no launcher attention signal. The launcher card never requests
attention.

## Module Entry And Navigation

Opening Games shows the playthrough collection first, presented as
server-paginated cards of accessible playthroughs.

Cards primarily surface:

- Game.
- Status.
- Derived global progress.

Cards secondarily surface:

- Platform.
- Start month and year.
- Tags.

The playthrough collection supports:

- Partial search by playthrough name and game name.
- Exact filters for game, platform, status, tags, creator, and visibility.
- Sorting by playthrough name, game name, start month/year, status, and derived
  progress.
- Bounded pagination following platform conventions.

Playthroughs are created, viewed, edited, and deleted through the established
Segaris URL-aware popup pattern, so card-list state survives dialog open and
close without a reload.

Each playthrough exposes a dedicated progress page reached from its card or
editor. The progress page is scoped to one playthrough. It shows the section list
on the left and the selected section's goals on the right. The selected section
is represented in route state so refresh and direct links preserve the active
section when possible.

Indicative frontend route shapes:

```text
/games
/games?playthroughId=123
/games?newPlaythrough=true
/games/playthroughs/123
/games/playthroughs/123?sectionId=45
/games/playthroughs/123?manageSections=true
```

If the shared shell or router ergonomics suggest a slightly different parameter
layout, preserve the same behaviour: collection state must survive dialog open
and close without a reload, and the progress page must remain scoped to one
playthrough.

## Validation

- Game name is required, trimmed, not whitespace-only, at most 200 characters,
  and unique case-insensitively.
- Game platform is one of the fixed platform values.
- Playthrough name is required, trimmed, not whitespace-only, and at most 200
  characters.
- The playthrough's game reference is required and must exist.
- `StartMonth` is required and must be between 1 and 12.
- `StartYear` is required and must be a valid positive year accepted by backend
  validation.
- Playthrough status and visibility are known values.
- Tags are trimmed, empty values are discarded, and duplicates are removed
  case-insensitively within a playthrough.
- Section name is required, trimmed, not whitespace-only, at most 200 characters,
  and unique case-insensitively within the same playthrough.
- Section color is one of the fixed palette values.
- Section ordering changes must keep a deterministic order within one
  playthrough.
- Goal text is required, trimmed, not whitespace-only, and at most 500
  characters.
- Goal completion is a boolean.

## Creation Defaults

A new playthrough starts with:

- Visibility `Public`.
- Status `Planning`.
- The selected game.
- The user-provided start month and year.
- No tags unless provided.
- No sections.

A new section starts with:

- The user-provided name.
- The selected color.
- A `SortOrder` after the current last section in the playthrough.
- No goals.

A new goal starts with:

- The user-provided text.
- `Completed` equal to `false`.
- A creation-order position after the current last goal in the section.

## Deletion

Deletion is physical, immediate, and irreversible after confirmation.

Deleting a playthrough deletes the playthrough together with all of its sections
and goals.

Deleting a section deletes the section together with all of its goals.

Deleting a goal removes only that goal. Remaining goals keep their established
creation order.

Deleting an unreferenced game removes only that catalogue row. Deleting a
referenced game requires replacing every affected playthrough's game reference
with another game and deleting the source game in the same transaction.

## Acceptance Criteria

The initial Games definition is satisfied when:

1. Administrators can manage the `Game` catalogue through Configuration, with a
   required unique name, fixed platform enum, ordering, and replace-only deletion
   when referenced by playthroughs.
2. Authenticated users can create, query, edit, and irreversibly delete accessible
   playthroughs with a required game, required name, required start month and
   year stored as integers, manual status, free-text tags, visibility, and
   standard ownership and audit metadata.
3. Playthrough statuses `Planning`, `Active`, and `Completed` are fixed,
   manually controlled, default to `Planning`, and never automatically change or
   validate against goal progress.
4. Tags are free text, have no catalogue, are normalized by trimming, removing
   empty values, and deduplicating case-insensitively while preserving displayed
   capitalization, and can be used for filtering.
5. A playthrough owns zero or more sections; no default section is created, each
   section has a required name unique within the playthrough, a fixed palette
   color token, and a manually managed order.
6. Section reordering is available through a dedicated popup or management mode,
   not through the main progress view.
7. A section owns goals whose required text is stored in creation order, cannot be
   reordered in the initial release, and can be quickly marked complete or
   incomplete without changing order.
8. Section and playthrough progress are computed on demand from current goals,
   expose completed and total counts, and are never persisted.
9. Visibility follows the Segaris public-collaboration and private-isolation
   baseline at the playthrough level, defaults to `Public`, is changed only by
   the creator, and is inherited by sections and goals; inaccessible records
   return the standard not-found behaviour.
10. Deleting a playthrough deletes all of its sections and goals; deleting a
    section deletes its goals; deleting a goal keeps no history.
11. The launcher card never requests attention.
12. Games opens on a server-paginated playthrough card collection with search,
    game/platform/status/tag/creator/visibility filters, sorting by name, game,
    start date, status, and derived progress, and URL-aware playthrough dialogs
    that preserve list state.
13. Each playthrough exposes a dedicated progress page with a one-level section
    list on the left and the selected section's goals on the right, preserving the
    selected section in route state when possible.
14. SQLite and PostgreSQL migrations, backend unit/integration/architecture
    tests, frontend component tests, and a representative Playwright journey
    verify the supported behaviour and privacy boundaries.

## Deferred Decisions

- Whether playthroughs should record end dates, completion dates, time played,
  play sessions, or historical progress snapshots.
- Whether games should later gain richer metadata such as genre, publisher,
  store, release date, cover image, external IDs, or metadata import.
- Whether platforms should become configurable rather than fixed.
- Whether playthroughs should support attachments, screenshots, notes, ratings,
  or reviews.
- Whether goals should later support reordering, due dates, priorities,
  sub-goals, notes, or history.
- Whether Games should publish progress or completion information to Analytics or
  a future Calendar module.
- Whether any status or incomplete-goal condition should later drive launcher
  attention.
