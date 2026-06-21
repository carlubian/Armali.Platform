using Segaris.Api.Modules.Firebird.Domain;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Firebird;

internal static class FirebirdPersonProblem
{
    public static ApiProblemException NotFound() => new(
        StatusCodes.Status404NotFound,
        FirebirdErrorCodes.PersonNotFound,
        "The requested Firebird person was not found.");

    public static ApiProblemException From(FirebirdValidationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Reason switch
        {
            FirebirdValidationReason.UnknownCatalogReference => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                FirebirdErrorCodes.UnknownCatalogReference,
                exception.Message),
            FirebirdValidationReason.VisibilityForbidden => new ApiProblemException(
                StatusCodes.Status403Forbidden,
                FirebirdErrorCodes.PersonVisibilityForbidden,
                exception.Message),
            _ => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                FirebirdErrorCodes.PersonValidation,
                exception.Message),
        };
    }
}
