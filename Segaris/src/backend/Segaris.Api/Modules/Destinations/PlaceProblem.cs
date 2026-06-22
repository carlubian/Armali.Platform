using Segaris.Api.Modules.Destinations.Domain;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Destinations;

internal static class PlaceProblem
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
            _ => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                DestinationsErrorCodes.PlaceValidation,
                exception.Message),
        };
    }

    public static ApiProblemException NotFound() => new(
        StatusCodes.Status404NotFound,
        DestinationsErrorCodes.PlaceNotFound,
        "Place not found.");
}
