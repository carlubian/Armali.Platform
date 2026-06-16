namespace Segaris.Api.Modules.Inventory.Contracts;

internal sealed record InventoryAttachmentResponse(
    string Id,
    string FileName,
    string ContentType,
    long Size,
    int CreatedById,
    DateTimeOffset CreatedAt);

internal sealed record InventoryItemSupplierResponse(int SupplierId, string SupplierName);

internal sealed record InventoryItemSummaryResponse(
    int Id,
    string Name,
    string Status,
    int CategoryId,
    string CategoryName,
    int LocationId,
    string LocationName,
    decimal CurrentStock,
    decimal MinimumStock,
    string Visibility,
    int CreatorId,
    string CreatorName);

internal sealed record InventoryItemResponse(
    int Id,
    string Name,
    string Status,
    string? Notes,
    int CategoryId,
    string CategoryName,
    int LocationId,
    string LocationName,
    decimal CurrentStock,
    decimal MinimumStock,
    string Visibility,
    IReadOnlyList<InventoryItemSupplierResponse> Suppliers,
    IReadOnlyList<InventoryAttachmentResponse> Attachments,
    int CreatedById,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    int? UpdatedById,
    string? UpdatedByName,
    DateTimeOffset UpdatedAt);

internal sealed record InventoryOrderSummaryResponse(
    int Id,
    int SupplierId,
    string SupplierName,
    string Status,
    DateOnly? OrderDate,
    DateOnly? ExpectedReceiptDate,
    int CurrencyId,
    string CurrencyCode,
    string Visibility,
    int CreatorId,
    string CreatorName);

internal sealed record InventoryOrderLineResponse(
    int Id,
    int ItemId,
    string ItemName,
    string ItemStatus,
    decimal Quantity,
    decimal LineTotal);

internal sealed record InventoryOrderResponse(
    int Id,
    int SupplierId,
    string SupplierName,
    string Status,
    DateOnly? OrderDate,
    DateOnly? ExpectedReceiptDate,
    int CurrencyId,
    string CurrencyCode,
    string? Notes,
    string Visibility,
    IReadOnlyList<InventoryOrderLineResponse> Lines,
    IReadOnlyList<InventoryAttachmentResponse> Attachments,
    int CreatedById,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    int? UpdatedById,
    string? UpdatedByName,
    DateTimeOffset UpdatedAt);
