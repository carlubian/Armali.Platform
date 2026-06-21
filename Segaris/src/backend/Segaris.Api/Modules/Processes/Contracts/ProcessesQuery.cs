namespace Segaris.Api.Modules.Processes.Contracts;

/// <summary>
/// Frozen sort and pagination vocabulary for <c>GET /api/processes</c>. The documented
/// default ordering is effective due date ascending with processes without an effective
/// date last, then identifier ascending. The effective due date is the global due date
/// when set, otherwise the next pending (frontier) step's due date. The nulls-last
/// behaviour is applied by the Wave 2 query implementation; this contract freezes the
/// field names and defaults.
/// </summary>
internal static class ProcessesQuery
{
    public static class SortFields
    {
        public const string Name = "name";
        public const string Category = "category";
        public const string Status = "status";
        public const string DueDate = "dueDate";
        public const string Visibility = "visibility";
        public const string Id = "id";

        public const string Default = DueDate;
        public const string TieBreaker = Id;
    }

    public static readonly IReadOnlySet<string> AllowedSortFields =
        new HashSet<string>(StringComparer.Ordinal)
        {
            SortFields.Name,
            SortFields.Category,
            SortFields.Status,
            SortFields.DueDate,
            SortFields.Visibility,
            SortFields.Id,
        };

    public static readonly IReadOnlyList<int> PageSizeOptions = [10, 25, 50, 100];

    public const string DefaultSortDirection = "asc";
}
