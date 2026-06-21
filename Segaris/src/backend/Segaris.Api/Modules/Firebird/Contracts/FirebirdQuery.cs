namespace Segaris.Api.Modules.Firebird.Contracts;

/// <summary>
/// Frozen sort and pagination vocabulary for <c>GET /api/people</c>. Birthday
/// ordering is calendar order (month, day), independent of today, with people
/// without birthdays sorted last and <c>id</c> as the final tie-breaker.
/// </summary>
internal static class FirebirdPeopleQuery
{
    public static class SortFields
    {
        public const string Name = "name";
        public const string Category = "category";
        public const string Status = "status";
        public const string Birthday = "birthday";
        public const string Visibility = "visibility";
        public const string Id = "id";

        public const string Default = Name;
        public const string TieBreaker = Id;
    }

    public static readonly IReadOnlySet<string> AllowedSortFields =
        new HashSet<string>(StringComparer.Ordinal)
        {
            SortFields.Name,
            SortFields.Category,
            SortFields.Status,
            SortFields.Birthday,
            SortFields.Visibility,
            SortFields.Id,
        };

    public static readonly IReadOnlyList<int> PageSizeOptions = [10, 25, 50, 100];

    public const string DefaultSortDirection = "asc";
}
