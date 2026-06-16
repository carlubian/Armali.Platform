using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Inventory.Mutations;

/// <summary>
/// Administrative lifecycle for the Inventory-owned location catalog. It follows the
/// same module-owned catalog conventions as
/// <see cref="InventoryCategoryManagementService"/>: creation at the tail, rename,
/// reorder, privacy-neutral deletion impact, final-row protection, and atomic
/// replace-and-delete, while keeping ownership inside Inventory.
/// </summary>
internal sealed class InventoryLocationManagementService(SegarisDbContext database, IClock clock)
{
    public async Task<InventoryLocationResponse> CreateAsync(CatalogItemRequest request, UserId actor, CancellationToken token)
    {
        var name = ValidateName(request.Name);
        await EnsureUniqueAsync(null, name.Normalized, token);
        var now = clock.UtcNow;
        var sortOrder = (await database.Set<InventoryLocation>().Select(value => (int?)value.SortOrder).MaxAsync(token) ?? -1) + 1;
        var location = new InventoryLocation { Name = name.Display, NormalizedName = name.Normalized, SortOrder = sortOrder, CreatedAt = now, CreatedBy = actor.Value, UpdatedAt = now, UpdatedBy = actor.Value };
        database.Add(location);
        await SaveAsync(token);
        return ToResponse(location);
    }

    public async Task<InventoryLocationResponse> UpdateAsync(int id, CatalogItemRequest request, UserId actor, CancellationToken token)
    {
        var location = await FindAsync(id, token);
        var name = ValidateName(request.Name);
        await EnsureUniqueAsync(id, name.Normalized, token);
        location.Name = name.Display;
        location.NormalizedName = name.Normalized;
        location.UpdatedAt = clock.UtcNow;
        location.UpdatedBy = actor.Value;
        await SaveAsync(token);
        return ToResponse(location);
    }

    public async Task MoveAsync(int id, CatalogMoveDirection direction, CancellationToken token)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var ordered = await database.Set<InventoryLocation>().OrderBy(value => value.SortOrder).ThenBy(value => value.Id).ToListAsync(token);
        var index = ordered.FindIndex(value => value.Id == id);
        if (index < 0) throw InventoryLocationProblem.NotFound();
        var target = direction == CatalogMoveDirection.Up ? index - 1 : index + 1;
        if (target < 0 || target >= ordered.Count) throw InventoryLocationProblem.Validation("direction", "The location cannot move beyond the catalog boundary.");
        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (var position = 0; position < ordered.Count; position++) ordered[position].SortOrder = position;
        await database.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
    }

    public async Task<CatalogDeletionImpactResponse> ImpactAsync(int id, CancellationToken token)
    {
        await FindAsync(id, token, tracked: false);
        var referenced = await database.Set<InventoryItem>().AnyAsync(item => item.LocationId == id, token);
        var count = await database.Set<InventoryLocation>().CountAsync(token);
        return new(referenced, !referenced && count > 1, false, false, count > 1);
    }

    public async Task DeleteAsync(int id, CancellationToken token)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var location = await FindAsync(id, token);
        if (await database.Set<InventoryLocation>().CountAsync(token) <= 1) throw InventoryLocationProblem.RequiredNotEmpty();
        if (await database.Set<InventoryItem>().AnyAsync(item => item.LocationId == id, token)) throw InventoryLocationProblem.Referenced();
        database.Remove(location);
        var remaining = await database.Set<InventoryLocation>().Where(value => value.Id != id).OrderBy(value => value.SortOrder).ThenBy(value => value.Id).ToListAsync(token);
        for (var position = 0; position < remaining.Count; position++) remaining[position].SortOrder = position;
        try { await database.SaveChangesAsync(token); await transaction.CommitAsync(token); }
        catch (DbUpdateException) { throw InventoryLocationProblem.Referenced(); }
    }

    public async Task ReplaceAndDeleteAsync(int id, CatalogReplacementRequest request, UserId actor, CancellationToken token)
    {
        if (request.ClearReferences
            || request.ExchangeRate is not null
            || request.ReplacementId is not { } replacementId
            || replacementId <= 0
            || replacementId == id)
        {
            throw InventoryLocationProblem.InvalidReplacement();
        }

        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var source = await FindAsync(id, token);
        if (!await database.Set<InventoryLocation>().AnyAsync(value => value.Id == replacementId, token))
        {
            throw InventoryLocationProblem.InvalidReplacement();
        }

        if (await database.Set<InventoryLocation>().CountAsync(token) <= 1)
        {
            throw InventoryLocationProblem.RequiredNotEmpty();
        }

        try
        {
            var items = await database.Set<InventoryItem>()
                .Where(item => item.LocationId == id)
                .ToListAsync(token);
            var occurredAt = clock.UtcNow;
            foreach (var item in items)
            {
                item.ReplaceLocation(replacementId, actor, occurredAt);
            }

            database.Remove(source);
            var remaining = await database.Set<InventoryLocation>()
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
            throw InventoryLocationProblem.MigrationConflict();
        }
    }

    private async Task<InventoryLocation> FindAsync(int id, CancellationToken token, bool tracked = true)
    {
        IQueryable<InventoryLocation> query = database.Set<InventoryLocation>();
        if (!tracked) query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(value => value.Id == id, token) ?? throw InventoryLocationProblem.NotFound();
    }

    private async Task EnsureUniqueAsync(int? id, string normalized, CancellationToken token)
    {
        if (await database.Set<InventoryLocation>().AnyAsync(value => value.NormalizedName == normalized && value.Id != id, token))
            throw InventoryLocationProblem.DuplicateName();
    }

    private async Task SaveAsync(CancellationToken token) { try { await database.SaveChangesAsync(token); } catch (DbUpdateException) { throw InventoryLocationProblem.DuplicateName(); } }

    private static (string Display, string Normalized) ValidateName(string? value)
    {
        var display = value?.Trim();
        if (string.IsNullOrWhiteSpace(display) || display.Length > InventoryValidation.LocationNameMaximumLength) throw InventoryLocationProblem.Validation("name", $"Name is required and may contain at most {InventoryValidation.LocationNameMaximumLength} characters.");
        return (display, InventoryCatalogNormalization.Normalize(display));
    }

    private static InventoryLocationResponse ToResponse(InventoryLocation value) => new(value.Id, value.Name, value.SortOrder);
}
