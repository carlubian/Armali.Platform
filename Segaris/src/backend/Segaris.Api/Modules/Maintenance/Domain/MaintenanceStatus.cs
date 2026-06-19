namespace Segaris.Api.Modules.Maintenance.Domain;

/// <summary>
/// The fixed, descriptive maintenance task status. It is manually controlled, blocks
/// no operation by itself, and is not managed through Configuration. Only
/// <see cref="Pending"/> and <see cref="InProgress"/> tasks participate in launcher
/// attention. Entering <see cref="Completed"/> sets the system-managed completion
/// date in <c>Europe/Madrid</c>; leaving it clears the completion date.
/// </summary>
internal enum MaintenanceStatus
{
    Pending,
    InProgress,
    Completed,
    Cancelled,
}
