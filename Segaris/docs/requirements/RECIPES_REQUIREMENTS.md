# Recipes Requirements

## Status

Phase 2 functional definition is complete. This document is the functional
source of truth for the initial Recipes implementation plan.

## Purpose

Recipes is the household's cooking module. It maintains a recipe collection and
lets the household plan weekly menus composed of those recipes. Each recipe
records its ingredients, its ordered preparation steps, and a photo of the dish;
each weekly menu lays out which recipes are cooked for each meal across a week.

`Recipes` is the interface name of the module. Its primary REST resources are
`recipes` (`/api/recipes`) and `menus` (`/api/recipes/menus`).

Recipes is an independent business module. Its only cross-business-module
dependency is a narrow, explicit reference from a recipe ingredient to an
Inventory item. This is the second cross-business-module reference in Segaris,
after Maintenance to Assets, and follows the same contract-inversion pattern so
Inventory never depends on Recipes.

## Initial Scope

- Maintain a `Recipe` collection with a name, a required category, an optional
  difficulty, optional servings and times, an ordered ingredient list, an ordered
  step list, free-text notes, attachments with an optional primary image, and
  visibility.
- Let each ingredient carry a required free-text name, an optional live link to an
  Inventory item, and an optional free-text quantity.
- Own the `RecipeCategory` catalogue, managed through Configuration with
  replace-only deletion.
- Plan weekly menus as `WeeklyMenu` entities, each pinned to one ISO week
  (Monday-anchored), with an optional name, visibility, and a fixed grid of four
  meal slots per day, where each slot holds zero or more live references to
  recipes.
- Present the recipe collection as a server-paginated thumbnail gallery with
  search, filters, and sorting, and present the menu planner as a weekly grid with
  week navigation.
- Reuse the shared entity-selection components for both the ingredient-to-item
  link and the menu-slot-to-recipe link through thin adapters.

## Excluded Scope

The initial Recipes implementation excludes:

- Nutrition, calories, or any dietary computation.
- Serving scaling that recomputes ingredient quantities.
- Shopping-list generation and any stock consumption against Inventory.
- Recipe ratings, favourites, cooking history, or "last cooked" tracking.
- Recipe import from a URL, a photo, or an external source.
- A recipe lifecycle status; a recipe is simply created and deleted.
- Free-text entries inside a menu slot; slots reference recipes only.
- Menus that span an arbitrary date range rather than one Monday-to-Sunday week.
- Menu attachments and menu-level launcher attention.
- Any launcher attention signal for the module.
- Analytics or Calendar integration.
- Spanish translations; the module ships English strings under an i18n namespace
  prepared for future translation.

## Recipe

A `Recipe` contains:

- A required name.
- A required `RecipeCategory` reference.
- An optional difficulty.
- Optional servings (a positive integer).
- An optional preparation time and an optional cook time, each in whole minutes.
- An ordered list of ingredients (zero or more).
- An ordered list of preparation steps (zero or more).
- Optional free-text notes.
- Attachments with an optional primary image.
- Visibility.
- Standard ownership and audit metadata.

A recipe has no lifecycle status. Recipes that are no longer wanted are deleted.

### Difficulty

Difficulty is optional and, when present, is one of the fixed values `Easy`,
`Medium`, or `Hard`. It is descriptive, is a fixed enum rather than a
Configuration catalogue, and blocks no operation.

### Ingredient

Each recipe owns an ordered list of ingredients. A `RecipeIngredient` contains:

- A required free-text name, trimmed and non-whitespace.
- An optional live reference to one Inventory item.
- An optional free-text quantity (for example `200 g`, `2 tbsp`, or `to taste`);
  units are not modelled as an entity and are written inside the quantity text.
- An explicit position within the recipe.
- An identifier and standard persistence metadata.

The same Inventory item may appear in more than one ingredient line. Ingredients
inherit the visibility and authorization of their owning recipe and are removed
when the recipe is deleted. The ingredient list is edited as part of the recipe
through full-collection replacement, preserving line identity where it survives an
edit.

### Step

Each recipe owns an ordered list of preparation steps. A `RecipeStep` contains:

- A required free-text instruction, trimmed and non-whitespace.
- An explicit position within the recipe.
- An identifier and standard persistence metadata.

Steps are shown in ascending position order. Steps have no attachments. Steps
inherit the visibility and authorization of their owning recipe, are removed when
the recipe is deleted, and are edited through full-collection replacement.

### Image And Attachments

- A recipe carries zero or more attachments through the shared platform attachment
  storage with the owner kind `Recipe`, under the shared attachment size and
  policy bounds.
- One attachment may be marked as the primary image and is shown as the gallery
  thumbnail; a recipe without a primary image falls back to its first image and
  then to a placeholder, following the Clothes and Assets pattern.
- Attachments are removed when their recipe is deleted.

## Ingredient-To-Item Reference

A recipe ingredient may optionally reference one Inventory item through a live
identifier (no snapshot):

