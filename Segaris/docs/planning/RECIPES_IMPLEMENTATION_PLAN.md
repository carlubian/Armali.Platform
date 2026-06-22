# Recipes Implementation Plan

## Purpose

This plan delivers the initial Recipes module defined in
`docs/requirements/RECIPES_REQUIREMENTS.md`. It translates the accepted functional
decisions into dependency-ordered Waves with explicit backend, frontend,
migration, and test work.

The requirements document remains authoritative for behaviour. This plan owns
delivery order and technical scope.

## Delivery Principles

- Preserve Recipes as a business module whose only cross-business-module
  dependency is a narrow, explicit reference from a recipe ingredient to an
  Inventory item.
- Reuse established Configuration, Attachments, privacy, REST, pagination,
  entity-selection, and frontend conventions where their semantics match.
- Reuse the shared `EntityReferenceField` and `EntitySelectorDialog` with thin
  per-entity adapters for both the ingredient-to-item link and the
  menu-slot-to-recipe link, without forking the selector or introducing a generic
  backend association model.
- Implement the Inventory item-deletion handling by contract inversion so the
  dependency direction stays Recipes to Inventory. Item deletion clears the
  ingredient link; it never reassigns and never blocks.
- Keep the visibility rules, the ingredient and menu reference rules, and the
  item-deletion behaviour explicit in backend validation rather than inferred only
  by the frontend.
- Do not introduce nutrition, serving scaling, shopping lists, stock consumption,
  ratings, cooking history, recipe import, a recipe lifecycle status, menu
  attachments, launcher attention, or any other cross-module dependency.
- Keep each Wave independently testable and record deferrals in `ROADMAP.md`
  instead of hiding them in implementation notes.

## Fixed Technical Contracts

### Backend Module

Recipes lives under `Segaris.Api.Modules.Recipes` and owns the recipe, the recipe
ingredient and step collections, the weekly menu and its slot references, the
`RecipeCategory` catalogue, attachment authorization, and the implementation of
the Inventory item-deletion reference contract. It consumes a narrow Inventory
read contract and the Configuration presentation boundary. It does not depend on
any other business module, and it contributes no launcher attention.

Indicative resource routes are:

```text
GET    /api/recipes
POST   /api/recipes
GET    /api/recipes/{recipeId}
PUT    /api/recipes/{recipeId}
DELETE /api/recipes/{recipeId}

GET    /api/recipes/{recipeId}/attachments
POST   /api/recipes/{recipeId}/attachments
GET    /api/recipes/{recipeId}/attachments/{attachmentId}
DELETE /api/recipes/{recipeId}/attachments/{attachmentId}
PUT    /api/recipes/{recipeId}/attachments/{attachmentId}/primary

GET    /api/recipes/menus
POST   /api/recipes/menus
GET    /api/recipes/menus/{menuId}
PUT    /api/recipes/menus/{menuId}
DELETE /api/recipes/menus/{menuId}

GET    /api/recipes/categories
```

Administrative category routes follow the existing module-owned catalogue
management pattern exposed through Configuration. All writes require antiforgery.
Missing and inaccessible records share the platform not-found behaviour so private
data is not disclosed.

### Persistence

The model contains module-owned entities with provider-specific migrations:

- `Recipe`
- `RecipeIngredient`
- `RecipeStep`
- `RecipeCategory`
- `WeeklyMenu`
- `WeeklyMenuSlotRecipe`

Recipes store the name, category reference, optional difficulty, optional servings
and times, notes, visibility, and standard audit metadata. Ingredients store the
free-text name, an optional opaque Inventory item identifier, an optional quantity
text, and a position. Steps store the instruction and a position. `WeeklyMenu`
stores the Monday-anchored week date, the optional name, visibility, and audit
metadata. `WeeklyMenuSlotRecipe` stores the owning menu, the day-of-week, the
fixed slot, and a recipe reference.

Owned ingredients, steps, attachments, and menu slot references are removed when
their parent is deleted. The Inventory item identifier is a stable opaque
reference, not a foreign key to Inventory entities; its integrity is preserved by
the deletion reference contract rather than a database constraint that would couple
the modules. The menu-to-recipe reference is intra-module and may use a normal
relationship with the recipe-deletion behaviour enforced in module logic.

Indexes must support recipe filters and deterministic sorting (name, then
identifier), the category reference migration, the ingredient item-reference lookup
used by the deletion handler, the menu lookup by week, and the menu slot recipe
lookup used on recipe deletion.

### Frontend Route

Recipes uses the protected lazy route `/recipes` with an internal menu-planner
surface under `/recipes/menus`.

