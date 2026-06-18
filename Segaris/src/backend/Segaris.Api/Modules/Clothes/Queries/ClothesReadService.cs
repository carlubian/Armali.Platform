using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Clothes.Contracts;
using Segaris.Api.Modules.Clothes.Domain;
using Segaris.Api.Modules.Identity;
using Segaris.Persistence;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Clothes.Queries;

/// <summary>
/// Read-side queries for Clothes. Wave 1 exposes the module-owned category and colour
/// catalogs in their deterministic order; Wave 2 adds the paginated garment
/// gallery and detail reads. Every garment query filters to accessible records
/// before projection, pagination, or detail lookup.
/// </summary>
internal sealed class ClothesReadService(SegarisDbContext database)
{
    public async Task<IReadOnlyList<ClothingCategoryResponse>> ListCategoriesAsync(CancellationToken cancellationToken)
    {
        return await database.Set<ClothingCategory>()
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Id)
            .Select(category => new ClothingCategoryResponse(category.Id, category.Name, category.SortOrder))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClothingColorResponse>> ListColorsAsync(CancellationToken cancellationToken)
    {
        return await database.Set<ClothingColor>()
            .AsNoTracking()
            .OrderBy(color => color.SortOrder)
            .ThenBy(color => color.Id)
            .Select(color => new ClothingColorResponse(color.Id, color.Name, color.ColorValue, color.SortOrder))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<PaginatedResponse<ClothesGarmentSummaryResponse>> ListGarmentsAsync(
        ClothesGarmentFilter filter,
        PaginationRequest pagination,
        SortRequest sort,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var garments = ApplyFilters(
            database.Set<ClothesGarment>().AsNoTracking().Where(ClothesGarmentPolicies.AccessibleTo(userId)),
            filter);

        var totalCount = await garments.CountAsync(cancellationToken);

        var rows = await ApplySort(garments, sort)
            .Skip(pagination.Offset)
            .Take(pagination.PageSize)
            .Select(garment => new GarmentSummaryRow(
                garment.Id,
                garment.Name,
                garment.CategoryId,
                database.Set<ClothingCategory>()
                    .Where(category => category.Id == garment.CategoryId).Select(category => category.Name).First(),
                garment.Status,
                garment.Size,
                garment.Visibility,
                garment.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == garment.CreatedBy).Select(user => user.DisplayName).First()))
            .ToArrayAsync(cancellationToken);

        var colors = await LoadColorsAsync(rows.Select(row => row.Id).ToArray(), cancellationToken);
        var page = rows.Select(row => new ClothesGarmentSummaryResponse(
            row.Id,
            row.Name,
            row.CategoryId,
            row.CategoryName,
            row.Status.ToString(),
            row.Size,
            colors.GetValueOrDefault(row.Id, []),
            row.Visibility.ToString(),
            PlaceholderThumbnail(),
            row.CreatorId,
            row.CreatorName))
            .ToArray();

        return PaginatedResponse<ClothesGarmentSummaryResponse>.Create(page, pagination, totalCount);
    }

    public Task<bool> GarmentAccessibleAsync(
        int garmentId,
        UserId userId,
        CancellationToken cancellationToken) =>
        database.Set<ClothesGarment>()
            .AsNoTracking()
            .Where(ClothesGarmentPolicies.AccessibleTo(userId))
            .AnyAsync(garment => garment.Id == garmentId, cancellationToken);

    public async Task<ClothesGarmentResponse?> GetGarmentAsync(
        int garmentId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var row = await database.Set<ClothesGarment>()
            .AsNoTracking()
            .Where(ClothesGarmentPolicies.AccessibleTo(userId))
            .Where(garment => garment.Id == garmentId)
            .Select(garment => new GarmentDetailRow(
                garment.Id,
                garment.Name,
                garment.CategoryId,
                database.Set<ClothingCategory>()
                    .Where(category => category.Id == garment.CategoryId).Select(category => category.Name).First(),
                garment.Status,
                garment.Size,
                garment.WashingCare,
                garment.DryingCare,
                garment.IroningCare,
                garment.DryCleaningCare,
                garment.Notes,
                garment.Visibility,
                garment.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == garment.CreatedBy).Select(user => user.DisplayName).First(),
                garment.CreatedAt,
                garment.UpdatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == garment.UpdatedBy).Select(user => user.DisplayName).First(),
                garment.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var colors = await LoadColorsAsync([row.Id], cancellationToken);
        return new ClothesGarmentResponse(
            row.Id,
            row.Name,
            row.CategoryId,
            row.CategoryName,
            row.Status.ToString(),
            row.Size,
            colors.GetValueOrDefault(row.Id, []),
            row.WashingCare?.ToString(),
            row.DryingCare?.ToString(),
            row.IroningCare?.ToString(),
            row.DryCleaningCare?.ToString(),
            row.Notes,
            row.Visibility.ToString(),
            PlaceholderThumbnail(),
            [],
            row.CreatedById,
            row.CreatedByName,
            row.CreatedAt,
            row.UpdatedById,
            row.UpdatedByName,
            row.UpdatedAt);
    }

