namespace Segaris.Api.Modules.Inventory.Contracts;

/// <summary>
/// Privacy-neutral impact for deleting an Inventory item that may be referenced by
/// other modules through Inventory-owned deletion contracts. It reports only that
/// references exist and how many, so the deletion flow can confirm that the links
/// will be cleared without disclosing another user's private records.
/// </summary>
internal sealed record InventoryItemDeletionImpactResponse(
    bool IsReferenced,
    int ReferenceCount);
