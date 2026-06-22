# Recipes Acceptance Record (Wave 9)

This document records the Wave 9 end-to-end, hardening, and acceptance pass for
the Recipes module against `docs/requirements/RECIPES_REQUIREMENTS.md` and the
exit criteria in `docs/planning/RECIPES_IMPLEMENTATION_PLAN.md`.

## Verification Approach

Wave 9 was executed as a focused hardening and acceptance pass, following the
accepted Capex, Configuration, Opex, Inventory, Travel, Assets, Maintenance,
Projects, and Processes precedents:

- Functional behaviour is covered by the automated suites delivered in Waves 0-8
  and gated on every pull request through the required CI checks (`Segaris
Backend`, `Segaris PostgreSQL`, `Segaris Compose`; see
  `docs/planning/BACKEND_CI_DECISIONS.md`).
- The repository scripts remain the required repeatable validation entry points:
  backend format verification, build, unit, API integration, architecture,
  provider migration coverage, PostgreSQL integration, frontend format, lint,
  type-check, unit, production build, and Playwright.
- The representative Playwright journey added below is compiled with the
  frontend test suite and runs against the Compose stack when seeded
  `SEGARIS_E2E_USERNAME` / `SEGARIS_E2E_PASSWORD` credentials are present.
- The OpenAPI surface and database indexes/query shape were verified statically
  against the implemented endpoints, module contracts, and paired provider
  migrations.

## End-To-End Journey

`tests/frontend/e2e/recipes.spec.ts` adds a single-user critical journey against
the full stack: sign in, create a disposable public Inventory item, open Recipes
from the launcher, exercise and clear a gallery filter, create a recipe with a
category, difficulty, servings, times, ingredients, an ingredient linked to the
Inventory item through the shared selector, steps, a staged image attachment, and
primary-image selection; create a second recipe; plan a weekly menu with both
recipes in the same slot; navigate weeks; delete the weekly menu; delete the
referenced Inventory item through the privacy-neutral impact dialog; reopen the
recipe and verify the ingredient link has been cleared while the free-text line
remains; then delete the safe test recipes. It is skipped without seeded
credentials, matching the other specs. Browser-level multi-account privacy is
deferred (see Deferred Items).

## Static Verification Results

### HTTP / OpenAPI Surface

All frozen Recipes routes are mapped under `/api/recipes` with explicit Minimal
API metadata and DTO contracts; EF Core entities are not exposed. The group
requires authentication, write routes apply antiforgery, and missing or private
records share the module not-found behaviour.

- **Recipes**: `GET /api/recipes`, `POST /api/recipes`,
  `GET /api/recipes/{recipeId}`, `PUT /api/recipes/{recipeId}`, and
  `DELETE /api/recipes/{recipeId}` carry named OpenAPI operations, summaries,
  typed responses, and problem responses for validation, visibility, inaccessible
  references, conflict/transient, and not-found/privacy paths.
- **Recipe attachments**: `GET /api/recipes/{recipeId}/attachments`,
  `POST /api/recipes/{recipeId}/attachments`,
  `GET /api/recipes/{recipeId}/attachments/{attachmentId}`,
  `DELETE /api/recipes/{recipeId}/attachments/{attachmentId}`, and
  `PUT /api/recipes/{recipeId}/attachments/{attachmentId}/primary` follow the
  shared attachment pattern, including upload size limits, owner authorization,
  cleanup, and primary-image validation.
- **Weekly menus**: `GET /api/recipes/menus`, `POST /api/recipes/menus`,
  `GET /api/recipes/menus/{menuId}`, `PUT /api/recipes/menus/{menuId}`, and
  `DELETE /api/recipes/menus/{menuId}` expose the Monday-anchored menu contract
  and typed validation/visibility/not-found responses.
- **Recipe categories**: `GET /api/recipes/categories` is an authenticated read.
  Administrator catalog management routes for create, update, move,
  deletion-impact, direct delete, and replace-and-delete use the established
  Configuration module-owned catalog boundary with antiforgery on writes.
- **Inventory deletion impact**: Inventory owns its item deletion reference
  contract and exposes privacy-neutral impact reporting; Recipes implements the
  handler that clears ingredient `ItemId` references transactionally without
  creating an Inventory-to-Recipes dependency.

