namespace Segaris.Api.Modules.Inventory.Contracts;

/// <summary>Body for creating an Inventory item.</summary>
internal sealed record CreateInventoryItemRequest(
    string? Name,
    string? Status,
    string? Notes,
    int CategoryId,
    int LocationId,
    decimal CurrentStock,
    decimal MinimumStock,
    IReadOnlyList<int> SupplierIds,
    string? Visibility);

/// <summary>Body for fully updating an Inventory item.</summary>
internal sealed record UpdateInventoryItemRequest(
    string? Name,
    string? Status,
    string? Notes,
    int CategoryId,
    int LocationId,
    decimal CurrentStock,
    decimal MinimumStock,
    IReadOnlyList<int> SupplierIds,
    string? Visibility);

/// <summary>Body for a quick stock adjustment on an item.</summary>
internal sealed record InventoryStockAdjustmentRequest(
    string? Direction,
    decimal Quantity);
