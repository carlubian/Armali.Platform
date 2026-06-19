namespace Segaris.Api.Modules.Maintenance.Domain;

/// <summary>
/// A Maintenance-owned catalogue row. It mirrors the shared-catalogue shape (display
/// name, normalized name for case-insensitive uniqueness, declaration order, and audit
/// metadata) while remaining owned by Maintenance and surfaced through Configuration.
/// Because a type is required on every task, a referenced value may only be replaced,
/// never cleared.
/// </summary>
internal sealed class MaintenanceType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
