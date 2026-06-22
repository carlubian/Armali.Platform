using System.Linq.Expressions;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Destinations.Domain;

internal static class DestinationPolicies
{
    public static Expression<Func<Destination, bool>> AccessibleTo(UserId userId) =>
        destination => destination.Visibility == RecordVisibility.Public || destination.CreatedBy == userId.Value;

    public static Expression<Func<Destination, bool>> MutableBy(UserId userId) => AccessibleTo(userId);
}
