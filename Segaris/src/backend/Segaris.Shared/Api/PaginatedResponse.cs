namespace Segaris.Shared.Api;

public sealed record PaginatedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public static PaginatedResponse<T> Create(
        IReadOnlyList<T> items,
        PaginationRequest pagination,
        int totalCount)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);

        return new(items, pagination.Page, pagination.PageSize, totalCount);
    }
}
