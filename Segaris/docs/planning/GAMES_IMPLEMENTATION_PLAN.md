# Games Implementation Plan

## Purpose

This plan delivers the initial Games module defined in
`docs/requirements/GAMES_REQUIREMENTS.md`. It translates the accepted functional
decisions into dependency-ordered Waves with explicit backend, frontend,
migration, and test work.

The requirements document remains authoritative for behaviour. This plan owns
delivery order and technical scope.

## Delivery Principles

- Keep Games an independent business module: it consumes only Identity, the
  Configuration presentation boundary, Launcher, and platform contracts, and it
  does not depend on or integrate with any other business module.
- Reuse established Configuration, privacy, REST, pagination, URL-state, and
  frontend form conventions where their semantics match.
- Keep the playthrough visibility rules, child-entity inheritance, tag
  normalization, section name uniqueness, and game replacement behaviour enforced
  by backend validation rather than inferred only by the frontend.
- Compute section and playthrough progress on demand from current goals; never
  persist completed/total counts or percentages.
- Keep goals in immutable creation order for the initial release.
- Keep each Wave independently testable and record deferrals in `ROADMAP.md`
  instead of hiding them in implementation notes.

## Fixed Technical Contracts

### Backend Module

Games lives under `Segaris.Api.Modules.Games` and owns the `Game` catalogue,
`Playthrough`, `Section`, and `Goal` entities, the fixed platform, status, and
section-colour enums, the tag normalization rules, section ordering, derived
progress projection, and the game catalogue management exposed through
Configuration. It does not depend on any other business module, and the launcher
attention contributor reports a constant non-attention state.

Indicative resource routes are:

```text
GET    /api/games/games

GET    /api/games/playthroughs
POST   /api/games/playthroughs
GET    /api/games/playthroughs/{playthroughId}
PUT    /api/games/playthroughs/{playthroughId}
DELETE /api/games/playthroughs/{playthroughId}

GET    /api/games/playthroughs/{playthroughId}/sections
POST   /api/games/playthroughs/{playthroughId}/sections
PUT    /api/games/playthroughs/{playthroughId}/sections/order
GET    /api/games/playthroughs/{playthroughId}/sections/{sectionId}
PUT    /api/games/playthroughs/{playthroughId}/sections/{sectionId}
DELETE /api/games/playthroughs/{playthroughId}/sections/{sectionId}

GET    /api/games/playthroughs/{playthroughId}/sections/{sectionId}/goals
POST   /api/games/playthroughs/{playthroughId}/sections/{sectionId}/goals
PUT    /api/games/playthroughs/{playthroughId}/sections/{sectionId}/goals/{goalId}
PUT    /api/games/playthroughs/{playthroughId}/sections/{sectionId}/goals/{goalId}/completion
DELETE /api/games/playthroughs/{playthroughId}/sections/{sectionId}/goals/{goalId}
```

Administrative game routes follow the existing module-owned catalogue management
pattern exposed through Configuration. All writes require antiforgery. Missing
and inaccessible playthroughs, sections, and goals share the platform not-found
behaviour so private data is not disclosed. Section and goal routes are always
scoped through their owning playthrough.

### Persistence

The model contains module-owned entities with provider-specific migrations:

- `Game`
- `Playthrough`
- `PlaythroughTag`
- `Section`
- `Goal`

Games store the required name, fixed platform, sort order, and catalogue audit
metadata. Playthroughs store the required game reference, required name,
`StartYear`, `StartMonth`, fixed manual status, visibility, and standard audit
metadata. Tags are stored as normalized child rows so filtering remains
database-backed and provider-compatible. Sections store the owning playthrough,
required name, fixed colour token, sort order, and audit metadata. Goals store
the owning section, text, completion flag, creation-order position, and audit
metadata.

Owned tags, sections, and goals are removed when their parent is deleted. Section
deletion removes its goals. Goal order is based on its creation-order position and
is not user-editable.

