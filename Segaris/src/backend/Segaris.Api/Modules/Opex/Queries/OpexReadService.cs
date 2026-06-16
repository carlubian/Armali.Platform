using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Opex.Contracts;
using Segaris.Api.Modules.Opex.Domain;
using Segaris.Persistence;
using Segaris.Shared.Api;
using Segaris.Shared.Attachments;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Opex.Queries;

/// <summary>
/// Read-side queries for the Opex category catalog, the paginated contracts list,
/// and contract detail. Every contract query is privacy-correct: it filters to the
/// contracts the supplied user may access before any aggregation, projection,
/// pagination, or detail lookup. Related catalog and audit display names are
/// resolved through correlated sub-queries, and the current-year realized amount is
/// aggregated from each contract's occurrences at the database level so pagination
/// and amount-based sorting never fall back to the client.
/// </summary>
internal sealed class OpexReadService(SegarisDbContext database, IAttachmentService attachments, IClock clock)
{
    private static readonly TimeZoneInfo Household = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");

    public async Task<IReadOnlyList<OpexCategoryResponse>> ListCategoriesAsync(CancellationToken cancellationToken)
    {
        return await database.Set<OpexCategory>()
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Id)
            .Select(category => new OpexCategoryResponse(category.Id, category.Name, category.SortOrder))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<PaginatedResponse<OpexContractSummaryResponse>> ListContractsAsync(
        OpexContractFilter filter,
        PaginationRequest pagination,
        SortRequest sort,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var (yearStart, yearEnd) = CurrentYearBounds();

        var contracts = ApplyFilters(
            database.Set<OpexContract>().AsNoTracking().Where(OpexContractPolicies.AccessibleTo(userId)),
            filter);

        var totalCount = await contracts.CountAsync(cancellationToken);

        var page = await ApplySort(contracts, sort, yearStart, yearEnd)
            .Skip(pagination.Offset)
            .Take(pagination.PageSize)
            .Select(contract => new ContractSummaryRow(
                contract.Id,
                contract.Name,
                contract.MovementType,
                contract.Status,
                contract.CategoryId,
                database.Set<OpexCategory>()
                    .Where(category => category.Id == contract.CategoryId).Select(category => category.Name).First(),
                contract.SupplierId,
                database.Set<SegarisSupplier>()
                    .Where(supplier => supplier.Id == contract.SupplierId).Select(supplier => supplier.Name).FirstOrDefault(),
                contract.CostCenterId,
                database.Set<SegarisCostCenter>()
                    .Where(costCenter => costCenter.Id == contract.CostCenterId).Select(costCenter => costCenter.Name).FirstOrDefault(),
                contract.CurrencyId,
                database.Set<SegarisCurrency>()
                    .Where(currency => currency.Id == contract.CurrencyId).Select(currency => currency.Code).First(),
                contract.ExpectedFrequency,
                contract.EstimatedAnnualAmount,
                contract.Occurrences
                    .Where(occurrence => occurrence.EffectiveDate >= yearStart && occurrence.EffectiveDate <= yearEnd)
                    .Sum(occurrence => (decimal?)occurrence.ActualAmount) ?? 0m,
                contract.Visibility,
                contract.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == contract.CreatedBy).Select(user => user.DisplayName).First()))
            .ToListAsync(cancellationToken);

