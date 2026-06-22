using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Recipes.Contracts;
using Segaris.Api.Modules.Recipes.Domain;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Recipes.Mutations;

/// <summary>
/// Write-side operations on Recipes. Authorization follows recipe visibility:
/// inaccessible recipes are reported as not found so private records are never
/// disclosed.
/// </summary>
internal sealed class RecipesRecipeWriteService(SegarisDbContext database, IClock clock)
{
    public async Task<int> CreateAsync(
        CreateRecipeRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var values = Map(
            request.Name,
            request.CategoryId,
            request.Difficulty,
            request.Servings,
            request.PreparationMinutes,
            request.CookMinutes,
            request.Ingredients,
            request.Steps,
            request.Notes,
            request.Visibility);
        var recipe = Recipe.Create(values, actorId, clock.UtcNow);
        await ValidateReferencesAsync(values, cancellationToken);

        database.Add(recipe);
        await database.SaveChangesAsync(cancellationToken);
        return recipe.Id;
    }

    public async Task<bool> UpdateAsync(
        int recipeId,
        UpdateRecipeRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var recipe = await database.Set<Recipe>()
            .Where(RecipePolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == recipeId)
            .Include(candidate => candidate.Ingredients)
            .Include(candidate => candidate.Steps)
            .FirstOrDefaultAsync(cancellationToken);
        if (recipe is null)
        {
            return false;
        }

        var values = Map(
            request.Name,
            request.CategoryId,
            request.Difficulty,
            request.Servings,
            request.PreparationMinutes,
            request.CookMinutes,
            request.Ingredients,
            request.Steps,
            request.Notes,
            request.Visibility);

        ValidateVisibilityChange(recipe, values.Visibility, actorId);
        recipe.Update(values, actorId, clock.UtcNow);
        await ValidateReferencesAsync(values, cancellationToken);

        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        int recipeId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var recipe = await database.Set<Recipe>()
            .Where(RecipePolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == recipeId)
            .FirstOrDefaultAsync(cancellationToken);
        if (recipe is null)
        {
            return false;
        }

        database.Remove(recipe);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ValidateReferencesAsync(
        RecipeValues values,
        CancellationToken cancellationToken)
    {
        var categoryExists = await database.Set<RecipeCategory>()
            .AnyAsync(category => category.Id == values.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new RecipesValidationException(
                "One or more Recipes catalog references do not exist.",
                RecipesValidationReason.CatalogReference);
        }
    }

    private static void ValidateVisibilityChange(
        Recipe recipe,
        RecordVisibility requestedVisibility,
        UserId actorId)
    {
        if (requestedVisibility != recipe.Visibility
            && !RecipePolicies.CanChangeVisibility(recipe, actorId))
        {
            throw new RecipesValidationException(
                "Only the creator may change recipe visibility.",
                RecipesValidationReason.VisibilityForbidden);
        }
    }

    private static RecipeValues Map(
        string? name,
        int categoryId,
        string? difficulty,
        int? servings,
        int? preparationMinutes,
        int? cookMinutes,
        IReadOnlyList<RecipeIngredientRequest>? ingredients,
        IReadOnlyList<RecipeStepRequest>? steps,
        string? notes,
        string? visibility) => new(
            name ?? string.Empty,
            categoryId,
            difficulty,
            servings,
            preparationMinutes,
            cookMinutes,
            (ingredients ?? []).Select(ingredient => new RecipeIngredientValues(
                ingredient.Name,
                ingredient.Quantity,
                ItemId: null)).ToArray(),
            (steps ?? []).Select(step => new RecipeStepValues(step.Instruction)).ToArray(),
            notes,
            ParseEnum(visibility, RecipesDefaults.Visibility, "visibility"));

    private static TEnum ParseEnum<TEnum>(string? value, TEnum defaultValue, string field)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new RecipesValidationException($"The {field} is not a recognized value.");
    }
}