Indexes must support game ordering and unique names, playthrough filters and
deterministic sorting, game replacement impact queries, tag filtering, section
lookup and uniqueness within a playthrough, section ordering, and goal lookup by
section in creation order.

### Frontend Routes

Games uses the protected lazy route `/games` for the playthrough card collection
and an internal, playthrough-scoped progress surface under
`/games/playthroughs/{playthroughId}`.

One practical route shape is:

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

### Configuration Integration

Configuration presents the Games catalogue alongside the other module-owned
catalogues. Games owns `Game` while exposing it through the established
Configuration presentation boundary. Because every playthrough requires a game, a
referenced value may only be **replaced**; replacement re-points the affected
playthroughs to the target game in the same transaction. The fixed platform enum
is edited as a field of the game row and is not itself a catalogue.

## Waves

### Wave 0: Contracts And Test Skeleton

Establish stable contracts before persistence or UI work begins.

Tasks:

1. Add the Games module shell and registration after Health.
2. Freeze game, playthrough, section, goal, section-order, and completion routes;
   the fixed platform, playthrough status, and section-colour enum values; DTOs;
   query contracts; tag normalization rules; stable error codes; and the absence
   of any launcher attention key.
3. Define Configuration-facing contracts for game catalogue management and
   replace-only referenced deletion without exposing Games entities.
4. Define frontend API, validation-schema, route-state, and query-key skeletons
   for the playthrough collection, playthrough dialog, progress page, section
   management, and goal operations.
5. Add architecture-test expectations: Games may consume Configuration, Launcher,
   Identity, and platform contracts but must not depend on Capex, Opex,
   Inventory, Travel, Assets, Maintenance, Projects, Processes, Firebird, Clothes,
   Mood, Recipes, Destinations, Health, Analytics, or Calendar, and no module
   depends on Games.
6. Add empty backend and frontend test fixtures shared by later Waves.

Tests:

- Unit tests for enum values, tag normalization, route constants, query bounds,
  and error-code stability.
- Architecture tests for permitted dependencies and the Games non-dependency
  rules.

Exit criteria:

- Public contracts are explicit and test-covered, and later Waves do not need to
  invent new route, ownership, enum, tag, or progress semantics.

### Wave 1: Domain, Persistence, And Game Catalogue

Implement the Games data model and module-owned catalogue on both providers.

Tasks:

1. Add `Game`, `Playthrough`, `PlaythroughTag`, `Section`, and `Goal`.
2. Enforce bounded, non-whitespace names; fixed platform, status, and colour enum
   values; `StartMonth` range; valid positive `StartYear`; section uniqueness
   within a playthrough; goal text bounds; visibility; and standard audit and
   ownership metadata.
3. Store tags as normalized child rows and enforce no case-insensitive duplicates
   within one playthrough.
4. Implement the constant non-attention launcher contributor.
5. Implement game catalogue reads plus administrator mutations through
   Configuration, including ordering, unique names, and final-row behaviour
   appropriate for an optional empty catalogue.
6. Implement game deletion impact and replace-only referenced deletion, atomically
   re-pointing all affected playthroughs to the target game and reporting impact
   privacy-neutrally.
7. Add provider-specific migrations and model snapshots.
8. Add indexes for game ordering/uniqueness, playthrough filters and sorting,
   game replacement, tag filtering, section uniqueness and ordering, and goal
   creation-order reads.

Tests:

- Domain tests for enum values, start month/year validation, tag normalization,
  section name uniqueness, section ordering, and goal creation-order allocation.
- SQLite and PostgreSQL migration tests, including upgrades and provider parity
  for case-insensitive uniqueness where required.
- Integration tests for game ordering, empty catalogue behaviour, referenced-game
  replacement, rollback, privacy-neutral impact reporting, and administrator
  authorization.

Exit criteria:

- Both providers persist the complete Games model, expose the game catalogue
  through Configuration, and safely replace referenced games.

### Wave 2: Playthrough Read And Mutation APIs

Deliver the playthrough backend contract, excluding section and goal mutation.

