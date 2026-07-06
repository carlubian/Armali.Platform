namespace Segaris.Api.Modules.Inventory;

/// <summary>Frozen route shapes for the Inventory HTTP surface.</summary>
internal static class InventoryApiRoutes
{
    public const string Tag = "Inventory";

    public const string Items = "inventory/items";
    public const string ItemById = "/{itemId:int}";
    public const string ItemDeletionImpact = "/{itemId:int}/deletion-impact";
    public const string ItemPriceHistory = "/{itemId:int}/price-history";
    public const string ItemStockAdjustments = "/{itemId:int}/stock-adjustments";
    public const string ItemAttachments = "/{itemId:int}/attachments";
    public const string ItemAttachmentById = "/{itemId:int}/attachments/{attachmentId}";

    public const string Orders = "inventory/orders";
    public const string OrderById = "/{orderId:int}";
    public const string OrderReceive = "/{orderId:int}/receive";
    public const string OrderAttachments = "/{orderId:int}/attachments";
    public const string OrderAttachmentById = "/{orderId:int}/attachments/{attachmentId}";

    public const string Categories = "inventory/categories";
    public const string CategoryById = "/{categoryId:int}";
    public const string CategoryMove = "/{categoryId:int}/move";
    public const string CategoryDeletionImpact = "/{categoryId:int}/deletion-impact";
    public const string CategoryReplaceAndDelete = "/{categoryId:int}/replace-and-delete";

    public const string Locations = "inventory/locations";
    public const string LocationById = "/{locationId:int}";
    public const string LocationMove = "/{locationId:int}/move";
    public const string LocationDeletionImpact = "/{locationId:int}/deletion-impact";
    public const string LocationReplaceAndDelete = "/{locationId:int}/replace-and-delete";
}
