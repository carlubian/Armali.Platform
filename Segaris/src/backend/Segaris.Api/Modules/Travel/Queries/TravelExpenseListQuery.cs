using Segaris.Api.Modules.Travel.Contracts;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Api;

namespace Segaris.Api.Modules.Travel.Queries;

internal sealed class TravelExpenseListQuery
{
    public string? Search { get; init; }

    public int? Category { get; init; }

    public int? Currency { get; init; }

    public int? Supplier { get; init; }

    public int? CostCenter { get; init; }

    public int? Page { get; init; }

    public int? PageSize { get; init; }

    public string? Sort { get; init; }

    public string? SortDirection { get; init; }

    public PaginationRequest ToPagination()
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var page = Page ?? PaginationRequest.DefaultPage;
        var pageSize = PageSize ?? PaginationRequest.DefaultPageSize;

        if (page < 1)
        {
            errors["page"] = ["Page must be at least 1."];
        }

        if (pageSize < 1 || pageSize > PaginationRequest.MaximumPageSize)
        {
            errors["pageSize"] =
            [$"Page size must be between 1 and {PaginationRequest.MaximumPageSize}."];
        }

        Throw(errors);
        return new PaginationRequest(page, pageSize);
    }

    public SortRequest ToSort()
    {
        try
        {
            return SortRequest.Create(
                Sort,
                SortDirection ?? TravelExpenseQuery.DefaultSortDirection,
                TravelExpenseQuery.AllowedSortFields,
                TravelExpenseQuery.SortFields.Default,
                TravelExpenseQuery.SortFields.TieBreaker);
        }
        catch (ArgumentException exception)
        {
            var field = exception.ParamName == "direction" ? "sortDirection" : "sort";
            throw Problem(field, exception.Message);
        }
    }

    public TravelExpenseFilter ToFilter()
    {
        var search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
        return new TravelExpenseFilter(search, Category, Currency, Supplier, CostCenter);
    }

    private static void Throw(Dictionary<string, string[]> errors)
    {
        if (errors.Count > 0)
        {
            throw new ApiProblemException(
                StatusCodes.Status400BadRequest,
                ApiErrorCodes.BadRequest,
                "One or more request values are invalid.",
                errors: errors);
        }
    }

    private static ApiProblemException Problem(string field, string message) => new(
        StatusCodes.Status400BadRequest,
        ApiErrorCodes.BadRequest,
        "One or more request values are invalid.",
        errors: new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [field] = [message],
        });
}

internal sealed record TravelExpenseFilter(
    string? Search,
    int? CategoryId,
    int? CurrencyId,
    int? SupplierId,
    int? CostCenterId);
