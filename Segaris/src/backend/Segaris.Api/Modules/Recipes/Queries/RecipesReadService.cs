using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Api.Modules.Recipes.Contracts;
using Segaris.Api.Modules.Recipes.Domain;
using Segaris.Persistence;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Recipes.Queries;

/// <summary>
/// Read-side queries for Recipes. Every recipe query filters to accessible records
/// before projection, pagination, or detail lookup.
/// </summary>
internal sealed class RecipesReadService(SegarisDbContext database, IInventoryItemReferenceReader itemReferences)
{
    public async Task<IReadOnlyList<RecipeCategoryResponse>> ListCategoriesAsync(CancellationToken cancellationToken)
    {
        return await database.Set<RecipeCategory>()
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Id)
            .Select(category => new RecipeCategoryResponse(category.Id, category.Name, category.SortOrder))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<PaginatedResponse<RecipeSummaryResponse>> ListRecipesAsync(
        RecipeFilter filter,
        PaginationRequest pagination,
        SortRequest sort,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var recipes = ApplyFilters(
            database.Set<Recipe>().AsNoTracking().Where(RecipePolicies.AccessibleTo(userId)),
            filter);

        var totalCount = await recipes.CountAsync(cancellationToken);

        var rows = await ApplySort(recipes, sort)
            .Skip(pagination.Offset)
            .Take(pagination.PageSize)
            .Select(recipe => new RecipeSummaryRow(
                recipe.Id,
                recipe.Name,
                recipe.CategoryId,
                database.Set<RecipeCategory>()
                    .Where(category => category.Id == recipe.CategoryId).Select(category => category.Name).First(),
                recipe.Difficulty,
                recipe.Visibility,
                recipe.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == recipe.CreatedBy).Select(user => user.DisplayName).First()))
            .ToArrayAsync(cancellationToken);

        var page = rows
            .Select(row => new RecipeSummaryResponse(
                row.Id,
                row.Name,
                row.CategoryId,
                row.CategoryName,
                row.Difficulty?.ToString(),
                row.Visibility.ToString(),
                PlaceholderThumbnail(),
                row.CreatorId,
                row.CreatorName))
            .ToArray();

