using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.Modules.Recipes.Domain;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.IntegrationTests.Recipes;

internal static class RecipesTestData
{
    private static readonly DateTimeOffset SeedNow = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    public static async Task<int> CategoryIdAsync(IServiceProvider services, string name)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<RecipeCategory>().Where(category => category.Name == name).Select(category => category.Id).SingleAsync();
    }

    public static async Task<bool> RecipeExistsAsync(IServiceProvider services, int recipeId)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<Recipe>().AnyAsync(recipe => recipe.Id == recipeId);
    }

    public static async Task<int> SeedRecipeAsync(
        IServiceProvider services,
        int creatorId,
        string name = "Recipe",
        string categoryName = "Other",
        RecipeDifficulty? difficulty = null,
        int? servings = null,
        int? preparationMinutes = null,
        int? cookMinutes = null,
        IReadOnlyList<RecipeIngredientValues>? ingredients = null,
        IReadOnlyList<RecipeStepValues>? steps = null,
        string? notes = null,
        RecordVisibility visibility = RecordVisibility.Public)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();

        var categoryId = await database.Set<RecipeCategory>()
            .Where(category => category.Name == categoryName)
            .Select(category => category.Id)
            .SingleAsync();

        var recipe = Recipe.Create(
            new RecipeValues(
                name,
                categoryId,
                difficulty?.ToString(),
                servings,
                preparationMinutes,
                cookMinutes,
                ingredients ?? [],
                steps ?? [],
                notes,
                visibility),
            new UserId(creatorId),
            SeedNow);

        database.Add(recipe);
        await database.SaveChangesAsync();
        return recipe.Id;
    }
}
