using Blackwing.Persistence;
using Blackwing.Persistence.Ingestion;
using Blackwing.Shared.Identity;
using Microsoft.EntityFrameworkCore;

namespace Blackwing.Api.Observability;

/// <summary>
/// Operator-facing observability endpoints. These expose only aggregate operational
/// health — never any user's images, tags, or file names — and are restricted to the
/// Admin role, consistent with admins managing the deployment but never seeing content.
/// </summary>
public static class OpsEndpoints
{
    public static IEndpointRouteBuilder MapOpsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/ops").RequireAuthorization(policy => policy.RequireRole(BlackwingRoles.Admin));
        group.MapGet("/ingestion", IngestionQueue);
        return endpoints;
    }

    /// <summary>
    /// Snapshot of the ingestion queue across the whole deployment: how many jobs sit in
    /// each state and how long the oldest unprocessed job has been waiting. This is the
    /// live counterpart to the <see cref="IngestionMetrics"/> counters — the value an
    /// operator checks to see whether the worker is keeping up.
    /// </summary>
    private static async Task<IResult> IngestionQueue(BlackwingDbContext database, CancellationToken cancellationToken)
    {
        var byStatus = await database.UploadJobs
            .GroupBy(job => job.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        long Count(UploadJobStatus status) => byStatus.FirstOrDefault(entry => entry.Status == status)?.Count ?? 0;

        var oldestPending = await database.UploadJobs
            .Where(job => job.Status == UploadJobStatus.Pending)
            .OrderBy(job => job.CreatedAt)
            .Select(job => (DateTimeOffset?)job.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var oldestPendingAgeSeconds = oldestPending is null
            ? (double?)null
            : Math.Max(0, (DateTimeOffset.UtcNow - oldestPending.Value).TotalSeconds);

        return Results.Ok(new IngestionQueueSnapshot(
            Pending: Count(UploadJobStatus.Pending),
            Processing: Count(UploadJobStatus.Processing),
            Completed: Count(UploadJobStatus.Completed),
            Failed: Count(UploadJobStatus.Failed),
            Duplicate: Count(UploadJobStatus.Duplicate),
            OldestPendingAgeSeconds: oldestPendingAgeSeconds));
    }
}

/// <summary>Aggregate ingestion-queue counts and backlog age. Contains no user content.</summary>
public sealed record IngestionQueueSnapshot(
    long Pending,
    long Processing,
    long Completed,
    long Failed,
    long Duplicate,
    double? OldestPendingAgeSeconds);
