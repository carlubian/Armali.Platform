# Firebird Acceptance Record (Wave 7)

This document records the Wave 7 end-to-end, hardening, and acceptance pass for
the Firebird module against `docs/requirements/FIREBIRD_REQUIREMENTS.md` and the
exit criteria in `docs/planning/FIREBIRD_IMPLEMENTATION_PLAN.md`.

## Verification Approach

Wave 7 was executed as a focused hardening and acceptance pass:

- Functional behaviour is covered by the automated suites delivered in Waves 0-6
  and gated through the repository validation scripts and CI checks.
- The fast local suites are expected to remain green through the repository
  scripts: backend format verification, build, unit, API integration,
  architecture, provider migration coverage, frontend format, lint, type-check,
  unit, production build, and Playwright.
- The representative Playwright journey added below is compiled with the
  frontend test suite and runs against the Compose stack when seeded
  `SEGARIS_E2E_USERNAME` / `SEGARIS_E2E_PASSWORD` administrator credentials are
  present.
- The OpenAPI surface and database indexes/query shape were verified statically
  against the implemented endpoints and paired provider migrations.

## End-To-End Journey

`tests/frontend/e2e/firebird.spec.ts` adds a single-user critical journey
against the full stack: sign in, create safe `PersonCategory` and
`UsernamePlatform` values through Configuration, open Firebird from the deployed
frontend, exercise an empty gallery filter, create a person with a category,
status, `02-29` birthday and avatar image, add a username and an interaction
through the dedicated popups, apply gallery filtering and birthday sorting,
replace the referenced category and platform in Configuration, verify the UI
reflects both replacements, then delete all disposable person and catalog data.
It is skipped without seeded administrator credentials, matching the other E2E
specs. The second-user privacy journey is deferred (see Deferred Items).

## Static Verification Results

### HTTP / OpenAPI Surface

All frozen Firebird routes are mapped under `/api/people` with explicit Minimal
API metadata and DTO contracts; EF Core entities are not exposed. The group
requires authentication, write routes apply antiforgery, administrative catalog
writes require the Admin policy, and missing or inaccessible records share the
module not-found behaviour.

- **People**: `GET /api/people`, `POST /api/people`,
  `GET /api/people/{personId}`, `PUT /api/people/{personId}`, and
  `DELETE /api/people/{personId}` carry named OpenAPI operations, summaries,
  typed success responses, and problem responses for validation, forbidden
  visibility changes, and not-found/privacy paths.
- **Avatar**: `GET /api/people/{personId}/avatar`,
  `PUT /api/people/{personId}/avatar`, and
  `DELETE /api/people/{personId}/avatar` follow the shared attachment pattern,
  including upload size limits, image-only validation, single-avatar
  replacement, and owner authorization through `Person`.
- **Usernames**: `GET /api/people/{personId}/usernames`,
  `POST /api/people/{personId}/usernames`,
  `PUT /api/people/{personId}/usernames/{usernameId}`, and
  `DELETE /api/people/{personId}/usernames/{usernameId}` expose DTOs and inherit
  the owning person's authorization and privacy behaviour.
- **Interactions**: `GET /api/people/{personId}/interactions`,
  `POST /api/people/{personId}/interactions`,
  `PUT /api/people/{personId}/interactions/{interactionId}`, and
  `DELETE /api/people/{personId}/interactions/{interactionId}` expose DTOs,
  enforce non-future dates, and inherit the owning person's authorization and
  privacy behaviour.
- **Catalogues**: `GET /api/people/categories` and
  `GET /api/people/platforms` are authenticated reads. Administrator catalog
  management routes for create, update, move, deletion-impact, direct delete,
  and replace-and-delete use the established Configuration module-owned catalog
  boundary with antiforgery on writes.

### Indexes And Query Shape

The Firebird persistence indexes exist in both SQLite and PostgreSQL migrations
(`FirebirdDomainPersistence`) and match the implemented query shapes:

| Index                                                        | Query that uses it                                      |
| ------------------------------------------------------------ | ------------------------------------------------------- |
| `firebird_person_categories (NormalizedName)` unique         | Category name uniqueness                                |
| `firebird_person_categories (SortOrder)`                     | Category ordering                                       |
| `firebird_username_platforms (NormalizedName)` unique        | Platform name uniqueness                                |
| `firebird_username_platforms (SortOrder)`                    | Platform ordering                                       |
| `firebird_people (CategoryId)`                               | Exact category filter and category reference migration  |
| `firebird_people (Status)`                                   | Exact status filter                                     |
| `firebird_people (Visibility)`                               | Exact visibility filter                                 |
| `firebird_people (CreatedBy, Visibility, Id)`                | Visibility/accessibility filter and creator filter      |
| `firebird_people (Name, Id)`                                 | Default deterministic name sorting                      |
| `firebird_people (BirthdayMonth, BirthdayDay, Id)`           | Birthday calendar sorting with identifier tie-break     |
| `firebird_people (Visibility, BirthdayMonth, BirthdayDay)`   | Launcher attention over accessible birthday candidates  |
| `firebird_usernames (PersonId, Id)`                          | Username reads by parent person                         |
| `firebird_usernames (PlatformId)`                            | Platform reference migration                            |
| `firebird_interactions (PersonId, Date, Id)`                 | Interaction reads by parent person in date order        |

List filtering, sorting, pagination, birthday ordering, and detail projection
run as `IQueryable` queries translated to SQL; the client never loads the full
result set. Partial search is an intentional database-backed `LIKE` scan across
person name and notes.

## Acceptance Criteria

Each criterion from `FIREBIRD_REQUIREMENTS.md` and its primary covering
evidence:

