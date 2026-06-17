namespace Segaris.Api.Modules.Inventory.Contracts;

internal sealed record InventoryCategoryRequest(string? Name);

internal sealed record InventoryCategoryResponse(int Id, string Name, int SortOrder);

internal sealed record InventoryLocationRequest(string? Name);

internal sealed record InventoryLocationResponse(int Id, string Name, int SortOrder);
