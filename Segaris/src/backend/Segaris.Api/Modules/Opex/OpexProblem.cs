using Segaris.Api.Modules.Opex.Domain;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Opex;

/// <summary>
/// Translates Opex contract and occurrence domain failures into the HTTP problem
/// responses carrying the frozen <see cref="OpexErrorCodes"/> values.
/// </summary>
internal static class OpexProblem
{
    public static ApiProblemException From(OpexValidationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Reason switch
        {
            OpexValidationReason.VisibilityForbidden => new ApiProblemException(
                StatusCodes.Status403Forbidden,
                OpexErrorCodes.VisibilityForbidden,
                exception.Message),
            OpexValidationReason.CatalogReference => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                OpexErrorCodes.UnknownCatalogReference,
                exception.Message),
            OpexValidationReason.DuplicateName => new ApiProblemException(
                StatusCodes.Status409Conflict,
                OpexErrorCodes.ContractDuplicateName,
                exception.Message),
            _ => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                OpexErrorCodes.ContractValidation,
                exception.Message),
        };
    }

    /// <summary>
    /// Maps an occurrence mutation failure. Occurrences carry only generic shape,
    /// amount, and date validation; they have no catalog, duplicate-name, or
    /// visibility reasons, so every failure surfaces as the frozen occurrence
    /// validation code.
    /// </summary>
    public static ApiProblemException FromOccurrence(OpexValidationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new ApiProblemException(
            StatusCodes.Status400BadRequest,
            OpexErrorCodes.OccurrenceValidation,
            exception.Message);
    }

    public static ApiProblemException ContractNotFound() => new(
        StatusCodes.Status404NotFound,
        OpexErrorCodes.ContractNotFound,
        "The requested Opex contract was not found.");

    public static ApiProblemException OccurrenceNotFound() => new(
        StatusCodes.Status404NotFound,
        OpexErrorCodes.OccurrenceNotFound,
        "The requested Opex occurrence was not found.");

    public static ApiProblemException AttachmentNotFound() => new(
        StatusCodes.Status404NotFound,
        OpexErrorCodes.AttachmentNotFound,
        "The requested attachment was not found.");

    public static ApiProblemException AttachmentInvalid(
        string field,
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null) => new(
        StatusCodes.Status400BadRequest,
        OpexErrorCodes.AttachmentInvalid,
        "The attachment is invalid.",
        errors: errors ?? new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [field] = [message],
        });
}
