namespace Segaris.Api.Modules.Recipes.Contracts;

/// <summary>Frozen query bounds for the paginated recipe gallery.</summary>
internal static class RecipeQuery
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