    private static IQueryable<ClothesGarment> ApplyFilters(
        IQueryable<ClothesGarment> garments,
        ClothesGarmentFilter filter)
    {
        if (filter.Search is { } search)
        {
            var pattern = $"%{Escape(search.ToLowerInvariant())}%";
            garments = garments.Where(garment =>
                EF.Functions.Like(garment.Name.ToLower(), pattern, "\\")
                || (garment.Size != null && EF.Functions.Like(garment.Size.ToLower(), pattern, "\\"))
                || (garment.Notes != null && EF.Functions.Like(garment.Notes.ToLower(), pattern, "\\")));
        }

        if (filter.CategoryId is { } categoryId)
        {
            garments = garments.Where(garment => garment.CategoryId == categoryId);
        }

        if (filter.Status is { } status)
        {
            garments = garments.Where(garment => garment.Status == status);
        }

        if (filter.ColorId is { } colorId)
        {
            garments = garments.Where(garment => garment.Colors.Any(color => color.ColorId == colorId));
        }

        if (filter.Visibility is { } visibility)
        {
            garments = garments.Where(garment => garment.Visibility == visibility);
        }

        if (filter.CreatorId is { } creatorId)
        {
            garments = garments.Where(garment => garment.CreatedBy == creatorId);
        }

        return garments;
    }

    private IQueryable<ClothesGarment> ApplySort(IQueryable<ClothesGarment> garments, SortRequest sort)
    {
        var ascending = sort.Direction == SortDirection.Ascending;

        IOrderedQueryable<ClothesGarment> ordered = sort.Field switch
        {
            ClothesGarmentQuery.SortFields.Name => ascending
                ? garments.OrderBy(garment => garment.Name)
                : garments.OrderByDescending(garment => garment.Name),
            ClothesGarmentQuery.SortFields.Category => ascending
                ? garments.OrderBy(garment => database.Set<ClothingCategory>()
                    .Where(category => category.Id == garment.CategoryId).Select(category => category.Name).First())
                : garments.OrderByDescending(garment => database.Set<ClothingCategory>()
                    .Where(category => category.Id == garment.CategoryId).Select(category => category.Name).First()),
            ClothesGarmentQuery.SortFields.Status => ascending
                ? garments.OrderBy(garment => garment.Status)
                : garments.OrderByDescending(garment => garment.Status),
            ClothesGarmentQuery.SortFields.Visibility => ascending
                ? garments.OrderBy(garment => garment.Visibility)
                : garments.OrderByDescending(garment => garment.Visibility),
            ClothesGarmentQuery.SortFields.TieBreaker => ascending
                ? garments.OrderBy(garment => garment.Id)
                : garments.OrderByDescending(garment => garment.Id),
            _ => ascending
                ? garments.OrderBy(garment => garment.Name)
                : garments.OrderByDescending(garment => garment.Name),
        };

        return ascending ? ordered.ThenBy(garment => garment.Id) : ordered.ThenByDescending(garment => garment.Id);
    }

    private async Task<Dictionary<int, IReadOnlyList<ClothingColorResponse>>> LoadColorsAsync(
        IReadOnlyCollection<int> garmentIds,
        CancellationToken cancellationToken)
    {
        if (garmentIds.Count == 0)
        {
            return [];
        }

        var rows = await database.Set<ClothesGarmentColor>()
            .AsNoTracking()
            .Where(association => garmentIds.Contains(association.GarmentId))
            .Select(association => new GarmentColorRow(
                association.GarmentId,
                association.ColorId,
                database.Set<ClothingColor>()
                    .Where(color => color.Id == association.ColorId).Select(color => color.Name).First(),
                database.Set<ClothingColor>()
                    .Where(color => color.Id == association.ColorId).Select(color => color.ColorValue).First(),
                database.Set<ClothingColor>()
                    .Where(color => color.Id == association.ColorId).Select(color => color.SortOrder).First()))
            .ToArrayAsync(cancellationToken);

        return rows
            .GroupBy(row => row.GarmentId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ClothingColorResponse>)group
                    .OrderBy(row => row.SortOrder)
                    .ThenBy(row => row.ColorId)
                    .Select(row => new ClothingColorResponse(row.ColorId, row.Name, row.ColorValue, row.SortOrder))
                    .ToArray());
    }

    private static ClothesThumbnailResponse PlaceholderThumbnail() => new(null, null, "placeholder");

    private static string Escape(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);

    private sealed record GarmentSummaryRow(
        int Id,
        string Name,
        int CategoryId,
        string CategoryName,
        ClothesGarmentStatus Status,
        string? Size,
        RecordVisibility Visibility,
        int CreatorId,
        string CreatorName);

    private sealed record GarmentDetailRow(
        int Id,
        string Name,
        int CategoryId,
        string CategoryName,
        ClothesGarmentStatus Status,
        string? Size,
        WashingCare? WashingCare,
        DryingCare? DryingCare,
        IroningCare? IroningCare,
        DryCleaningCare? DryCleaningCare,
        string? Notes,
        RecordVisibility Visibility,
        int CreatedById,
        string CreatedByName,
        DateTimeOffset CreatedAt,
        int UpdatedById,
        string UpdatedByName,
        DateTimeOffset UpdatedAt);

    private sealed record GarmentColorRow(
        int GarmentId,
        int ColorId,
        string Name,
        string ColorValue,
        int SortOrder);
}
