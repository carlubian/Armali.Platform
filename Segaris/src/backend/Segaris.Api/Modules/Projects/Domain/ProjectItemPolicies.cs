using System.Linq.Expressions;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Projects.Domain;

internal static class ProjectItemPolicies
{
    public static Expression<Func<TItem, bool>> AccessibleTo<TItem>(UserId userId)
        where TItem : ProjectItem =>
        item => item.Visibility == RecordVisibility.Public || item.CreatedBy == userId.Value;

    public static Expression<Func<TItem, bool>> MutableBy<TItem>(UserId userId)
        where TItem : ProjectItem =>
        AccessibleTo<TItem>(userId);
}
