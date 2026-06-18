using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Clothes.Contracts;
using Segaris.Api.Modules.Clothes.Domain;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Clothes.Mutations;

/// <summary>
/// Administrative lifecycle for the Clothes-owned colour catalog. It follows the same
/// module-owned conventions as the category catalog and additionally requires and
/// validates a canonical <c>#RRGGBB</c> colour value on every row. Colour is optional
/// and multi-valued on garments, so deletion impact reports a clearing path; the
/// reference-migrating replace-or-clear path is delivered with the Configuration
/// reference handlers in a later wave.
/// </summary>
internal sealed class ClothingColorManagementService(SegarisDbContext database, IClock clock)
{
    public async Task<ClothingColorResponse> CreateAsync(CatalogItemRequest request, UserId actor, CancellationToken token)
    {
        var name = ValidateName(request.Name);
        var colorValue = ValidateColorValue(request.ColorValue);
        await EnsureUniqueAsync(null, name.Normalized, token);
        var now = clock.UtcNow;
        var sortOrder = (await database.Set<ClothingColor>().Select(value => (int?)value.SortOrder).MaxAsync(token) ?? -1) + 1;
        var color = new ClothingColor { Name = name.Display, NormalizedName = name.Normalized, ColorValue = colorValue, SortOrder = sortOrder, CreatedAt = now, CreatedBy = actor.Value, UpdatedAt = now, UpdatedBy = actor.Value };
        database.Add(color);
        await SaveAsync(token);
        return ToResponse(color);
    }

    public async Task<ClothingColorResponse> UpdateAsync(int id, CatalogItemRequest request, UserId actor, CancellationToken token)
    {
        var color = await FindAsync(id, token);
        var name = ValidateName(request.Name);
        var colorValue = ValidateColorValue(request.ColorValue);
        await EnsureUniqueAsync(id, name.Normalized, token);
        color.Name = name.Display;
        color.NormalizedName = name.Normalized;
        color.ColorValue = colorValue;
        color.UpdatedAt = clock.UtcNow;
        color.UpdatedBy = actor.Value;
        await SaveAsync(token);
        return ToResponse(color);
    }

    public async Task MoveAsync(int id, CatalogMoveDirection direction, CancellationToken token)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var ordered = await database.Set<ClothingColor>().OrderBy(value => value.SortOrder).ThenBy(value => value.Id).ToListAsync(token);
        var index = ordered.FindIndex(value => value.Id == id);
        if (index < 0) throw ClothesColorProblem.NotFound();
        var target = direction == CatalogMoveDirection.Up ? index - 1 : index + 1;
        if (target < 0 || target >= ordered.Count) throw ClothesColorProblem.Validation("direction", "The colour cannot move beyond the catalog boundary.");
        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (var position = 0; position < ordered.Count; position++) ordered[position].SortOrder = position;
        await database.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
    }

    public async Task<CatalogDeletionImpactResponse> ImpactAsync(int id, CancellationToken token)
    {
        await FindAsync(id, token, tracked: false);
        var referenced = await database.Set<ClothesGarmentColor>().AnyAsync(association => association.ColorId == id, token);
        var count = await database.Set<ClothingColor>().CountAsync(token);
        return new(referenced, !referenced && count > 1, referenced, false, count > 1);
    }

    public async Task DeleteAsync(int id, CancellationToken token)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var color = await FindAsync(id, token);
        if (await database.Set<ClothingColor>().CountAsync(token) <= 1) throw ClothesColorProblem.RequiredNotEmpty();
        if (await database.Set<ClothesGarmentColor>().AnyAsync(association => association.ColorId == id, token)) throw ClothesColorProblem.Referenced();
        database.Remove(color);
        var remaining = await database.Set<ClothingColor>().Where(value => value.Id != id).OrderBy(value => value.SortOrder).ThenBy(value => value.Id).ToListAsync(token);
        for (var position = 0; position < remaining.Count; position++) remaining[position].SortOrder = position;
        try { await database.SaveChangesAsync(token); await transaction.CommitAsync(token); }
        catch (DbUpdateException) { throw ClothesColorProblem.Referenced(); }
    }

    private async Task<ClothingColor> FindAsync(int id, CancellationToken token, bool tracked = true)
    {
        IQueryable<ClothingColor> query = database.Set<ClothingColor>();
        if (!tracked) query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(value => value.Id == id, token) ?? throw ClothesColorProblem.NotFound();
    }

    private async Task EnsureUniqueAsync(int? id, string normalized, CancellationToken token)
    {
        if (await database.Set<ClothingColor>().AnyAsync(value => value.NormalizedName == normalized && value.Id != id, token))
            throw ClothesColorProblem.DuplicateName();
    }

    private async Task SaveAsync(CancellationToken token) { try { await database.SaveChangesAsync(token); } catch (DbUpdateException) { throw ClothesColorProblem.DuplicateName(); } }

    private static (string Display, string Normalized) ValidateName(string? value)
    {
        var display = value?.Trim();
        if (string.IsNullOrWhiteSpace(display) || display.Length > ClothesValidation.ColorNameMaximumLength) throw ClothesColorProblem.Validation("name", $"Name is required and may contain at most {ClothesValidation.ColorNameMaximumLength} characters.");
        return (display, ClothesCatalogNormalization.Normalize(display));
    }

    private static string ValidateColorValue(string? value)
    {
        try
        {
            return ClothesValidation.ValidateColorValue(value);
        }
        catch (ClothesValidationException exception)
        {
            throw ClothesColorProblem.Validation("colorValue", exception.Message);
        }
    }

    private static ClothingColorResponse ToResponse(ClothingColor value) => new(value.Id, value.Name, value.ColorValue, value.SortOrder);
}
