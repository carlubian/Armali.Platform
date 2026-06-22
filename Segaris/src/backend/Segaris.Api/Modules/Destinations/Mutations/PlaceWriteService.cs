using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Destinations.Contracts;
using Segaris.Api.Modules.Destinations.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Destinations.Mutations;

/// <summary>
/// Write-side operations for the destination-scoped place sub-resource. Every
/// operation is bounded to its owning destination and inherits that destination's
/// authorization: a place may only be created, edited, or deleted by a user who may
/// mutate its destination. When the destination is not accessible, or the place does
/// not belong to it, the operation reports a miss so the HTTP surface answers with
/// the platform not-found behaviour.
/// </summary>
internal sealed class PlaceWriteService(SegarisDbContext database, IClock clock)
{
    public async Task<int?> CreateAsync(
        int destinationId,
        CreatePlaceRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await DestinationMutableAsync(destinationId, actorId, cancellationToken))
        {
            return null;
        }

        var values = MapValues(request.Name, request.CategoryId, request.Rating, request.Review, request.Address);
        var place = Place.Create(destinationId, values, actorId, clock.UtcNow);
        await ValidateCategoryAsync(values.CategoryId, cancellationToken);

        database.Add(place);
        await database.SaveChangesAsync(cancellationToken);
        return place.Id;
    }

    public async Task<bool> UpdateAsync(
        int destinationId,
        int placeId,
        UpdatePlaceRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await DestinationMutableAsync(destinationId, actorId, cancellationToken))
        {
            return false;
        }

        var place = await database.Set<Place>()
            .Where(candidate => candidate.DestinationId == destinationId && candidate.Id == placeId)
            .FirstOrDefaultAsync(cancellationToken);
        if (place is null)
        {
            return false;
        }

        var values = MapValues(request.Name, request.CategoryId, request.Rating, request.Review, request.Address);
        place.Update(values, actorId, clock.UtcNow);
        await ValidateCategoryAsync(values.CategoryId, cancellationToken);

        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        int destinationId,
        int placeId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        if (!await DestinationMutableAsync(destinationId, actorId, cancellationToken))
        {
            return false;
        }

        var place = await database.Set<Place>()
            .Where(candidate => candidate.DestinationId == destinationId && candidate.Id == placeId)
            .FirstOrDefaultAsync(cancellationToken);
        if (place is null)
        {
            return false;
        }

        database.Remove(place);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static PlaceValues MapValues(string? name, int categoryId, int? rating, string? review, string? address) =>
        new(name ?? string.Empty, categoryId, rating, review, address);

    private async Task<bool> DestinationMutableAsync(int destinationId, UserId actorId, CancellationToken cancellationToken) =>
        await database.Set<Destination>()
            .Where(DestinationPolicies.MutableBy(actorId))
            .AnyAsync(destination => destination.Id == destinationId, cancellationToken);

    private async Task ValidateCategoryAsync(int categoryId, CancellationToken cancellationToken)
    {
        var exists = await database.Set<PlaceCategory>()
            .AnyAsync(category => category.Id == categoryId, cancellationToken);
        if (!exists)
        {
            throw new DestinationsValidationException(
                "The selected place category does not exist.",
                DestinationsValidationReason.CatalogReference);
        }
    }
}
