using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Maintenance.Contracts;

internal sealed record MaintenanceTaskDueCalendarProjection(
    int TaskId,
    string Title,
    string Status,
    DateOnly DueDate,
    string? TargetRoute);

internal interface IMaintenanceCalendarProjectionProvider
{
    Task<IReadOnlyList<MaintenanceTaskDueCalendarProjection>> ListCalendarDueTasksAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken);
}
