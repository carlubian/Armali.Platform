using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Capex.Contracts;
using Segaris.Api.Modules.Capex.Domain;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Identity;
using Segaris.Persistence;
using Segaris.Shared.Api;
using Segaris.Shared.Attachments;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Capex.Queries;

/// <summary>
/// Read-side queries for the Capex categories, the paginated Entries list, and
/// entry detail. Every query is privacy-correct: it filters to the entries the
/// supplied user may access before any projection, pagination, or detail lookup.
/// Related catalog and audit display names are resolved through correlated
/// sub-queries so pagination and name-based sorting happen at the database level.
/// </summary>
internal sealed class CapexReadService(SegarisDbContext database, IAttachmentService attachments)
{
    public async Task<IReadOnlyList<CapexCategoryResponse>> ListCategoriesAsync(
        CancellationToken cancellationToken) =>
        await database.Set<CapexCategory>()
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ThenBy(category => category.Id)
            .Select(category => new CapexCategoryResponse(category.Id, category.Code, category.Name))
            .ToArrayAsync(cancellationToken);

    public async Task<PaginatedResponse<CapexEntrySummaryResponse>> ListEntriesAsync(
        CapexEntryFilter filter,
        PaginationRequest pagination,
        SortRequest sort,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var entries = ApplyFilters(
            database.Set<CapexEntry>().AsNoTracking().Where(CapexEntryPolicies.AccessibleTo(userId)),
            filter);

        var totalCount = await entries.CountAsync(cancellationToken);

        var page = await ApplySort(entries, sort)
            .Skip(pagination.Offset)
            .Take(pagination.PageSize)
            .Select(entry => new EntrySummaryRow(
                entry.Id,
                entry.Title,
                entry.MovementType,
                entry.Status,
                entry.DueDate,
                entry.CategoryId,
                database.Set<CapexCategory>()
                    .Where(category => category.Id == entry.CategoryId).Select(category => category.Name).First(),
                entry.SupplierId,
                database.Set<SegarisSupplier>()
                    .Where(supplier => supplier.Id == entry.SupplierId).Select(supplier => supplier.Name).FirstOrDefault(),
                entry.CostCenterId,
                database.Set<SegarisCostCenter>()
                    .Where(costCenter => costCenter.Id == entry.CostCenterId).Select(costCenter => costCenter.Name).FirstOrDefault(),
                entry.CurrencyId,
                database.Set<SegarisCurrency>()
                    .Where(currency => currency.Id == entry.CurrencyId).Select(currency => currency.Code).First(),
                entry.TotalAmount,
                entry.Visibility,
                entry.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == entry.CreatedBy).Select(user => user.DisplayName).First()))
            .ToListAsync(cancellationToken);

