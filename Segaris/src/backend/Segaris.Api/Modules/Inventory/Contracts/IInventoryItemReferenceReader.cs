using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Inventory.Contracts;

/// <summary>
/// The narrow, privacy-respecting projection of an Inventory item published for
/// cross-module references. It carries only the stable identifier, the display
/// name, and the visibility needed to enforce a consumer's visibility rule. It is
/// never an EF Core entity and exposes no other item detail.
/// </summary>
internal sealed record InventoryItemReference(int ItemId, string Name, RecordVisibility Visibility);

/// <summary>
/// Narrow read contract published by Inventory and consumed by modules that hold a
/// live reference to an item (initially Recipes, whose ingredients may link to an
/// item). It validates that a referenced item exists and is accessible to a viewer,
/// resolves its display name, and exposes its visibility so the consumer can apply
/// its own visibility rule. Inventory owns and enforces accessibility and visibility
/// here; a consumer may not derive access from an identifier alone.
///
/// Keeping this contract in the Inventory namespace preserves the
/// <c>Recipes -> Inventory</c> dependency direction: Recipes consumes the contract,
/// Inventory never references Recipes.
/// </summary>
internal interface IInventoryItemReferenceReader
{
    /// <summary>
    /// Resolves a single item reference for <paramref name="viewer"/>, applying the
    /// Inventory accessibility rules. Returns <see langword="null"/> when the item
    /// does not exist or is not accessible to the viewer, matching the platform
    /// not-found behaviour so private items are not disclosed.
    /// </summary>
    Task<InventoryItemReference?> FindAccessibleAsync(
        int itemId,
        UserId viewer,
        CancellationToken cancellationToken);

    /// <summary>
    /// Resolves the display names of several items for <paramref name="viewer"/> in
    /// one query, used to project the resolved-item names of a recipe's ingredient
    /// lines. Items that do not exist or are not accessible to the viewer are
    /// omitted, so the consumer renders a neutral placeholder for the missing
    /// identifiers.
    /// </summary>
    Task<IReadOnlyDictionary<int, InventoryItemReference>> ResolveAccessibleAsync(
        IReadOnlyCollection<int> itemIds,
        UserId viewer,
        CancellationToken cancellationToken);
}
