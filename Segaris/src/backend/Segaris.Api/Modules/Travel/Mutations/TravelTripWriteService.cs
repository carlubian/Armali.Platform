using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Travel.Contracts;
using Segaris.Api.Modules.Travel.Domain;
using Segaris.Persistence;
using Segaris.Shared.Attachments;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Travel.Mutations;

/// <summary>
/// Write-side operations for Travel trips. Trips are the authorization root for
/// itinerary entries, expenses, and attachments: public trips are collaboratively
/// mutable while private trips remain creator-only. Visibility changes are limited
/// to the creator so a collaborator cannot privatize someone else's public trip.
/// </summary>
internal sealed class TravelTripWriteService(
    SegarisDbContext database,
    IAttachmentService attachments,
    IClock clock)
{
    public async Task<int> CreateAsync(
        CreateTravelTripRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var values = await MapCreateAsync(request, cancellationToken);
        var trip = TravelTrip.Create(values, actorId, clock.UtcNow);
        await ValidateTripTypeAsync(values.TripTypeId, cancellationToken);

        database.Add(trip);
        await database.SaveChangesAsync(cancellationToken);
        return trip.Id;
    }

    public async Task<bool> UpdateAsync(
        int tripId,
        UpdateTravelTripRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var trip = await database.Set<TravelTrip>()
            .Where(TravelTripPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == tripId)
            .Include(candidate => candidate.Itinerary)
            .FirstOrDefaultAsync(cancellationToken);
        if (trip is null)
        {
            return false;
        }

        var values = MapUpdate(request);
        ValidateVisibilityChange(trip, values.Visibility, actorId);
        trip.Update(values, actorId, clock.UtcNow);
        await ValidateTripTypeAsync(values.TripTypeId, cancellationToken);

        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        int tripId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var trip = await database.Set<TravelTrip>()
            .Where(TravelTripPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == tripId)
            .FirstOrDefaultAsync(cancellationToken);
        if (trip is null)
        {
            return false;
        }

        var expenseIds = await database.Set<TravelExpense>()
            .AsNoTracking()
            .Where(expense => expense.TripId == tripId)
            .Select(expense => expense.Id)
            .ToArrayAsync(cancellationToken);

        database.Remove(trip);
        await database.SaveChangesAsync(cancellationToken);

        await DeleteAttachmentsAsync(TravelAttachments.TripOwner(tripId), cancellationToken);
        foreach (var expenseId in expenseIds)
        {
            await DeleteAttachmentsAsync(TravelAttachments.ExpenseOwner(expenseId), cancellationToken);
        }

        return true;
    }

    private async Task<TravelTripValues> MapCreateAsync(
        CreateTravelTripRequest request,
        CancellationToken cancellationToken)
    {
        var startDate = request.StartDate == default
            ? TravelDefaults.Today(clock.UtcNow)
            : request.StartDate;
        var endDate = request.EndDate == default ? startDate : request.EndDate;
        var tripTypeId = request.TripTypeId > 0
            ? request.TripTypeId
            : await DefaultTripTypeIdAsync(cancellationToken);

        return new(
            request.Name ?? string.Empty,
            tripTypeId,
            request.Destination,
            startDate,
            endDate,
            ParseEnum(request.Status, TravelDefaults.TripStatus, "status"),
            request.Notes,
            ParseEnum(request.Visibility, TravelDefaults.Visibility, "visibility"),
            MapItinerary(request.Itinerary));
    }

    private static TravelTripValues MapUpdate(UpdateTravelTripRequest request) =>
        new(
            request.Name ?? string.Empty,
            request.TripTypeId,
            request.Destination,
            request.StartDate,
            request.EndDate,
            ParseEnum(request.Status, TravelDefaults.TripStatus, "status"),
            request.Notes,
            ParseEnum(request.Visibility, TravelDefaults.Visibility, "visibility"),
            MapItinerary(request.Itinerary));

    private static IReadOnlyList<TravelItineraryEntryValues> MapItinerary(
        IReadOnlyList<TravelItineraryEntryRequest>? itinerary) =>
        itinerary?.Select(entry => new TravelItineraryEntryValues(
            entry.Date,
            entry.Time,
            entry.Title ?? string.Empty,
            entry.Place,
            entry.ReservationLocator,
            entry.Note)).ToArray() ?? [];

    private async Task<int> DefaultTripTypeIdAsync(CancellationToken cancellationToken)
    {
        var id = await database.Set<TravelTripType>()
            .AsNoTracking()
            .OrderBy(tripType => tripType.SortOrder)
            .ThenBy(tripType => tripType.Id)
            .Select(tripType => (int?)tripType.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return id ?? throw new TravelValidationException(
            "A trip type is required.",
            TravelValidationReason.CatalogReference);
    }

    private async Task ValidateTripTypeAsync(int tripTypeId, CancellationToken cancellationToken)
    {
        var exists = await database.Set<TravelTripType>()
            .AnyAsync(tripType => tripType.Id == tripTypeId, cancellationToken);
        if (!exists)
        {
            throw new TravelValidationException(
                "The selected trip type does not exist.",
                TravelValidationReason.CatalogReference);
        }
    }

    private static void ValidateVisibilityChange(
        TravelTrip trip,
        RecordVisibility requestedVisibility,
        UserId actorId)
    {
        if (trip.Visibility == requestedVisibility || trip.CreatedBy == actorId.Value)
        {
            return;
        }

        throw new TravelValidationException(
            "Only the creator may change trip visibility.",
            TravelValidationReason.VisibilityForbidden);
    }

    private async Task DeleteAttachmentsAsync(AttachmentOwner owner, CancellationToken cancellationToken)
    {
        var descriptors = await attachments.ListByOwnerAsync(owner, cancellationToken);
        foreach (var descriptor in descriptors)
        {
            await attachments.DeleteAsync(descriptor.Id, owner, cancellationToken);
        }
    }

    private static TEnum ParseEnum<TEnum>(string? value, TEnum defaultValue, string field)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new TravelValidationException($"The {field} is not a recognized value.");
    }
}
