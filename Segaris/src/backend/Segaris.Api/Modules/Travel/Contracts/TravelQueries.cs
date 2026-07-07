namespace Segaris.Api.Modules.Travel.Contracts;

internal static class TravelTripQuery
{
    public static class SortFields
    {
        public const string Name = "name";
        public const string TripType = "tripType";
        public const string Destination = "destination";
        public const string StartDate = "startDate";
        public const string EndDate = "endDate";
        public const string Status = "status";
        public const string Visibility = "visibility";
        public const string Id = "id";

        public const string Default = StartDate;
        public const string TieBreaker = Id;
    }

    public static readonly IReadOnlySet<string> AllowedSortFields =
        new HashSet<string>(StringComparer.Ordinal)
        {
            SortFields.Name,
            SortFields.TripType,
            SortFields.Destination,
            SortFields.StartDate,
            SortFields.EndDate,
            SortFields.Status,
            SortFields.Visibility,
            SortFields.Id,
        };

    public static readonly IReadOnlyList<int> PageSizeOptions = [10, 25, 50, 100];

    public const string DefaultSortDirection = "desc";
}

internal static class TravelExpenseQuery
{
    public static class SortFields
    {
        public const string Date = "date";
        public const string Category = "category";
        public const string Description = "description";
        public const string Amount = "amount";
        public const string Currency = "currency";
        public const string Supplier = "supplier";
        public const string CostCenter = "costCenter";
        public const string Id = "id";

        public const string Default = Date;
        public const string TieBreaker = Id;
    }

    public static readonly IReadOnlySet<string> AllowedSortFields =
        new HashSet<string>(StringComparer.Ordinal)
        {
            SortFields.Date,
            SortFields.Category,
            SortFields.Description,
            SortFields.Amount,
            SortFields.Currency,
            SortFields.Supplier,
            SortFields.CostCenter,
            SortFields.Id,
        };

    public static readonly IReadOnlyList<int> PageSizeOptions = [10, 25, 50, 100];

    public const string DefaultSortDirection = "desc";
}
