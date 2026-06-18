using Segaris.Api.Modules.Clothes.Domain;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Clothes;

/// <summary>
/// Translates Clothes garment domain failures into the HTTP problem responses
/// carrying the frozen <see cref="ClothesErrorCodes"/> values. Missing and
/// inaccessible garments share not-found semantics so private data is not
/// disclosed.
/// </summary>
internal static class ClothesGarmentProblem
{
    public static ApiProblemException NotFound() => new(
        StatusCodes.Status404NotFound,
        ClothesErrorCodes.GarmentNotFound,
        "The requested Clothes garment was not found.");

    public static ApiProblemException From(ClothesValidationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Reason switch
        {
            ClothesValidationReason.CatalogReference => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                ClothesErrorCodes.UnknownCatalogReference,
                exception.Message),
            ClothesValidationReason.VisibilityForbidden => new ApiProblemException(
                StatusCodes.Status403Forbidden,
                ClothesErrorCodes.GarmentVisibilityForbidden,
                exception.Message),
            _ => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                ClothesErrorCodes.GarmentValidation,
                exception.Message),
        };
    }
}
