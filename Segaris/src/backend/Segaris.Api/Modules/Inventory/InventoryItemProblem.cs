using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Inventory;

/// <summary>
/// Translates Inventory item domain failures into the HTTP problem responses
/// carrying the frozen <see cref="InventoryErrorCodes"/> values. A missing or
/// inaccessible item shares the platform not-found behavior so a private item is
/// never disclosed.
/// </summary>
internal static class InventoryItemProblem
{
    public static ApiProblemException NotFound() => new(
        StatusCodes.Status404NotFound,
        InventoryErrorCodes.ItemNotFound,
        "The requested Inventory item was not found.");

    public static ApiProblemException From(InventoryValidationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Reason switch
        {
            InventoryValidationReason.SupplierRequired => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                InventoryErrorCodes.ItemSupplierRequired,
                exception.Message),
            InventoryValidationReason.NegativeStock => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                InventoryErrorCodes.StockNegativeResult,
                exception.Message),
            InventoryValidationReason.CatalogReference => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                InventoryErrorCodes.UnknownCatalogReference,
                exception.Message),
            InventoryValidationReason.VisibilityForbidden => new ApiProblemException(
                StatusCodes.Status403Forbidden,
                InventoryErrorCodes.ItemVisibilityForbidden,
                exception.Message),
            InventoryValidationReason.ReferencedByOrder => new ApiProblemException(
                StatusCodes.Status409Conflict,
                InventoryErrorCodes.ItemReferencedByOrder,
                exception.Message),
            _ => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                InventoryErrorCodes.ItemValidation,
                exception.Message),
        };
    }

    public static ApiProblemException AttachmentNotFound() => new(
        StatusCodes.Status404NotFound,
        InventoryErrorCodes.AttachmentNotFound,
        "The requested attachment was not found.");

    public static ApiProblemException AttachmentInvalid(
        string field,
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null) => new(
        StatusCodes.Status400BadRequest,
        InventoryErrorCodes.AttachmentInvalid,
        "The attachment is invalid.",
        errors: errors ?? new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [field] = [message],
        });
}