The recipe collection is a server-paginated thumbnail gallery with URL-backed list
state and dialog state, following the Clothes and Assets pattern. The menu planner
is a weekly grid with URL-backed week and dialog state. One practical route shape
is:

```text
/recipes
/recipes?recipeId=123
/recipes?newRecipe=true
/recipes/menus?week=2026-06-22
/recipes/menus?week=2026-06-22&menuId=45
/recipes/menus?week=2026-06-22&newMenu=true
```

If the shared shell or router ergonomics suggest a slightly different parameter
layout, preserve the same behaviour: gallery and grid state must survive dialog
open and close without a reload.

### Inventory Integration

Recipes consumes a narrow Inventory read contract to validate an item reference,
resolve its display name, and evaluate accessibility and visibility for the
visibility rule. Inventory owns this contract.

Inventory additionally defines a deletion reference contract that consumers
implement to report and resolve references when an item is deleted. Recipes
registers an implementation that clears the item link on all referencing
ingredients within the deletion transaction. Inventory enumerates registered
implementations during deletion; it never queries Recipes entities. This mirrors
the existing Assets-to-Maintenance launcher-attention and reference-handler
inversion patterns. If Inventory does not yet expose these contracts, defining and
exposing them is part of this plan's Wave 0 and Wave 4.

### Configuration Integration

Configuration presents the Recipes catalogue alongside the other module-owned
catalogues. Recipes owns `RecipeCategory` while exposing it through the established
Configuration presentation boundary. Because a category is required on every
recipe, a referenced value may only be **replaced**; replacement re-points the
affected recipes to the target value.

## Waves

### Wave 0: Contracts And Test Skeleton

Establish stable contracts before persistence or UI work begins.

Tasks:

1. Add the Recipes module shell and registration after Inventory.
2. Freeze recipe and menu routes, the difficulty and meal-slot enums and their
   fixed values, DTOs, query contracts, stable error codes, the attachment owner
   kind (`Recipe`), and the absence of any launcher attention key.
3. Define Configuration-facing contracts for category reference handling without
   exposing Recipes entities.
4. Define the Inventory read contract and the Inventory item-deletion reference
   contract that Recipes will consume and implement, owned by Inventory.
5. Define frontend API, validation-schema, route-state, and query-key skeletons for
   both the recipe and menu surfaces.
6. Add architecture-test expectations: Recipes may consume Configuration,
   Inventory, and platform contracts but must not depend on Capex, Opex, Travel,
   Assets, Maintenance, Projects, Processes, Firebird, Clothes, or Mood, and
   Inventory must not depend on Recipes.
7. Add empty backend and frontend test fixtures shared by later Waves.

Tests:

- Unit tests for enum values, route constants, query bounds, and error-code
  stability.
- Architecture tests for permitted dependencies, the Inventory-to-Recipes
  non-dependency, and published contracts.

Exit criteria:

- Public contracts are explicit and test-covered, and later Waves do not need to
  invent new route, ownership, or cross-module semantics.

### Wave 1: Domain, Persistence, And Catalogue

Implement the Recipes data model and module-owned catalogue on both providers.

Tasks:

1. Add `Recipe`, `RecipeIngredient`, `RecipeStep`, `RecipeCategory`, `WeeklyMenu`,
   and `WeeklyMenuSlotRecipe`.
2. Enforce the required category relationship, bounded strings, the optional
   difficulty, servings and time bounds, ordered ingredient and step positions,
   the Monday-anchored menu week normalisation, the fixed day-and-slot grid, and
   visibility and audit metadata.
3. Seed the accepted initial category values once.
4. Implement module-owned category reads plus administrator mutations through
   Configuration, including ordering and final-row protection.
5. Add provider-specific migrations and model snapshots.
6. Add indexes for recipe filters and sorting, category reference migration, the
   ingredient item-reference lookup, the menu lookup by week, and the menu slot
   recipe lookup.

Tests:

- Domain tests for difficulty, slot and day values, week normalisation, and
  position ordering.
- SQLite and PostgreSQL migration tests, including upgrades and idempotent
  catalogue initialization.
- Integration tests for category ordering, final-row protection, and administrator
  authorization.

Exit criteria:

- Both providers persist the complete model and expose a configurable category
  catalogue.

### Wave 2: Recipe Read And Mutation APIs

Deliver the core recipe backend contract, excluding the ingredient item link.

Tasks:

1. Implement the paginated recipe gallery query and the recipe detail query.
2. Implement partial search across name and notes.
3. Implement exact filters for category and difficulty and deterministic sorting
   with the default ordering (name ascending, then identifier ascending).
4. Implement create, update, and delete for recipes with full validation and the
   documented creation defaults, including ordered ingredient (free-text name and
   quantity only) and step collections through full-collection replacement.
