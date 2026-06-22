using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Destinations.Contracts;
using Segaris.Api.Modules.Destinations.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Destinations.Mutations;

/// <summary>
/// Administrative lifecycle for the Destinations-owned destination category catalogue.
/// It mirrors the established module-owned catalogue conventions (creation at the
/// catalogue tail, rename, reorder, privacy-neutral deletion impact, final-row
/// protection, and atomic replace-and-delete) while keeping ownership inside
/// Destinations. Because every destination requires a category, references may only be
/// replaced and never cleared.
/// </summary>
internal sealed class DestinationCategoryManagementService(SegarisDbContext database, IClock clock)
{
    public async Task<DestinationCategoryResponse> CreateAsync(DestinationCategoryRequest request, UserId actor, CancellationToken token)
    {
        var name = ValidateName(request.Name);
        await EnsureUniqueAsync(null, name.Normalized, token);
        var now = clock.UtcNow;
        var sortOrder = (await database.Set<DestinationCategory>().Select(c => (int?)c.SortOrder).MaxAsync(token) ?? -1) + 1;
        var category = new DestinationCategory
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

    public async Task<DestinationCategoryResponse> UpdateAsync(int id, DestinationCategoryRequest request, UserId actor, CancellationToken token)
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
        var ordered = await database.Set<DestinationCategory>()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
            .ToListAsync(token);
        var index = ordered.FindIndex(c => c.Id == id);
        if (index < 0) throw DestinationCategoryProblem.NotFound();
        var target = direction == CatalogMoveDirection.Up ? index - 1 : index + 1;
        if (target < 0 || target >= ordered.Count)
            throw DestinationCategoryProblem.Validation("direction", "The category cannot move beyond the catalogue boundary.");
        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (var position = 0; position < ordered.Count; position++) ordered[position].SortOrder = position;
        await database.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
    }

    public async Task<CatalogDeletionImpactResponse> ImpactAsync(int id, CancellationToken token)
    {
        await FindAsync(id, token, tracked: false);
        var referenced = await database.Set<Destination>().AnyAsync(d => d.CategoryId == id, token);
        var count = await database.Set<DestinationCategory>().CountAsync(token);
        return new(referenced, !referenced && count > 1, false, false, count > 1);
    }

    public async Task DeleteAsync(int id, CancellationToken token)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var category = await FindAsync(id, token);
        if (await database.Set<DestinationCategory>().CountAsync(token) <= 1)
            throw DestinationCategoryProblem.RequiredNotEmpty();
        if (await database.Set<Destination>().AnyAsync(d => d.CategoryId == id, token))
            throw DestinationCategoryProblem.Referenced();
        database.Remove(category);
        var remaining = await database.Set<DestinationCategory>()
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
            throw DestinationCategoryProblem.Referenced();
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
            throw DestinationCategoryProblem.InvalidReplacement();
        }

        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var source = await FindAsync(id, token);
        if (!await database.Set<DestinationCategory>().AnyAsync(c => c.Id == replacementId, token))
        {
            throw DestinationCategoryProblem.InvalidReplacement();
        }

        if (await database.Set<DestinationCategory>().CountAsync(token) <= 1)
        {
            throw DestinationCategoryProblem.RequiredNotEmpty();
        }

        try
        {
            var destinations = await database.Set<Destination>()
                .Where(d => d.CategoryId == id)
                .ToListAsync(token);
            var occurredAt = clock.UtcNow;
            foreach (var destination in destinations)
            {
                destination.ReplaceCategory(replacementId, actor, occurredAt);
            }

            database.Remove(source);
            var remaining = await database.Set<DestinationCategory>()
                .Where(c => c.Id != id)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
                .ToListAsync(token);
            for (var position = 0; position < remaining.Count; position++) remaining[position].SortOrder = position;

            await database.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        }
        catch (DbUpdateException)
        {
            throw DestinationCategoryProblem.MigrationConflict();
        }
    }

    private async Task<DestinationCategory> FindAsync(int id, CancellationToken token, bool tracked = true)
    {
        IQueryable<DestinationCategory> query = database.Set<DestinationCategory>();
        if (!tracked) query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(c => c.Id == id, token)
               ?? throw DestinationCategoryProblem.NotFound();
    }

    private async Task EnsureUniqueAsync(int? id, string normalized, CancellationToken token)
    {
        if (await database.Set<DestinationCategory>()
                .AnyAsync(c => c.NormalizedName == normalized && c.Id != id, token))
        {
            throw DestinationCategoryProblem.DuplicateName();
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
            throw DestinationCategoryProblem.DuplicateName();
        }
    }

    private static (string Display, string Normalized) ValidateName(string? value)
    {
        var display = value?.Trim();
        if (string.IsNullOrWhiteSpace(display) || display.Length > DestinationsDefaults.CategoryNameMaximumLength)
        {
            throw DestinationCategoryProblem.Validation(
                "name",
                $"Name is required and may contain at most {DestinationsDefaults.CategoryNameMaximumLength} characters.");
        }

        return (display, DestinationsCatalogNormalization.Normalize(display));
    }

    private static DestinationCategoryResponse ToResponse(DestinationCategory category) =>
        new(category.Id, category.Name, category.SortOrder);
}
