using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Assets.Contracts;
using Segaris.Api.Modules.Assets.Domain;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Assets.Mutations;

/// <summary>
/// Administrative lifecycle for the Assets-owned category catalog. It mirrors the
/// established module-owned catalog conventions (creation at the catalog tail,
/// rename, reorder, privacy-neutral deletion impact, final-row protection, and
/// atomic replace-and-delete) while keeping ownership inside Assets. Because a
/// category is required on every asset, a referenced value may only be replaced,
/// never cleared.
/// </summary>
internal sealed class AssetCategoryManagementService(SegarisDbContext database, IClock clock)
{
    public async Task<AssetCategoryResponse> CreateAsync(CatalogItemRequest request, UserId actor, CancellationToken token)
    {
        var name = ValidateName(request.Name);
        await EnsureUniqueAsync(null, name.Normalized, token);
        var now = clock.UtcNow;
        var sortOrder = (await database.Set<AssetCategory>().Select(value => (int?)value.SortOrder).MaxAsync(token) ?? -1) + 1;
        var category = new AssetCategory { Name = name.Display, NormalizedName = name.Normalized, SortOrder = sortOrder, CreatedAt = now, CreatedBy = actor.Value, UpdatedAt = now, UpdatedBy = actor.Value };
        database.Add(category);
        await SaveAsync(token);
        return ToResponse(category);
    }

    public async Task<AssetCategoryResponse> UpdateAsync(int id, CatalogItemRequest request, UserId actor, CancellationToken token)
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
        var ordered = await database.Set<AssetCategory>().OrderBy(value => value.SortOrder).ThenBy(value => value.Id).ToListAsync(token);
        var index = ordered.FindIndex(value => value.Id == id);
        if (index < 0) throw AssetCategoryProblem.NotFound();
        var target = direction == CatalogMoveDirection.Up ? index - 1 : index + 1;
        if (target < 0 || target >= ordered.Count) throw AssetCategoryProblem.Validation("direction", "The category cannot move beyond the catalog boundary.");
        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (var position = 0; position < ordered.Count; position++) ordered[position].SortOrder = position;
        await database.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
    }

    public async Task<CatalogDeletionImpactResponse> ImpactAsync(int id, CancellationToken token)
    {
        await FindAsync(id, token, tracked: false);
        var referenced = await database.Set<Asset>().AnyAsync(asset => asset.CategoryId == id, token);
        var count = await database.Set<AssetCategory>().CountAsync(token);
        return new(referenced, !referenced && count > 1, false, false, count > 1);
    }

    public async Task DeleteAsync(int id, CancellationToken token)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var category = await FindAsync(id, token);
        if (await database.Set<AssetCategory>().CountAsync(token) <= 1) throw AssetCategoryProblem.RequiredNotEmpty();
        if (await database.Set<Asset>().AnyAsync(asset => asset.CategoryId == id, token)) throw AssetCategoryProblem.Referenced();
        database.Remove(category);
        var remaining = await database.Set<AssetCategory>().Where(value => value.Id != id).OrderBy(value => value.SortOrder).ThenBy(value => value.Id).ToListAsync(token);
        for (var position = 0; position < remaining.Count; position++) remaining[position].SortOrder = position;
        try { await database.SaveChangesAsync(token); await transaction.CommitAsync(token); }
        catch (DbUpdateException) { throw AssetCategoryProblem.Referenced(); }
    }

    public async Task ReplaceAndDeleteAsync(int id, CatalogReplacementRequest request, UserId actor, CancellationToken token)
    {
        if (request.ClearReferences
            || request.ExchangeRate is not null
            || request.ReplacementId is not { } replacementId
            || replacementId <= 0
            || replacementId == id)
        {
            throw AssetCategoryProblem.InvalidReplacement();
        }

        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var source = await FindAsync(id, token);
        if (!await database.Set<AssetCategory>().AnyAsync(value => value.Id == replacementId, token))
        {
            throw AssetCategoryProblem.InvalidReplacement();
        }

        if (await database.Set<AssetCategory>().CountAsync(token) <= 1)
        {
            throw AssetCategoryProblem.RequiredNotEmpty();
        }

        try
        {
            var assets = await database.Set<Asset>()
                .Where(asset => asset.CategoryId == id)
                .ToListAsync(token);
            var occurredAt = clock.UtcNow;
            foreach (var asset in assets)
            {
                asset.ReplaceCategory(replacementId, actor, occurredAt);
            }

            database.Remove(source);
            var remaining = await database.Set<AssetCategory>()
                .Where(value => value.Id != id)
                .OrderBy(value => value.SortOrder)
                .ThenBy(value => value.Id)
                .ToListAsync(token);
            for (var position = 0; position < remaining.Count; position++) remaining[position].SortOrder = position;

            await database.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        }
        catch (DbUpdateException)
        {
            throw AssetCategoryProblem.MigrationConflict();
        }
    }

    private async Task<AssetCategory> FindAsync(int id, CancellationToken token, bool tracked = true)
    {
        IQueryable<AssetCategory> query = database.Set<AssetCategory>();
        if (!tracked) query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(value => value.Id == id, token) ?? throw AssetCategoryProblem.NotFound();
    }

    private async Task EnsureUniqueAsync(int? id, string normalized, CancellationToken token)
    {
        if (await database.Set<AssetCategory>().AnyAsync(value => value.NormalizedName == normalized && value.Id != id, token))
            throw AssetCategoryProblem.DuplicateName();
    }

    private async Task SaveAsync(CancellationToken token) { try { await database.SaveChangesAsync(token); } catch (DbUpdateException) { throw AssetCategoryProblem.DuplicateName(); } }

    private static (string Display, string Normalized) ValidateName(string? value)
    {
        var display = value?.Trim();
        if (string.IsNullOrWhiteSpace(display) || display.Length > AssetValidation.CategoryNameMaximumLength) throw AssetCategoryProblem.Validation("name", $"Name is required and may contain at most {AssetValidation.CategoryNameMaximumLength} characters.");
        return (display, AssetCatalogNormalization.Normalize(display));
    }

    private static AssetCategoryResponse ToResponse(AssetCategory value) => new(value.Id, value.Name, value.SortOrder);
}
