namespace Segaris.Api.Modules.Destinations.Contracts;

/// <summary>Frozen query bounds for the paginated destination gallery.</summary>
internal static class DestinationQuery
{
    public static class SortFields
    {
        public const string Name = "name";
        public const string Category = "category";
        public const string Id = "id";

        public const string Default = Name;
        public const string TieBreaker = Id;
    }

    public static readonly IReadOnlySet<string> AllowedSortFields =
        new HashSet<string>(StringComparer.Ordinal)
        {
            SortFields.Name,
            SortFields.Category,
            SortFields.Id,
        };

    public static readonly IReadOnlyList<int> PageSizeOptions = [10, 25, 50, 100];

    public const string DefaultSortDirection = "asc";
}

/// <summary>Frozen query bounds for the destination-scoped places list.</summary>
internal static class PlaceQuery
{
    public static class SortFields
    {
        public const string Name = "name";
        public const string Category = "category";
        public const string Rating = "rating";
        public const string Id = "id";

        public const string Default = Name;
        public const string TieBreaker = Id;
    }

    public static readonly IReadOnlySet<string> AllowedSortFields =
        new HashSet<string>(StringComparer.Ordinal)
        {
            SortFields.Name,
            SortFields.Category,
            SortFields.Rating,
            SortFields.Id,
        };

    public static readonly IReadOnlyList<int> PageSizeOptions = [10, 25, 50, 100];

    public const string DefaultSortDirection = "asc";
}
