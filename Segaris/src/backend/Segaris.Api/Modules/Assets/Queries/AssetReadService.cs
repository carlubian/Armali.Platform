using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Assets.Contracts;
using Segaris.Api.Modules.Assets.Domain;
using Segaris.Api.Modules.Identity;
using Segaris.Persistence;
using Segaris.Shared.Api;
using Segaris.Shared.Attachments;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Assets.Queries;

/// <summary>
/// Read surface for Assets: the module-owned category and location catalog reads,
/// the paginated asset table, and asset detail. Every asset query is privacy-correct:
/// it filters to the assets the supplied user may access before any projection,
/// pagination, or detail lookup. Related catalog and audit display names are resolved
/// through correlated sub-queries. Attachment descriptors are loaded after the
/// privacy-filtered asset page/detail is known, then used to resolve primary-image
/// thumbnails and attachment DTOs.
/// </summary>
internal sealed class AssetReadService(SegarisDbContext database, IAttachmentService attachments)
{
    public async Task<IReadOnlyList<AssetCategoryResponse>> ListCategoriesAsync(CancellationToken cancellationToken)
    {
        return await database.Set<AssetCategory>()
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Id)
            .Select(category => new AssetCategoryResponse(category.Id, category.Name, category.SortOrder))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AssetLocationResponse>> ListLocationsAsync(CancellationToken cancellationToken)
    {
        return await database.Set<AssetLocation>()
            .AsNoTracking()
            .OrderBy(location => location.SortOrder)
            .ThenBy(location => location.Id)
            .Select(location => new AssetLocationResponse(location.Id, location.Name, location.SortOrder))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<PaginatedResponse<AssetSummaryResponse>> ListAssetsAsync(
        AssetFilter filter,
        PaginationRequest pagination,
        SortRequest sort,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var assets = ApplyFilters(
            database.Set<Asset>().AsNoTracking().Where(AssetPolicies.AccessibleTo(userId)),
            filter);

        var totalCount = await assets.CountAsync(cancellationToken);

        var page = await ApplySort(assets, sort)
            .Skip(pagination.Offset)
            .Take(pagination.PageSize)
            .Select(asset => new SummaryRow(
                asset.Id,
                asset.Name,
                asset.Code,
                asset.CategoryId,
                database.Set<AssetCategory>()
                    .Where(category => category.Id == asset.CategoryId).Select(category => category.Name).First(),
                asset.LocationId,
                database.Set<AssetLocation>()
                    .Where(location => location.Id == asset.LocationId).Select(location => location.Name).First(),
                asset.Status,
                asset.ExpectedEndOfLifeDate,
                asset.Visibility,
                asset.PrimaryAttachmentId,
                asset.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == asset.CreatedBy).Select(user => user.DisplayName).First()))
            .ToListAsync(cancellationToken);

        var items = new AssetSummaryResponse[page.Count];
        for (var index = 0; index < page.Count; index++)
        {
            var row = page[index];
            var descriptors = await attachments.ListByOwnerAsync(
                AssetsAttachments.AssetOwner(row.Id),
                cancellationToken);
            items[index] = ToSummary(row, descriptors);
        }

        return PaginatedResponse<AssetSummaryResponse>.Create(items, pagination, totalCount);
    }

    /// <summary>
    /// Returns whether the asset exists and is accessible to the user. Attachment
    /// routes (Wave 3) resolve their authorization through this before touching the
    /// platform attachment service, so a private asset is reported as not found
    /// rather than disclosed.
    /// </summary>
    public Task<bool> AssetAccessibleAsync(
        int assetId,
        UserId userId,
        CancellationToken cancellationToken) =>
        database.Set<Asset>()
            .AsNoTracking()
            .Where(AssetPolicies.AccessibleTo(userId))
            .AnyAsync(asset => asset.Id == assetId, cancellationToken);

    /// <summary>
    /// Lists attachments for an accessible asset, flagging the stored primary image.
    /// Returns <c>null</c> for missing or inaccessible assets to preserve not-found
    /// privacy behaviour.
    /// </summary>
    public async Task<IReadOnlyList<AssetAttachmentResponse>?> ListAssetAttachmentsAsync(
        int assetId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var asset = await database.Set<Asset>()
            .AsNoTracking()
            .Where(AssetPolicies.AccessibleTo(userId))
            .Where(candidate => candidate.Id == assetId)
            .Select(candidate => new { candidate.PrimaryAttachmentId })
            .FirstOrDefaultAsync(cancellationToken);
        if (asset is null)
        {
            return null;
        }

        var descriptors = await attachments.ListByOwnerAsync(
            AssetsAttachments.AssetOwner(assetId),
            cancellationToken);
        return descriptors
            .Select(descriptor => ToAttachmentResponse(descriptor, asset.PrimaryAttachmentId))
            .ToArray();
    }

