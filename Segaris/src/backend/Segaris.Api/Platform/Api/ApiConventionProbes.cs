using Microsoft.AspNetCore.Mvc;
using Segaris.Shared.Api;

namespace Segaris.Api.Platform.Api;

internal static class ApiConventionProbes
{
    private static readonly HashSet<string> AllowedSortFields = new(StringComparer.Ordinal)
    {
        "id",
        "name",
    };

    public static void MapApiConventionProbes(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup("platform/conventions", "Platform conventions");

        group.MapGet("/pagination", (
            [AsParameters] PaginationQuery query,
            HttpRequest request) =>
        {
            var pagination = query.ToRequest();
            _ = ParseSort(
                request.Query["sort"].FirstOrDefault(),
                request.Query["sortDirection"].FirstOrDefault());
            return TypedResults.Ok(PaginatedResponse<string>.Create([], pagination, 0));
        })
        .WithSummary("Validates the standard pagination contract");

        group.MapPost("/echo", (ConventionRequest request) =>
            TypedResults.Ok(new ConventionResponse(request.DisplayName)))
            .WithSummary("Validates explicit JSON transport contracts");

        group.MapGet("/hidden", ThrowNotFound)
            .WithSummary("Validates privacy-preserving not-found responses");

        group.MapGet("/unexpected", ThrowUnexpected)
            .WithSummary("Validates safe unexpected-error responses");

        group.MapGet("/cancellation", (CancellationToken cancellationToken) =>
            TypedResults.Ok(new CancellationResponse(cancellationToken.CanBeCanceled)))
            .WithSummary("Validates request cancellation propagation");

        group.MapGet("/problems/{statusCode:int}", ThrowProblem)
            .WithSummary("Validates the standard problem status mapping");
    }

    internal sealed record ConventionRequest(string DisplayName);

    internal sealed record ConventionResponse(string DisplayName);

    internal sealed record CancellationResponse(bool CanBeCanceled);

    private static IResult ThrowNotFound() => throw ApiProblemException.NotFound();

    private static IResult ThrowUnexpected() => throw new InvalidOperationException("Probe failure");

    private static SortRequest ParseSort(string? sort, string? direction)
    {
        try
        {
            return SortRequest.Create(sort, direction, AllowedSortFields, "name", "id");
        }
        catch (ArgumentException exception)
        {
            throw new ApiProblemException(
                StatusCodes.Status400BadRequest,
                ApiErrorCodes.BadRequest,
                "One or more request values are invalid.",
                errors: new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    [exception.ParamName == "direction" ? "sortDirection" : "sort"] =
                        [exception.Message],
                });
        }
    }

    private static IResult ThrowProblem(int statusCode)
    {
        var code = ApiErrorCodes.ForStatus(statusCode);
        if (statusCode is not (
            StatusCodes.Status401Unauthorized
            or StatusCodes.Status403Forbidden
            or StatusCodes.Status409Conflict
            or StatusCodes.Status422UnprocessableEntity
            or StatusCodes.Status503ServiceUnavailable))
        {
            throw new BadHttpRequestException("Unsupported probe status.");
        }

        throw new ApiProblemException(statusCode, code, "Probe problem.");
    }
}