- Recipes consumes a narrow Inventory read contract to validate the referenced
  item, resolve its display name, and evaluate accessibility for the current user.
  Inventory owns this contract.
- A `Public` recipe may reference only `Public` items; a `Private` recipe may
  reference any item its creator can access. This mirrors the Maintenance-to-Assets
  visibility rule.
- When an item cannot be resolved for the viewer, the ingredient shows a neutral
  placeholder and retains its free-text name.
- The reference is optional, so an ingredient without an item link is a valid,
  complete ingredient.

### Item Deletion

Deleting an Inventory item referenced by recipe ingredients is **not** blocked.
Inventory enumerates registered reference contracts on deletion; Recipes
implements one that clears the item link on every affected ingredient within the
deletion transaction, leaving the ingredient line intact as free text. The
behaviour is implemented by contract inversion so the dependency direction stays
Recipes to Inventory and Inventory never queries Recipes entities. Impact
reporting is privacy-neutral and never discloses another user's private recipes.

## WeeklyMenu

A `WeeklyMenu` plans the meals of one week. It contains:

- A required ISO week, stored as the civil date of that week's Monday in
  `Europe/Madrid`. The grid is always the seven days Monday to Sunday.
- An optional name (for example `Diet week` or `Guests`) used to distinguish
  multiple menus in the same week.
- A grid of meal slots: for each of the seven days, the four fixed slots
  `Breakfast`, `Lunch`, `Snack`, and `Dinner`.
- Visibility.
- Standard ownership and audit metadata.

Each slot holds zero or more live references to recipes. A slot accepts no
free-text content. The same recipe may appear in multiple slots, days, or menus.
Multiple menus may exist for the same week, subject to visibility. A menu is
created explicitly; navigating to a week does not implicitly create a menu, and a
week with no menu simply has none.

### Menu-To-Recipe Reference

- A `Public` menu may reference only `Public` recipes; a `Private` menu may
  reference any recipe its creator can access. This is an intra-module link, so no
  contract inversion is required.
- Each referenced recipe resolves its display name and primary image for the grid,
  with a neutral placeholder when the recipe is not resolvable for the viewer.
- Deleting a recipe that a menu references removes it from every slot that
  references it, without blocking the deletion.

## Visibility And Authorization

Recipes and menus use the platform-standard visibility values:

- `Public`
- `Private`

New recipes and menus default to `Public`. The standard Segaris baseline applies:

- A user can view and edit their own records and public records.
- A private recipe or menu remains creator-only, including from administrators.
- Any authenticated user may edit a public record.
- Only the creator may change a record's visibility.

Ingredients, steps, attachments, and menu slot references inherit the visibility
and authorization of their owning recipe or menu. These constraints are enforced
by the backend regardless of the client. Missing and inaccessible records share
the platform not-found behaviour so private data is not disclosed.

## Catalogues And Configuration Integration

Recipes owns one catalogue, presented alongside the other module-owned catalogues
through the established Configuration presentation boundary:

- `RecipeCategory`: a required name and an order. Because every recipe requires a
  category, a referenced value may only be **replaced**; replacement re-points the
  affected recipes to the target value.

Administrator CRUD, ordering, final-row protection, and the replacement dialog
with a privacy-neutral impact summary follow the established module-owned
catalogue pattern. Difficulty and the meal-slot set are fixed enums, not managed
through Configuration.

Accepted initial catalogue values, seeded once:

- `RecipeCategory`: `Breakfast`, `Starter`, `Main`, `Dessert`, `Drink`, `Sauce`,
  `Other`.

## Attention

Recipes contributes no launcher attention signal. The launcher card never
requests attention.

## Module Entry And Navigation

Opening Recipes shows the **recipe collection** first, presented as a
server-paginated thumbnail gallery of accessible recipes. Each card shows the
primary image (or a placeholder), the name, and the category.

- Search matches the recipe name (and notes where practical).
- Filters cover category and difficulty.
- Sorting covers name and category.
- Recipes are created, viewed, edited, and deleted through the established Segaris
  URL-aware popup pattern, so gallery state survives dialog open and close without
  a reload.
- The ingredient and step lists are edited inside the recipe editor; the
  ingredient item link is selected through the shared entity selector.

The module also exposes a **menu planner** reached through internal navigation. It
presents the selected week as a grid of seven days by four slots, with week
navigation (previous/next/current) anchored on Monday in `Europe/Madrid`. Menus
are created, edited, and deleted through URL-aware popups, and a slot's recipes
are chosen through the shared entity selector.

Indicative frontend route shapes:

```text
/recipes
/recipes?recipeId=123
/recipes?newRecipe=true
/recipes/menus?week=2026-06-22
/recipes/menus?week=2026-06-22&menuId=45
/recipes/menus?week=2026-06-22&newMenu=true
```

If the shared shell or router ergonomics suggest a slightly different parameter
layout, preserve the same behaviour: list and grid state must survive dialog open
and close without a reload.

## Validation

- Recipe name is required, trimmed, not whitespace-only, and at most 200
  characters.
