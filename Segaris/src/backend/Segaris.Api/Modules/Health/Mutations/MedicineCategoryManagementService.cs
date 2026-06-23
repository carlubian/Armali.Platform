using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Health.Contracts;
using Segaris.Api.Modules.Health.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Health.Mutations;

/// <summary>
/// Administrative lifecycle for the Health-owned medicine category catalogue. It
/// mirrors the established module-owned catalogue conventions (creation at the
/// catalogue tail, rename, reorder, privacy-neutral deletion impact, final-row
/// protection, and atomic replace-and-delete) while keeping ownership inside Health.
/// Because every medicine requires a category, references may only be replaced and
/// never cleared.
/// </summary>
internal sealed class MedicineCategoryManagementService(SegarisDbContext database, IClock clock)
{
    public async Task<MedicineCategoryResponse> CreateAsync(CatalogItemRequest request, UserId actor, CancellationToken token)
    {
        var name = ValidateName(request.Name);
        await EnsureUniqueAsync(null, name.Normalized, token);
        var now = clock.UtcNow;
        var sortOrder = (await database.Set<MedicineCategory>().Select(c => (int?)c.SortOrder).MaxAsync(token) ?? -1) + 1;
        var category = new MedicineCategory
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

    public async Task<MedicineCategoryResponse> UpdateAsync(int id, CatalogItemRequest request, UserId actor, CancellationToken token)
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
        var ordered = await database.Set<MedicineCategory>()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
            .ToListAsync(token);
        var index = ordered.FindIndex(c => c.Id == id);
        if (index < 0) throw MedicineCategoryProblem.NotFound();
        var target = direction == CatalogMoveDirection.Up ? index - 1 : index + 1;
        if (target < 0 || target >= ordered.Count)
            throw MedicineCategoryProblem.Validation("direction", "The category cannot move beyond the catalogue boundary.");
        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (var position = 0; position < ordered.Count; position++) ordered[position].SortOrder = position;
        await database.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
    }

    public async Task<CatalogDeletionImpactResponse> ImpactAsync(int id, CancellationToken token)
    {
        await FindAsync(id, token, tracked: false);
        var referenced = await database.Set<Medicine>().AnyAsync(m => m.CategoryId == id, token);
        var count = await database.Set<MedicineCategory>().CountAsync(token);
        return new(referenced, !referenced && count > 1, false, false, count > 1);
    }

    public async Task DeleteAsync(int id, CancellationToken token)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var category = await FindAsync(id, token);
        if (await database.Set<MedicineCategory>().CountAsync(token) <= 1)
            throw MedicineCategoryProblem.RequiredNotEmpty();
        if (await database.Set<Medicine>().AnyAsync(m => m.CategoryId == id, token))
            throw MedicineCategoryProblem.Referenced();
        database.Remove(category);
        var remaining = await database.Set<MedicineCategory>()
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
            throw MedicineCategoryProblem.Referenced();
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
            throw MedicineCategoryProblem.InvalidReplacement();
        }

        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var source = await FindAsync(id, token);
        if (!await database.Set<MedicineCategory>().AnyAsync(c => c.Id == replacementId, token))
        {
            throw MedicineCategoryProblem.InvalidReplacement();
        }

        if (await database.Set<MedicineCategory>().CountAsync(token) <= 1)
        {
            throw MedicineCategoryProblem.RequiredNotEmpty();
        }

        try
        {
            var medicines = await database.Set<Medicine>()
                .Where(m => m.CategoryId == id)
                .ToListAsync(token);
            var occurredAt = clock.UtcNow;
            foreach (var medicine in medicines)
            {
                medicine.ReplaceCategory(replacementId, actor, occurredAt);
            }

            database.Remove(source);
            var remaining = await database.Set<MedicineCategory>()
                .Where(c => c.Id != id)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
                .ToListAsync(token);
            for (var position = 0; position < remaining.Count; position++) remaining[position].SortOrder = position;

            await database.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        }
        catch (DbUpdateException)
        {
            throw MedicineCategoryProblem.MigrationConflict();
        }
    }

    private async Task<MedicineCategory> FindAsync(int id, CancellationToken token, bool tracked = true)
    {
        IQueryable<MedicineCategory> query = database.Set<MedicineCategory>();
        if (!tracked) query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(c => c.Id == id, token)
               ?? throw MedicineCategoryProblem.NotFound();
    }

    private async Task EnsureUniqueAsync(int? id, string normalized, CancellationToken token)
    {
        if (await database.Set<MedicineCategory>()
                .AnyAsync(c => c.NormalizedName == normalized && c.Id != id, token))
        {
            throw MedicineCategoryProblem.DuplicateName();
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
            throw MedicineCategoryProblem.DuplicateName();
        }
    }

    private static (string Display, string Normalized) ValidateName(string? value)
    {
        var display = value?.Trim();
        if (string.IsNullOrWhiteSpace(display) || display.Length > HealthDefaults.CategoryNameMaximumLength)
        {
            throw MedicineCategoryProblem.Validation(
                "name",
                $"Name is required and may contain at most {HealthDefaults.CategoryNameMaximumLength} characters.");
        }

        return (display, HealthCatalogNormalization.Normalize(display));
    }

    private static MedicineCategoryResponse ToResponse(MedicineCategory category) =>
        new(category.Id, category.Name, category.SortOrder);
}
