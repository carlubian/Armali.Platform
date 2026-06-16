using System.Collections.Frozen;

namespace Segaris.Api.Modules.Opex.Contracts;

internal static class OpexContractQuery
{
    public static class Parameters
    {
        public const string Search = "search";
        public const string Type = "type";
        public const string Status = "status";
        public const string Category = "category";
        public const string Supplier = "supplier";
        public const string CostCenter = "costCenter";
        public const string Currency = "currency";
        public const string Frequency = "frequency";
        public const string Visibility = "visibility";
        public const string Creator = "creator";
        public const string Page = "page";
        public const string PageSize = "pageSize";
        public const string Sort = "sort";
        public const string SortDirection = "sortDirection";
    }

    public static class SortFields
    {
        public const string Name = "name";
        public const string Type = "type";
        public const string Status = "status";
        public const string Category = "category";
        public const string Supplier = "supplier";
        public const string Frequency = "frequency";
        public const string EstimatedAnnualAmount = "estimatedAnnualAmount";
        public const string RealizedCurrentYearAmount = "realizedCurrentYearAmount";
        public const string Currency = "currency";
        public const string TieBreaker = "id";
        public const string Default = Name;
    }

    public static readonly FrozenSet<string> AllowedSortFields = new[]
    {
        SortFields.Name,
        SortFields.Type,
        SortFields.Status,
        SortFields.Category,
        SortFields.Supplier,
        SortFields.Frequency,
        SortFields.EstimatedAnnualAmount,
        SortFields.RealizedCurrentYearAmount,
        SortFields.Currency,
        SortFields.TieBreaker,
    }.ToFrozenSet(StringComparer.Ordinal);

    public static readonly IReadOnlyList<int> PageSizeOptions = [10, 25, 50, 100];
}

internal static class OpexOccurrenceQuery
{
    public const string Page = "page";
    public const string PageSize = "pageSize";
    public const string DefaultSort = "effectiveDate";
    public const string TieBreaker = "id";
    public static readonly IReadOnlyList<int> PageSizeOptions = [10, 25, 50, 100];
}
