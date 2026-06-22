using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Recipes.Contracts;
using Segaris.Api.Modules.Recipes.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Recipes.Mutations;

/// <summary>
/// Administrative lifecycle for the Recipes-owned category catalog. It mirrors the
/// established module-owned catalog conventions (creation at the catalog tail,
/// rename, reorder, privacy-neutral deletion impact, final-row protection, and
/// atomic replace-and-delete) while keeping ownership inside Recipes. Because every
/// recipe requires a category, references may only be replaced and never cleared.
/// </summary>
internal sealed class RecipesCategoryManagementService(SegarisDbContext database, IClock clock)
{
    public async Task<RecipeCategoryResponse> CreateAsync(CatalogItemRequest request, UserId actor, CancellationToken token)
    {
        var name = ValidateName(request.Name);
        await EnsureUniqueAsync(null, name.Normalized, token);
        var now = clock.UtcNow;
        var sortOrder = (await database.Set<RecipeCategory>().Select(c => (int?)c.SortOrder).MaxAsync(token) ?? -1) + 1;
        var category = new RecipeCategory
        {
            Name = name.Display,
            NormalizedName = name.Normalized,
            SortOrder = sortOrder,
            CreatedAt = now,
            CreatedBy = actor.Value,
            UpdatedAt = now,
            UpdatedBy = actor.Value,
        };
        database.Add(category);
        await SaveAsync(token);
        return ToResponse(category);
    }

    public async Task<RecipeCategoryResponse> UpdateAsync(int id, CatalogItemRequest request, UserId actor, CancellationToken token)
    {
        var category = await FindAsync(id, token);
        var name = ValidateName(request.Name);
        await EnsureUniqueAsync(id, name.Normalized, token);
        category.Name = name.Display;
        category.NormalizedName = name.Normalized;
        category.UpdatedAt = clock.UtcNow;
        category.UpdatedBy = actor.Value;
        await SaveAsync(token);
        return ToResponse(category);
    }

    public async Task MoveAsync(int id, CatalogMoveDirection direction, CancellationToken token)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var ordered = await database.Set<RecipeCategory>()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
            .ToListAsync(token);
        var index = ordered.FindIndex(c => c.Id == id);
        if (index < 0) throw RecipesCategoryProblem.NotFound();
        var target = direction == CatalogMoveDirection.Up ? index - 1 : index + 1;
        if (target < 0 || target >= ordered.Count)
            throw RecipesCategoryProblem.Validation("direction", "The category cannot move beyond the catalog boundary.");
        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (var position = 0; position < ordered.Count; position++) ordered[position].SortOrder = position;
        await database.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
    }

    public async Task<CatalogDeletionImpactResponse> ImpactAsync(int id, CancellationToken token)
    {
        await FindAsync(id, token, tracked: false);
        var referenced = await database.Set<Recipe>().AnyAsync(r => r.CategoryId == id, token);
        var count = await database.Set<RecipeCategory>().CountAsync(token);
        return new(referenced, !referenced && count > 1, false, false, count > 1);
    }

    public async Task DeleteAsync(int id, CancellationToken token)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var category = await FindAsync(id, token);
        if (await database.Set<RecipeCategory>().CountAsync(token) <= 1)
            throw RecipesCategoryProblem.RequiredNotEmpty();
        if (await database.Set<Recipe>().AnyAsync(r => r.CategoryId == id, token))
            throw RecipesCategoryProblem.Referenced();
        database.Remove(category);
        var remaining = await database.Set<RecipeCategory>()
            .Where(c => c.Id != id)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
            .ToListAsync(token);
        for (var position = 0; position < remaining.Count; position++) remaining[position].SortOrder = position;
        try
        {
            await database.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        }
        catch (DbUpdateException)
        {
            throw RecipesCategoryProblem.Referenced();
        }
    }

    public async Task ReplaceAndDeleteAsync(int id, CatalogReplacementRequest request, UserId actor, CancellationToken token)
    {
        if (request.ClearReferences
            || request.ExchangeRate is not null
            || request.ReplacementId is not { } replacementId
            || replacementId <= 0
            || replacementId == id)
        {
            throw RecipesCategoryProblem.InvalidReplacement();
        }

        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var source = await FindAsync(id, token);
        if (!await database.Set<RecipeCategory>().AnyAsync(c => c.Id == replacementId, token))
        {
            throw RecipesCategoryProblem.InvalidReplacement();
        }

        if (await database.Set<RecipeCategory>().CountAsync(token) <= 1)
        {
            throw RecipesCategoryProblem.RequiredNotEmpty();
        }

        try
        {
            var recipes = await database.Set<Recipe>()
                .Where(r => r.CategoryId == id)
                .ToListAsync(token);
            var occurredAt = clock.UtcNow;
            foreach (var recipe in recipes)
            {
                recipe.ReplaceCategory(replacementId, actor, occurredAt);
            }

            database.Remove(source);
            var remaining = await database.Set<RecipeCategory>()
                .Where(c => c.Id != id)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
                .ToListAsync(token);
            for (var position = 0; position < remaining.Count; position++) remaining[position].SortOrder = position;

            await database.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        }
        catch (DbUpdateException)
        {
            throw RecipesCategoryProblem.MigrationConflict();
        }
    }

    private async Task<RecipeCategory> FindAsync(int id, CancellationToken token, bool tracked = true)
    {
        IQueryable<RecipeCategory> query = database.Set<RecipeCategory>();
        if (!tracked) query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(c => c.Id == id, token)
               ?? throw RecipesCategoryProblem.NotFound();
    }

    private async Task EnsureUniqueAsync(int? id, string normalized, CancellationToken token)
    {
        if (await database.Set<RecipeCategory>()
                .AnyAsync(c => c.NormalizedName == normalized && c.Id != id, token))
        {
            throw RecipesCategoryProblem.DuplicateName();
        }
    }

    private async Task SaveAsync(CancellationToken token)
    {
        try
        {
            await database.SaveChangesAsync(token);
        }
        catch (DbUpdateException)
        {
            throw RecipesCategoryProblem.DuplicateName();
        }
    }

    private static (string Display, string Normalized) ValidateName(string? value)
    {
        var display = value?.Trim();
        if (string.IsNullOrWhiteSpace(display) || display.Length > RecipesDefaults.CategoryNameMaximumLength)
        {
            throw RecipesCategoryProblem.Validation(
                "name",
                $"Name is required and may contain at most {RecipesDefaults.CategoryNameMaximumLength} characters.");
        }

        return (display, RecipesCatalogNormalization.Normalize(display));
    }

    private static RecipeCategoryResponse ToResponse(RecipeCategory category) =>
        new(category.Id, category.Name, category.SortOrder);
}
