namespace Segaris.Api.Modules.Projects.Domain;

/// <summary>
/// Insert-only allocator row for the shared Projects number. Its generated identifier
/// is assigned to exactly one new project or activity and remains stable afterwards.
/// </summary>
internal sealed class ProjectNumberAllocation
{
    public int Id { get; set; }
    public DateTimeOffset AllocatedAt { get; set; }
}
