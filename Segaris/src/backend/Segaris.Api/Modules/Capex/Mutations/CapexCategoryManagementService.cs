using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Capex.Contracts;
using Segaris.Api.Modules.Capex.Domain;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Capex.Mutations;

internal sealed class CapexCategoryManagementService(SegarisDbContext database, IClock clock)
{
    public async Task<CapexCategoryResponse> CreateAsync(CatalogItemRequest request, UserId actor, CancellationToken token)
    {
        var name = ValidateName(request.Name);
        await EnsureUniqueAsync(null, name.Normalized, token);
        var now = clock.UtcNow;
        var sortOrder = (await database.Set<CapexCategory>().Select(value => (int?)value.SortOrder).MaxAsync(token) ?? -1) + 1;
        var category = new CapexCategory { Name = name.Display, NormalizedName = name.Normalized, SortOrder = sortOrder, CreatedAt = now, CreatedBy = actor.Value, UpdatedAt = now, UpdatedBy = actor.Value };
        database.Add(category);
        await SaveAsync(token);
        return ToResponse(category);
    }

    public async Task<CapexCategoryResponse> UpdateAsync(int id, CatalogItemRequest request, UserId actor, CancellationToken token)
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
        var ordered = await database.Set<CapexCategory>().OrderBy(value => value.SortOrder).ThenBy(value => value.Id).ToListAsync(token);
        var index = ordered.FindIndex(value => value.Id == id);
        if (index < 0) throw CapexCategoryProblem.NotFound();
        var target = direction == CatalogMoveDirection.Up ? index - 1 : index + 1;
        if (target < 0 || target >= ordered.Count) throw CapexCategoryProblem.Validation("direction", "The category cannot move beyond the catalog boundary.");
        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (var position = 0; position < ordered.Count; position++) ordered[position].SortOrder = position;
        await database.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
    }

    public async Task<CatalogDeletionImpactResponse> ImpactAsync(int id, CancellationToken token)
    {
        await FindAsync(id, token, tracked: false);
        var referenced = await database.Set<CapexEntry>().AnyAsync(entry => entry.CategoryId == id, token);
        var count = await database.Set<CapexCategory>().CountAsync(token);
        return new(referenced, !referenced && count > 1, false, false, count > 1);
    }

    public async Task DeleteAsync(int id, CancellationToken token)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var category = await FindAsync(id, token);
        if (await database.Set<CapexCategory>().CountAsync(token) <= 1) throw CapexCategoryProblem.RequiredNotEmpty();
        if (await database.Set<CapexEntry>().AnyAsync(entry => entry.CategoryId == id, token)) throw CapexCategoryProblem.Referenced();
        database.Remove(category);
        var remaining = await database.Set<CapexCategory>().Where(value => value.Id != id).OrderBy(value => value.SortOrder).ThenBy(value => value.Id).ToListAsync(token);
        for (var position = 0; position < remaining.Count; position++) remaining[position].SortOrder = position;
        try { await database.SaveChangesAsync(token); await transaction.CommitAsync(token); }
        catch (DbUpdateException) { throw CapexCategoryProblem.Referenced(); }
    }

    public async Task ReplaceAndDeleteAsync(int id, CatalogReplacementRequest request, UserId actor, CancellationToken token)
    {
        if (request.ClearReferences
            || request.ExchangeRate is not null
            || request.ReplacementId is not { } replacementId
            || replacementId <= 0
            || replacementId == id)
        {
            throw CapexCategoryProblem.InvalidReplacement();
        }

        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var source = await FindAsync(id, token);
        if (!await database.Set<CapexCategory>().AnyAsync(value => value.Id == replacementId, token))
        {
            throw CapexCategoryProblem.InvalidReplacement();
        }

        if (await database.Set<CapexCategory>().CountAsync(token) <= 1)
        {
            throw CapexCategoryProblem.RequiredNotEmpty();
        }

        try
        {
            var entries = await database.Set<CapexEntry>()
                .Where(entry => entry.CategoryId == id)
                .ToListAsync(token);
            var occurredAt = clock.UtcNow;
            foreach (var entry in entries)
            {
                entry.ReplaceCategory(replacementId, actor, occurredAt);
            }

            database.Remove(source);
            var remaining = await database.Set<CapexCategory>()
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
            throw CapexCategoryProblem.MigrationConflict();
        }
    }

    private async Task<CapexCategory> FindAsync(int id, CancellationToken token, bool tracked = true)
    {
        IQueryable<CapexCategory> query = database.Set<CapexCategory>();
        if (!tracked) query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(value => value.Id == id, token) ?? throw CapexCategoryProblem.NotFound();
    }

    private async Task EnsureUniqueAsync(int? id, string normalized, CancellationToken token)
    {
        if (await database.Set<CapexCategory>().AnyAsync(value => value.NormalizedName == normalized && value.Id != id, token))
            throw CapexCategoryProblem.DuplicateName();
    }

    private async Task SaveAsync(CancellationToken token) { try { await database.SaveChangesAsync(token); } catch (DbUpdateException) { throw CapexCategoryProblem.DuplicateName(); } }

    private static (string Display, string Normalized) ValidateName(string? value)
    {
        var display = value?.Trim();
        if (string.IsNullOrWhiteSpace(display) || display.Length > CapexCategoryNormalization.NameMaximumLength) throw CapexCategoryProblem.Validation("name", $"Name is required and may contain at most {CapexCategoryNormalization.NameMaximumLength} characters.");
        return (display, CapexCategoryNormalization.Normalize(display));
    }

    private static CapexCategoryResponse ToResponse(CapexCategory value) => new(value.Id, value.Name, value.SortOrder);
}
