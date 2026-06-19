namespace Segaris.Api.Modules.Maintenance.Contracts;

/// <summary>
/// Frozen sort and pagination vocabulary for <c>GET /api/maintenance/tasks</c>. The
/// documented default ordering is due date ascending with tasks without a due date
/// last, then identifier ascending. The nulls-last behaviour is applied by the
/// Wave 2 query implementation; this contract freezes the field names and defaults.
/// </summary>
internal static class MaintenanceQuery
{
    public static class SortFields
    {
        public const string Title = "title";
        public const string Type = "type";
        public const string Status = "status";
        public const string Priority = "priority";
        public const string DueDate = "dueDate";
        public const string Visibility = "visibility";
        public const string Id = "id";

        public const string Default = DueDate;
        public const string TieBreaker = Id;
    }

    public static readonly IReadOnlySet<string> AllowedSortFields =
        new HashSet<string>(StringComparer.Ordinal)
        {
            SortFields.Title,
            SortFields.Type,
            SortFields.Status,
            SortFields.Priority,
            SortFields.DueDate,
            SortFields.Visibility,
            SortFields.Id,
        };

    public static readonly IReadOnlyList<int> PageSizeOptions = [10, 25, 50, 100];

    public const string DefaultSortDirection = "asc";
}
