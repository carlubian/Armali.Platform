using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Processes.Contracts;
using Segaris.Api.Modules.Processes.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Processes.Mutations;

/// <summary>
/// Administrative lifecycle for the Processes-owned <c>ProcessCategory</c> catalogue. It
/// mirrors the established module-owned catalogue conventions (creation at the catalogue
/// tail, rename, reorder, privacy-neutral deletion impact, final-row protection, and
/// atomic replace-and-delete) while keeping ownership inside Processes. Because a category
/// is required on every process, a referenced value may only be replaced, never cleared.
/// </summary>
internal sealed class ProcessCategoryManagementService(SegarisDbContext database, IClock clock)
{
    public async Task<ProcessCategoryResponse> CreateAsync(ProcessCategoryRequest request, UserId actor, CancellationToken token)
    {
        var name = ValidateName(request.Name);
        await EnsureUniqueAsync(null, name.Normalized, token);
        var now = clock.UtcNow;
        var sortOrder = (await database.Set<ProcessCategory>().Select(value => (int?)value.SortOrder).MaxAsync(token) ?? -1) + 1;
        var category = new ProcessCategory { Name = name.Display, NormalizedName = name.Normalized, SortOrder = sortOrder, CreatedAt = now, CreatedBy = actor.Value, UpdatedAt = now, UpdatedBy = actor.Value };
        database.Add(category);
        await SaveAsync(token);
        return ToResponse(category);
    }

    public async Task<ProcessCategoryResponse> UpdateAsync(int id, ProcessCategoryRequest request, UserId actor, CancellationToken token)
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
        var ordered = await database.Set<ProcessCategory>().OrderBy(value => value.SortOrder).ThenBy(value => value.Id).ToListAsync(token);
        var index = ordered.FindIndex(value => value.Id == id);
        if (index < 0) throw ProcessCategoryProblem.NotFound();
        var target = direction == CatalogMoveDirection.Up ? index - 1 : index + 1;
        if (target < 0 || target >= ordered.Count) throw ProcessCategoryProblem.Validation("direction", "The process category cannot move beyond the catalogue boundary.");
        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (var position = 0; position < ordered.Count; position++) ordered[position].SortOrder = position;
        await database.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
    }

    public async Task<CatalogDeletionImpactResponse> ImpactAsync(int id, CancellationToken token)
    {
        await FindAsync(id, token, tracked: false);
        var referenced = await database.Set<Process>().AnyAsync(process => process.CategoryId == id, token);
        var count = await database.Set<ProcessCategory>().CountAsync(token);
        return new(referenced, !referenced && count > 1, false, false, count > 1);
    }

    public async Task DeleteAsync(int id, CancellationToken token)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var category = await FindAsync(id, token);
        if (await database.Set<ProcessCategory>().CountAsync(token) <= 1) throw ProcessCategoryProblem.RequiredNotEmpty();
        if (await database.Set<Process>().AnyAsync(process => process.CategoryId == id, token)) throw ProcessCategoryProblem.Referenced();
        database.Remove(category);
        var remaining = await database.Set<ProcessCategory>().Where(value => value.Id != id).OrderBy(value => value.SortOrder).ThenBy(value => value.Id).ToListAsync(token);
        for (var position = 0; position < remaining.Count; position++) remaining[position].SortOrder = position;
        try { await database.SaveChangesAsync(token); await transaction.CommitAsync(token); }
        catch (DbUpdateException) { throw ProcessCategoryProblem.Referenced(); }
    }

    public async Task ReplaceAndDeleteAsync(int id, CatalogReplacementRequest request, UserId actor, CancellationToken token)
    {
        if (request.ClearReferences
            || request.ExchangeRate is not null
            || request.ReplacementId is not { } replacementId
            || replacementId <= 0
            || replacementId == id)
        {
            throw ProcessCategoryProblem.InvalidReplacement();
        }

        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var source = await FindAsync(id, token);
        if (!await database.Set<ProcessCategory>().AnyAsync(value => value.Id == replacementId, token))
        {
            throw ProcessCategoryProblem.InvalidReplacement();
        }

        if (await database.Set<ProcessCategory>().CountAsync(token) <= 1)
        {
            throw ProcessCategoryProblem.RequiredNotEmpty();
        }

        try
        {
            var processes = await database.Set<Process>()
                .Where(process => process.CategoryId == id)
                .ToListAsync(token);
            var occurredAt = clock.UtcNow;
            foreach (var process in processes)
            {
                process.ReplaceCategory(replacementId, actor, occurredAt);
            }

            database.Remove(source);
            var remaining = await database.Set<ProcessCategory>()
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
            throw ProcessCategoryProblem.MigrationConflict();
        }
    }

    private async Task<ProcessCategory> FindAsync(int id, CancellationToken token, bool tracked = true)
    {
        IQueryable<ProcessCategory> query = database.Set<ProcessCategory>();
        if (!tracked) query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(value => value.Id == id, token) ?? throw ProcessCategoryProblem.NotFound();
    }

    private async Task EnsureUniqueAsync(int? id, string normalized, CancellationToken token)
    {
        if (await database.Set<ProcessCategory>().AnyAsync(value => value.NormalizedName == normalized && value.Id != id, token))
            throw ProcessCategoryProblem.DuplicateName();
    }

    private async Task SaveAsync(CancellationToken token) { try { await database.SaveChangesAsync(token); } catch (DbUpdateException) { throw ProcessCategoryProblem.DuplicateName(); } }

    private static (string Display, string Normalized) ValidateName(string? value)
    {
        var display = value?.Trim();
        if (string.IsNullOrWhiteSpace(display) || display.Length > ProcessesValidation.CategoryNameMaximumLength) throw ProcessCategoryProblem.Validation("name", $"Name is required and may contain at most {ProcessesValidation.CategoryNameMaximumLength} characters.");
        return (display, ProcessesCatalogNormalization.Normalize(display));
    }

    private static ProcessCategoryResponse ToResponse(ProcessCategory value) => new(value.Id, value.Name, value.SortOrder);
}
