using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Travel.Contracts;
using Segaris.Api.Modules.Travel.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Travel.Mutations;

/// <summary>
/// Administrative lifecycle for the Travel-owned trip-type catalog. It mirrors the
/// established module-owned catalog conventions (creation at the catalog tail,
/// rename, reorder, privacy-neutral deletion impact, final-row protection, and
/// atomic replace-and-delete) while keeping ownership inside Travel.
/// </summary>
internal sealed class TravelTripTypeManagementService(SegarisDbContext database, IClock clock)
{
    public async Task<TravelTripTypeResponse> CreateAsync(TravelTripTypeRequest request, UserId actor, CancellationToken token)
    {
        var name = ValidateName(request.Name);
        await EnsureUniqueAsync(null, name.Normalized, token);
        var now = clock.UtcNow;
        var sortOrder = (await database.Set<TravelTripType>().Select(value => (int?)value.SortOrder).MaxAsync(token) ?? -1) + 1;
        var tripType = new TravelTripType { Name = name.Display, NormalizedName = name.Normalized, SortOrder = sortOrder, CreatedAt = now, CreatedBy = actor.Value, UpdatedAt = now, UpdatedBy = actor.Value };
        database.Add(tripType);
        await SaveAsync(token);
        return ToResponse(tripType);
    }

    public async Task<TravelTripTypeResponse> UpdateAsync(int id, TravelTripTypeRequest request, UserId actor, CancellationToken token)
    {
        var tripType = await FindAsync(id, token);
        var name = ValidateName(request.Name);
        await EnsureUniqueAsync(id, name.Normalized, token);
        tripType.Name = name.Display;
        tripType.NormalizedName = name.Normalized;
        tripType.UpdatedAt = clock.UtcNow;
        tripType.UpdatedBy = actor.Value;
        await SaveAsync(token);
        return ToResponse(tripType);
    }

    public async Task MoveAsync(int id, CatalogMoveDirection direction, CancellationToken token)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var ordered = await database.Set<TravelTripType>().OrderBy(value => value.SortOrder).ThenBy(value => value.Id).ToListAsync(token);
        var index = ordered.FindIndex(value => value.Id == id);
        if (index < 0) throw TravelTripTypeProblem.NotFound();
        var target = direction == CatalogMoveDirection.Up ? index - 1 : index + 1;
        if (target < 0 || target >= ordered.Count) throw TravelTripTypeProblem.Validation("direction", "The trip type cannot move beyond the catalog boundary.");
        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (var position = 0; position < ordered.Count; position++) ordered[position].SortOrder = position;
        await database.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
    }

    public async Task<CatalogDeletionImpactResponse> ImpactAsync(int id, CancellationToken token)
    {
        await FindAsync(id, token, tracked: false);
        var referenced = await database.Set<TravelTrip>().AnyAsync(trip => trip.TripTypeId == id, token);
        var count = await database.Set<TravelTripType>().CountAsync(token);
        return new(referenced, !referenced && count > 1, false, false, count > 1);
    }

    public async Task DeleteAsync(int id, CancellationToken token)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var tripType = await FindAsync(id, token);
        if (await database.Set<TravelTripType>().CountAsync(token) <= 1) throw TravelTripTypeProblem.RequiredNotEmpty();
        if (await database.Set<TravelTrip>().AnyAsync(trip => trip.TripTypeId == id, token)) throw TravelTripTypeProblem.Referenced();
        database.Remove(tripType);
        var remaining = await database.Set<TravelTripType>().Where(value => value.Id != id).OrderBy(value => value.SortOrder).ThenBy(value => value.Id).ToListAsync(token);
        for (var position = 0; position < remaining.Count; position++) remaining[position].SortOrder = position;
        try { await database.SaveChangesAsync(token); await transaction.CommitAsync(token); }
        catch (DbUpdateException) { throw TravelTripTypeProblem.Referenced(); }
    }

    public async Task ReplaceAndDeleteAsync(int id, CatalogReplacementRequest request, UserId actor, CancellationToken token)
    {
        if (request.ClearReferences
            || request.ExchangeRate is not null
            || request.ReplacementId is not { } replacementId
            || replacementId <= 0
            || replacementId == id)
        {
            throw TravelTripTypeProblem.InvalidReplacement();
        }

        await using var transaction = await database.Database.BeginTransactionAsync(token);
        var source = await FindAsync(id, token);
        if (!await database.Set<TravelTripType>().AnyAsync(value => value.Id == replacementId, token))
        {
            throw TravelTripTypeProblem.InvalidReplacement();
        }

        if (await database.Set<TravelTripType>().CountAsync(token) <= 1)
        {
            throw TravelTripTypeProblem.RequiredNotEmpty();
        }

        try
        {
            var trips = await database.Set<TravelTrip>()
                .Where(trip => trip.TripTypeId == id)
                .ToListAsync(token);
            var occurredAt = clock.UtcNow;
            foreach (var trip in trips)
            {
                trip.ReplaceTripType(replacementId, actor, occurredAt);
            }

            database.Remove(source);
            var remaining = await database.Set<TravelTripType>()
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
            throw TravelTripTypeProblem.MigrationConflict();
        }
    }

    private async Task<TravelTripType> FindAsync(int id, CancellationToken token, bool tracked = true)
    {
        IQueryable<TravelTripType> query = database.Set<TravelTripType>();
        if (!tracked) query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(value => value.Id == id, token) ?? throw TravelTripTypeProblem.NotFound();
    }

    private async Task EnsureUniqueAsync(int? id, string normalized, CancellationToken token)
    {
        if (await database.Set<TravelTripType>().AnyAsync(value => value.NormalizedName == normalized && value.Id != id, token))
            throw TravelTripTypeProblem.DuplicateName();
    }

    private async Task SaveAsync(CancellationToken token) { try { await database.SaveChangesAsync(token); } catch (DbUpdateException) { throw TravelTripTypeProblem.DuplicateName(); } }

    private static (string Display, string Normalized) ValidateName(string? value)
    {
        var display = value?.Trim();
        if (string.IsNullOrWhiteSpace(display) || display.Length > TravelValidation.CatalogNameMaxLength) throw TravelTripTypeProblem.Validation("name", $"Name is required and may contain at most {TravelValidation.CatalogNameMaxLength} characters.");
        return (display, TravelCatalogNormalization.Normalize(display));
    }

    private static TravelTripTypeResponse ToResponse(TravelTripType value) => new(value.Id, value.Name, value.SortOrder);
}
