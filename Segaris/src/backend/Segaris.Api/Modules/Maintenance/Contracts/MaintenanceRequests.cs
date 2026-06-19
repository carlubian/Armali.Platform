namespace Segaris.Api.Modules.Maintenance.Contracts;

/// <summary>
/// Frozen request contract for <c>POST /api/maintenance/tasks</c>. <see cref="Status"/>,
/// <see cref="Priority"/>, and <see cref="Visibility"/> are the fixed string
/// vocabularies (the enum member names and the platform visibility names).
/// <see cref="MaintenanceTypeId"/> is the required database-assigned type identifier.
/// <see cref="DueDate"/> is an optional civil date with no past/future boundary.
/// <see cref="AssetId"/> is the optional live Assets reference, subject to the
/// visibility rule. The completion date is system-managed and is never accepted from
/// the client.
/// </summary>
internal sealed record CreateMaintenanceTaskRequest(
    string? Title,
    int MaintenanceTypeId,
    string? Status,
    string? Priority,
    DateOnly? DueDate,
    string? Notes,
    int? AssetId,
    string? Visibility);

/// <summary>
/// Frozen request contract for <c>PUT /api/maintenance/tasks/{taskId}</c>. The update
/// fully replaces the task's editable fields in one transaction. The completion date
/// remains system-managed and is not accepted from the client.
/// </summary>
internal sealed record UpdateMaintenanceTaskRequest(
    string? Title,
    int MaintenanceTypeId,
    string? Status,
    string? Priority,
    DateOnly? DueDate,
    string? Notes,
    int? AssetId,
    string? Visibility);
