using Segaris.Api.Modules.Travel.Domain;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Travel;

internal static class TravelTripProblem
{
    public static ApiProblemException From(TravelValidationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Reason switch
        {
            TravelValidationReason.Itinerary => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                TravelErrorCodes.ItineraryValidation,
                exception.Message),
            TravelValidationReason.CatalogReference => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                TravelErrorCodes.UnknownCatalogReference,
                exception.Message),
            TravelValidationReason.VisibilityForbidden => new ApiProblemException(
                StatusCodes.Status403Forbidden,
                TravelErrorCodes.TripVisibilityForbidden,
                exception.Message),
            _ => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                TravelErrorCodes.TripValidation,
                exception.Message),
        };
    }

    public static ApiProblemException NotFound() => new(
        StatusCodes.Status404NotFound,
        TravelErrorCodes.TripNotFound,
        "Trip not found.");

    public static ApiProblemException AttachmentNotFound() => new(
        StatusCodes.Status404NotFound,
        TravelErrorCodes.AttachmentNotFound,
        "The requested attachment was not found.");

    public static ApiProblemException AttachmentInvalid(
        string field,
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null) => new(
        StatusCodes.Status400BadRequest,
        TravelErrorCodes.AttachmentInvalid,
        "The attachment is invalid.",
        errors: errors ?? new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [field] = [message],
        });
}
