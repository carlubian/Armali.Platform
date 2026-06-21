using System.Linq.Expressions;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Processes.Domain;

/// <summary>
/// Visibility rules for Processes. Public processes are collaborative; private
/// processes are creator-only and are reported as not found to other users.
/// </summary>
internal static class ProcessPolicies
{
    public static Expression<Func<Process, bool>> AccessibleTo(UserId userId) =>
        process => process.Visibility == RecordVisibility.Public || process.CreatedBy == userId.Value;

    public static Expression<Func<Process, bool>> MutableBy(UserId userId) => AccessibleTo(userId);

    public static bool CanChangeVisibility(Process process, UserId userId) =>
        process.CreatedBy == userId.Value;
}
