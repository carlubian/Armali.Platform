using System.Linq.Expressions;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Capex.Domain;

internal static class CapexEntryPolicies
{
    public static Expression<Func<CapexEntry, bool>> AccessibleTo(UserId userId) =>
        entry => entry.Visibility == RecordVisibility.Public || entry.CreatedBy == userId.Value;

    public static Expression<Func<CapexEntry, bool>> MutableBy(UserId userId) => AccessibleTo(userId);

    public static bool CanChangeVisibility(CapexEntry entry, UserId userId) =>
        entry.CreatedBy == userId.Value;
}
