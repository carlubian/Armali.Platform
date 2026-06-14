namespace Segaris.Api.Modules.Capex.Contracts;

/// <summary>
/// Frozen row contract for the paginated Entries list returned as
/// <c>PaginatedResponse&lt;CapexEntrySummaryResponse&gt;</c>. Amounts and the
/// currency are kept separate; the client never aggregates across currencies.
/// </summary>
internal sealed record CapexEntrySummaryResponse(
    int Id,
    string Title,
    string MovementType,
    string Status,
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
    string Visibility,
    int CreatorId,
    string CreatorName);

/// <summary>
/// Frozen item line contract returned in entry detail, in persisted order.
/// </summary>
internal sealed record CapexEntryItemResponse(
    int Id,
    int Position,
    string Description,
    decimal Quantity,
    decimal UnitAmount,
    decimal LineAmount);

/// <summary>
/// Frozen attachment descriptor contract for an entry's attachments.
/// </summary>
internal sealed record CapexAttachmentResponse(
    string Id,
    string FileName,
    string ContentType,
    long Size,
    int CreatedById,
    DateTimeOffset CreatedAt);

/// <summary>
/// Frozen detail contract for <c>GET /api/capex/entries/{entryId}</c> and the
/// body returned by create/update. Carries the ordered items, attachment
/// descriptors, and audit display data.
/// </summary>
internal sealed record CapexEntryResponse(
    int Id,
    string Title,
    string MovementType,
    string Status,
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
    string Visibility,
    decimal TotalAmount,
    IReadOnlyList<CapexEntryItemResponse> Items,
    IReadOnlyList<CapexAttachmentResponse> Attachments,
    int CreatedById,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    int? UpdatedById,
    string? UpdatedByName,
    DateTimeOffset UpdatedAt);
