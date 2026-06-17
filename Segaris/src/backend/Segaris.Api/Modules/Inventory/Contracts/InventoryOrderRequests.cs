namespace Segaris.Api.Modules.Inventory.Contracts;

/// <summary>A single order line in a create or update request.</summary>
internal sealed record InventoryOrderLineRequest(
    int ItemId,
    decimal Quantity,
    decimal LineTotal);

/// <summary>Body for creating an Inventory order with its full set of lines.</summary>
internal sealed record CreateInventoryOrderRequest(
    int SupplierId,
    string? Status,
    int CurrencyId,
    DateOnly? OrderDate,
    DateOnly? ExpectedReceiptDate,
    string? Notes,
    string? Visibility,
    IReadOnlyList<InventoryOrderLineRequest> Lines);

/// <summary>Body for fully updating an Inventory order with line replacement.</summary>
internal sealed record UpdateInventoryOrderRequest(
    int SupplierId,
    string? Status,
    int CurrencyId,
    DateOnly? OrderDate,
    DateOnly? ExpectedReceiptDate,
    string? Notes,
    string? Visibility,
    IReadOnlyList<InventoryOrderLineRequest> Lines);
