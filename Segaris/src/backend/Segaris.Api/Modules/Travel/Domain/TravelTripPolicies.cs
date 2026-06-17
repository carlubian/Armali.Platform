using System.Linq.Expressions;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Travel.Domain;

internal static class TravelTripPolicies
{
    public static Expression<Func<TravelTrip, bool>> AccessibleTo(UserId userId) =>
        trip => trip.Visibility == RecordVisibility.Public || trip.CreatedBy == userId.Value;

    public static Expression<Func<TravelTrip, bool>> MutableBy(UserId userId) => AccessibleTo(userId);
}
