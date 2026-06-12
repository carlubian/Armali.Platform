namespace Segaris.Shared.Api;

public readonly record struct PaginationRequest
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 25;
    public const int MaximumPageSize = 100;

    public PaginationRequest(int page = DefaultPage, int pageSize = DefaultPageSize)
    {
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be at least 1.");
        }

        if (pageSize < 1 || pageSize > MaximumPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                $"Page size must be between 1 and {MaximumPageSize}.");
        }

        Page = page;
        PageSize = pageSize;
    }

    public int Page { get; }

    public int PageSize { get; }

    public int Offset => checked((Page - 1) * PageSize);
}
