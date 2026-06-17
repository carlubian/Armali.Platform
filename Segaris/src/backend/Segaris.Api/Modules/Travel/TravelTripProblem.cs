using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Travel;

internal static class TravelTripProblem
{
    public static ApiProblemException NotFound() => new(
        StatusCodes.Status404NotFound,
        TravelErrorCodes.TripNotFound,
        "Trip not found.");
}
