using Segaris.Api.Platform.Api;
using Segaris.Shared.Api;

namespace Segaris.Api.Modules.Opex.Queries;

/// <summary>
/// Bound query surface for
/// <c>GET /api/opex/contracts/{contractId}/occurrences</c>. Occurrences have a
/// single fixed chronological ordering (effective date ascending, identifier
/// ascending as the stable tie-breaker), so only the pagination inputs are
/// accepted; the conversion validates them and projects them onto the platform
/// pagination primitive.
/// </summary>
internal sealed class OpexOccurrenceListQuery
{
    public int? Page { get; init; }

    public int? PageSize { get; init; }

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
