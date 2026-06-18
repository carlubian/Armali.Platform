using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Clothes.Contracts;
using Segaris.Api.Modules.Clothes.Domain;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Clothes.Mutations;

/// <summary>
/// Administrative lifecycle for the Clothes-owned category catalog. It mirrors the
/// established module-owned catalog conventions (creation at the catalog tail, rename,
/// reorder, privacy-neutral deletion impact, and final-row protection) while keeping
/// ownership inside Clothes. A category is required on every garment, so the
/// reference-migrating replace-and-delete path is delivered with the Configuration
/// reference handlers in a later wave.
/// </summary>
internal sealed class ClothingCategoryManagementService(SegarisDbContext database, IClock clock)
{
    public async Task<ClothingCategoryResponse> CreateAsync(CatalogItemRequest request, UserId actor, CancellationToken token)
    {
        var name = ValidateName(request.Name);
        await EnsureUniqueAsync(null, name.Normalized, token);
        var now = clock.UtcNow;
        var sortOrder = (await database.Set<ClothingCategory>().Select(value => (int?)value.SortOrder).MaxAsync(token) ?? -1) + 1;
        var category = new ClothingCategory { Name = name.Display, NormalizedName = name.Normalized, SortOrder = sortOrder, CreatedAt = now, CreatedBy = actor.Value, UpdatedAt = now, UpdatedBy = actor.Value };
        database.Add(category);
        await SaveAsync(token);
        return ToResponse(category);
    }

    public async Task<ClothingCategoryResponse> UpdateAsync(int id, CatalogItemRequest request, UserId actor, CancellationToken token)
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
        var ordered = await database.Set<ClothingCategory>().OrderBy(value => value.SortOrder).ThenBy(value => value.Id).ToListAsync(token);
        var index = ordered.FindIndex(value => value.Id == id);
        if (index < 0) throw ClothesCategoryProblem.NotFound();
        var target = direction == CatalogMoveDirection.Up ? index - 1 : index + 1;
        if (target < 0 || target >= ordered.Count) throw ClothesCategoryProblem.Validation("direction", "The category cannot move beyond the catalog boundary.");
        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (var position = 0; position < ordered.Count; position++) ordered[position].SortOrder = position;
        await database.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
    }

    public async Task<CatalogDeletionImpactResponse> ImpactAsync(int id, CancellationToken token)
    {
        await FindAsync(id, token, tracked: false);
        var referenced = await database.Set<ClothesGarment>().AnyAsync(garment => garment.CategoryId == id, token);
        var count = await database.Set<ClothingCategory>().CountAsync(token);
        return new(referenced, !referenced && count > 1, false, false, count > 1);
    }

    public async Task DeleteAsync(int id, CancellationToken token)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var category = await FindAsync(id, token);
        if (await database.Set<ClothingCategory>().CountAsync(token) <= 1) throw ClothesCategoryProblem.RequiredNotEmpty();
        if (await database.Set<ClothesGarment>().AnyAsync(garment => garment.CategoryId == id, token)) throw ClothesCategoryProblem.Referenced();
        database.Remove(category);
        var remaining = await database.Set<ClothingCategory>().Where(value => value.Id != id).OrderBy(value => value.SortOrder).ThenBy(value => value.Id).ToListAsync(token);
        for (var position = 0; position < remaining.Count; position++) remaining[position].SortOrder = position;
        try { await database.SaveChangesAsync(token); await transaction.CommitAsync(token); }
        catch (DbUpdateException) { throw ClothesCategoryProblem.Referenced(); }
    }

    private async Task<ClothingCategory> FindAsync(int id, CancellationToken token, bool tracked = true)
    {
        IQueryable<ClothingCategory> query = database.Set<ClothingCategory>();
        if (!tracked) query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(value => value.Id == id, token) ?? throw ClothesCategoryProblem.NotFound();
    }

    private async Task EnsureUniqueAsync(int? id, string normalized, CancellationToken token)
    {
        if (await database.Set<ClothingCategory>().AnyAsync(value => value.NormalizedName == normalized && value.Id != id, token))
            throw ClothesCategoryProblem.DuplicateName();
    }

    private async Task SaveAsync(CancellationToken token) { try { await database.SaveChangesAsync(token); } catch (DbUpdateException) { throw ClothesCategoryProblem.DuplicateName(); } }

    private static (string Display, string Normalized) ValidateName(string? value)
    {
        var display = value?.Trim();
        if (string.IsNullOrWhiteSpace(display) || display.Length > ClothesValidation.CategoryNameMaximumLength) throw ClothesCategoryProblem.Validation("name", $"Name is required and may contain at most {ClothesValidation.CategoryNameMaximumLength} characters.");
        return (display, ClothesCatalogNormalization.Normalize(display));
    }

    private static ClothingCategoryResponse ToResponse(ClothingCategory value) => new(value.Id, value.Name, value.SortOrder);
}