    public async Task<AssetResponse?> GetAssetAsync(
        int assetId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var row = await database.Set<Asset>()
            .AsNoTracking()
            .Where(AssetPolicies.AccessibleTo(userId))
            .Where(asset => asset.Id == assetId)
            .Select(asset => new DetailRow(
                asset.Id,
                asset.Name,
                asset.Code,
                asset.CategoryId,
                database.Set<AssetCategory>()
                    .Where(category => category.Id == asset.CategoryId).Select(category => category.Name).First(),
                asset.LocationId,
                database.Set<AssetLocation>()
                    .Where(location => location.Id == asset.LocationId).Select(location => location.Name).First(),
                asset.Status,
                asset.BrandModel,
                asset.SerialNumber,
                asset.AcquisitionDate,
                asset.ExpectedEndOfLifeDate,
                asset.Notes,
                asset.Visibility,
                asset.PrimaryAttachmentId,
                asset.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == asset.CreatedBy).Select(user => user.DisplayName).First(),
                asset.CreatedAt,
                asset.UpdatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == asset.UpdatedBy).Select(user => user.DisplayName).First(),
                asset.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var descriptors = await attachments.ListByOwnerAsync(
            AssetsAttachments.AssetOwner(row.Id),
            cancellationToken);
        return new AssetResponse(
            row.Id,
            row.Name,
            row.Code,
            row.CategoryId,
            row.CategoryName,
            row.LocationId,
            row.LocationName,
            row.Status.ToString(),
            row.BrandModel,
            row.SerialNumber,
            row.AcquisitionDate,
            row.ExpectedEndOfLifeDate,
            row.Notes,
            row.Visibility.ToString(),
            AssetThumbnailResolver.Resolve(row.Id, row.PrimaryAttachmentId, descriptors),
            descriptors.Select(descriptor => ToAttachmentResponse(descriptor, row.PrimaryAttachmentId)).ToArray(),
            row.CreatedById,
            row.CreatedByName,
            row.CreatedAt,
            row.UpdatedById,
            row.UpdatedByName,
            row.UpdatedAt);
    }

    private static IQueryable<Asset> ApplyFilters(IQueryable<Asset> assets, AssetFilter filter)
    {
        if (filter.Search is { } search)
        {
            var pattern = $"%{Escape(search.ToLowerInvariant())}%";
            assets = assets.Where(asset =>
                EF.Functions.Like(asset.Name.ToLower(), pattern, "\\")
                || (asset.Code != null && EF.Functions.Like(asset.Code.ToLower(), pattern, "\\"))
                || (asset.BrandModel != null && EF.Functions.Like(asset.BrandModel.ToLower(), pattern, "\\"))
                || (asset.SerialNumber != null && EF.Functions.Like(asset.SerialNumber.ToLower(), pattern, "\\"))
                || (asset.Notes != null && EF.Functions.Like(asset.Notes.ToLower(), pattern, "\\")));
        }

        if (filter.CategoryId is { } categoryId)
        {
            assets = assets.Where(asset => asset.CategoryId == categoryId);
        }

        if (filter.LocationId is { } locationId)
        {
            assets = assets.Where(asset => asset.LocationId == locationId);
        }

        if (filter.Status is { } status)
        {
            assets = assets.Where(asset => asset.Status == status);
        }

        if (filter.Visibility is { } visibility)
        {
            assets = assets.Where(asset => asset.Visibility == visibility);
        }

        if (filter.CreatorId is { } creatorId)
        {
            assets = assets.Where(asset => asset.CreatedBy == creatorId);
        }

        return assets;
    }