        var items = page.Select(ToSummary).ToArray();
        return PaginatedResponse<CapexEntrySummaryResponse>.Create(items, pagination, totalCount);
    }

    /// <summary>
    /// Returns whether the entry exists and is accessible to the user. Attachment
    /// operations inherit the entry's visibility, so every attachment route checks
    /// this before touching the platform attachment service.
    /// </summary>
    public Task<bool> EntryAccessibleAsync(
        int entryId,
        UserId userId,
        CancellationToken cancellationToken) =>
        database.Set<CapexEntry>()
            .AsNoTracking()
            .Where(CapexEntryPolicies.AccessibleTo(userId))
            .AnyAsync(entry => entry.Id == entryId, cancellationToken);

    public async Task<CapexEntryResponse?> GetEntryAsync(
        int entryId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var row = await database.Set<CapexEntry>()
            .AsNoTracking()
            .Where(CapexEntryPolicies.AccessibleTo(userId))
            .Where(entry => entry.Id == entryId)
            .Select(entry => new EntryDetailRow(
                entry.Id,
                entry.Title,
                entry.MovementType,
                entry.Status,
                entry.DueDate,
                entry.CategoryId,
                database.Set<CapexCategory>()
                    .Where(category => category.Id == entry.CategoryId).Select(category => category.Name).First(),
                entry.SupplierId,
                database.Set<SegarisSupplier>()
                    .Where(supplier => supplier.Id == entry.SupplierId).Select(supplier => supplier.Name).FirstOrDefault(),
                entry.CostCenterId,
                database.Set<SegarisCostCenter>()
                    .Where(costCenter => costCenter.Id == entry.CostCenterId).Select(costCenter => costCenter.Name).FirstOrDefault(),
                entry.CurrencyId,
                database.Set<SegarisCurrency>()
                    .Where(currency => currency.Id == entry.CurrencyId).Select(currency => currency.Code).First(),
                entry.Notes,
                entry.Visibility,
                entry.TotalAmount,
                entry.Items
                    .OrderBy(item => item.Position)
                    .Select(item => new CapexEntryItemResponse(
                        item.Id, item.Position, item.Description, item.Quantity, item.UnitAmount, item.LineAmount))
                    .ToList(),
                entry.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == entry.CreatedBy).Select(user => user.DisplayName).First(),
                entry.CreatedAt,
                entry.UpdatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == entry.UpdatedBy).Select(user => user.DisplayName).First(),
                entry.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var descriptors = await attachments.ListByOwnerAsync(CapexAttachments.Owner(entryId), cancellationToken);
        var attachmentResponses = descriptors.Select(ToAttachment).ToArray();

        return new CapexEntryResponse(
            row.Id,
            row.Title,
            row.MovementType.ToString(),
            row.Status.ToString(),
            row.DueDate,
            row.CategoryId,
            row.CategoryName,
            row.SupplierId,
            row.SupplierName,
            row.CostCenterId,
            row.CostCenterName,
            row.CurrencyId,
            row.CurrencyCode,
            row.Notes,
            row.Visibility.ToString(),
            row.TotalAmount,
            row.Items,
            attachmentResponses,
            row.CreatedById,
            row.CreatedByName,
            row.CreatedAt,
            row.UpdatedById,
            row.UpdatedByName,
            row.UpdatedAt);
    }

    private static IQueryable<CapexEntry> ApplyFilters(IQueryable<CapexEntry> entries, CapexEntryFilter filter)
    {
        if (filter.Search is { } search)
        {
            var pattern = $"%{Escape(search.ToLowerInvariant())}%";
            entries = entries.Where(entry =>
                EF.Functions.Like(entry.Title.ToLower(), pattern, "\\")
                || (entry.Notes != null && EF.Functions.Like(entry.Notes.ToLower(), pattern, "\\"))
                || entry.Items.Any(item => EF.Functions.Like(item.Description.ToLower(), pattern, "\\")));
        }

        if (filter.From is { } from)
        {
            entries = entries.Where(entry => entry.DueDate >= from);
        }

        if (filter.To is { } to)
        {
            entries = entries.Where(entry => entry.DueDate <= to);
        }

        if (filter.MovementType is { } movementType)
        {
            entries = entries.Where(entry => entry.MovementType == movementType);
        }

        if (filter.Status is { } status)
        {
            entries = entries.Where(entry => entry.Status == status);
        }

        if (filter.CategoryId is { } categoryId)
        {
            entries = entries.Where(entry => entry.CategoryId == categoryId);
        }

        if (filter.SupplierId is { } supplierId)
        {
            entries = entries.Where(entry => entry.SupplierId == supplierId);
        }

        if (filter.CostCenterId is { } costCenterId)
        {
            entries = entries.Where(entry => entry.CostCenterId == costCenterId);
        }

        if (filter.CurrencyId is { } currencyId)
        {
            entries = entries.Where(entry => entry.CurrencyId == currencyId);
        }

        if (filter.Visibility is { } visibility)
        {
            entries = entries.Where(entry => entry.Visibility == visibility);
        }

        if (filter.CreatorId is { } creatorId)
        {
            entries = entries.Where(entry => entry.CreatedBy == creatorId);
        }

        return entries;
    }

    private IQueryable<CapexEntry> ApplySort(IQueryable<CapexEntry> entries, SortRequest sort)
    {
        var ascending = sort.Direction == SortDirection.Ascending;

        IOrderedQueryable<CapexEntry> ordered = sort.Field switch
        {
            CapexEntryQuery.SortFields.Title => ascending
                ? entries.OrderBy(entry => entry.Title)
                : entries.OrderByDescending(entry => entry.Title),
            CapexEntryQuery.SortFields.Type => ascending
                ? entries.OrderBy(entry => entry.MovementType)
                : entries.OrderByDescending(entry => entry.MovementType),
            CapexEntryQuery.SortFields.Status => ascending
                ? entries.OrderBy(entry => entry.Status)
                : entries.OrderByDescending(entry => entry.Status),
            CapexEntryQuery.SortFields.Category => ascending
                ? entries.OrderBy(entry => database.Set<CapexCategory>()
                    .Where(category => category.Id == entry.CategoryId).Select(category => category.Name).First())
                : entries.OrderByDescending(entry => database.Set<CapexCategory>()
                    .Where(category => category.Id == entry.CategoryId).Select(category => category.Name).First()),
            // Optional supplier and cost center sort their nulls last in either
            // direction so the ordering is deterministic across providers.
            CapexEntryQuery.SortFields.Supplier => ascending
                ? entries.OrderBy(entry => entry.SupplierId == null).ThenBy(entry => database.Set<SegarisSupplier>()
                    .Where(supplier => supplier.Id == entry.SupplierId).Select(supplier => supplier.Name).FirstOrDefault())
                : entries.OrderBy(entry => entry.SupplierId == null).ThenByDescending(entry => database.Set<SegarisSupplier>()
                    .Where(supplier => supplier.Id == entry.SupplierId).Select(supplier => supplier.Name).FirstOrDefault()),
            CapexEntryQuery.SortFields.CostCenter => ascending
                ? entries.OrderBy(entry => entry.CostCenterId == null).ThenBy(entry => database.Set<SegarisCostCenter>()
                    .Where(costCenter => costCenter.Id == entry.CostCenterId).Select(costCenter => costCenter.Name).FirstOrDefault())
                : entries.OrderBy(entry => entry.CostCenterId == null).ThenByDescending(entry => database.Set<SegarisCostCenter>()
                    .Where(costCenter => costCenter.Id == entry.CostCenterId).Select(costCenter => costCenter.Name).FirstOrDefault()),
            CapexEntryQuery.SortFields.Total => ascending
                ? entries.OrderBy(entry => entry.TotalAmount)
                : entries.OrderByDescending(entry => entry.TotalAmount),
            CapexEntryQuery.SortFields.Currency => ascending
                ? entries.OrderBy(entry => database.Set<SegarisCurrency>()
                    .Where(currency => currency.Id == entry.CurrencyId).Select(currency => currency.Code).First())
                : entries.OrderByDescending(entry => database.Set<SegarisCurrency>()
                    .Where(currency => currency.Id == entry.CurrencyId).Select(currency => currency.Code).First()),
            CapexEntryQuery.SortFields.TieBreaker => ascending
                ? entries.OrderBy(entry => entry.Id)
                : entries.OrderByDescending(entry => entry.Id),
            _ => ascending
                ? entries.OrderBy(entry => entry.DueDate)
                : entries.OrderByDescending(entry => entry.DueDate),
        };

        // Every ordering ends with the entry identifier descending as the stable
        // tie-breaker required by the contract.
        return ordered.ThenByDescending(entry => entry.Id);
    }

    private static CapexEntrySummaryResponse ToSummary(EntrySummaryRow row) => new(
        row.Id,
        row.Title,
        row.MovementType.ToString(),
        row.Status.ToString(),
        row.DueDate,
        row.CategoryId,
        row.CategoryName,
        row.SupplierId,
        row.SupplierName,
        row.CostCenterId,
        row.CostCenterName,
        row.CurrencyId,
        row.CurrencyCode,
        row.TotalAmount,
        row.Visibility.ToString(),
        row.CreatorId,
        row.CreatorName);

    private static CapexAttachmentResponse ToAttachment(AttachmentDescriptor descriptor) => new(
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

    private sealed record EntrySummaryRow(
        int Id,
        string Title,
        CapexMovementType MovementType,
        CapexEntryStatus Status,
        DateOnly DueDate,
        int CategoryId,
        string CategoryName,
        int? SupplierId,
        string? SupplierName,
        int? CostCenterId,
        string? CostCenterName,
        int CurrencyId,
        string CurrencyCode,
        decimal TotalAmount,
        RecordVisibility Visibility,
        int CreatorId,
        string CreatorName);

    private sealed record EntryDetailRow(
        int Id,
        string Title,
        CapexMovementType MovementType,
        CapexEntryStatus Status,
        DateOnly DueDate,
        int CategoryId,
        string CategoryName,
        int? SupplierId,
        string? SupplierName,
        int? CostCenterId,
        string? CostCenterName,
        int CurrencyId,
        string CurrencyCode,
        string? Notes,
        RecordVisibility Visibility,
        decimal TotalAmount,
        IReadOnlyList<CapexEntryItemResponse> Items,
        int CreatedById,
        string CreatedByName,
        DateTimeOffset CreatedAt,
        int UpdatedById,
        string UpdatedByName,
        DateTimeOffset UpdatedAt);
}
