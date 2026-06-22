using Segaris.Api.Modules.Destinations.Contracts;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Api;

namespace Segaris.Api.Modules.Destinations.Queries;

internal sealed class DestinationListQuery
{
    public string? Search { get; init; }

    public int? Category { get; init; }

    public bool? IsSchengenArea { get; init; }

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

        if (!DestinationQuery.PageSizeOptions.Contains(pageSize))
        {
            errors["pageSize"] =
            [$"Page size must be one of: {string.Join(", ", DestinationQuery.PageSizeOptions)}."];
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
                SortDirection ?? DestinationQuery.DefaultSortDirection,
                DestinationQuery.AllowedSortFields,
                DestinationQuery.SortFields.Default,
                DestinationQuery.SortFields.TieBreaker);
        }
        catch (ArgumentException exception)
        {
            var field = exception.ParamName == "direction" ? "sortDirection" : "sort";
            throw Problem(field, exception.Message);
        }
    }

    public DestinationFilter ToFilter()
    {
        var search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
        return new DestinationFilter(search, Category, IsSchengenArea);
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

internal sealed record DestinationFilter(
    string? Search,
    int? CategoryId,
    bool? IsSchengenArea);