Tasks:

1. Implement the paginated playthrough card query and the playthrough detail
   query, including game name, platform, status, start month/year, tags, and
   derived global completed/total progress.
2. Implement partial search across playthrough name and game name.
3. Implement exact filters for game, platform, status, tag, creator, and
   visibility.
4. Implement deterministic sorting by playthrough name, game name, start
   month/year, status, and derived progress, with stable tie-breakers.
5. Implement create, update, and delete for playthroughs with full validation and
   documented creation defaults.
6. Enforce game validity, tag normalization, status validity, visibility
   transitions, creator-only visibility change, and standard public-collaboration
   and private-isolation authorization.
7. Delete owned tags, sections, and goals when a playthrough is deleted.

Tests:

- API integration tests for pagination, filters, search, sorting, defaults,
  required fields, start month/year validation, tag normalization, derived
  progress projection, visibility isolation, and not-found privacy behaviour.
- Two-user privacy tests for public collaboration and private isolation.

Exit criteria:

- Users can browse and fully manage accessible playthroughs through the backend
  with correct validation, derived progress, tags, and privacy.

### Wave 3: Section And Goal APIs

Add the playthrough-scoped progress structure and quick completion operations.

Tasks:

1. Implement playthrough-scoped section listing and detail queries, projecting
   each section's derived completed/total progress.
2. Implement section create, update, delete, and dedicated reorder operations,
   enforcing unique names within the playthrough and fixed colour values.
3. Implement section deletion as physical deletion of the section and all owned
   goals.
4. Implement section-scoped goal listing in creation order.
5. Implement goal create, edit, delete, and quick completion toggle/update,
   preserving creation order regardless of completion state.
6. Recompute and expose section and playthrough derived progress after section and
   goal mutations.
7. Inherit playthrough visibility and authorization for all section and goal
   operations and return not-found for inaccessible playthroughs or mismatched
   nested identifiers.

Tests:

- API integration tests for section CRUD, section name uniqueness, colour
  validation, reorder behaviour, section progress, section deletion cleanup, goal
  CRUD, quick completion, creation-order preservation, playthrough progress
  recomputation, visibility inheritance, and not-found privacy behaviour.
- Tests confirming section and goal routes cannot be used across mismatched
  playthrough or section parents.

Exit criteria:

- Users can fully manage a playthrough's sections and goals under playthrough
  scope and visibility, and derived progress remains correct without persisted
  aggregate values.

### Wave 4: Games Frontend Collection

Build the user-facing playthrough collection experience.

Tasks:

1. Add the lazy `/games` route, module error boundary, translation namespace, and a
   launcher card wired to the constant non-attention state.
2. Build the server-paginated playthrough card collection with URL-backed search,
   game/platform/status/tag/creator/visibility filters, sorting, and bounded
   pagination.
3. Render cards with game, status, and derived global progress as primary
   properties, and platform, start month/year, and tags as secondary properties.
4. Build the playthrough dialog with React Hook Form and Zod, covering name, game
   selector, start month/year, status, tags, visibility guards, and deletion
   confirmation.
5. Add navigation from a card and from the editor to the dedicated progress page.
6. Wire mutation feedback and list/query invalidation.

Tests:

- Frontend API and component tests for route state, filters, sorting, pagination,
  card rendering, playthrough validation, tag normalization display, privacy-safe
  errors, deletion confirmation, and navigation to the progress page.
- Accessibility tests for dialog focus, keyboard operation, and error association.

Exit criteria:

- Users can complete the full playthrough collection workflow without page reloads
  while preserving card-list state.

### Wave 5: Games Frontend Progress Page

Build the immersive, playthrough-scoped section and goal experience.

Tasks:

1. Build the `/games/playthroughs/{playthroughId}` page as a two-pane layout with
   a one-level section list on the left and selected-section goals on the right.
2. Preserve selected section in URL state when possible, and handle empty
   playthroughs with no default section.
3. Render playthrough context, status, game, tags, start month/year, and derived
   global progress in the page header.
