using Segaris.Api.Modules.Recipes.Domain;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.UnitTests;

public sealed class RecipesDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 10, 0, 0, TimeSpan.Zero);
    private static readonly UserId Creator = new(1);

    // ── Week normalization ──────────────────────────────────────────────────────

    [Fact]
    public void Week_that_is_already_monday_is_unchanged()
    {
        var monday = new DateOnly(2026, 6, 22); // known Monday
        Assert.Equal(DayOfWeek.Monday, monday.DayOfWeek);
        Assert.Equal(monday, WeeklyMenu.NormalizeWeek(monday));
    }

    [Theory]
    [InlineData(2026, 6, 23, 2026, 6, 22)] // Tuesday  → previous Monday
    [InlineData(2026, 6, 24, 2026, 6, 22)] // Wednesday → previous Monday
    [InlineData(2026, 6, 25, 2026, 6, 22)] // Thursday  → previous Monday
    [InlineData(2026, 6, 26, 2026, 6, 22)] // Friday    → previous Monday
    [InlineData(2026, 6, 27, 2026, 6, 22)] // Saturday  → previous Monday
    [InlineData(2026, 6, 28, 2026, 6, 22)] // Sunday    → previous Monday
    public void Week_is_normalized_to_the_preceding_monday(
        int year, int month, int day,
        int expectedYear, int expectedMonth, int expectedDay)
    {
        var date = new DateOnly(year, month, day);
        var expected = new DateOnly(expectedYear, expectedMonth, expectedDay);
        Assert.Equal(expected, WeeklyMenu.NormalizeWeek(date));
    }

    [Fact]
    public void Weekly_menu_create_normalizes_week_to_monday()
    {
        var thursday = new DateOnly(2026, 6, 25);
        var menu = WeeklyMenu.Create(MenuValues(thursday), Creator, Now);
        Assert.Equal(new DateOnly(2026, 6, 22), menu.Week);
    }

    [Fact]
    public void Weekly_menu_update_normalizes_week_to_monday()
    {
        var menu = WeeklyMenu.Create(MenuValues(new DateOnly(2026, 6, 22)), Creator, Now);
        var saturday = new DateOnly(2026, 6, 27);
        menu.Update(MenuValues(saturday), Creator, Now);
        Assert.Equal(new DateOnly(2026, 6, 22), menu.Week);
    }

    // ── Ingredient positions ────────────────────────────────────────────────────

    [Fact]
    public void Recipe_assigns_positions_from_index_on_create()
    {
        var recipe = Recipe.Create(RecipeValues(
            ingredients:
            [
                new("Egg", "4 units", null),
                new("Salt", "to taste", null),
                new("Oil", null, null),
            ]),
            Creator,
            Now);

        Assert.Equal(0, recipe.Ingredients[0].Position);
        Assert.Equal(1, recipe.Ingredients[1].Position);
        Assert.Equal(2, recipe.Ingredients[2].Position);
    }

    [Fact]
    public void Recipe_assigns_step_positions_from_index_on_create()
    {
        var recipe = Recipe.Create(RecipeValues(
            steps:
            [
                new("Beat eggs"),
                new("Add salt"),
                new("Cook on low heat"),
            ]),
            Creator,
            Now);

        Assert.Equal(0, recipe.Steps[0].Position);
        Assert.Equal(1, recipe.Steps[1].Position);
        Assert.Equal(2, recipe.Steps[2].Position);
    }

    [Fact]
    public void Recipe_replaces_ingredients_in_full_on_update()
    {
        var recipe = Recipe.Create(
            RecipeValues(ingredients: [new("Egg", null, null), new("Salt", null, null)]),
            Creator,
            Now);

        recipe.Update(RecipeValues(ingredients: [new("Flour", "200g", null)]), Creator, Now);

        Assert.Single(recipe.Ingredients);
        Assert.Equal("Flour", recipe.Ingredients[0].Name);
        Assert.Equal(0, recipe.Ingredients[0].Position);
    }

    [Fact]
    public void Recipe_replaces_steps_in_full_on_update()
    {
        var recipe = Recipe.Create(
            RecipeValues(steps: [new("Step one"), new("Step two")]),
            Creator,
            Now);

        recipe.Update(RecipeValues(steps: [new("New step")]), Creator, Now);

        Assert.Single(recipe.Steps);
        Assert.Equal("New step", recipe.Steps[0].Instruction);
        Assert.Equal(0, recipe.Steps[0].Position);
    }

    // ── Difficulty parsing ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("Easy")]
    [InlineData("easy")]
    [InlineData("EASY")]
    public void Recipe_accepts_case_insensitive_difficulty(string value)
    {
        var recipe = Recipe.Create(RecipeValues(difficulty: value), Creator, Now);
        Assert.Equal(RecipeDifficulty.Easy, recipe.Difficulty);
    }

    [Fact]
    public void Recipe_accepts_null_difficulty()
    {
        var recipe = Recipe.Create(RecipeValues(difficulty: null), Creator, Now);
        Assert.Null(recipe.Difficulty);
    }

    [Fact]
    public void Recipe_rejects_unknown_difficulty()
    {
        Assert.Throws<RecipesValidationException>(
            () => Recipe.Create(RecipeValues(difficulty: "Extreme"), Creator, Now));
    }

    // ── Recipe validation ───────────────────────────────────────────────────────

    [Fact]
    public void Recipe_trims_name_and_stamps_audit()
    {
        var recipe = Recipe.Create(RecipeValues(name: " Tortilla "), Creator, Now);

        Assert.Equal("Tortilla", recipe.Name);
        Assert.Equal(Creator.Value, recipe.CreatedBy);
        Assert.Equal(Creator.Value, recipe.UpdatedBy);
        Assert.Equal(Now, recipe.CreatedAt);
        Assert.Equal(Now, recipe.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Recipe_rejects_blank_name(string name)
    {
        Assert.Throws<RecipesValidationException>(
            () => Recipe.Create(RecipeValues(name: name), Creator, Now));
    }

    [Fact]
    public void Recipe_rejects_invalid_category()
    {
        Assert.Throws<RecipesValidationException>(
            () => Recipe.Create(RecipeValues(categoryId: 0), Creator, Now));
    }

    [Fact]
    public void Recipe_rejects_invalid_visibility()
    {
        Assert.Throws<RecipesValidationException>(
            () => Recipe.Create(RecipeValues(visibility: (RecordVisibility)99), Creator, Now));
    }

    [Fact]
    public void Recipe_rejects_non_utc_timestamp()
    {
        var localNow = new DateTimeOffset(2026, 6, 22, 10, 0, 0, TimeSpan.FromHours(2));
        Assert.Throws<RecipesValidationException>(
            () => Recipe.Create(RecipeValues(), Creator, localNow));
    }

    [Fact]
    public void Recipe_rejects_servings_out_of_range()
    {
        Assert.Throws<RecipesValidationException>(
            () => Recipe.Create(RecipeValues(servings: 0), Creator, Now));
        Assert.Throws<RecipesValidationException>(
            () => Recipe.Create(RecipeValues(servings: RecipesValidation.MaximumServings + 1), Creator, Now));
    }

    [Fact]
    public void Recipe_rejects_preparation_minutes_out_of_range()
    {
        Assert.Throws<RecipesValidationException>(
            () => Recipe.Create(RecipeValues(preparationMinutes: 0), Creator, Now));
    }

    [Fact]
    public void Recipe_accepts_null_servings_and_times()
    {
        var recipe = Recipe.Create(RecipeValues(servings: null, preparationMinutes: null, cookMinutes: null), Creator, Now);
        Assert.Null(recipe.Servings);
        Assert.Null(recipe.PreparationMinutes);
        Assert.Null(recipe.CookMinutes);
    }

    [Fact]
    public void Recipe_rejects_too_many_ingredients()
    {
        var ingredients = Enumerable
            .Range(0, RecipesValidation.MaximumIngredients + 1)
            .Select(_ => new RecipeIngredientValues("Egg", null, null))
            .ToArray();

        Assert.Throws<RecipesValidationException>(
            () => Recipe.Create(RecipeValues(ingredients: ingredients), Creator, Now));
    }

    [Fact]
    public void Recipe_rejects_too_many_steps()
    {
        var steps = Enumerable
            .Range(0, RecipesValidation.MaximumSteps + 1)
            .Select(_ => new RecipeStepValues("Some step"))
            .ToArray();

        Assert.Throws<RecipesValidationException>(
            () => Recipe.Create(RecipeValues(steps: steps), Creator, Now));
    }

    // ── Category replace ────────────────────────────────────────────────────────

    [Fact]
    public void Recipe_replace_category_stamps_modification()
    {
        var recipe = Recipe.Create(RecipeValues(categoryId: 1), Creator, Now);
        var later = Now.AddHours(1);
        recipe.ReplaceCategory(2, Creator, later);

        Assert.Equal(2, recipe.CategoryId);
        Assert.Equal(later, recipe.UpdatedAt);
    }

    [Fact]
    public void Recipe_replace_category_rejects_zero_id()
    {
        var recipe = Recipe.Create(RecipeValues(), Creator, Now);
        Assert.Throws<RecipesValidationException>(
            () => recipe.ReplaceCategory(0, Creator, Now));
    }

    // ── Menu slot duplicate protection ─────────────────────────────────────────

    [Fact]
    public void Weekly_menu_rejects_duplicate_recipe_in_same_slot()
    {
        var slots = new List<WeeklyMenuSlotValues>
        {
            new("Monday", "Lunch", [1, 1]),
        };
        Assert.Throws<RecipesValidationException>(
            () => WeeklyMenu.Create(MenuValues(new DateOnly(2026, 6, 22), slots), Creator, Now));
    }

    [Fact]
    public void Weekly_menu_rejects_invalid_day()
    {
        var slots = new List<WeeklyMenuSlotValues>
        {
            new("Funday", "Lunch", [1]),
        };
        Assert.Throws<RecipesValidationException>(
            () => WeeklyMenu.Create(MenuValues(new DateOnly(2026, 6, 22), slots), Creator, Now));
    }

    [Fact]
    public void Weekly_menu_rejects_invalid_slot()
    {
        var slots = new List<WeeklyMenuSlotValues>
        {
            new("Monday", "Brunch", [1]),
        };
        Assert.Throws<RecipesValidationException>(
            () => WeeklyMenu.Create(MenuValues(new DateOnly(2026, 6, 22), slots), Creator, Now));
    }

    [Fact]
    public void Weekly_menu_remove_recipe_slots_clears_matching_entries()
    {
        var slots = new List<WeeklyMenuSlotValues>
        {
            new("Monday", "Lunch", [1, 2]),
            new("Tuesday", "Dinner", [1]),
        };
        var menu = WeeklyMenu.Create(MenuValues(new DateOnly(2026, 6, 22), slots), Creator, Now);
        Assert.Equal(3, menu.SlotRecipes.Count);

        menu.RemoveRecipeSlots(recipeId: 1);

        Assert.Single(menu.SlotRecipes);
        Assert.Equal(2, menu.SlotRecipes[0].RecipeId);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static RecipeValues RecipeValues(
        string? name = "Tortilla",
        int categoryId = 1,
        string? difficulty = null,
        int? servings = null,
        int? preparationMinutes = null,
        int? cookMinutes = null,
        IReadOnlyList<RecipeIngredientValues>? ingredients = null,
        IReadOnlyList<RecipeStepValues>? steps = null,
        string? notes = null,
        RecordVisibility visibility = RecordVisibility.Public) =>
        new(name, categoryId, difficulty, servings, preparationMinutes, cookMinutes,
            ingredients ?? [], steps ?? [], notes, visibility);

    private static WeeklyMenuValues MenuValues(
        DateOnly? week = null,
        IReadOnlyList<WeeklyMenuSlotValues>? slots = null) =>
        new(week ?? new DateOnly(2026, 6, 22), Name: null, RecordVisibility.Public, slots ?? []);
}
