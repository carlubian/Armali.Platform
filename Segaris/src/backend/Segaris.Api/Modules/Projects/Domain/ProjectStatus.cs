namespace Segaris.Api.Modules.Projects.Domain;

/// <summary>
/// The fixed, descriptive status shared by every project and activity. It is manually
/// controlled, blocks no operation by itself, and is not managed through Configuration.
/// New items default to <see cref="Planning"/>.
/// </summary>
internal enum ProjectStatus
{
    Planning,
    Active,
    Completed,
    OnHold,
    Cancelled,
}
