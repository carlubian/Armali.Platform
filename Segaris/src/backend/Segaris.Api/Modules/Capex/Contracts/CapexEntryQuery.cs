using System.Collections.Frozen;

namespace Segaris.Api.Modules.Capex.Contracts;

/// <summary>
/// Frozen query-parameter vocabulary for the paginated Entries list
/// (<c>GET /api/capex/entries</c>). Pagination and sorting reuse the platform
/// parameter names (<c>page</c>, <c>pageSize</c>, <c>sort</c>, <c>sortDirection</c>);
/// the remaining names are the allow-listed Capex search and filter inputs.
/// </summary>
internal static class CapexEntryQuery
{
    public static class Parameters
    {
        public const string Search = "search";
        public const string From = "from";
        public const string To = "to";
        public const string Type = "type";
        public const string Status = "status";
        public const string Category = "category";
        public const string Supplier = "supplier";
        public const string CostCenter = "costCenter";
        public const string Currency = "currency";
        public const string Visibility = "visibility";
        public const string Creator = "creator";
        public const string Page = "page";
        public const string PageSize = "pageSize";
        public const string Sort = "sort";
        public const string SortDirection = "sortDirection";
    }

    /// <summary>Allow-listed <c>sort</c> values plus the stable tie-breaker.</summary>
    public static class SortFields
    {
        public const string Title = "title";
        public const string Type = "type";
        public const string Status = "status";
        public const string DueDate = "dueDate";
        public const string Category = "category";
        public const string Supplier = "supplier";
        public const string CostCenter = "costCenter";
        public const string Total = "total";
        public const string Currency = "currency";

        /// <summary>Deterministic tie-breaker appended to every ordering.</summary>
        public const string TieBreaker = "id";

        public const string Default = DueDate;
    }

    /// <summary>The complete allow-list passed to <c>SortRequest.Create</c>.</summary>
    public static readonly FrozenSet<string> AllowedSortFields = new[]
    {
        SortFields.Title,
        SortFields.Type,
        SortFields.Status,
        SortFields.DueDate,
        SortFields.Category,
        SortFields.Supplier,
        SortFields.CostCenter,
        SortFields.Total,
        SortFields.Currency,
        SortFields.TieBreaker,
    }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>User-selectable page sizes; the default remains the platform default of 25.</summary>
    public static readonly IReadOnlyList<int> PageSizeOptions = [10, 25, 50, 100];
}