5. Enforce category validity, visibility transitions, creator-only visibility
   change, and standard public-collaboration and private-isolation authorization.

Tests:

- API integration tests for pagination, filters, search, sorting, defaults,
  required fields, ingredient and step replacement and ordering, visibility
  isolation, and not-found privacy behaviour.
- Two-user privacy tests for public collaboration and private isolation.

Exit criteria:

- Users can browse and fully manage accessible recipes through the backend with
  correct validation and privacy, without the ingredient item link.

### Wave 3: Ingredient Item Reference

Add the optional live ingredient link to Inventory and its visibility rule.

Tasks:

1. Consume the Inventory read contract to validate the referenced item, resolve its
   display name, and evaluate accessibility for the current user.
2. Accept, store, and clear the optional item identifier on each ingredient on
   create and update.
3. Enforce the visibility rule on create, update, visibility change, and item
   change: a public recipe may reference only public items; a private recipe may
   reference any item its creator can access.
4. Surface the resolved item name in the recipe detail projection, with a neutral
   placeholder when the item is not resolvable for the viewer.

Tests:

- API integration tests for linking, unlinking, the visibility rule and its
  rejection paths, name resolution, and the neutral placeholder.
- Two-user tests confirming a public recipe cannot expose a private item.

Exit criteria:

- Ingredients can be linked to accessible items under the visibility rule, and the
  link never discloses a private item.

### Wave 4: Inventory Item Deletion Handling

Integrate Recipes into Inventory item deletion safely through contract inversion.

Tasks:

1. Ensure Inventory exposes a deletion reference contract; if it does not yet,
   add it owned by Inventory without coupling Inventory to consumers.
2. Implement the contract in Recipes: report the number of ingredients referencing
   an item and clear the link on all of them within the deletion transaction,
   leaving each ingredient line intact as free text.
3. Wire Inventory item deletion to enumerate registered reference contracts,
   evaluate impact, perform the clearing atomically, and roll back on any failure.
4. Provide privacy-neutral impact reporting that never discloses private recipes of
   other users.
5. Invalidate affected Recipes and Inventory frontend queries after a successful
   item deletion.

Tests:

- Cross-module integration tests for clearing links across mixed-ownership recipes,
  rollback, and privacy-neutral impact reporting.
- SQLite and PostgreSQL coverage for the atomic delete-and-clear behaviour.
- Architecture tests confirming Inventory gains no dependency on Recipes.

Exit criteria:

- An Inventory item referenced by ingredients can be deleted; its links are cleared
  atomically, the ingredient lines survive as free text, and the dependency
  direction is preserved.

### Wave 5: Weekly Menu APIs

Deliver the weekly menu backend contract.

Tasks:

1. Implement explicit menu create, the menu detail query, update, and delete, with
   the Monday-anchored week normalisation and optional name.
2. Implement the menu listing/lookup by week and the four-slot, seven-day grid
   projection with zero or more recipe references per slot.
3. Enforce the menu-to-recipe visibility rule on create, update, visibility change,
   and slot change: a public menu may reference only public recipes; a private menu
   may reference any recipe its creator can access.
4. Resolve each referenced recipe's name and primary image for the grid, with a
   neutral placeholder when it is not resolvable.
5. Remove a deleted recipe from every slot that references it within the recipe
   deletion transaction, without blocking the deletion.
6. Allow multiple menus per week, subject to visibility and standard authorization.

Tests:

- API integration tests for explicit creation, week normalisation, multiple menus
  per week, slot grid round-trip, the visibility rule and its rejection paths,
  recipe name/image resolution and placeholder, and recipe-deletion slot cleanup.
- Two-user tests confirming a public menu cannot expose a private recipe.

Exit criteria:

- Users can plan weekly menus with multiple recipes per slot under the visibility
  rule, and recipe deletion never leaves a dangling slot reference.

### Wave 6: Recipe Attachments

Deliver recipe attachments with a primary image.

Tasks:

1. Add recipe attachment listing, upload, download, delete, and set-primary routes
   using the shared attachment policies.
2. Inherit recipe visibility and authorization for attachments.
3. Implement the gallery thumbnail resolution (primary image, then first image,
   then placeholder).
4. Clean up attachments on recipe deletion.

Tests:

- Attachment tests for round-trip behaviour, primary-image selection, thumbnail
  fallback, authorization, validation failures, and filesystem cleanup on recipe
  deletion.

Exit criteria:

- Recipes support multiple attachments with an optional primary image that respect
  recipe visibility and are cleaned up on deletion.

### Wave 7: Recipes Frontend Collection

Build the user-facing recipe experience.

Tasks:

1. Add the lazy `/recipes` route, module error boundary, translation namespace, and
   a launcher card with no attention state.
