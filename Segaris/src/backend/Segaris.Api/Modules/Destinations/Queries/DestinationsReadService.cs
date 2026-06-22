using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Destinations.Contracts;
using Segaris.Api.Modules.Destinations.Domain;
using Segaris.Api.Modules.Identity;
using Segaris.Persistence;
using Segaris.Shared.Api;
using Segaris.Shared.Attachments;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Destinations.Queries;

/// <summary>Read-side queries for accessible Destinations records.</summary>
internal sealed class DestinationsReadService(SegarisDbContext database, IAttachmentService attachments)
{
    public async Task<PaginatedResponse<DestinationSummaryResponse>> ListDestinationsAsync(
        DestinationFilter filter,
        PaginationRequest pagination,
        SortRequest sort,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var destinations = ApplyFilters(
            database.Set<Destination>().AsNoTracking().Where(DestinationPolicies.AccessibleTo(userId)),
            filter);

        var totalCount = await destinations.CountAsync(cancellationToken);
        var rows = await ApplySort(destinations, sort)
            .Skip(pagination.Offset)
            .Take(pagination.PageSize)
            .Select(destination => new DestinationSummaryRow(
                destination.Id,
                destination.Name,
                destination.CategoryId,
                database.Set<DestinationCategory>()
                    .Where(category => category.Id == destination.CategoryId).Select(category => category.Name).First(),
                destination.Country,
                destination.IsSchengenArea,
                destination.Visibility,
                destination.PrimaryAttachmentId,
                destination.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == destination.CreatedBy).Select(user => user.DisplayName).First()))
            .ToListAsync(cancellationToken);

