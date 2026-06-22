using Segaris.Api.Modules.Destinations.Contracts;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Api;

namespace Segaris.Api.Modules.Destinations.Queries;

/// <summary>
/// Binds the destination-scoped place list query string. The owning
/// <c>destinationId</c> is supplied by the route, never by the query, so the page
/// can never leave its destination scope.
/// </summary>
internal sealed class PlaceListQuery
{
    public string? Search { get; init; }

    public int? Category { get; init; }

    public int? Rating { get; init; }

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

        if (!PlaceQuery.PageSizeOptions.Contains(pageSize))
        {
            errors["pageSize"] =
            [$"Page size must be one of: {string.Join(", ", PlaceQuery.PageSizeOptions)}."];
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
                SortDirection ?? PlaceQuery.DefaultSortDirection,
                PlaceQuery.AllowedSortFields,
                PlaceQuery.SortFields.Default,
                PlaceQuery.SortFields.TieBreaker);
        }
        catch (ArgumentException exception)
        {
            var field = exception.ParamName == "direction" ? "sortDirection" : "sort";
            throw Problem(field, exception.Message);
        }
    }

    public PlaceFilter ToFilter()
    {
        var search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
        return new PlaceFilter(search, Category, Rating);
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

internal sealed record PlaceFilter(
    string? Search,
    int? CategoryId,
    int? Rating);
