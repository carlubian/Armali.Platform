using Segaris.Shared.Api;

namespace Segaris.Api.Modules.Inventory;

/// <summary>Stable machine-readable Inventory failures.</summary>
internal static class InventoryErrorCodes
{
    public static readonly ErrorCode ItemNotFound = new("inventory.item.not_found");
    public static readonly ErrorCode ItemValidation = new("inventory.item.validation");
    public static readonly ErrorCode ItemSupplierRequired = new("inventory.item.supplier_required");
    public static readonly ErrorCode ItemReferencedByOrder = new("inventory.item.referenced");
    public static readonly ErrorCode ItemVisibilityForbidden = new("inventory.item.visibility_forbidden");

    public static readonly ErrorCode StockNegativeResult = new("inventory.stock.negative_result");

    public static readonly ErrorCode OrderNotFound = new("inventory.order.not_found");
    public static readonly ErrorCode OrderValidation = new("inventory.order.validation");
    public static readonly ErrorCode OrderNotActive = new("inventory.order.not_active");
    public static readonly ErrorCode OrderReceivedLocked = new("inventory.order.received_locked");
    public static readonly ErrorCode OrderVisibilityForbidden = new("inventory.order.visibility_forbidden");
    public static readonly ErrorCode OrderLineSupplierNotAllowed = new("inventory.order.line.supplier_not_allowed");
    public static readonly ErrorCode OrderLineItemNotAccessible = new("inventory.order.line.item_not_accessible");

    public static readonly ErrorCode UnknownCatalogReference = new("inventory.catalog.unknown_reference");

    public static readonly ErrorCode AttachmentNotFound = new("inventory.attachment.not_found");
    public static readonly ErrorCode AttachmentInvalid = new("inventory.attachment.invalid");

    public static readonly ErrorCode CategoryNotFound = new("inventory.category.not_found");
    public static readonly ErrorCode CategoryValidation = new("inventory.category.validation");
    public static readonly ErrorCode CategoryDuplicateName = new("inventory.category.duplicate_name");
    public static readonly ErrorCode CategoryRequiredNotEmpty = new("inventory.category.required_not_empty");
    public static readonly ErrorCode CategoryReferenced = new("inventory.category.referenced");
    public static readonly ErrorCode CategoryInvalidReplacement = new("inventory.category.invalid_replacement");
    public static readonly ErrorCode CategoryMigrationConflict = new("inventory.category.migration_conflict");

    public static readonly ErrorCode LocationNotFound = new("inventory.location.not_found");
    public static readonly ErrorCode LocationValidation = new("inventory.location.validation");
    public static readonly ErrorCode LocationDuplicateName = new("inventory.location.duplicate_name");
    public static readonly ErrorCode LocationRequiredNotEmpty = new("inventory.location.required_not_empty");
    public static readonly ErrorCode LocationReferenced = new("inventory.location.referenced");
    public static readonly ErrorCode LocationInvalidReplacement = new("inventory.location.invalid_replacement");
    public static readonly ErrorCode LocationMigrationConflict = new("inventory.location.migration_conflict");
}