        var ratings = await RatingAggregatesAsync(rows.Select(row => row.Id), cancellationToken);
        var page = new DestinationSummaryResponse[rows.Count];
        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            ratings.TryGetValue(row.Id, out var rating);
            var descriptors = await attachments.ListByOwnerAsync(
                DestinationsAttachments.DestinationOwner(row.Id),
                cancellationToken);
            page[index] = new DestinationSummaryResponse(
                row.Id,
                row.Name,
                row.CategoryId,
                row.CategoryName,
                row.Country,
                row.IsSchengenArea,
                rating?.Average,
                rating?.Count ?? 0,
                row.Visibility.ToString(),
                DestinationThumbnailResolver.Resolve(row.Id, row.PrimaryAttachmentId, descriptors),
                row.CreatorId,
                row.CreatorName);
        }

        return PaginatedResponse<DestinationSummaryResponse>.Create(page, pagination, totalCount);
    }

    public Task<bool> DestinationAccessibleAsync(
        int destinationId,
        UserId userId,
        CancellationToken cancellationToken) =>
        database.Set<Destination>()
            .AsNoTracking()
            .Where(DestinationPolicies.AccessibleTo(userId))
            .AnyAsync(destination => destination.Id == destinationId, cancellationToken);

    public async Task<IReadOnlyList<DestinationAttachmentResponse>?> ListDestinationAttachmentsAsync(
        int destinationId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var destination = await database.Set<Destination>()
            .AsNoTracking()
            .Where(DestinationPolicies.AccessibleTo(userId))
            .Where(candidate => candidate.Id == destinationId)
            .Select(candidate => new { candidate.PrimaryAttachmentId })
            .FirstOrDefaultAsync(cancellationToken);
        if (destination is null)
        {
            return null;
        }

        var descriptors = await attachments.ListByOwnerAsync(
            DestinationsAttachments.DestinationOwner(destinationId),
            cancellationToken);
        return descriptors
            .Select(descriptor => ToAttachmentResponse(descriptor, destination.PrimaryAttachmentId))
            .ToArray();
    }

    public async Task<DestinationResponse?> GetDestinationAsync(
        int destinationId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var row = await database.Set<Destination>()
            .AsNoTracking()
            .Where(DestinationPolicies.AccessibleTo(userId))
            .Where(destination => destination.Id == destinationId)
            .Select(destination => new DestinationDetailRow(
                destination.Id,
                destination.Name,
                destination.CategoryId,
                database.Set<DestinationCategory>()
                    .Where(category => category.Id == destination.CategoryId).Select(category => category.Name).First(),
                destination.Country,
                destination.EntryRequirements,
                destination.IsSchengenArea,
                destination.Notes,
                destination.Visibility,
                destination.PrimaryAttachmentId,
                destination.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == destination.CreatedBy).Select(user => user.DisplayName).First(),
                destination.CreatedAt,
                destination.UpdatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == destination.UpdatedBy).Select(user => user.DisplayName).FirstOrDefault(),
                destination.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var ratings = await RatingAggregatesAsync([row.Id], cancellationToken);
        ratings.TryGetValue(row.Id, out var rating);
        var descriptors = await attachments.ListByOwnerAsync(
            DestinationsAttachments.DestinationOwner(row.Id),
            cancellationToken);

        return new DestinationResponse(
            row.Id,
            row.Name,
            row.CategoryId,
            row.CategoryName,
            row.Country,
            row.EntryRequirements,
            row.IsSchengenArea,
            row.Notes,
            rating?.Average,
            rating?.Count ?? 0,
            row.Visibility.ToString(),
            DestinationThumbnailResolver.Resolve(row.Id, row.PrimaryAttachmentId, descriptors),
            descriptors.Select(descriptor => ToAttachmentResponse(descriptor, row.PrimaryAttachmentId)).ToArray(),
            row.CreatedById,
            row.CreatedByName,
            row.CreatedAt,
            row.UpdatedById,
            row.UpdatedByName,
            row.UpdatedAt);
    }

    private IQueryable<Destination> ApplyFilters(
        IQueryable<Destination> destinations,
        DestinationFilter filter)
    {
        if (filter.Search is { } search)
        {
            var pattern = $"%{Escape(search.ToLowerInvariant())}%";
            destinations = destinations.Where(destination =>
                EF.Functions.Like(destination.Name.ToLower(), pattern, "\\"));
        }

        if (filter.CategoryId is { } categoryId)
        {
            destinations = destinations.Where(destination => destination.CategoryId == categoryId);
        }

        if (filter.IsSchengenArea is { } isSchengenArea)
        {
            destinations = destinations.Where(destination => destination.IsSchengenArea == isSchengenArea);
        }

        return destinations;
    }

    private IQueryable<Destination> ApplySort(IQueryable<Destination> destinations, SortRequest sort)
    {
        var ascending = sort.Direction == SortDirection.Ascending;

        IOrderedQueryable<Destination> ordered = sort.Field switch
        {
            DestinationQuery.SortFields.Category => ascending
                ? destinations.OrderBy(destination => database.Set<DestinationCategory>()
                    .Where(category => category.Id == destination.CategoryId).Select(category => category.Name).First())
                : destinations.OrderByDescending(destination => database.Set<DestinationCategory>()
                    .Where(category => category.Id == destination.CategoryId).Select(category => category.Name).First()),
            DestinationQuery.SortFields.TieBreaker => ascending
                ? destinations.OrderBy(destination => destination.Id)
                : destinations.OrderByDescending(destination => destination.Id),
            _ => ascending
                ? destinations.OrderBy(destination => destination.Name)
                : destinations.OrderByDescending(destination => destination.Name),
        };

        return ascending ? ordered.ThenBy(destination => destination.Id) : ordered.ThenByDescending(destination => destination.Id);
    }

    private async Task<IReadOnlyDictionary<int, RatingAggregate>> RatingAggregatesAsync(
        IEnumerable<int> destinationIds,
        CancellationToken cancellationToken)
    {
        var ids = destinationIds.ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<int, RatingAggregate>();
        }

        var rows = await database.Set<Place>()
            .AsNoTracking()
            .Where(place => ids.Contains(place.DestinationId) && place.Rating != null)
            .GroupBy(place => place.DestinationId)
            .Select(group => new
            {
                DestinationId = group.Key,
                Average = group.Average(place => place.Rating!.Value),
                Count = group.Count(),
            })
            .ToArrayAsync(cancellationToken);

        return rows.ToDictionary(
            row => row.DestinationId,
            row => new RatingAggregate(decimal.Round((decimal)row.Average, 2), row.Count));
    }

    private static string Escape(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);

    private static DestinationAttachmentResponse ToAttachmentResponse(
        AttachmentDescriptor descriptor,
        int? primaryAttachmentId) => new(
        descriptor.Id.Value.ToString(CultureInfo.InvariantCulture),
        descriptor.FileName,
        descriptor.ContentType,
        descriptor.Size,
        descriptor.CreatedBy.Value,
        descriptor.CreatedAt,
        descriptor.Id.Value == primaryAttachmentId);

    private sealed record RatingAggregate(decimal Average, int Count);

    private sealed record DestinationSummaryRow(
        int Id,
        string Name,
        int CategoryId,
        string CategoryName,
        string? Country,
        bool IsSchengenArea,
        RecordVisibility Visibility,
        int? PrimaryAttachmentId,
        int CreatorId,
        string CreatorName);

    private sealed record DestinationDetailRow(
        int Id,
        string Name,
        int CategoryId,
        string CategoryName,
        string? Country,
        string? EntryRequirements,
        bool IsSchengenArea,
        string? Notes,
        RecordVisibility Visibility,
        int? PrimaryAttachmentId,
        int CreatedById,
        string CreatedByName,
        DateTimeOffset CreatedAt,
        int? UpdatedById,
        string? UpdatedByName,
        DateTimeOffset? UpdatedAt);
}
