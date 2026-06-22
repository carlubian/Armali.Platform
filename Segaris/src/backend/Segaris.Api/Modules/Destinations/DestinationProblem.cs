using Segaris.Api.Modules.Destinations.Domain;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Destinations;

internal static class DestinationProblem
{
    public static ApiProblemException From(DestinationsValidationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Reason switch
        {
            DestinationsValidationReason.CatalogReference => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                DestinationsErrorCodes.UnknownCatalogReference,
                exception.Message),
            DestinationsValidationReason.VisibilityForbidden => new ApiProblemException(
                StatusCodes.Status403Forbidden,
                DestinationsErrorCodes.DestinationVisibilityForbidden,
                exception.Message),
            _ => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                DestinationsErrorCodes.DestinationValidation,
                exception.Message),
        };
    }

    public static ApiProblemException NotFound() => new(
        StatusCodes.Status404NotFound,
        DestinationsErrorCodes.DestinationNotFound,
        "Destination not found.");
}
