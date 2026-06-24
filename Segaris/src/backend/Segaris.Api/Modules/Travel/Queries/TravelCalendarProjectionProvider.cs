using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Destinations.Contracts;
using Segaris.Api.Modules.Travel.Contracts;
using Segaris.Api.Modules.Travel.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Travel.Queries;

/// <summary>
/// Publishes accessible, non-<see cref="TravelTripStatus.Cancelled"/> trips whose civil
/// date range intersects the requested Calendar range. The trip is projected as a single
/// continuous all-day range; Calendar renders it across every covered day. Destination
/// display names are resolved through the Destinations read contract so private
/// destinations are never disclosed.
/// </summary>
internal sealed class TravelCalendarProjectionProvider(
    SegarisDbContext database,
    IDestinationReferenceReader destinationReferences) : ITravelCalendarProjectionProvider
{
    public async Task<IReadOnlyList<TravelTripCalendarProjection>> ListCalendarTripsAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken)
    {
        var rows = await database.Set<TravelTrip>()
            .AsNoTracking()
            .Where(TravelTripPolicies.AccessibleTo(viewer))
            .Where(trip => trip.Status != TravelTripStatus.Cancelled)
            .Where(trip => trip.StartDate <= to && trip.EndDate >= from)
            .Select(trip => new TripRow(
                trip.Id,
                trip.Name,
                trip.DestinationId,
                trip.StartDate,
                trip.EndDate,
                trip.Status))
            .ToArrayAsync(cancellationToken);

        var destinationIds = rows
            .Where(row => row.DestinationId is not null)
            .Select(row => row.DestinationId!.Value)
            .Distinct()
            .ToArray();
        var destinations = destinationIds.Length == 0
            ? new Dictionary<int, DestinationReference>()
            : await destinationReferences.ResolveAccessibleAsync(destinationIds, viewer, cancellationToken);

        return rows
            .Select(row => new TravelTripCalendarProjection(
                row.Id,
                row.Name,
                row.DestinationId is { } destinationId
                    && destinations.TryGetValue(destinationId, out var destination)
                        ? destination.Name
                        : null,
                row.StartDate,
                row.EndDate,
                row.Status.ToString(),
                $"/travel?tripId={row.Id}"))
            .ToArray();
    }

    private sealed record TripRow(
        int Id,
        string Name,
        int? DestinationId,
        DateOnly StartDate,
        DateOnly EndDate,
        TravelTripStatus Status);
}
