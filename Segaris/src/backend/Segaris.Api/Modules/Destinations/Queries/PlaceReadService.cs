using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Destinations.Contracts;
using Segaris.Api.Modules.Destinations.Domain;
using Segaris.Persistence;
using Segaris.Shared.Api;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Destinations.Queries;

/// <summary>
/// Read-side queries for the destination-scoped place sub-resource. Every query is
/// bounded to a single owning destination and inherits that destination's
/// visibility: when the destination is not accessible the queries return
/// <see langword="null"/> so the HTTP surface can answer with the platform
/// not-found behaviour and never disclose another user's private destination or its
/// places.
/// </summary>
internal sealed class PlaceReadService(SegarisDbContext database)
{
    public async Task<PaginatedResponse<PlaceSummaryResponse>?> ListPlacesAsync(
        int destinationId,
        PlaceFilter filter,
        PaginationRequest pagination,
        SortRequest sort,
        UserId userId,
        CancellationToken cancellationToken)
    {
        if (!await DestinationAccessibleAsync(destinationId, userId, cancellationToken))
        {
            return null;
        }

        var places = ApplyFilters(
            database.Set<Place>().AsNoTracking().Where(place => place.DestinationId == destinationId),
            filter);

        var totalCount = await places.CountAsync(cancellationToken);
        var page = await ApplySort(places, sort)
            .Skip(pagination.Offset)
            .Take(pagination.PageSize)
            .Select(PlaceProjection())
            .ToArrayAsync(cancellationToken);

        return PaginatedResponse<PlaceSummaryResponse>.Create(page, pagination, totalCount);
    }

    public async Task<PlaceSummaryResponse?> GetPlaceAsync(
        int destinationId,
        int placeId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        if (!await DestinationAccessibleAsync(destinationId, userId, cancellationToken))
        {
            return null;
        }

        return await database.Set<Place>()
            .AsNoTracking()
            .Where(place => place.DestinationId == destinationId && place.Id == placeId)
            .Select(PlaceProjection())
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<bool> DestinationAccessibleAsync(int destinationId, UserId userId, CancellationToken cancellationToken) =>
        await database.Set<Destination>()
            .AsNoTracking()
            .Where(DestinationPolicies.AccessibleTo(userId))
            .AnyAsync(destination => destination.Id == destinationId, cancellationToken);

    private System.Linq.Expressions.Expression<Func<Place, PlaceSummaryResponse>> PlaceProjection() =>
        place => new PlaceSummaryResponse(
            place.Id,
            place.DestinationId,
            place.Name,
            place.CategoryId,
            database.Set<PlaceCategory>()
                .Where(category => category.Id == place.CategoryId).Select(category => category.Name).First(),
            place.Rating,
            place.Review,
            place.Address,
            place.CreatedAt,
            place.UpdatedAt);

    private IQueryable<Place> ApplyFilters(IQueryable<Place> places, PlaceFilter filter)
    {
        if (filter.Search is { } search)
        {
            var pattern = $"%{Escape(search.ToLowerInvariant())}%";
            places = places.Where(place => EF.Functions.Like(place.Name.ToLower(), pattern, "\\"));
        }

        if (filter.CategoryId is { } categoryId)
        {
            places = places.Where(place => place.CategoryId == categoryId);
        }

        if (filter.Rating is { } rating)
        {
            places = places.Where(place => place.Rating == rating);
        }

        return places;
    }

    private IQueryable<Place> ApplySort(IQueryable<Place> places, SortRequest sort)
    {
        var ascending = sort.Direction == SortDirection.Ascending;

        IOrderedQueryable<Place> ordered = sort.Field switch
        {
            PlaceQuery.SortFields.Category => ascending
                ? places.OrderBy(place => database.Set<PlaceCategory>()
                    .Where(category => category.Id == place.CategoryId).Select(category => category.Name).First())
                : places.OrderByDescending(place => database.Set<PlaceCategory>()
                    .Where(category => category.Id == place.CategoryId).Select(category => category.Name).First()),
            PlaceQuery.SortFields.Rating => ascending
                ? places.OrderBy(place => place.Rating)
                : places.OrderByDescending(place => place.Rating),
            PlaceQuery.SortFields.TieBreaker => ascending
                ? places.OrderBy(place => place.Id)
                : places.OrderByDescending(place => place.Id),
            _ => ascending
                ? places.OrderBy(place => place.Name)
                : places.OrderByDescending(place => place.Name),
        };

        return ascending ? ordered.ThenBy(place => place.Id) : ordered.ThenByDescending(place => place.Id);
    }

    private static string Escape(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);
}
