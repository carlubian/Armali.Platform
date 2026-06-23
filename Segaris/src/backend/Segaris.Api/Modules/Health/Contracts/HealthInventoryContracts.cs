using Segaris.Api.Modules.Inventory.Contracts;

namespace Segaris.Api.Modules.Health.Contracts;

/// <summary>
/// Documents the Inventory seam consumed by Health. Inventory owns the item read
/// and deletion-reference contracts; Health consumes the reader and later
/// implements the deletion handler without Inventory referencing Health.
/// </summary>
internal static class HealthInventoryContracts
{
    public static Type ItemReferenceReader => typeof(IInventoryItemReferenceReader);

    public static Type ItemDeletionReferenceHandler => typeof(IInventoryItemDeletionReferenceHandler);
}
