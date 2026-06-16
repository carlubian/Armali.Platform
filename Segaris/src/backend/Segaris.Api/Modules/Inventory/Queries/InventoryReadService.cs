using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Persistence;
using Segaris.Shared.Api;
using Segaris.Shared.Attachments;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Inventory.Queries;

/// <summary>
/// Read-side queries for Inventory. Wave 1 exposed the module-owned category and
/// location catalogs in their deterministic order; Wave 2 adds the paginated,
/// filtered, and sorted item list and the item detail read. Every item query is
/// privacy-correct: it filters to the items the supplied user may access before any
/// projection, pagination, or detail lookup. Related catalog and audit display
/// names are resolved through correlated sub-queries.
/// </summary>
internal sealed class InventoryReadService(SegarisDbContext database, IAttachmentService attachments)
{
    public async Task<IReadOnlyList<InventoryCategoryResponse>> ListCategoriesAsync(CancellationToken cancellationToken)
    {
        return await database.Set<InventoryCategory>()
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Id)
            .Select(category => new InventoryCategoryResponse(category.Id, category.Name, category.SortOrder))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryLocationResponse>> ListLocationsAsync(CancellationToken cancellationToken)
    {
        return await database.Set<InventoryLocation>()
            .AsNoTracking()
            .OrderBy(location => location.SortOrder)
            .ThenBy(location => location.Id)
            .Select(location => new InventoryLocationResponse(location.Id, location.Name, location.SortOrder))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<PaginatedResponse<InventoryItemSummaryResponse>> ListItemsAsync(
        InventoryItemFilter filter,
        PaginationRequest pagination,
        SortRequest sort,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var items = ApplyFilters(
            database.Set<InventoryItem>().AsNoTracking().Where(InventoryItemPolicies.AccessibleTo(userId)),
            filter);

        var totalCount = await items.CountAsync(cancellationToken);

        var page = await ApplySort(items, sort)
            .Skip(pagination.Offset)
            .Take(pagination.PageSize)
            .Select(item => new InventoryItemSummaryResponse(
                item.Id,
                item.Name,
                item.Status.ToString(),
                item.CategoryId,
                database.Set<InventoryCategory>()
                    .Where(category => category.Id == item.CategoryId).Select(category => category.Name).First(),
                item.LocationId,
                database.Set<InventoryLocation>()
                    .Where(location => location.Id == item.LocationId).Select(location => location.Name).First(),
                item.CurrentStock,
                item.MinimumStock,
                item.Visibility.ToString(),
                item.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == item.CreatedBy).Select(user => user.DisplayName).First()))
            .ToArrayAsync(cancellationToken);

        return PaginatedResponse<InventoryItemSummaryResponse>.Create(page, pagination, totalCount);
    }

    /// <summary>
    /// Returns whether the item exists and is accessible to the user. Mutation and
    /// attachment routes resolve their authorization through this before touching
    /// nested resources.
    /// </summary>
    public Task<bool> ItemAccessibleAsync(
        int itemId,
        UserId userId,
        CancellationToken cancellationToken) =>
        database.Set<InventoryItem>()
            .AsNoTracking()
            .Where(InventoryItemPolicies.AccessibleTo(userId))
            .AnyAsync(item => item.Id == itemId, cancellationToken);

