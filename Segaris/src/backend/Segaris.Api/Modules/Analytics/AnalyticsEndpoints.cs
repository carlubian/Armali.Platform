using Segaris.Api.Modules.Analytics.Contracts;
using Segaris.Api.Modules.Analytics.Projection;
using Segaris.Api.Modules.Analytics.Queries;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Analytics;

internal static class AnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup(AnalyticsApiRoutes.Analytics, AnalyticsApiRoutes.Tag)
            .RequireAuthorization();

        group.MapGet(AnalyticsApiRoutes.Overview, GetOverviewAsync)
            .WithName("GetAnalyticsOverview")
            .WithSummary("Returns yearly Analytics overview charts and totals")
            .Produces<AnalyticsOverviewResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }

    private static async Task<IResult> GetOverviewAsync(
        HttpRequest request,
        AnalyticsOverviewService overview,
        ICurrentUser currentUser,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        AnalyticsYearQuery query;
        try
        {
            query = AnalyticsYearQuery.Parse(
                request.Query[AnalyticsApiRoutes.QueryParameters.Year].FirstOrDefault(),
                clock);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw AnalyticsProblem.YearInvalid();
        }
        catch (ArgumentException)
        {
            throw AnalyticsProblem.YearInvalid();
        }

        return TypedResults.Ok(await overview.GetOverviewAsync(query, userId, cancellationToken));
    }
}
