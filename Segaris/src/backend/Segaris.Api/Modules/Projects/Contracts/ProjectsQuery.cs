namespace Segaris.Api.Modules.Projects.Contracts;

/// <summary>
/// Frozen ordering vocabulary for the Projects reads. The tree has a single natural
/// ordering and exposes no client-controlled sort, filter, or pagination: programs and
/// axes are ordered by code ascending and projects and activities by their global number
/// ascending. These constants freeze the field names the Wave 3 query implementation
/// orders by.
/// </summary>
internal static class ProjectsQuery
{
    /// <summary>The field programs and axes are ordered by (code ascending).</summary>
    public const string NodeOrderField = "code";

    /// <summary>The field projects and activities are ordered by (number ascending).</summary>
    public const string ItemOrderField = "number";

    public const string OrderDirection = "asc";
}