        return PaginatedResponse<RecipeSummaryResponse>.Create(page, pagination, totalCount);
    }

    public async Task<RecipeResponse?> GetRecipeAsync(
        int recipeId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var row = await database.Set<Recipe>()
            .AsNoTracking()
            .Where(RecipePolicies.AccessibleTo(userId))
            .Where(recipe => recipe.Id == recipeId)
            .Select(recipe => new RecipeDetailRow(
                recipe.Id,
                recipe.Name,
                recipe.CategoryId,
                database.Set<RecipeCategory>()
                    .Where(category => category.Id == recipe.CategoryId).Select(category => category.Name).First(),
                recipe.Difficulty,
                recipe.Servings,
                recipe.PreparationMinutes,
                recipe.CookMinutes,
                recipe.Notes,
                recipe.Visibility,
                recipe.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == recipe.CreatedBy).Select(user => user.DisplayName).First(),
                recipe.CreatedAt,
                recipe.UpdatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == recipe.UpdatedBy).Select(user => user.DisplayName).First(),
                recipe.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var ingredientRows = await database.Set<RecipeIngredient>()
            .AsNoTracking()
            .Where(ingredient => ingredient.RecipeId == row.Id)
            .OrderBy(ingredient => ingredient.Position)
            .ThenBy(ingredient => ingredient.Id)
            .Select(ingredient => new RecipeIngredientRow(
                ingredient.Id,
                ingredient.Name,
                ingredient.Quantity,
                ingredient.ItemId,
                ingredient.Position))
            .ToArrayAsync(cancellationToken);

        var itemIds = ingredientRows
            .Select(ingredient => ingredient.ItemId)
            .Where(itemId => itemId.HasValue)
            .Select(itemId => itemId!.Value)
            .Distinct()
            .ToArray();
        var resolvedItems = await itemReferences.ResolveAccessibleAsync(itemIds, userId, cancellationToken);
        var ingredients = ingredientRows
            .Select(ingredient => new RecipeIngredientResponse(
                ingredient.Id,
                ingredient.Name,
                ingredient.Quantity,
                ingredient.ItemId,
                ingredient.ItemId is { } itemId && resolvedItems.TryGetValue(itemId, out var item)
                    ? item.Name
                    : null,
                ingredient.Position))
            .ToArray();

        var steps = await database.Set<RecipeStep>()
            .AsNoTracking()
            .Where(step => step.RecipeId == row.Id)
            .OrderBy(step => step.Position)
            .ThenBy(step => step.Id)
            .Select(step => new RecipeStepResponse(step.Id, step.Instruction, step.Position))
            .ToArrayAsync(cancellationToken);

        return new RecipeResponse(
            row.Id,
            row.Name,
            row.CategoryId,
            row.CategoryName,
            row.Difficulty?.ToString(),
            row.Servings,
            row.PreparationMinutes,
            row.CookMinutes,
            ingredients,
            steps,
            row.Notes,
            row.Visibility.ToString(),
            PlaceholderThumbnail(),
            Attachments: [],
            row.CreatedById,
            row.CreatedByName,
            row.CreatedAt,
            row.UpdatedById,
            row.UpdatedByName,
            row.UpdatedAt);
    }

    private static IQueryable<Recipe> ApplyFilters(
        IQueryable<Recipe> recipes,
        RecipeFilter filter)
    {
        if (filter.Search is { } search)
        {
            var pattern = $"%{Escape(search.ToLowerInvariant())}%";
            recipes = recipes.Where(recipe =>
                EF.Functions.Like(recipe.Name.ToLower(), pattern, "\\")
                || (recipe.Notes != null && EF.Functions.Like(recipe.Notes.ToLower(), pattern, "\\")));
        }

        if (filter.CategoryId is { } categoryId)
        {
            recipes = recipes.Where(recipe => recipe.CategoryId == categoryId);
        }

        if (filter.Difficulty is { } difficulty)
        {
            recipes = recipes.Where(recipe => recipe.Difficulty == difficulty);
        }

        return recipes;
    }

    private IQueryable<Recipe> ApplySort(IQueryable<Recipe> recipes, SortRequest sort)
    {
        var ascending = sort.Direction == SortDirection.Ascending;

        IOrderedQueryable<Recipe> ordered = sort.Field switch
        {
            RecipeQuery.SortFields.Category => ascending
                ? recipes.OrderBy(recipe => database.Set<RecipeCategory>()
                    .Where(category => category.Id == recipe.CategoryId).Select(category => category.Name).First())
                : recipes.OrderByDescending(recipe => database.Set<RecipeCategory>()
                    .Where(category => category.Id == recipe.CategoryId).Select(category => category.Name).First()),
            RecipeQuery.SortFields.TieBreaker => ascending
                ? recipes.OrderBy(recipe => recipe.Id)
                : recipes.OrderByDescending(recipe => recipe.Id),
            _ => ascending
                ? recipes.OrderBy(recipe => recipe.Name)
                : recipes.OrderByDescending(recipe => recipe.Name),
        };

        return ascending ? ordered.ThenBy(recipe => recipe.Id) : ordered.ThenByDescending(recipe => recipe.Id);
    }

    private static RecipeThumbnailResponse PlaceholderThumbnail() => new(null, null, "placeholder");

    private static string Escape(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);

    private sealed record RecipeSummaryRow(
        int Id,
        string Name,
        int CategoryId,
        string CategoryName,
        RecipeDifficulty? Difficulty,
        RecordVisibility Visibility,
        int CreatorId,
        string CreatorName);

    private sealed record RecipeIngredientRow(
        int Id,
        string Name,
        string? Quantity,
        int? ItemId,
        int Position);

    private sealed record RecipeDetailRow(
        int Id,
        string Name,
        int CategoryId,
        string CategoryName,
        RecipeDifficulty? Difficulty,
        int? Servings,
        int? PreparationMinutes,
        int? CookMinutes,
        string? Notes,
        RecordVisibility Visibility,
        int CreatedById,
        string CreatedByName,
        DateTimeOffset CreatedAt,
        int UpdatedById,
        string UpdatedByName,
        DateTimeOffset UpdatedAt);
}