    public async Task<InventoryItemResponse?> GetItemAsync(
        int itemId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var row = await database.Set<InventoryItem>()
            .AsNoTracking()
            .Where(InventoryItemPolicies.AccessibleTo(userId))
            .Where(item => item.Id == itemId)
            .Select(item => new ItemDetailRow(
                item.Id,
                item.Name,
                item.Status,
                item.Notes,
                item.CategoryId,
                database.Set<InventoryCategory>()
                    .Where(category => category.Id == item.CategoryId).Select(category => category.Name).First(),
                item.LocationId,
                database.Set<InventoryLocation>()
                    .Where(location => location.Id == item.LocationId).Select(location => location.Name).First(),
                item.CurrentStock,
                item.MinimumStock,
                item.Visibility,
                item.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == item.CreatedBy).Select(user => user.DisplayName).First(),
                item.CreatedAt,
                item.UpdatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == item.UpdatedBy).Select(user => user.DisplayName).First(),
                item.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        // Allowed suppliers are resolved in a separate flat query and ordered by
        // display name for a deterministic projection.
        var suppliers = await database.Set<InventoryItemSupplier>()
            .AsNoTracking()
            .Where(association => association.ItemId == itemId)
            .Select(association => new InventoryItemSupplierResponse(
                association.SupplierId,
                database.Set<SegarisSupplier>()
                    .Where(supplier => supplier.Id == association.SupplierId).Select(supplier => supplier.Name).First()))
            .ToArrayAsync(cancellationToken);
        var orderedSuppliers = suppliers
            .OrderBy(supplier => supplier.SupplierName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var descriptors = await attachments.ListByOwnerAsync(InventoryAttachments.ItemOwner(itemId), cancellationToken);
        var attachmentResponses = descriptors.Select(ToAttachment).ToArray();

        return new InventoryItemResponse(
            row.Id,
            row.Name,
            row.Status.ToString(),
            row.Notes,
            row.CategoryId,
            row.CategoryName,
            row.LocationId,
            row.LocationName,
            row.CurrentStock,
            row.MinimumStock,
            row.Visibility.ToString(),
            orderedSuppliers,
            attachmentResponses,
            row.CreatedById,
            row.CreatedByName,
            row.CreatedAt,
            row.UpdatedById,
            row.UpdatedByName,
            row.UpdatedAt);
    }

    private static IQueryable<InventoryItem> ApplyFilters(IQueryable<InventoryItem> items, InventoryItemFilter filter)
    {
        if (filter.Search is { } search)
        {
            var pattern = $"%{Escape(search.ToLowerInvariant())}%";
            items = items.Where(item =>
                EF.Functions.Like(item.Name.ToLower(), pattern, "\\")
                || (item.Notes != null && EF.Functions.Like(item.Notes.ToLower(), pattern, "\\")));
        }

        if (filter.Status is { } status)
        {
            items = items.Where(item => item.Status == status);
        }

        if (filter.CategoryId is { } categoryId)
        {
            items = items.Where(item => item.CategoryId == categoryId);
        }

        if (filter.LocationId is { } locationId)
        {
            items = items.Where(item => item.LocationId == locationId);
        }

        if (filter.SupplierId is { } supplierId)
        {
            items = items.Where(item => item.Suppliers.Any(association => association.SupplierId == supplierId));
        }

        if (filter.Visibility is { } visibility)
        {
            items = items.Where(item => item.Visibility == visibility);
        }

        if (filter.CreatorId is { } creatorId)
        {
            items = items.Where(item => item.CreatedBy == creatorId);
        }

        return items;
    }

    private IQueryable<InventoryItem> ApplySort(IQueryable<InventoryItem> items, SortRequest sort)
    {
        var ascending = sort.Direction == SortDirection.Ascending;

        IOrderedQueryable<InventoryItem> ordered = sort.Field switch
        {
            InventoryItemQuery.SortFields.Name => ascending
                ? items.OrderBy(item => item.Name)
                : items.OrderByDescending(item => item.Name),
            InventoryItemQuery.SortFields.Status => ascending
                ? items.OrderBy(item => item.Status)
                : items.OrderByDescending(item => item.Status),
            InventoryItemQuery.SortFields.Category => ascending
                ? items.OrderBy(item => database.Set<InventoryCategory>()
                    .Where(category => category.Id == item.CategoryId).Select(category => category.Name).First())
                : items.OrderByDescending(item => database.Set<InventoryCategory>()
                    .Where(category => category.Id == item.CategoryId).Select(category => category.Name).First()),
            InventoryItemQuery.SortFields.Location => ascending
                ? items.OrderBy(item => database.Set<InventoryLocation>()
                    .Where(location => location.Id == item.LocationId).Select(location => location.Name).First())
                : items.OrderByDescending(item => database.Set<InventoryLocation>()
                    .Where(location => location.Id == item.LocationId).Select(location => location.Name).First()),
            InventoryItemQuery.SortFields.CurrentStock => ascending
                ? items.OrderBy(item => item.CurrentStock)
                : items.OrderByDescending(item => item.CurrentStock),
            InventoryItemQuery.SortFields.MinimumStock => ascending
                ? items.OrderBy(item => item.MinimumStock)
                : items.OrderByDescending(item => item.MinimumStock),
            InventoryItemQuery.SortFields.Visibility => ascending
                ? items.OrderBy(item => item.Visibility)
                : items.OrderByDescending(item => item.Visibility),
            InventoryItemQuery.SortFields.TieBreaker => ascending
                ? items.OrderBy(item => item.Id)
                : items.OrderByDescending(item => item.Id),
            _ => ascending
                ? items.OrderBy(item => item.Name)
                : items.OrderByDescending(item => item.Name),
        };

        // Every ordering ends with the item identifier ascending as the stable
        // tie-breaker; this also realizes the documented default ordering of name
        // ascending then identifier ascending.
        return ordered.ThenBy(item => item.Id);
    }

    private static InventoryAttachmentResponse ToAttachment(AttachmentDescriptor descriptor) => new(
        descriptor.Id.Value.ToString(CultureInfo.InvariantCulture),
        descriptor.FileName,
        descriptor.ContentType,
        descriptor.Size,
        descriptor.CreatedBy.Value,
        descriptor.CreatedAt);

    private static string Escape(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);

    private sealed record ItemDetailRow(
        int Id,
        string Name,
        InventoryItemStatus Status,
        string? Notes,
        int CategoryId,
        string CategoryName,
        int LocationId,
        string LocationName,
        decimal CurrentStock,
        decimal MinimumStock,
        RecordVisibility Visibility,
        int CreatedById,
        string CreatedByName,
        DateTimeOffset CreatedAt,
        int UpdatedById,
        string UpdatedByName,
        DateTimeOffset UpdatedAt);
}