4. Build section create/edit/delete flows and the dedicated section management
   popup or mode for section reordering, including fixed-palette colour selection.
5. Build goal create/edit/delete flows and the quick inline completion checkbox or
   equivalent control, preserving creation order after completion changes.
6. Reflect section and playthrough progress updates after goal mutations without
   persisting aggregate values.
7. Wire clear navigation back to the playthrough collection.

Tests:

- Component tests for empty playthrough state, selected-section route state,
  section list rendering, section colour tokens, section CRUD, section reorder
  management, goal CRUD, quick completion, creation-order preservation, and
  derived progress updates.
- Accessibility tests for pane navigation, checkbox operation, dialog focus,
  keyboard operation, and error association.

Exit criteria:

- Users can manage a playthrough's sections and goals on a dedicated page that
  stays scoped to that playthrough and preserves selected-section state.

### Wave 6: Configuration Frontend For Games

Surface game catalogue management and referenced-game replacement in the
Configuration UI.

Tasks:

1. Add the Games section to the Configuration UI for the `Game` catalogue,
   including create, edit, reorder, and delete actions.
2. Build the game editor with name and fixed platform selector.
3. Build the referenced-game deletion flow with a replacement selector and
   privacy-neutral impact summary.
4. Surface empty-catalogue states clearly, since Games intentionally ships no
   seeded game values.
5. Invalidate the relevant Games and Configuration caches after catalogue
   mutations.

Tests:

- Component tests for game catalogue CRUD, platform selection, name uniqueness
  feedback, reorder controls, empty state, replacement dialog, privacy-neutral
  impact messaging, and cache invalidation.

Exit criteria:

- Administrators can manage games through Configuration, and referenced games can
  be deleted only through safe replacement.

### Wave 7: End-To-End, Hardening, And Acceptance

Validate the implemented behaviour across both providers and the deployed
frontend/backend boundary.

Tasks:

1. Run backend format, build, unit, API, PostgreSQL, migration, and architecture
   suites through repository scripts.
2. Run frontend format, lint, type-check, unit, build, and Playwright suites.
3. Add a representative Playwright journey covering login, opening Configuration,
   creating a game, opening Games, creating a playthrough with start month/year,
   status, and tags, opening the progress page, creating sections, reordering
   sections through the management flow, adding goals, toggling completion,
   observing section and playthrough progress, returning to the collection, and
   deleting safe test data.
4. Add coverage for referenced-game replacement through Configuration, confirming
   the playthrough keeps its progress data while pointing at the replacement game.
5. Review OpenAPI for Games game, playthrough, section, goal, order, and
   completion routes.
6. Verify keyboard behaviour, dialog scrolling, filtered card invalidation,
   progress-page scoping, quick completion interaction, and narrow desktop widths.
7. Map every criterion in `docs/requirements/GAMES_REQUIREMENTS.md` to covering
   code and tests in a Games acceptance record.
8. Update `ROADMAP.md` to mark Games as implemented and accepted and to record
   only intentional remaining deferrals.

Exit criteria:

- All relevant repository scripts pass, both providers are covered, the Compose
  journey succeeds, and every accepted Games requirement is implemented or
  explicitly deferred.

## Suggested Pull Request Boundaries

1. Games contracts, persistence, and game catalogue backend (Waves 0-1).
2. Playthrough, section, and goal APIs (Waves 2-3).
3. Games collection and progress page frontend (Waves 4-5).
4. Configuration frontend integration (Wave 6).
5. End-to-end, hardening, and acceptance (Wave 7).

## Plan Completion Criteria

The plan is complete when all Waves meet their exit criteria and the Games
requirements document describes implemented behaviour rather than only functional
intent.

End dates, completion dates, time played, play sessions, progress history, richer
game metadata, cover images, external imports, configurable platforms,
attachments, screenshots, ratings, goal reordering, due dates, sub-goals,
Analytics or Calendar integration, and launcher attention remain separate future
planning topics.
