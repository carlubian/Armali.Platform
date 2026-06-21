using Segaris.Api.Modules.Firebird.Domain;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Firebird;

internal static class FirebirdSubResourceProblem
{
    public static ApiProblemException AvatarNotFound() => new(
        StatusCodes.Status404NotFound,
        FirebirdErrorCodes.AvatarNotFound,
        "The requested Firebird avatar was not found.");

    public static ApiProblemException AvatarInvalid(
        string field,
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null) => new(
        StatusCodes.Status400BadRequest,
        FirebirdErrorCodes.AvatarInvalid,
        "The avatar is invalid.",
        errors: errors ?? new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [field] = [message],
        });

    public static ApiProblemException UsernameNotFound() => new(
        StatusCodes.Status404NotFound,
        FirebirdErrorCodes.UsernameNotFound,
        "The requested Firebird username was not found.");

    public static ApiProblemException InteractionNotFound() => new(
        StatusCodes.Status404NotFound,
        FirebirdErrorCodes.InteractionNotFound,
        "The requested Firebird interaction was not found.");

    public static ApiProblemException From(FirebirdValidationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Reason switch
        {
            FirebirdValidationReason.UnknownCatalogReference => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                FirebirdErrorCodes.UnknownCatalogReference,
                exception.Message),
            FirebirdValidationReason.UsernameValidation => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                FirebirdErrorCodes.UsernameValidation,
                exception.Message),
            FirebirdValidationReason.InteractionValidation => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                FirebirdErrorCodes.InteractionValidation,
                exception.Message),
            _ => FirebirdPersonProblem.From(exception),
        };
    }
}
