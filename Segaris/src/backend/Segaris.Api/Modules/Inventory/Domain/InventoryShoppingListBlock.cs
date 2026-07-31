namespace Segaris.Api.Modules.Inventory.Domain;

/// <summary>
/// The block a shopping list entry belongs to. An item whose current stock is
/// strictly below its minimum stock is <c>Required</c>; one whose current stock
/// equals its minimum stock is <c>Optional</c>. Only <c>Required</c> entries carry
/// a replenishment quantity.
/// </summary>
internal enum InventoryShoppingListBlock
{
    Required,
    Optional,
}
