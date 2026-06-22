using System.Text.Json;
using Segaris.Api.Modules.Recipes;
using Segaris.Api.Modules.Recipes.Contracts;
using Segaris.Api.Modules.Recipes.Domain;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.UnitTests;

public sealed class RecipesContractTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Fixed_vocabularies_are_frozen()
    {
        Assert.Equal(["Easy", "Medium", "Hard"], Enum.GetNames<RecipeDifficulty>());
        Assert.Equal(["Breakfast", "Lunch", "Snack", "Dinner"], Enum.GetNames<MealSlot>());
    }

    [Fact]
    public void Menu_grid_shape_is_frozen()
    {
        Assert.Equal(
            [MealSlot.Breakfast, MealSlot.Lunch, MealSlot.Snack, MealSlot.Dinner],
            RecipesDefaults.MealSlots);
        Assert.Equal(
            [
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
                DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday,
            ],
            RecipesDefaults.MenuDays);
        Assert.Equal(DayOfWeek.Monday, RecipesDefaults.MenuDays[0]);
    }

    [Fact]
    public void Creation_defaults_and_validation_bounds_are_frozen()
    {
        Assert.Equal(RecordVisibility.Public, RecipesDefaults.Visibility);
        Assert.Equal(200, RecipesDefaults.NameMaximumLength);
        Assert.Equal(2000, RecipesDefaults.NotesMaximumLength);
        Assert.Equal(200, RecipesDefaults.IngredientNameMaximumLength);
        Assert.Equal(100, RecipesDefaults.IngredientQuantityMaximumLength);
        Assert.Equal(1000, RecipesDefaults.StepInstructionMaximumLength);
        Assert.Equal(200, RecipesDefaults.MenuNameMaximumLength);
        Assert.Equal(200, RecipesDefaults.CategoryNameMaximumLength);
    }

    [Fact]
    public void Routes_freeze_recipes_menus_categories_attachments_and_primary_image()
    {
        Assert.Equal("recipes", RecipesApiRoutes.Recipes);
        Assert.Equal("/{recipeId:int}", RecipesApiRoutes.RecipeById);
        Assert.Equal("/{recipeId:int}/attachments", RecipesApiRoutes.RecipeAttachments);
        Assert.Equal("/{recipeId:int}/attachments/{attachmentId}", RecipesApiRoutes.RecipeAttachmentById);
        Assert.Equal(
            "/{recipeId:int}/attachments/{attachmentId}/primary",
            RecipesApiRoutes.RecipePrimaryAttachment);
        Assert.Equal("recipes/menus", RecipesApiRoutes.Menus);
        Assert.Equal("/{menuId:int}", RecipesApiRoutes.MenuById);
        Assert.Equal("recipes/categories", RecipesApiRoutes.Categories);
    }

    [Fact]
    public void Recipe_sort_and_pagination_contracts_are_frozen()
    {
        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal) { "name", "category", "id" },
            RecipeQuery.AllowedSortFields);
        Assert.Equal("name", RecipeQuery.SortFields.Default);
        Assert.Equal("id", RecipeQuery.SortFields.TieBreaker);
        Assert.Equal("asc", RecipeQuery.DefaultSortDirection);
        Assert.Equal([10, 25, 50, 100], RecipeQuery.PageSizeOptions);
    }

    [Fact]
    public void Default_recipe_sort_is_name_ascending_with_identifier_tie_breaker()
    {
        var sort = SortRequest.Create(
            null,
            null,
            RecipeQuery.AllowedSortFields,
            RecipeQuery.SortFields.Default,
            RecipeQuery.SortFields.TieBreaker);

        Assert.Equal("name", sort.Field);
        Assert.Equal(SortDirection.Ascending, sort.Direction);
        Assert.Equal("id", sort.TieBreakerField);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Pagination_rejects_page_sizes_outside_platform_bounds(int pageSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PaginationRequest(1, pageSize));
    }

    [Fact]
    public void Configuration_facing_catalog_contracts_are_explicit()
    {
        Assert.Empty(RecipesConfigurationContracts.SharedReferenceKinds);
        Assert.Equal(
            [RecipesCatalogKind.RecipeCategories],
            RecipesConfigurationContracts.OwnedCatalogs.Select(descriptor => descriptor.Kind).ToArray());

        var categories = RecipesConfigurationContracts.OwnedCatalogs[0];
        Assert.Equal("categories", categories.RouteSegment);
        Assert.True(categories.IsRequired);
        Assert.False(categories.SupportsClearing);
    }

    [Fact]
    public void Error_codes_are_namespaced_and_stable()
    {
        Assert.Equal("recipes.recipe.not_found", RecipesErrorCodes.RecipeNotFound.Value);
        Assert.Equal("recipes.recipe.validation", RecipesErrorCodes.RecipeValidation.Value);
        Assert.Equal("recipes.recipe.visibility_forbidden", RecipesErrorCodes.RecipeVisibilityForbidden.Value);
        Assert.Equal("recipes.ingredient.item_not_accessible", RecipesErrorCodes.IngredientItemNotAccessible.Value);
        Assert.Equal("recipes.menu.not_found", RecipesErrorCodes.MenuNotFound.Value);
        Assert.Equal("recipes.menu.recipe_not_accessible", RecipesErrorCodes.MenuRecipeNotAccessible.Value);
        Assert.Equal("recipes.catalog.unknown_reference", RecipesErrorCodes.UnknownCatalogReference.Value);
        Assert.Equal("recipes.attachment.not_found", RecipesErrorCodes.AttachmentNotFound.Value);
        Assert.Equal("recipes.attachment.primary_invalid", RecipesErrorCodes.AttachmentPrimaryInvalid.Value);
        Assert.Equal("recipes.category.not_found", RecipesErrorCodes.CategoryNotFound.Value);
        Assert.Equal("recipes.category.referenced", RecipesErrorCodes.CategoryReferenced.Value);
    }

    [Fact]
    public void Attachment_owner_uses_recipe_kind()
    {
        var recipe = RecipesAttachments.RecipeOwner(12);

        Assert.Equal(("Recipes", "Recipe", "12"), (recipe.Module, recipe.EntityType, recipe.EntityId));
    }

    [Fact]
    public void Recipe_request_serializes_ingredients_and_steps_to_the_frozen_wire_shape()
    {
        var request = new CreateRecipeRequest(
            "Tortilla",
            CategoryId: 1,
            Difficulty: "Easy",
            Servings: 4,
            PreparationMinutes: 10,
            CookMinutes: 15,
            Ingredients:
            [
                new RecipeIngredientRequest("Egg", "4 units", ItemId: 7),
                new RecipeIngredientRequest("Salt", "to taste", ItemId: null),
            ],
            Steps:
            [
                new RecipeStepRequest("Beat the eggs"),
                new RecipeStepRequest("Cook on low heat"),
            ],
            Notes: null,
            Visibility: "Public");

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(request, Web));
        var root = document.RootElement;
        Assert.Equal("Tortilla", root.GetProperty("name").GetString());
        Assert.Equal(1, root.GetProperty("categoryId").GetInt32());
        Assert.Equal("Easy", root.GetProperty("difficulty").GetString());

        var ingredients = root.GetProperty("ingredients");
        Assert.Equal(2, ingredients.GetArrayLength());
        Assert.Equal("Egg", ingredients[0].GetProperty("name").GetString());
        Assert.Equal(7, ingredients[0].GetProperty("itemId").GetInt32());
        Assert.Equal(JsonValueKind.Null, ingredients[1].GetProperty("itemId").ValueKind);

        var steps = root.GetProperty("steps");
        Assert.Equal(2, steps.GetArrayLength());
        Assert.Equal("Beat the eggs", steps[0].GetProperty("instruction").GetString());
    }

    [Fact]
    public void Weekly_menu_request_serializes_the_slot_grid_to_the_frozen_wire_shape()
    {
        var request = new CreateWeeklyMenuRequest(
            Week: new DateOnly(2026, 6, 22),
            Name: "Guests",
            Visibility: "Public",
            Slots:
            [
                new WeeklyMenuSlotRequest("Monday", "Lunch", [1, 2]),
                new WeeklyMenuSlotRequest("Sunday", "Dinner", [3]),
            ]);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(request, Web));
        var root = document.RootElement;
        Assert.Equal("2026-06-22", root.GetProperty("week").GetString());
        Assert.Equal("Guests", root.GetProperty("name").GetString());

        var slots = root.GetProperty("slots");
        Assert.Equal(2, slots.GetArrayLength());
        Assert.Equal("Monday", slots[0].GetProperty("day").GetString());
        Assert.Equal("Lunch", slots[0].GetProperty("slot").GetString());
        Assert.Equal(
            [1, 2],
            slots[0].GetProperty("recipeIds").EnumerateArray().Select(value => value.GetInt32()).ToArray());
    }
}
