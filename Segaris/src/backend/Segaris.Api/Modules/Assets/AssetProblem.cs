using Segaris.Api.Modules.Assets.Contracts;
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

    public static ApiProblemException DeletionReferenced() => new(
        StatusCodes.Status409Conflict,
        AssetsErrorCodes.AssetDeletionReferenced,
        "The asset is referenced by another module and must be reassigned before deletion.");

    public static ApiProblemException InvalidReassignment(string message) => new(
        StatusCodes.Status400BadRequest,
        AssetsErrorCodes.AssetInvalidReassignment,
        "The asset deletion reassignment is invalid.",
        errors: new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["targetAssetId"] = [message],
        });

    public static ApiProblemException ReassignmentBlocked(AssetReassignmentBlockedException exception) => new(
        StatusCodes.Status409Conflict,
        exception.Code,
        exception.Message);

    public static ApiProblemException AttachmentNotFound() => new(
        StatusCodes.Status404NotFound,
        AssetsErrorCodes.AttachmentNotFound,
        "The requested asset attachment was not found.");

    public static ApiProblemException AttachmentInvalid(
        string field,
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null) => new(
        StatusCodes.Status400BadRequest,
        AssetsErrorCodes.AttachmentInvalid,
        "The attachment is invalid.",
        errors: errors ?? new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [field] = [message],
        });

    public static ApiProblemException PrimaryNotImage() => new(
        StatusCodes.Status400BadRequest,
        AssetsErrorCodes.AttachmentPrimaryInvalid,
        "Only image attachments can be marked as the primary image.");
}
