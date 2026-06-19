using Segaris.Api.Modules.Assets.Domain;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Assets;

/// <summary>
/// Translates Asset domain failures into the HTTP problem responses carrying the
/// frozen <see cref="AssetsErrorCodes"/> values. A failed catalog reference is a
/// bad request, a duplicate code is a conflict, a forbidden visibility change is a
/// 403, and every other shape failure is the generic asset validation code.
/// </summary>
internal static class AssetProblem
{
    public static ApiProblemException From(AssetValidationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Reason switch
        {
            AssetValidationReason.VisibilityForbidden => new ApiProblemException(
                StatusCodes.Status403Forbidden,
                AssetsErrorCodes.AssetVisibilityForbidden,
                exception.Message),
            AssetValidationReason.CatalogReference => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                AssetsErrorCodes.UnknownCatalogReference,
                exception.Message),
            AssetValidationReason.DuplicateCode => new ApiProblemException(
                StatusCodes.Status409Conflict,
                AssetsErrorCodes.AssetDuplicateCode,
                exception.Message),
            _ => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                AssetsErrorCodes.AssetValidation,
                exception.Message),
        };
    }

    public static ApiProblemException NotFound() => new(
        StatusCodes.Status404NotFound,
        AssetsErrorCodes.AssetNotFound,
        "The requested asset was not found.");
}
