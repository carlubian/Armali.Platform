using Segaris.Api.Modules.Inventory.Mutations;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Inventory;

/// <summary>
/// Translates Inventory order failures into the HTTP problem responses carrying
/// the frozen <see cref="InventoryErrorCodes"/> values.
/// </summary>
internal static class InventoryOrderProblem
{
    public static ApiProblemException NotFound() => new(
        StatusCodes.Status404NotFound,
        InventoryErrorCodes.OrderNotFound,
        "The requested Inventory order was not found.");

    public static ApiProblemException From(InventoryOrderValidationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Reason switch
        {
            InventoryOrderValidationReason.ReceivedLocked => new ApiProblemException(
                StatusCodes.Status409Conflict,
                InventoryErrorCodes.OrderReceivedLocked,
                exception.Message),
            InventoryOrderValidationReason.VisibilityForbidden => new ApiProblemException(
                StatusCodes.Status403Forbidden,
                InventoryErrorCodes.OrderVisibilityForbidden,
                exception.Message),
            InventoryOrderValidationReason.LineSupplierNotAllowed => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                InventoryErrorCodes.OrderLineSupplierNotAllowed,
                exception.Message),
            InventoryOrderValidationReason.LineItemNotAccessible => new ApiProblemException(
                StatusCodes.Status404NotFound,
                InventoryErrorCodes.OrderLineItemNotAccessible,
                exception.Message),
            InventoryOrderValidationReason.CatalogReference => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                InventoryErrorCodes.UnknownCatalogReference,
                exception.Message),
            _ => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                InventoryErrorCodes.OrderValidation,
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
