using System.Globalization;
using Segaris.Shared.Attachments;

namespace Segaris.Api.Modules.Inventory;

/// <summary>Frozen attachment owner kinds for items and orders.</summary>
internal static class InventoryAttachments
{
    public const string Module = "Inventory";
    public const string ItemEntityType = "Item";
    public const string OrderEntityType = "Order";

    public static AttachmentOwner ItemOwner(int itemId) =>
        new(Module, ItemEntityType, itemId.ToString(CultureInfo.InvariantCulture));

    public static AttachmentOwner OrderOwner(int orderId) =>
        new(Module, OrderEntityType, orderId.ToString(CultureInfo.InvariantCulture));
}
