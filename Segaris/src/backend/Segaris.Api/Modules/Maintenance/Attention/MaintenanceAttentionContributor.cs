using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Launcher.Contracts;
using Segaris.Api.Modules.Maintenance.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Maintenance.Attention;

/// <summary>
/// Contributes the Maintenance launcher card's attention state. Attention is
/// required when the current user can access at least one <c>Pending</c> or
/// <c>InProgress</c> task with a due date in the past or through the next 7
/// natural days in the household <c>Europe/Madrid</c> civil date.
/// </summary>
internal sealed class MaintenanceAttentionContributor(
    SegarisDbContext database,
    ICurrentUser currentUser,
    IClock clock) : ILauncherAttentionContributor
{
    public string Module => MaintenanceLauncherCard.ModuleKey;

    public async Task<bool> RequiresAttentionAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return false;
        }

        var windowEnd = MaintenanceCivilDate.Today(clock).AddDays(7);
        return await database.Set<MaintenanceTask>()
            .AsNoTracking()
            .Where(MaintenanceTaskPolicies.AccessibleTo(userId))
            .AnyAsync(
                task => (task.Status == MaintenanceStatus.Pending || task.Status == MaintenanceStatus.InProgress)
                    && task.DueDate.HasValue
                    && task.DueDate.Value <= windowEnd,
                cancellationToken);
    }
}
