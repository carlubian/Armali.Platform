using Segaris.Shared.Api;

namespace Segaris.Api.Platform.Api;

internal sealed class PaginationQuery
{
    public int? Page { get; init; }

    public int? PageSize { get; init; }

    public PaginationRequest ToRequest()
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

        if (errors.Count > 0)
        {
            throw new ApiProblemException(
                StatusCodes.Status400BadRequest,
                ApiErrorCodes.BadRequest,
                "One or more request values are invalid.",
                errors: errors);
        }

        return new PaginationRequest(page, pageSize);
    }

}