### Indexes And Query Shape

The recommended indexes exist in both SQLite and PostgreSQL migrations
(`RecipesDomainPersistence` and `RecipesPrimaryAttachment`) and match the
implemented query shapes:

| Index                                            | Query that uses it                                             |
| ------------------------------------------------ | -------------------------------------------------------------- |
| `recipes (Name, Id)`                             | Default deterministic ordering (name asc, id asc tie-breaker)  |
| `recipes (CreatedBy, Visibility, Id)`            | Visibility/accessibility filter and creator filter             |
| `recipes (CategoryId)`                           | Exact category filters and category reference migration        |
| `recipes (Visibility)`                           | Exact visibility filters and selector visibility constraints   |
| `recipe_categories (NormalizedName)` unique      | Catalog name uniqueness                                        |
| `recipe_categories (SortOrder)`                  | Default catalog ordering                                       |
| `recipe_ingredients (RecipeId, Position)` unique | Ordered ingredient projection and full-collection replacement  |
| `recipe_ingredients (ItemId)` filtered           | Inventory item-deletion reference lookup                       |
| `recipe_steps (RecipeId, Position)` unique       | Ordered method-step projection and full-collection replacement |
| `recipe_menus (Week)`                            | Menu lookup and list by Monday-anchored week                   |
| `recipe_menus (CreatedBy, Visibility, Id)`       | Menu visibility/accessibility filtering                        |
| `recipe_menu_slots (RecipeId)`                   | Recipe-deletion slot cleanup and referenced-by-menu lookup     |

List filtering, sorting, pagination, and partial search run as `IQueryable`
queries translated to SQL; the client never loads the full result set. Partial
search remains the accepted database-backed `LIKE` scan across recipe names and
notes.

## Acceptance Criteria

Each criterion from `RECIPES_REQUIREMENTS.md` and its primary covering evidence:

