namespace Segaris.Api.Modules.Processes.Domain;

/// <summary>
/// A Processes-owned catalogue row. It mirrors the shared-catalogue shape (display name,
/// normalized name for case-insensitive uniqueness, declaration order, and audit
/// metadata) while remaining owned by Processes and surfaced through Configuration.
/// Because a category is required on every process, a referenced value may only be
/// replaced, never cleared.
/// </summary>
internal sealed class ProcessCategory
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