2. Build the server-paginated thumbnail gallery with URL-backed search, filters,
   sorting, and bounded pagination.
3. Build the recipe dialog with React Hook Form and Zod, covering name, category,
   difficulty, servings, times, notes, the ordered ingredient editor (free-text
   name, quantity, and an item picker constrained by the visibility rule via an
   `InventoryItemEntitySelector` adapter), the ordered step editor, visibility
   guards, and recipe attachments with primary-image selection.
4. Wire deletion confirmation and list invalidation.

Tests:

- Frontend API and component tests for route state, filters, sorting, pagination,
  recipe validation, ingredient and step ordering and replacement, the
  visibility-constrained item picker, privacy-safe errors, and attachment flows.
- Accessibility tests for dialog focus, keyboard operation, and error association.

Exit criteria:

- Users can complete the full recipe workflow without page reloads while preserving
  gallery state.

### Wave 8: Menu Planner Frontend And Configuration Integration

Build the menu planner and surface the Recipes catalogue and item-deletion impact.

Tasks:

1. Build the `/recipes/menus` weekly grid with Monday-anchored week navigation
   (previous/next/current), URL-backed week and dialog state, and a seven-day by
   four-slot layout showing each slot's recipes with thumbnails and placeholders.
2. Build the menu dialog (name, visibility) and slot editing, choosing recipes
   through a `RecipeEntitySelector` adapter over the shared selector, constrained by
   the menu visibility rule.
3. Add the Recipes section to the Configuration UI for `RecipeCategory`, including
   reorder controls and the replacement dialog with a privacy-neutral impact
   summary.
4. Extend the Inventory item-deletion flow in the Inventory frontend to present the
   privacy-neutral recipe-reference impact and confirm the link-clearing outcome.
5. Invalidate the relevant Recipes, Inventory, and Configuration caches after
   structural mutations.

Tests:

- Component tests for week navigation, the grid, menu CRUD, the slot recipe picker
  constrained by visibility, category CRUD and reorder, the replacement dialog, the
  item-deletion impact confirmation, and cache invalidation.

Exit criteria:

- Users can plan weekly menus and manage the category catalogue through the UI, and
  deleting a referenced Inventory item shows a privacy-neutral impact before
  clearing the ingredient links.

### Wave 9: End-To-End, Hardening, And Acceptance

Status: **Delivered**. Acceptance evidence is recorded in
`docs/planning/RECIPES_ACCEPTANCE.md`.

Validate the implemented behaviour across both providers and the deployed
frontend/backend boundary.

Tasks:

1. Run backend format, build, unit, API, PostgreSQL, migration, and architecture
   suites through repository scripts.
2. Run frontend format, lint, type-check, unit, build, and Playwright suites.
3. Add a representative Playwright journey covering login, opening Recipes, creating
   a recipe with a category, ingredients (one linked to an Inventory item), and
   steps, attaching and selecting a primary image, planning a weekly menu with
   multiple recipes in a slot, navigating weeks, and deleting safe test data; plus
   deleting a referenced Inventory item and confirming the ingredient link is
   cleared.
4. Review OpenAPI for Recipes recipe, menu, category, and attachment routes and the
   Inventory deletion changes.
5. Verify keyboard behaviour, dialog scrolling, filtered gallery invalidation, grid
   week navigation, and narrow desktop widths.
6. Map every criterion in `docs/requirements/RECIPES_REQUIREMENTS.md` to covering
   code and tests in a Recipes acceptance record.
7. Update `ROADMAP.md` to mark Recipes as implemented and accepted and to record
   only intentional remaining deferrals.

Exit criteria:

- All relevant repository scripts pass, both providers are covered, the Compose
  journey succeeds, and every accepted Recipes requirement is implemented or
  explicitly deferred.

## Suggested Pull Request Boundaries

1. Recipes contracts, persistence, and module-owned catalogue (Waves 0-1).
2. Recipe read and mutation APIs (Wave 2).
3. Ingredient item reference and Inventory item-deletion handling (Waves 3-4).
4. Weekly menu APIs and recipe attachments (Waves 5-6).
5. Recipes collection, menu planner, Configuration, and Inventory frontend
   integration (Waves 7-8).
6. End-to-end, hardening, and acceptance (Wave 9).

## Plan Completion Criteria

The plan is complete when all Waves meet their exit criteria and the Recipes
requirements document describes implemented behaviour rather than only functional
intent.

Nutrition, serving scaling, shopping lists, stock consumption, ratings, cooking
history, recipe import, ingredient unit catalogues, arbitrary-range or attachment-
bearing menus, menu-level attention, and Analytics or Calendar integration remain
separate future planning topics.