        var items = page.Select(ToSummary).ToArray();
        return PaginatedResponse<OpexContractSummaryResponse>.Create(items, pagination, totalCount);
    }

    /// <summary>
    /// Returns whether the contract exists and is accessible to the user.
    /// Occurrence and attachment routes resolve their authorization through this
    /// before touching the platform attachment service or nested resources.
    /// </summary>
    public Task<bool> ContractAccessibleAsync(
        int contractId,
        UserId userId,
        CancellationToken cancellationToken) =>
        database.Set<OpexContract>()
            .AsNoTracking()
            .Where(OpexContractPolicies.AccessibleTo(userId))
            .AnyAsync(contract => contract.Id == contractId, cancellationToken);

    /// <summary>
    /// Returns whether an occurrence exists within the given contract. Occurrence
    /// attachment routes call this after <see cref="ContractAccessibleAsync"/> so an
    /// occurrence identifier from another contract is reported as not found rather
    /// than reached through.
    /// </summary>
    public Task<bool> OccurrenceExistsAsync(
        int contractId,
        int occurrenceId,
        CancellationToken cancellationToken) =>
        database.Set<OpexOccurrence>()
            .AsNoTracking()
            .AnyAsync(
                occurrence => occurrence.ContractId == contractId && occurrence.Id == occurrenceId,
                cancellationToken);

    public async Task<OpexContractResponse?> GetContractAsync(
        int contractId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var row = await database.Set<OpexContract>()
            .AsNoTracking()
            .Where(OpexContractPolicies.AccessibleTo(userId))
            .Where(contract => contract.Id == contractId)
            .Select(contract => new ContractDetailRow(
                contract.Id,
                contract.Name,
                contract.MovementType,
                contract.Status,
                contract.StartDate,
                contract.ClosedDate,
                contract.EstimatedAnnualAmount,
                contract.ExpectedFrequency,
                contract.CategoryId,
                database.Set<OpexCategory>()
                    .Where(category => category.Id == contract.CategoryId).Select(category => category.Name).First(),
                contract.SupplierId,
                database.Set<SegarisSupplier>()
                    .Where(supplier => supplier.Id == contract.SupplierId).Select(supplier => supplier.Name).FirstOrDefault(),
                contract.CostCenterId,
                database.Set<SegarisCostCenter>()
                    .Where(costCenter => costCenter.Id == contract.CostCenterId).Select(costCenter => costCenter.Name).FirstOrDefault(),
                contract.CurrencyId,
                database.Set<SegarisCurrency>()
                    .Where(currency => currency.Id == contract.CurrencyId).Select(currency => currency.Code).First(),
                contract.Notes,
                contract.Visibility,
                contract.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == contract.CreatedBy).Select(user => user.DisplayName).First(),
                contract.CreatedAt,
                contract.UpdatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == contract.UpdatedBy).Select(user => user.DisplayName).First(),
                contract.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var descriptors = await attachments.ListByOwnerAsync(OpexAttachments.ContractOwner(contractId), cancellationToken);
        var attachmentResponses = descriptors.Select(ToAttachment).ToArray();

        return new OpexContractResponse(
            row.Id,
            row.Name,
            row.MovementType.ToString(),
            row.Status.ToString(),
            row.StartDate,
            row.ClosedDate,
            row.EstimatedAnnualAmount,
            row.ExpectedFrequency.ToString(),
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
            attachmentResponses,
            row.CreatedById,
            row.CreatedByName,
            row.CreatedAt,
            row.UpdatedById,
            row.UpdatedByName,
            row.UpdatedAt);
    }

    /// <summary>
    /// Lists the occurrences of a contract in their fixed chronological order
    /// (effective date ascending, identifier ascending as the stable tie-breaker).
    /// The query is scoped to the supplied contract; callers resolve parent-contract
    /// accessibility through <see cref="ContractAccessibleAsync"/> beforehand so a
    /// private contract's movements are never exposed.
    /// </summary>
    public async Task<PaginatedResponse<OpexOccurrenceSummaryResponse>> ListOccurrencesAsync(
        int contractId,
        PaginationRequest pagination,
        CancellationToken cancellationToken)
    {
        var occurrences = database.Set<OpexOccurrence>()
            .AsNoTracking()
            .Where(occurrence => occurrence.ContractId == contractId);

        var totalCount = await occurrences.CountAsync(cancellationToken);

        var page = await occurrences
            .OrderBy(occurrence => occurrence.EffectiveDate)
            .ThenBy(occurrence => occurrence.Id)
            .Skip(pagination.Offset)
            .Take(pagination.PageSize)
            .Select(occurrence => new OpexOccurrenceSummaryResponse(
                occurrence.Id,
                occurrence.EffectiveDate,
                occurrence.ActualAmount,
                occurrence.Description))
            .ToArrayAsync(cancellationToken);

        return PaginatedResponse<OpexOccurrenceSummaryResponse>.Create(page, pagination, totalCount);
    }

    /// <summary>
    /// Returns the detail of one occurrence scoped to its parent contract, with its
    /// attachments. An occurrence identifier that belongs to a different contract
    /// returns <c>null</c> so it cannot be used to reach across contracts. Callers
    /// resolve parent-contract accessibility through
    /// <see cref="ContractAccessibleAsync"/> beforehand.
    /// </summary>
    public async Task<OpexOccurrenceResponse?> GetOccurrenceAsync(
        int contractId,
        int occurrenceId,
        CancellationToken cancellationToken)
    {
        var row = await database.Set<OpexOccurrence>()
            .AsNoTracking()
            .Where(occurrence => occurrence.ContractId == contractId && occurrence.Id == occurrenceId)
            .Select(occurrence => new OccurrenceDetailRow(
                occurrence.Id,
                occurrence.ContractId,
                occurrence.EffectiveDate,
                occurrence.ActualAmount,
                occurrence.Description,
                occurrence.Notes,
                occurrence.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == occurrence.CreatedBy).Select(user => user.DisplayName).First(),
                occurrence.CreatedAt,
                occurrence.UpdatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == occurrence.UpdatedBy).Select(user => user.DisplayName).First(),
                occurrence.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var descriptors = await attachments.ListByOwnerAsync(OpexAttachments.OccurrenceOwner(occurrenceId), cancellationToken);
        var attachmentResponses = descriptors.Select(ToAttachment).ToArray();

        return new OpexOccurrenceResponse(
            row.Id,
            row.ContractId,
            row.EffectiveDate,
            row.ActualAmount,
            row.Description,
            row.Notes,
            attachmentResponses,
            row.CreatedById,
            row.CreatedByName,
            row.CreatedAt,
            row.UpdatedById,
            row.UpdatedByName,
            row.UpdatedAt);
    }

    private (DateOnly Start, DateOnly End) CurrentYearBounds()
    {
        // The realized amount covers the natural year in the household time zone.
        // Occurrence effective dates are civil dates, so the boundary is evaluated
        // in Europe/Madrid rather than by converting through UTC.
        var today = TimeZoneInfo.ConvertTime(clock.UtcNow, Household);
        var year = today.Year;
        return (new DateOnly(year, 1, 1), new DateOnly(year, 12, 31));
    }

    private static IQueryable<OpexContract> ApplyFilters(IQueryable<OpexContract> contracts, OpexContractFilter filter)
    {
        if (filter.Search is { } search)
        {
            var pattern = $"%{Escape(search.ToLowerInvariant())}%";
            contracts = contracts.Where(contract =>
                EF.Functions.Like(contract.Name.ToLower(), pattern, "\\")
                || (contract.Notes != null && EF.Functions.Like(contract.Notes.ToLower(), pattern, "\\")));
        }

        if (filter.MovementType is { } movementType)
        {
            contracts = contracts.Where(contract => contract.MovementType == movementType);
        }

        if (filter.Status is { } status)
        {
            contracts = contracts.Where(contract => contract.Status == status);
        }

        if (filter.CategoryId is { } categoryId)
        {
            contracts = contracts.Where(contract => contract.CategoryId == categoryId);
        }

        if (filter.SupplierId is { } supplierId)
        {
            contracts = contracts.Where(contract => contract.SupplierId == supplierId);
        }

        if (filter.CostCenterId is { } costCenterId)
        {
            contracts = contracts.Where(contract => contract.CostCenterId == costCenterId);
        }

        if (filter.CurrencyId is { } currencyId)
        {
            contracts = contracts.Where(contract => contract.CurrencyId == currencyId);
        }

        if (filter.Frequency is { } frequency)
        {
            contracts = contracts.Where(contract => contract.ExpectedFrequency == frequency);
        }

        if (filter.Visibility is { } visibility)
        {
            contracts = contracts.Where(contract => contract.Visibility == visibility);
        }

        if (filter.CreatorId is { } creatorId)
        {
            contracts = contracts.Where(contract => contract.CreatedBy == creatorId);
        }

        return contracts;
    }

    private IQueryable<OpexContract> ApplySort(
        IQueryable<OpexContract> contracts,
        SortRequest sort,
        DateOnly yearStart,
        DateOnly yearEnd)
    {
        var ascending = sort.Direction == SortDirection.Ascending;

        IOrderedQueryable<OpexContract> ordered = sort.Field switch
        {
            OpexContractQuery.SortFields.Name => ascending
                ? contracts.OrderBy(contract => contract.Name)
                : contracts.OrderByDescending(contract => contract.Name),
            OpexContractQuery.SortFields.Type => ascending
                ? contracts.OrderBy(contract => contract.MovementType)
                : contracts.OrderByDescending(contract => contract.MovementType),
            OpexContractQuery.SortFields.Status => ascending
                ? contracts.OrderBy(contract => contract.Status)
                : contracts.OrderByDescending(contract => contract.Status),
            OpexContractQuery.SortFields.Category => ascending
                ? contracts.OrderBy(contract => database.Set<OpexCategory>()
                    .Where(category => category.Id == contract.CategoryId).Select(category => category.Name).First())
                : contracts.OrderByDescending(contract => database.Set<OpexCategory>()
                    .Where(category => category.Id == contract.CategoryId).Select(category => category.Name).First()),
            // Optional supplier sorts its nulls last in either direction so the
            // ordering is deterministic across providers.
            OpexContractQuery.SortFields.Supplier => ascending
                ? contracts.OrderBy(contract => contract.SupplierId == null).ThenBy(contract => database.Set<SegarisSupplier>()
                    .Where(supplier => supplier.Id == contract.SupplierId).Select(supplier => supplier.Name).FirstOrDefault())
                : contracts.OrderBy(contract => contract.SupplierId == null).ThenByDescending(contract => database.Set<SegarisSupplier>()
                    .Where(supplier => supplier.Id == contract.SupplierId).Select(supplier => supplier.Name).FirstOrDefault()),
            OpexContractQuery.SortFields.Frequency => ascending
                ? contracts.OrderBy(contract => contract.ExpectedFrequency)
                : contracts.OrderByDescending(contract => contract.ExpectedFrequency),
            // The optional annual estimate sorts its nulls last in either direction.
            OpexContractQuery.SortFields.EstimatedAnnualAmount => ascending
                ? contracts.OrderBy(contract => contract.EstimatedAnnualAmount == null).ThenBy(contract => contract.EstimatedAnnualAmount)
                : contracts.OrderBy(contract => contract.EstimatedAnnualAmount == null).ThenByDescending(contract => contract.EstimatedAnnualAmount),
            OpexContractQuery.SortFields.RealizedCurrentYearAmount => ascending
                ? contracts.OrderBy(contract => contract.Occurrences
                    .Where(occurrence => occurrence.EffectiveDate >= yearStart && occurrence.EffectiveDate <= yearEnd)
                    .Sum(occurrence => (decimal?)occurrence.ActualAmount) ?? 0m)
                : contracts.OrderByDescending(contract => contract.Occurrences
                    .Where(occurrence => occurrence.EffectiveDate >= yearStart && occurrence.EffectiveDate <= yearEnd)
                    .Sum(occurrence => (decimal?)occurrence.ActualAmount) ?? 0m),
            OpexContractQuery.SortFields.Currency => ascending
                ? contracts.OrderBy(contract => database.Set<SegarisCurrency>()
                    .Where(currency => currency.Id == contract.CurrencyId).Select(currency => currency.Code).First())
                : contracts.OrderByDescending(contract => database.Set<SegarisCurrency>()
                    .Where(currency => currency.Id == contract.CurrencyId).Select(currency => currency.Code).First()),
            OpexContractQuery.SortFields.TieBreaker => ascending
                ? contracts.OrderBy(contract => contract.Id)
                : contracts.OrderByDescending(contract => contract.Id),
            _ => ascending
                ? contracts.OrderBy(contract => contract.Name)
                : contracts.OrderByDescending(contract => contract.Name),
        };

        // Every ordering ends with the contract identifier descending as the stable
        // tie-breaker required by the contract.
        return ordered.ThenByDescending(contract => contract.Id);
    }

    private static OpexContractSummaryResponse ToSummary(ContractSummaryRow row) => new(
        row.Id,
        row.Name,
        row.MovementType.ToString(),
        row.Status.ToString(),
        row.CategoryId,
        row.CategoryName,
        row.SupplierId,
        row.SupplierName,
        row.CostCenterId,
        row.CostCenterName,
        row.CurrencyId,
        row.CurrencyCode,
        row.ExpectedFrequency.ToString(),
        row.EstimatedAnnualAmount,
        row.RealizedCurrentYearAmount,
        row.Visibility.ToString(),
        row.CreatorId,
        row.CreatorName);

    private static OpexAttachmentResponse ToAttachment(AttachmentDescriptor descriptor) => new(
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

    private sealed record ContractSummaryRow(
        int Id,
        string Name,
        OpexMovementType MovementType,
        OpexContractStatus Status,
        int CategoryId,
        string CategoryName,
        int? SupplierId,
        string? SupplierName,
        int? CostCenterId,
        string? CostCenterName,
        int CurrencyId,
        string CurrencyCode,
        OpexExpectedFrequency ExpectedFrequency,
        decimal? EstimatedAnnualAmount,
        decimal RealizedCurrentYearAmount,
        RecordVisibility Visibility,
        int CreatorId,
        string CreatorName);

    private sealed record ContractDetailRow(
        int Id,
        string Name,
        OpexMovementType MovementType,
        OpexContractStatus Status,
        DateOnly? StartDate,
        DateOnly? ClosedDate,
        decimal? EstimatedAnnualAmount,
        OpexExpectedFrequency ExpectedFrequency,
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
        int CreatedById,
        string CreatedByName,
        DateTimeOffset CreatedAt,
        int UpdatedById,
        string UpdatedByName,
        DateTimeOffset UpdatedAt);

    private sealed record OccurrenceDetailRow(
        int Id,
        int ContractId,
        DateOnly EffectiveDate,
        decimal ActualAmount,
        string? Description,
        string? Notes,
        int CreatedById,
        string CreatedByName,
        DateTimeOffset CreatedAt,
        int UpdatedById,
        string UpdatedByName,
        DateTimeOffset UpdatedAt);
}
