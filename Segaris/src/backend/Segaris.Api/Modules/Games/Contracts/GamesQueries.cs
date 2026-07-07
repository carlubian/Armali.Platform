namespace Segaris.Api.Modules.Games.Contracts;

/// <summary>
/// Frozen query bounds for the paginated playthrough collection. Sorting is
/// deterministic with a stable identifier tie-breaker; the start-date sort is by
/// start year then start month.
/// </summary>
internal static class PlaythroughQuery
{
    public static class SortFields
    {
        public const string Name = "name";
        public const string Game = "game";
        public const string StartDate = "startDate";
        public const string Status = "status";
        public const string Progress = "progress";
        public const string Id = "id";

        public const string Default = Name;
        public const string TieBreaker = Id;
    }

    public static readonly IReadOnlySet<string> AllowedSortFields =
        new HashSet<string>(StringComparer.Ordinal)
        {
            SortFields.Name,
            SortFields.Game,
            SortFields.StartDate,
            SortFields.Status,
            SortFields.Progress,
            SortFields.Id,
        };

    public static readonly IReadOnlyList<int> PageSizeOptions = [10, 25, 50, 100];

    public const string DefaultSortDirection = "asc";
}
