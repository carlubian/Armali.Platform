using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Travel.Contracts;
using Segaris.Api.Modules.Travel.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Travel.Mutations;

/// <summary>
/// Administrative lifecycle for the Travel-owned expense-category catalog. It follows
/// the same module-owned catalog conventions as
/// <see cref="TravelTripTypeManagementService"/>: creation at the tail, rename,
/// reorder, privacy-neutral deletion impact, final-row protection, and atomic
/// replace-and-delete, while keeping ownership inside Travel.
/// </summary>
internal sealed class TravelExpenseCategoryManagementService(SegarisDbContext database, IClock clock)
{
    public async Task<TravelExpenseCategoryResponse> CreateAsync(TravelExpenseCategoryRequest request, UserId actor, CancellationToken token)
    {
        var name = ValidateName(request.Name);
        await EnsureUniqueAsync(null, name.Normalized, token);
        var now = clock.UtcNow;
        var sortOrder = (await database.Set<TravelExpenseCategory>().Select(value => (int?)value.SortOrder).MaxAsync(token) ?? -1) + 1;
        var category = new TravelExpenseCategory { Name = name.Display, NormalizedName = name.Normalized, SortOrder = sortOrder, CreatedAt = now, CreatedBy = actor.Value, UpdatedAt = now, UpdatedBy = actor.Value };
        database.Add(category);
        await SaveAsync(token);
        return ToResponse(category);
    }

    public async Task<TravelExpenseCategoryResponse> UpdateAsync(int id, TravelExpenseCategoryRequest request, UserId actor, CancellationToken token)
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
        var ordered = await database.Set<TravelExpenseCategory>().OrderBy(value => value.SortOrder).ThenBy(value => value.Id).ToListAsync(token);
        var index = ordered.FindIndex(value => value.Id == id);
        if (index < 0) throw TravelExpenseCategoryProblem.NotFound();
        var target = direction == CatalogMoveDirection.Up ? index - 1 : index + 1;
        if (target < 0 || target >= ordered.Count) throw TravelExpenseCategoryProblem.Validation("direction", "The expense category cannot move beyond the catalog boundary.");
        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (var position = 0; position < ordered.Count; position++) ordered[position].SortOrder = position;
        await database.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
    }

    public async Task<CatalogDeletionImpactResponse> ImpactAsync(int id, CancellationToken token)
    {
        await FindAsync(id, token, tracked: false);
        var referenced = await database.Set<TravelExpense>().AnyAsync(expense => expense.ExpenseCategoryId == id, token);
        var count = await database.Set<TravelExpenseCategory>().CountAsync(token);
        return new(referenced, !referenced && count > 1, false, false, count > 1);
    }

    public async Task DeleteAsync(int id, CancellationToken token)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var category = await FindAsync(id, token);
        if (await database.Set<TravelExpenseCategory>().CountAsync(token) <= 1) throw TravelExpenseCategoryProblem.RequiredNotEmpty();
        if (await database.Set<TravelExpense>().AnyAsync(expense => expense.ExpenseCategoryId == id, token)) throw TravelExpenseCategoryProblem.Referenced();
        database.Remove(category);
        var remaining = await database.Set<TravelExpenseCategory>().Where(value => value.Id != id).OrderBy(value => value.SortOrder).ThenBy(value => value.Id).ToListAsync(token);
        for (var position = 0; position < remaining.Count; position++) remaining[position].SortOrder = position;
        try { await database.SaveChangesAsync(token); await transaction.CommitAsync(token); }
        catch (DbUpdateException) { throw TravelExpenseCategoryProblem.Referenced(); }
    }

    public async Task ReplaceAndDeleteAsync(int id, CatalogReplacementRequest request, UserId actor, CancellationToken token)
    {
        if (request.ClearReferences
            || request.ExchangeRate is not null
            || request.ReplacementId is not { } replacementId
            || replacementId <= 0
            || replacementId == id)
        {
            throw TravelExpenseCategoryProblem.InvalidReplacement();
        }

        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var source = await FindAsync(id, token);
        if (!await database.Set<TravelExpenseCategory>().AnyAsync(value => value.Id == replacementId, token))
        {
            throw TravelExpenseCategoryProblem.InvalidReplacement();
        }

        if (await database.Set<TravelExpenseCategory>().CountAsync(token) <= 1)
        {
            throw TravelExpenseCategoryProblem.RequiredNotEmpty();
        }

        try
        {
            var expenses = await database.Set<TravelExpense>()
                .Where(expense => expense.ExpenseCategoryId == id)
                .ToListAsync(token);
            var occurredAt = clock.UtcNow;
            foreach (var expense in expenses)
            {
                expense.ReplaceExpenseCategory(replacementId, actor, occurredAt);
            }

            database.Remove(source);
            var remaining = await database.Set<TravelExpenseCategory>()
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
            throw TravelExpenseCategoryProblem.MigrationConflict();
        }
    }

    private async Task<TravelExpenseCategory> FindAsync(int id, CancellationToken token, bool tracked = true)
    {
        IQueryable<TravelExpenseCategory> query = database.Set<TravelExpenseCategory>();
        if (!tracked) query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(value => value.Id == id, token) ?? throw TravelExpenseCategoryProblem.NotFound();
    }

    private async Task EnsureUniqueAsync(int? id, string normalized, CancellationToken token)
    {
        if (await database.Set<TravelExpenseCategory>().AnyAsync(value => value.NormalizedName == normalized && value.Id != id, token))
            throw TravelExpenseCategoryProblem.DuplicateName();
    }

    private async Task SaveAsync(CancellationToken token) { try { await database.SaveChangesAsync(token); } catch (DbUpdateException) { throw TravelExpenseCategoryProblem.DuplicateName(); } }

    private static (string Display, string Normalized) ValidateName(string? value)
    {
        var display = value?.Trim();
        if (string.IsNullOrWhiteSpace(display) || display.Length > TravelValidation.CatalogNameMaxLength) throw TravelExpenseCategoryProblem.Validation("name", $"Name is required and may contain at most {TravelValidation.CatalogNameMaxLength} characters.");
        return (display, TravelCatalogNormalization.Normalize(display));
    }

    private static TravelExpenseCategoryResponse ToResponse(TravelExpenseCategory value) => new(value.Id, value.Name, value.SortOrder);
}
