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

    public static ApiProblemException AttachmentNotFound() => new(
        StatusCodes.Status404NotFound,
        ClothesErrorCodes.AttachmentNotFound,
        "The requested attachment was not found.");

    public static ApiProblemException AttachmentInvalid(
        string field,
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null) => new(
        StatusCodes.Status400BadRequest,
        ClothesErrorCodes.AttachmentInvalid,
        "The attachment is invalid.",
        errors: errors ?? new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [field] = [message],
        });

    public static ApiProblemException PrimaryNotImage() => new(
        StatusCodes.Status400BadRequest,
        ClothesErrorCodes.AttachmentPrimaryInvalid,
        "Only image attachments can be marked as the primary image.");

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
