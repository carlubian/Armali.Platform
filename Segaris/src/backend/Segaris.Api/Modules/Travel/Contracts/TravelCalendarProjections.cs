using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Travel.Contracts;

internal sealed record TravelTripCalendarProjection(
    int TripId,
    string Name,
    string? Destination,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    string? TargetRoute);

internal interface ITravelCalendarProjectionProvider
{
    Task<IReadOnlyList<TravelTripCalendarProjection>> ListCalendarTripsAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken);
}
