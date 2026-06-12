using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Identity;

namespace Segaris.Api.Platform.Jobs;

/// <summary>
/// Testing-only endpoints that drive <see cref="ProbeJobHandler"/> so the generic job
/// lifecycle can be exercised over HTTP without a real domain. Not mapped outside Testing.
/// </summary>
internal static class JobProbeEndpoints
{
    public static void MapJobProbes(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup("platform/jobs", "Job probes")
            .RequireAuthorization();

        group.MapPost("", StartAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Starts a probe job for integration testing");
        group.MapGet("/{id:int}", GetAsync)
            .WithSummary("Returns probe job status for integration testing");
        group.MapPost("/{id:int}/cancel", CancelAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Requests cancellation of a probe job for integration testing");
    }

    private static async Task<IResult> StartAsync(
        StartProbeRequest request,
        JobService jobs,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var status = await jobs.EnqueueAsync(
            ProbeJobHandler.JobType,
            request.Mode,
            currentUser.UserId,
            cancellationToken);
        return TypedResults.Accepted($"/api/platform/jobs/{status.Id}", status);
    }

    private static async Task<IResult> GetAsync(
        int id,
        JobService jobs,
        CancellationToken cancellationToken)
    {
        var status = await jobs.GetAsync(id, cancellationToken);
        return status is null ? throw ApiProblemException.NotFound() : TypedResults.Ok(status);
    }

    private static async Task<IResult> CancelAsync(
        int id,
        JobService jobs,
        CancellationToken cancellationToken)
    {
        var status = await jobs.RequestCancellationAsync(id, cancellationToken);
        return status is null ? throw ApiProblemException.NotFound() : TypedResults.Ok(status);
    }

    internal sealed record StartProbeRequest(string? Mode);
}
