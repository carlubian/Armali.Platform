namespace Segaris.Api.Modules.Assets.Contracts;

internal static class AssetQuery
{
    public static class SortFields
    {
        public const string Name = "name";
        public const string Code = "code";
        public const string Category = "category";
        public const string Location = "location";
        public const string Status = "status";
        public const string ExpectedEndOfLife = "expectedEndOfLife";
        public const string Visibility = "visibility";
        public const string Id = "id";

        public const string Default = Name;
        public const string TieBreaker = Id;
    }

    public static readonly IReadOnlySet<string> AllowedSortFields =
        new HashSet<string>(StringComparer.Ordinal)
        {
            SortFields.Name,
            SortFields.Code,
            SortFields.Category,
            SortFields.Location,
            SortFields.Status,
            SortFields.ExpectedEndOfLife,
            SortFields.Visibility,
            SortFields.Id,
        };

    public static readonly IReadOnlyList<int> PageSizeOptions = [10, 25, 50, 100];

    public const string DefaultSortDirection = "asc";
}
