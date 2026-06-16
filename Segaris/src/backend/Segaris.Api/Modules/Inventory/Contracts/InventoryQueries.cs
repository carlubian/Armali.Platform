using System.Collections.Frozen;

namespace Segaris.Api.Modules.Inventory.Contracts;

internal static class InventoryItemQuery
{
    public static class Parameters
    {
        public const string Search = "search";
        public const string Status = "status";
        public const string Category = "category";
        public const string Location = "location";
        public const string Supplier = "supplier";
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
        public const string Status = "status";
        public const string Category = "category";
        public const string Location = "location";
        public const string CurrentStock = "currentStock";
        public const string MinimumStock = "minimumStock";
        public const string Visibility = "visibility";
        public const string TieBreaker = "id";
        public const string Default = Name;
    }

    public static readonly FrozenSet<string> AllowedSortFields = new[]
    {
        SortFields.Name,
        SortFields.Status,
        SortFields.Category,
        SortFields.Location,
        SortFields.CurrentStock,
        SortFields.MinimumStock,
        SortFields.Visibility,
        SortFields.TieBreaker,
    }.ToFrozenSet(StringComparer.Ordinal);

    public const string DefaultSortDirection = "asc";

    public static readonly IReadOnlyList<int> PageSizeOptions = [10, 25, 50, 100];
}

internal static class InventoryOrderQuery
{
    public static class Parameters
    {
        public const string Search = "search";
        public const string Supplier = "supplier";
        public const string Status = "status";
        public const string Currency = "currency";
        public const string Visibility = "visibility";
        public const string Creator = "creator";
        public const string Page = "page";
        public const string PageSize = "pageSize";
        public const string Sort = "sort";
        public const string SortDirection = "sortDirection";
    }

    public static class SortFields
    {
        public const string Supplier = "supplier";
        public const string Status = "status";
        public const string OrderDate = "orderDate";
        public const string ExpectedReceiptDate = "expectedReceiptDate";
        public const string Currency = "currency";
        public const string Visibility = "visibility";
        public const string TieBreaker = "id";
        public const string Default = OrderDate;
    }

    public static readonly FrozenSet<string> AllowedSortFields = new[]
    {
        SortFields.Supplier,
        SortFields.Status,
        SortFields.OrderDate,
        SortFields.ExpectedReceiptDate,
        SortFields.Currency,
        SortFields.Visibility,
        SortFields.TieBreaker,
    }.ToFrozenSet(StringComparer.Ordinal);

    public const string DefaultSortDirection = "desc";

    public static readonly IReadOnlyList<int> PageSizeOptions = [10, 25, 50, 100];
}