- The recipe category reference is required and must exist.
- Difficulty is either absent or one of `Easy`, `Medium`, `Hard`.
- Servings, when present, is a positive integer; preparation time and cook time,
  when present, are non-negative integers in minutes.
- Recipe notes are optional and at most 2,000 characters.
- Each ingredient name is required, trimmed, not whitespace-only, and at most 200
  characters; the quantity is optional and at most 100 characters; the item
  reference, when present, must exist and satisfy the visibility rule.
- Each step instruction is required, trimmed, not whitespace-only, and at most
  1,000 characters.
- Attachments, when present, are within the shared attachment policy bounds; at
  most one attachment is the primary image.
- Menu week is required and is normalised to the Monday of its ISO week; the menu
  name is optional, trimmed, and at most 200 characters.
- Each menu slot recipe reference must exist and satisfy the visibility rule.
- Recipe and menu visibility are known values.
- Catalogue names are required, trimmed, not whitespace-only, and at most 200
  characters.

## Creation Defaults

A new recipe starts with:

- Visibility `Public`.
- No difficulty, servings, or times.
- No ingredients, steps, notes, or attachments.

A new weekly menu starts with:

- Visibility `Public`.
- No name.
- The week containing the day from which it was created, anchored on its Monday.
- Empty slots.

## Acceptance Criteria

The initial Recipes definition is satisfied when:

1. A `Recipe` carries a required name, a required `RecipeCategory`, an optional
   difficulty, optional servings and preparation/cook times, an ordered ingredient
   list, an ordered step list, optional notes, attachments with an optional primary
   image, and visibility, with standard ownership and audit metadata, and has no
   lifecycle status.
2. The difficulties `Easy`, `Medium`, and `Hard` are available, optional,
   descriptive, and block no operation.
3. A recipe owns an ordered list of ingredients, each with a required free-text
   name, an optional free-text quantity, and an optional live Inventory item
   reference, allowing the same item on multiple lines, edited through
   full-collection replacement.
4. A recipe owns an ordered list of steps, each a required free-text instruction
   shown in position order, with no attachments, edited through full-collection
   replacement.
5. A recipe carries zero or more attachments accepted under the shared policy, with
   at most one primary image used as the gallery thumbnail and a fallback to the
   first image and then a placeholder, all removed on recipe deletion.
6. An ingredient may reference one Inventory item under the Maintenance-style
   visibility rule (a public recipe references only public items; a private recipe
   references any accessible item), resolving the item name with a neutral
   placeholder when it is not resolvable.
7. Deleting an Inventory item referenced by ingredients clears the link on every
   affected ingredient within the deletion transaction, leaves the ingredient line
   as free text, never blocks deletion, reports impact privacy-neutrally, and is
   implemented by contract inversion so Inventory does not depend on Recipes.
8. A `WeeklyMenu` is pinned to one Monday-anchored ISO week with an optional name
   and visibility, exposes a fixed grid of seven days by four slots
   (`Breakfast`/`Lunch`/`Snack`/`Dinner`), holds zero or more recipe references per
   slot with no free text, is created explicitly, and allows multiple menus per
   week.
9. A menu references recipes under the visibility rule (a public menu references
   only public recipes; a private menu references any accessible recipe), resolves
   each recipe with a neutral placeholder when unresolvable, and removes a deleted
   recipe from every slot without blocking the deletion.
10. Recipes owns the `RecipeCategory` catalogue through Configuration, required and
    replace-only, seeded with the accepted initial values, while difficulty and the
    meal-slot set remain fixed enums.
11. Visibility follows the Segaris public-collaboration and private-isolation
    baseline, defaults to `Public`, is changed only by the creator, and is
    inherited by ingredients, steps, attachments, and menu slot references;
    inaccessible records return the standard not-found behaviour.
12. The launcher card never requests attention.
13. Recipes opens on a server-paginated recipe gallery with name search, category
    and difficulty filters, and name/category sorting, and exposes a weekly menu
    planner grid with Monday-anchored week navigation, both using URL-aware dialogs
    that preserve list and grid state, with the ingredient and recipe links chosen
    through the shared entity selector.
14. SQLite and PostgreSQL migrations, backend unit/integration/architecture tests,
    frontend component tests, and a representative Playwright journey verify the
    supported behaviour and privacy boundaries.

## Deferred Decisions

- Whether Recipes should generate a shopping list or consume Inventory stock when a
  menu is cooked.
- Whether servings should drive automatic ingredient-quantity scaling.
- Whether recipes should gain nutrition data, ratings, favourites, a cooking
  history, or a "last cooked" signal.
- Whether recipes should be importable from a URL, a photo, or an external source.
- Whether menus should span arbitrary date ranges, gain attachments, or contribute
  launcher attention (for example a week with no menu).
- Whether ingredient units should become a first-class catalogue rather than
  free-text quantities.
- Whether Recipes should publish read contracts to Analytics or project menu dates
  into a Calendar module.
