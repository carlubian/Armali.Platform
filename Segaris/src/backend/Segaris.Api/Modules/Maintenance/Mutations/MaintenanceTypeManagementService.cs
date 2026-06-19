using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Maintenance.Contracts;
using Segaris.Api.Modules.Maintenance.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Maintenance.Mutations;

/// <summary>
/// Administrative lifecycle for the Maintenance-owned <c>MaintenanceType</c> catalogue.
/// It mirrors the established module-owned catalogue conventions (creation at the
/// catalogue tail, rename, reorder, privacy-neutral deletion impact, final-row
/// protection, and atomic replace-and-delete) while keeping ownership inside
/// Maintenance. Because a type is required on every task, a referenced value may only
/// be replaced, never cleared.
/// </summary>
internal sealed class MaintenanceTypeManagementService(SegarisDbContext database, IClock clock)
{
    public async Task<MaintenanceTypeResponse> CreateAsync(CatalogItemRequest request, UserId actor, CancellationToken token)
    {
        var name = ValidateName(request.Name);
        await EnsureUniqueAsync(null, name.Normalized, token);
        var now = clock.UtcNow;
        var sortOrder = (await database.Set<MaintenanceType>().Select(value => (int?)value.SortOrder).MaxAsync(token) ?? -1) + 1;
        var type = new MaintenanceType { Name = name.Display, NormalizedName = name.Normalized, SortOrder = sortOrder, CreatedAt = now, CreatedBy = actor.Value, UpdatedAt = now, UpdatedBy = actor.Value };
        database.Add(type);
        await SaveAsync(token);
        return ToResponse(type);
    }

    public async Task<MaintenanceTypeResponse> UpdateAsync(int id, CatalogItemRequest request, UserId actor, CancellationToken token)
    {
        var type = await FindAsync(id, token);
        var name = ValidateName(request.Name);
        await EnsureUniqueAsync(id, name.Normalized, token);
        type.Name = name.Display;
        type.NormalizedName = name.Normalized;
        type.UpdatedAt = clock.UtcNow;
        type.UpdatedBy = actor.Value;
        await SaveAsync(token);
        return ToResponse(type);
    }

    public async Task MoveAsync(int id, CatalogMoveDirection direction, CancellationToken token)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var ordered = await database.Set<MaintenanceType>().OrderBy(value => value.SortOrder).ThenBy(value => value.Id).ToListAsync(token);
        var index = ordered.FindIndex(value => value.Id == id);
        if (index < 0) throw MaintenanceTypeProblem.NotFound();
        var target = direction == CatalogMoveDirection.Up ? index - 1 : index + 1;
        if (target < 0 || target >= ordered.Count) throw MaintenanceTypeProblem.Validation("direction", "The maintenance type cannot move beyond the catalogue boundary.");
        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (var position = 0; position < ordered.Count; position++) ordered[position].SortOrder = position;
        await database.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
    }

    public async Task<CatalogDeletionImpactResponse> ImpactAsync(int id, CancellationToken token)
    {
        await FindAsync(id, token, tracked: false);
        var referenced = await database.Set<MaintenanceTask>().AnyAsync(task => task.MaintenanceTypeId == id, token);
        var count = await database.Set<MaintenanceType>().CountAsync(token);
        return new(referenced, !referenced && count > 1, false, false, count > 1);
    }

    public async Task DeleteAsync(int id, CancellationToken token)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var type = await FindAsync(id, token);
        if (await database.Set<MaintenanceType>().CountAsync(token) <= 1) throw MaintenanceTypeProblem.RequiredNotEmpty();
        if (await database.Set<MaintenanceTask>().AnyAsync(task => task.MaintenanceTypeId == id, token)) throw MaintenanceTypeProblem.Referenced();
        database.Remove(type);
        var remaining = await database.Set<MaintenanceType>().Where(value => value.Id != id).OrderBy(value => value.SortOrder).ThenBy(value => value.Id).ToListAsync(token);
        for (var position = 0; position < remaining.Count; position++) remaining[position].SortOrder = position;
        try { await database.SaveChangesAsync(token); await transaction.CommitAsync(token); }
        catch (DbUpdateException) { throw MaintenanceTypeProblem.Referenced(); }
    }

    public async Task ReplaceAndDeleteAsync(int id, CatalogReplacementRequest request, UserId actor, CancellationToken token)
    {
        if (request.ClearReferences
            || request.ExchangeRate is not null
            || request.ReplacementId is not { } replacementId
            || replacementId <= 0
            || replacementId == id)
        {
            throw MaintenanceTypeProblem.InvalidReplacement();
        }

        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var source = await FindAsync(id, token);
        if (!await database.Set<MaintenanceType>().AnyAsync(value => value.Id == replacementId, token))
        {
            throw MaintenanceTypeProblem.InvalidReplacement();
        }

        if (await database.Set<MaintenanceType>().CountAsync(token) <= 1)
        {
            throw MaintenanceTypeProblem.RequiredNotEmpty();
        }

        try
        {
            var tasks = await database.Set<MaintenanceTask>()
                .Where(task => task.MaintenanceTypeId == id)
                .ToListAsync(token);
            var occurredAt = clock.UtcNow;
            foreach (var task in tasks)
            {
                task.ReplaceType(replacementId, actor, occurredAt);
            }

            database.Remove(source);
            var remaining = await database.Set<MaintenanceType>()
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
            throw MaintenanceTypeProblem.MigrationConflict();
        }
    }

    private async Task<MaintenanceType> FindAsync(int id, CancellationToken token, bool tracked = true)
    {
        IQueryable<MaintenanceType> query = database.Set<MaintenanceType>();
        if (!tracked) query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(value => value.Id == id, token) ?? throw MaintenanceTypeProblem.NotFound();
    }

    private async Task EnsureUniqueAsync(int? id, string normalized, CancellationToken token)
    {
        if (await database.Set<MaintenanceType>().AnyAsync(value => value.NormalizedName == normalized && value.Id != id, token))
            throw MaintenanceTypeProblem.DuplicateName();
    }

    private async Task SaveAsync(CancellationToken token) { try { await database.SaveChangesAsync(token); } catch (DbUpdateException) { throw MaintenanceTypeProblem.DuplicateName(); } }

    private static (string Display, string Normalized) ValidateName(string? value)
    {
        var display = value?.Trim();
        if (string.IsNullOrWhiteSpace(display) || display.Length > MaintenanceValidation.TypeNameMaximumLength) throw MaintenanceTypeProblem.Validation("name", $"Name is required and may contain at most {MaintenanceValidation.TypeNameMaximumLength} characters.");
        return (display, MaintenanceCatalogNormalization.Normalize(display));
    }

    private static MaintenanceTypeResponse ToResponse(MaintenanceType value) => new(value.Id, value.Name, value.SortOrder);
}