    private IQueryable<Asset> ApplySort(IQueryable<Asset> assets, SortRequest sort)
    {
        var ascending = sort.Direction == SortDirection.Ascending;

        IOrderedQueryable<Asset> ordered = sort.Field switch
        {
            AssetQuery.SortFields.Name => ascending
                ? assets.OrderBy(asset => asset.Name)
                : assets.OrderByDescending(asset => asset.Name),
            // The optional code sorts its nulls last in either direction.
            AssetQuery.SortFields.Code => ascending
                ? assets.OrderBy(asset => asset.Code == null).ThenBy(asset => asset.Code)
                : assets.OrderBy(asset => asset.Code == null).ThenByDescending(asset => asset.Code),
            AssetQuery.SortFields.Category => ascending
                ? assets.OrderBy(asset => database.Set<AssetCategory>()
                    .Where(category => category.Id == asset.CategoryId).Select(category => category.Name).First())
                : assets.OrderByDescending(asset => database.Set<AssetCategory>()
                    .Where(category => category.Id == asset.CategoryId).Select(category => category.Name).First()),
            AssetQuery.SortFields.Location => ascending
                ? assets.OrderBy(asset => database.Set<AssetLocation>()
                    .Where(location => location.Id == asset.LocationId).Select(location => location.Name).First())
                : assets.OrderByDescending(asset => database.Set<AssetLocation>()
                    .Where(location => location.Id == asset.LocationId).Select(location => location.Name).First()),
            AssetQuery.SortFields.Status => ascending
                ? assets.OrderBy(asset => asset.Status)
                : assets.OrderByDescending(asset => asset.Status),
            // The optional expected end of life sorts its nulls last in either direction.
            AssetQuery.SortFields.ExpectedEndOfLife => ascending
                ? assets.OrderBy(asset => asset.ExpectedEndOfLifeDate == null).ThenBy(asset => asset.ExpectedEndOfLifeDate)
                : assets.OrderBy(asset => asset.ExpectedEndOfLifeDate == null).ThenByDescending(asset => asset.ExpectedEndOfLifeDate),
            AssetQuery.SortFields.Visibility => ascending
                ? assets.OrderBy(asset => asset.Visibility)
                : assets.OrderByDescending(asset => asset.Visibility),
            AssetQuery.SortFields.Id => ascending
                ? assets.OrderBy(asset => asset.Id)
                : assets.OrderByDescending(asset => asset.Id),
            _ => ascending
                ? assets.OrderBy(asset => asset.Name)
                : assets.OrderByDescending(asset => asset.Name),
        };

        // Every ordering ends with the asset identifier ascending as the stable
        // tie-breaker required by the documented default ordering.
        return ordered.ThenBy(asset => asset.Id);
    }

    private static AssetSummaryResponse ToSummary(
        SummaryRow row,
        IReadOnlyList<AttachmentDescriptor> descriptors) => new(
        row.Id,
        row.Name,
        row.Code,
        row.CategoryId,
        row.CategoryName,
        row.LocationId,
        row.LocationName,
        row.Status.ToString(),
        row.ExpectedEndOfLifeDate,
        row.Visibility.ToString(),
        AssetThumbnailResolver.Resolve(row.Id, row.PrimaryAttachmentId, descriptors),
        row.CreatorId,
        row.CreatorName);

    private static AssetAttachmentResponse ToAttachmentResponse(
        AttachmentDescriptor descriptor,
        int? primaryAttachmentId) => new(
        descriptor.Id.Value.ToString(CultureInfo.InvariantCulture),
        descriptor.FileName,
        descriptor.ContentType,
        descriptor.Size,
        descriptor.CreatedBy.Value,
        descriptor.CreatedAt,
        descriptor.Id.Value == primaryAttachmentId);

    private static string Escape(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);

    private sealed record SummaryRow(
        int Id,
        string Name,
        string? Code,
        int CategoryId,
        string CategoryName,
        int LocationId,
        string LocationName,
        AssetStatus Status,
        DateOnly? ExpectedEndOfLifeDate,
        RecordVisibility Visibility,
        int? PrimaryAttachmentId,
        int CreatorId,
        string CreatorName);

    private sealed record DetailRow(
        int Id,
        string Name,
        string? Code,
        int CategoryId,
        string CategoryName,
        int LocationId,
        string LocationName,
        AssetStatus Status,
        string? BrandModel,
        string? SerialNumber,
        DateOnly? AcquisitionDate,
        DateOnly? ExpectedEndOfLifeDate,
        string? Notes,
        RecordVisibility Visibility,
        int? PrimaryAttachmentId,
        int CreatedById,
        string CreatedByName,
        DateTimeOffset CreatedAt,
        int UpdatedById,
        string UpdatedByName,
        DateTimeOffset UpdatedAt);
}