| #   | Criterion                                                                                                                                                                          | Status                | Primary evidence                                                                                                                                                                                                                                  |
| --- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | A `Person` carries required name/category/status, optional day/month birthday, notes, optional avatar, visibility, and audit metadata                                               | Met                   | `Person`, `PersonValues`, `FirebirdModelContributor`, provider migrations, `FirebirdDomainTests`, `FirebirdPersonEndpointTests`, `FirebirdPage.test.tsx`, `firebird.spec.ts`                                                                      |
| 2   | Statuses `Unknown`, `Active`, `Unavailable`, and `Blocked` are available, default to `Unknown`, and block no operation by themselves                                                | Met                   | `PersonStatus`, `FirebirdDefaults`, `FirebirdContractTests`, `FirebirdPersonEndpointTests`, `FirebirdPage.test.tsx`, `firebird.spec.ts`                                                                                                           |
| 3   | Birthday stores only month/day, is all-or-nothing, accepts `02-29`, sorts in calendar order with nulls last, and is shown as day/month only                                        | Met                   | `FirebirdBirthday`, `FirebirdBirthdayRules`, `FirebirdContractTests`, `FirebirdDomainTests`, `FirebirdPersonEndpointTests`, `personForm.ts`, `FirebirdPage.test.tsx`, `firebird.spec.ts`                                                          |
| 4   | A person carries at most one image avatar, replacing uploads clean up the prior file, gallery displays avatar/placeholder, and no other person attachments exist                   | Met                   | `FirebirdAttachments`, `FirebirdAvatarService`, `FirebirdAvatarResponseFactory`, `FirebirdSubResourceEndpointTests`, `AvatarControl`, `FirebirdPage.test.tsx`, `firebird.spec.ts`                                                                |
| 5   | A person owns zero or more usernames with required `UsernamePlatform`, required handle, optional notes, repeated platforms allowed, and no URL or primary flag                      | Met                   | `Username`, `UsernameValues`, `FirebirdSubResourceService`, `FirebirdSubResourceEndpointTests`, `FirebirdContractTests`, `UsernamesDialog`, `FirebirdPage.test.tsx`, `firebird.spec.ts`                                                          |
| 6   | A person owns zero or more interactions with required non-future date and description, listed descending, with no type or attachment                                                | Met                   | `Interaction`, `InteractionValues`, `FirebirdSubResourceService`, `FirebirdSubResourceEndpointTests`, `FirebirdContractTests`, `InteractionsDialog`, `FirebirdPage.test.tsx`, `firebird.spec.ts`                                                 |
| 7   | Firebird owns required replace-only `PersonCategory` and `UsernamePlatform` catalogues through Configuration, seeded with accepted initial values                                   | Met                   | `FirebirdSeeder`, `PersonCategoryManagementService`, `UsernamePlatformManagementService`, `FirebirdCatalogEndpointTests`, `ConfigurationPage.test.tsx`, `catalogs.ts`, `firebird.spec.ts`                                                         |
| 8   | Visibility follows public-collaboration/private-isolation, defaults to `Public`, creator-only visibility changes, and inherited authorization for usernames, interactions, avatar | Met                   | `PersonPolicies`, `FirebirdPersonWriteService`, `FirebirdSubResourceService`, `FirebirdAvatarService`, `FirebirdPersonEndpointTests`, `FirebirdSubResourceEndpointTests`, `FirebirdAttentionTests`                                               |
| 9   | Launcher attention is true only for accessible birthdays within today through today plus seven natural days in `Europe/Madrid`, wrapping year-end and excluding missing/private   | Met                   | `FirebirdAttentionContributor`, `FirebirdBirthdayRules`, `FirebirdAttentionTests`, launcher integration                                                                                                                                           |
| 10  | Firebird opens on a server-paginated avatar gallery with search, category/status filters, name/birthday sorting, URL-aware person editor, and username/interaction popups          | Met                   | `FirebirdPersonListQuery`, `FirebirdPersonReadService`, `FirebirdPage`, `peopleState.ts`, `contracts.test.ts`, `FirebirdPage.test.tsx`, `firebird.spec.ts`                                                                                       |
| 11  | SQLite/PostgreSQL migrations, backend unit/integration/architecture tests, frontend component tests, and a representative Playwright journey verify supported behaviour/privacy    | Met (single-user E2E) | `MigrationTests`, `PostgresPersistenceTests`, `ModuleBoundaryTests`, `FirebirdDomainTests`, `FirebirdContractTests`, Firebird API suites, `contracts.test.ts`, `FirebirdPage.test.tsx`, `ConfigurationPage.test.tsx`, `firebird.spec.ts`         |

## Deferred Items

Recorded in `ROADMAP.md`:

- **Second-user Firebird privacy E2E journey.** Public-collaboration and
  private-isolation behaviour is covered by API integration tests
  (`FirebirdPersonEndpointTests`, `FirebirdSubResourceEndpointTests`,
  `FirebirdAttentionTests`); the browser-level multi-session journey waits on
  multi-account Playwright infrastructure, matching the deferred patterns for
  earlier modules.
- **PostgreSQL representative-volume query-plan benchmark.** Index existence and
  database-level query shape are verified; a large-dataset `EXPLAIN ANALYZE`
  benchmark waits on a representative seeding/benchmark harness.
- **Future Firebird scope.** Standalone reminders, birthday years, recurring
  events, richer usernames/interactions, multiple person attachments,
  relationships between people, Entity Link, Analytics, and Calendar integration
  remain future versions.

## Documentation Outcomes

- `README.md`: no change; repository-wide commands and setup were unaffected by
  the Firebird waves.
- `ROADMAP.md`: Firebird implementation marked accepted; the intentional
  deferrals above recorded.
- `docs/planning/FIREBIRD_IMPLEMENTATION_PLAN.md`: Wave 7 status updated to
  point at this record.
