namespace Segaris.Api.Modules.Maintenance.Domain;

/// <summary>
/// The fixed maintenance task priority. It is required, defaults to
/// <see cref="Medium"/>, is not managed through Configuration, and is used for
/// sorting and filtering.
/// </summary>
internal enum MaintenancePriority
{
    Low,
    Medium,
    High,
}
