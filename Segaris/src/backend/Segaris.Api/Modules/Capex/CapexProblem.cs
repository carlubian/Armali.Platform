using Segaris.Api.Modules.Capex.Domain;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Capex;

/// <summary>
/// Translates Capex domain failures into the HTTP problem responses carrying the
/// frozen <see cref="CapexErrorCodes"/> values.
/// </summary>
internal static class CapexProblem
{
    public static ApiProblemException From(CapexValidationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Reason switch
        {
            CapexValidationReason.VisibilityForbidden => new ApiProblemException(
                StatusCodes.Status403Forbidden,
                CapexErrorCodes.VisibilityForbidden,
                exception.Message),
            CapexValidationReason.CatalogReference => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                CapexErrorCodes.UnknownCatalogReference,
                exception.Message),
            _ => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                CapexErrorCodes.EntryValidation,
                exception.Message),
        };
    }

    public static ApiProblemException EntryNotFound() => new(
        StatusCodes.Status404NotFound,
        CapexErrorCodes.EntryNotFound,
        "The requested Capex entry was not found.");

    public static ApiProblemException AttachmentNotFound() => new(
        StatusCodes.Status404NotFound,
        CapexErrorCodes.AttachmentNotFound,
        "The requested attachment was not found.");

    public static ApiProblemException AttachmentInvalid(
        string field,
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null) => new(
        StatusCodes.Status400BadRequest,
        CapexErrorCodes.AttachmentInvalid,
        "The attachment is invalid.",
        errors: errors ?? new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [field] = [message],
        });
}
