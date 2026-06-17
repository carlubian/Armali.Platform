namespace Segaris.Api.Modules.Inventory.Domain;

/// <summary>
/// Direction of a quick stock adjustment. The quantity is always a positive
/// value; the direction decides whether it is added to or subtracted from the
/// item's current stock.
/// </summary>
internal enum InventoryStockAdjustmentDirection
{
    Increase,
    Decrease,
}