| #   | Criterion                                                                                                                                                                            | Status                | Primary evidence                                                                                                                                                                           |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | --------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 1   | Recipe fields, defaults, ordered collections, attachments, visibility, ownership/audit, and no lifecycle status                                                                      | Met                   | `Recipe`, `RecipesModelContributor`, `RecipesDomainTests`, `RecipesWave2EndpointTests`, `RecipesWave6AttachmentTests`, `RecipesPage.test.tsx`, `recipes.spec.ts`                           |
| 2   | Difficulties `Easy`, `Medium`, and `Hard` are optional descriptive fixed values                                                                                                      | Met                   | `RecipesContractTests`, `RecipesDomainTests`, `recipeForm.ts`, `RecipesPage.test.tsx`                                                                                                      |
| 3   | Ingredients have required free text, optional quantity, optional live Inventory item reference, duplicate item references, and full-collection replacement                           | Met                   | `RecipesWave2EndpointTests`, `RecipesWave3IngredientItemTests`, `RecipeRequestBuilder`, `RecipesPage.test.tsx`, `recipes.spec.ts`                                                          |
| 4   | Steps have required instructions, position ordering, no attachments, and full-collection replacement                                                                                 | Met                   | `RecipesWave2EndpointTests`, `RecipesDomainTests`, `recipeForm.ts`, `RecipesPage.test.tsx`, `recipes.spec.ts`                                                                              |
| 5   | Recipe attachments follow shared policy with at most one primary image, thumbnail fallback, and deletion cleanup                                                                     | Met                   | `RecipesWave6AttachmentTests`, `RecipeAttachments`, `RecipesReadService`, provider migrations, `recipes.spec.ts`                                                                           |
| 6   | Ingredient-to-item references obey the visibility rule and resolve names with neutral placeholders when unresolved                                                                   | Met                   | `RecipesWave3IngredientItemTests`, `InventoryItemEntitySelector`, `RecipesReadService`, `RecipesPage.test.tsx`, `recipes.spec.ts`                                                          |
| 7   | Inventory item deletion clears every ingredient link atomically, preserves free text, never blocks deletion, reports privacy-neutral impact, and preserves dependency inversion      | Met                   | `InventoryItemDeletionReferenceTests`, `RecipesInventoryItemDeletionReferenceHandler`, `IInventoryItemDeletionReferenceHandler`, `ModuleBoundaryTests`, `InventoryPage`, `recipes.spec.ts` |
| 8   | Weekly menus are Monday-anchored, have optional names/visibility, fixed seven-by-four grid, zero or more recipe refs per slot, explicit creation, and multiple menus per week        | Met                   | `RecipesWave5WeeklyMenuTests`, `WeeklyMenusReadService`, `WeeklyMenusWriteService`, `menusState.ts`, `RecipesPage.test.tsx`, `recipes.spec.ts`                                             |
| 9   | Menu-to-recipe references obey visibility, resolve names/images safely, and recipe deletion removes slot references                                                                  | Met                   | `RecipesWave5WeeklyMenuTests`, `RecipesRecipeWriteService`, `RecipeThumbnailResolver`, `RecipeEntitySelector`, `recipes.spec.ts`                                                           |
| 10  | `RecipeCategory` is module-owned through Configuration, required, replace-only, seeded with accepted values, while difficulty and meal slots are fixed enums                         | Met                   | `RecipesCatalogEndpointTests`, `RecipesCategoryManagementService`, `RecipesSeeder`, `ConfigurationPage.test.tsx`, Configuration catalog integration                                        |
| 11  | Public collaboration/private isolation baseline, public defaults, creator-only visibility changes, inherited child visibility, and privacy-safe not-found                            | Met                   | `RecipesWave2EndpointTests`, `RecipesWave3IngredientItemTests`, `RecipesWave5WeeklyMenuTests`, `RecipesWave6AttachmentTests`, `RecipePolicies`                                             |
| 12  | Launcher card never requests attention                                                                                                                                               | Met                   | `RecipesModule`, `ModuleRegistrationTests`, `LauncherPage`, `LauncherPage.test.tsx`                                                                                                        |
| 13  | Frontend opens on URL-backed paginated gallery with filters/sorting and exposes URL-backed weekly menu planner with shared selectors                                                 | Met                   | `AppRouter`, `RecipesPage`, `recipesState.ts`, `menusState.ts`, `InventoryItemEntitySelector`, `RecipeEntitySelector`, `RecipesPage.test.tsx`, `recipes.spec.ts`                           |
| 14  | SQLite/PostgreSQL migrations, backend unit/integration/architecture tests, frontend component tests, and a representative Playwright journey verify behaviour and privacy boundaries | Met (single-user E2E) | `MigrationTests`, `PostgresPersistenceTests`, `ModuleBoundaryTests`, `ModuleRegistrationTests`, Recipes unit/API suites, `contracts.test.ts`, `RecipesPage.test.tsx`, `recipes.spec.ts`    |

## Deferred Items

Recorded in `ROADMAP.md`:

- **Second-user Recipes privacy E2E journey.** Public-collaboration and
  private-isolation behaviour is covered by API integration tests
  (`RecipesWave2EndpointTests`, `RecipesWave3IngredientItemTests`,
  `RecipesWave5WeeklyMenuTests`, `RecipesWave6AttachmentTests`,
  `InventoryItemDeletionReferenceTests`); the browser-level multi-session journey
  waits on multi-account Playwright infrastructure, matching the deferred Capex,
  Configuration, Opex, Inventory, Travel, Assets, Maintenance, Projects, and
  Processes patterns.
- **PostgreSQL representative-volume query-plan benchmark.** Index existence and
  database-level query shape are verified; a large-dataset `EXPLAIN ANALYZE`
  benchmark waits on a representative seeding/benchmark harness.
- Future product scope remains deferred by requirements: nutrition, serving
  scaling, shopping lists, stock consumption, ratings, cooking history, recipe
  import, ingredient unit catalogues, arbitrary-range or attachment-bearing
  menus, menu-level attention, and Analytics or Calendar integration.

## Documentation Outcomes

- `README.md`: no change; repository-wide commands and setup were unaffected by
  the Recipes waves.
- `ROADMAP.md`: Recipes implementation marked accepted; the deferred items above
  recorded.
- `docs/planning/RECIPES_IMPLEMENTATION_PLAN.md`: Wave 9 status updated to point
  at this record.
