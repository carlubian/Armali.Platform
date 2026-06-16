using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Opex.Contracts;
using Segaris.Api.Modules.Opex.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Opex.Mutations;

/// <summary>
/// Administrative lifecycle for the Opex-owned category catalog. It mirrors the
/// Capex category management conventions (creation at the catalog tail, rename,
/// reorder, privacy-neutral deletion impact, final-row protection, and atomic
/// replace-and-delete) while keeping ownership inside Opex.
/// </summary>
internal sealed class OpexCategoryManagementService(SegarisDbContext database, IClock clock)
{
    public async Task<OpexCategoryResponse> CreateAsync(CatalogItemRequest request, UserId actor, CancellationToken token)
    {
        var name = ValidateName(request.Name);
        await EnsureUniqueAsync(null, name.Normalized, token);
        var now = clock.UtcNow;
        var sortOrder = (await database.Set<OpexCategory>().Select(value => (int?)value.SortOrder).MaxAsync(token) ?? -1) + 1;
        var category = new OpexCategory { Name = name.Display, NormalizedName = name.Normalized, SortOrder = sortOrder, CreatedAt = now, CreatedBy = actor.Value, UpdatedAt = now, UpdatedBy = actor.Value };
        database.Add(category);
        await SaveAsync(token);
        return ToResponse(category);
    }

    public async Task<OpexCategoryResponse> UpdateAsync(int id, CatalogItemRequest request, UserId actor, CancellationToken token)
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
        var ordered = await database.Set<OpexCategory>().OrderBy(value => value.SortOrder).ThenBy(value => value.Id).ToListAsync(token);
        var index = ordered.FindIndex(value => value.Id == id);
        if (index < 0) throw OpexCategoryProblem.NotFound();
        var target = direction == CatalogMoveDirection.Up ? index - 1 : index + 1;
        if (target < 0 || target >= ordered.Count) throw OpexCategoryProblem.Validation("direction", "The category cannot move beyond the catalog boundary.");
        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (var position = 0; position < ordered.Count; position++) ordered[position].SortOrder = position;
        await database.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
    }

    public async Task<CatalogDeletionImpactResponse> ImpactAsync(int id, CancellationToken token)
    {
        await FindAsync(id, token, tracked: false);
        var referenced = await database.Set<OpexContract>().AnyAsync(contract => contract.CategoryId == id, token);
        var count = await database.Set<OpexCategory>().CountAsync(token);
        return new(referenced, !referenced && count > 1, false, false, count > 1);
    }

    public async Task DeleteAsync(int id, CancellationToken token)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var category = await FindAsync(id, token);
        if (await database.Set<OpexCategory>().CountAsync(token) <= 1) throw OpexCategoryProblem.RequiredNotEmpty();
        if (await database.Set<OpexContract>().AnyAsync(contract => contract.CategoryId == id, token)) throw OpexCategoryProblem.Referenced();
        database.Remove(category);
        var remaining = await database.Set<OpexCategory>().Where(value => value.Id != id).OrderBy(value => value.SortOrder).ThenBy(value => value.Id).ToListAsync(token);
        for (var position = 0; position < remaining.Count; position++) remaining[position].SortOrder = position;
        try { await database.SaveChangesAsync(token); await transaction.CommitAsync(token); }
        catch (DbUpdateException) { throw OpexCategoryProblem.Referenced(); }
    }

    public async Task ReplaceAndDeleteAsync(int id, CatalogReplacementRequest request, UserId actor, CancellationToken token)
    {
        if (request.ClearReferences
            || request.ExchangeRate is not null
            || request.ReplacementId is not { } replacementId
            || replacementId <= 0
            || replacementId == id)
        {
            throw OpexCategoryProblem.InvalidReplacement();
        }

        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var source = await FindAsync(id, token);
        if (!await database.Set<OpexCategory>().AnyAsync(value => value.Id == replacementId, token))
        {
            throw OpexCategoryProblem.InvalidReplacement();
        }

        if (await database.Set<OpexCategory>().CountAsync(token) <= 1)
        {
            throw OpexCategoryProblem.RequiredNotEmpty();
        }

        try
        {
            var contracts = await database.Set<OpexContract>()
                .Where(contract => contract.CategoryId == id)
                .ToListAsync(token);
            var occurredAt = clock.UtcNow;
            foreach (var contract in contracts)
            {
                contract.ReplaceCategory(replacementId, actor, occurredAt);
            }

            database.Remove(source);
            var remaining = await database.Set<OpexCategory>()
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
            throw OpexCategoryProblem.MigrationConflict();
        }
    }

    private async Task<OpexCategory> FindAsync(int id, CancellationToken token, bool tracked = true)
    {
        IQueryable<OpexCategory> query = database.Set<OpexCategory>();
        if (!tracked) query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(value => value.Id == id, token) ?? throw OpexCategoryProblem.NotFound();
    }

    private async Task EnsureUniqueAsync(int? id, string normalized, CancellationToken token)
    {
        if (await database.Set<OpexCategory>().AnyAsync(value => value.NormalizedName == normalized && value.Id != id, token))
            throw OpexCategoryProblem.DuplicateName();
    }

    private async Task SaveAsync(CancellationToken token) { try { await database.SaveChangesAsync(token); } catch (DbUpdateException) { throw OpexCategoryProblem.DuplicateName(); } }

    private static (string Display, string Normalized) ValidateName(string? value)
    {
        var display = value?.Trim();
        if (string.IsNullOrWhiteSpace(display) || display.Length > OpexCategoryNormalization.NameMaximumLength) throw OpexCategoryProblem.Validation("name", $"Name is required and may contain at most {OpexCategoryNormalization.NameMaximumLength} characters.");
        return (display, OpexCategoryNormalization.Normalize(display));
    }

    private static OpexCategoryResponse ToResponse(OpexCategory value) => new(value.Id, value.Name, value.SortOrder);
}
