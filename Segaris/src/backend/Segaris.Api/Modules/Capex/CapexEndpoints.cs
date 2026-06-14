using Segaris.Api.Modules.Capex.Contracts;
using Segaris.Api.Modules.Capex.Queries;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Api;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Capex;

/// <summary>
/// Maps the Capex read endpoints introduced in Wave 3: the category catalog, the
/// paginated Entries list, and entry detail. The mutation and attachment routes
/// frozen in <see cref="CapexApiRoutes"/> are added in Wave 4.
/// </summary>
internal static class CapexEndpoints
{
    public static void MapCapexEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup("capex", CapexApiRoutes.Tag)
            .RequireAuthorization();

        group.MapGet("/categories", ListCategoriesAsync)
            .WithName("ListCapexCategories")
            .WithSummary("Returns the Capex category catalog")
            .Produces<IReadOnlyList<CapexCategoryResponse>>();

        group.MapGet("/entries", ListEntriesAsync)
            .WithName("ListCapexEntries")
            .WithSummary("Returns a paginated, filtered, and sorted list of accessible entries")
            .Produces<PaginatedResponse<CapexEntrySummaryResponse>>();

        group.MapGet("/entries/{entryId:int}", GetEntryAsync)
            .WithName("GetCapexEntry")
            .WithSummary("Returns the detail of an accessible entry with its ordered items and attachments")
            .Produces<CapexEntryResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> ListCategoriesAsync(
        CapexReadService read,
        CancellationToken cancellationToken)
    {
        var categories = await read.ListCategoriesAsync(cancellationToken);
        return TypedResults.Ok(categories);
    }

    private static async Task<IResult> ListEntriesAsync(
        [AsParameters] CapexEntryListQuery query,
        CapexReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var pagination = query.ToPagination();
        var sort = query.ToSort();
        var filter = query.ToFilter();

        var result = await read.ListEntriesAsync(filter, pagination, sort, userId, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetEntryAsync(
        int entryId,
        CapexReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var entry = await read.GetEntryAsync(entryId, userId, cancellationToken);
        if (entry is null)
        {
            throw new ApiProblemException(
                StatusCodes.Status404NotFound,
                CapexErrorCodes.EntryNotFound,
                "The requested Capex entry was not found.");
        }

        return TypedResults.Ok(entry);
    }
}
