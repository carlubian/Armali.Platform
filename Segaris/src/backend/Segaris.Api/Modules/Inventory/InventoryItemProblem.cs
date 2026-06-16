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
            _ => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                InventoryErrorCodes.ItemValidation,
                exception.Message),
        };
    }
}
