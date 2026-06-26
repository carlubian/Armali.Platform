using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Analytics;

internal static class AnalyticsProblem
{
    public static ApiProblemException YearInvalid() => new(
        StatusCodes.Status400BadRequest,
        AnalyticsErrorCodes.YearInvalid,
        "Analytics year is invalid.",
        errors: new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [AnalyticsApiRoutes.QueryParameters.Year] =
            [
                $"Year must be a four-digit value between {Projection.AnalyticsYearQuery.MinimumYear} and {Projection.AnalyticsYearQuery.MaximumYear}.",
            ],
        });
}
