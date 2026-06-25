using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Maintenance.Contracts;
using Segaris.Api.Modules.Maintenance.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Maintenance.Queries;

/// <summary>
/// Publishes due dates for accessible tasks that are <see cref="MaintenanceStatus.Pending"/>
/// or <see cref="MaintenanceStatus.InProgress"/> and carry a due date inside the requested
/// range. Completed and cancelled tasks, and tasks without a due date, are excluded.
/// </summary>
internal sealed class MaintenanceCalendarProjectionProvider(SegarisDbContext database)
    : IMaintenanceCalendarProjectionProvider
{
    public async Task<IReadOnlyList<MaintenanceTaskDueCalendarProjection>> ListCalendarDueTasksAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken)
    {
        return await database.Set<MaintenanceTask>()
            .AsNoTracking()
            .Where(MaintenanceTaskPolicies.AccessibleTo(viewer))
            .Where(task => task.Status == MaintenanceStatus.Pending
                || task.Status == MaintenanceStatus.InProgress)
            .Where(task => task.DueDate != null
                && task.DueDate >= from
                && task.DueDate <= to)
            .Select(task => new MaintenanceTaskDueCalendarProjection(
                task.Id,
                task.Title,
                task.Status.ToString(),
                task.DueDate!.Value,
                $"/maintenance?taskId={task.Id}"))
            .ToArrayAsync(cancellationToken);
    }
}
