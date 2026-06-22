using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Destinations.Contracts;
using Segaris.Api.Modules.Travel.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Travel.Mutations;

/// <summary>
/// Travel implementation of the Destinations-owned deletion-reference contract.
/// Destinations enumerates this handler through DI and never references Travel
/// entities directly.
/// </summary>
internal sealed class TravelDestinationDeletionReferenceHandler(SegarisDbContext database)
    : IDestinationDeletionReferenceHandler
{
    public Task<int> CountReferencesAsync(int destinationId, CancellationToken cancellationToken) =>
        database.Set<TravelTrip>()
            .AsNoTracking()
            .CountAsync(trip => trip.DestinationId == destinationId, cancellationToken);

    public async Task ClearReferencesAsync(
        DestinationDeletionClearing clearing,
        CancellationToken cancellationToken)
    {
        var trips = await database.Set<TravelTrip>()
            .Where(trip => trip.DestinationId == clearing.DestinationId)
            .ToListAsync(cancellationToken);

        foreach (var trip in trips)
        {
            trip.ClearDestinationReference(clearing.DestinationId, clearing.Actor, clearing.OccurredAt);
        }
    }
}
