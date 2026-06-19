using System.Linq.Expressions;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Maintenance.Domain;

/// <summary>
/// Visibility rules for Maintenance tasks. Public tasks are collaborative; private
/// tasks are creator-only and are reported as not found to other users.
/// </summary>
internal static class MaintenanceTaskPolicies
{
    public static Expression<Func<MaintenanceTask, bool>> AccessibleTo(UserId userId) =>
        task => task.Visibility == RecordVisibility.Public || task.CreatedBy == userId.Value;

    public static Expression<Func<MaintenanceTask, bool>> MutableBy(UserId userId) => AccessibleTo(userId);

    public static bool CanChangeVisibility(MaintenanceTask task, UserId userId) =>
        task.CreatedBy == userId.Value;
}
