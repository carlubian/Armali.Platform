using Microsoft.Extensions.Options;
using Segaris.Api.Configuration;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Platform.Api;
using Segaris.Api.Platform.Jobs;
using Segaris.Persistence;
using Segaris.Shared.Identity;

namespace Segaris.Api.Platform.Backup;

/// <summary>
/// Administrative endpoints to start a backup and query its job status. Generation runs as a
/// persistent background job; the completed package is written to the configured backups
/// directory and an external household service retrieves it from there. The package is never
/// streamed through this API.
/// </summary>
internal static class BackupEndpoints
{
    public static void MapBackupEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup("backup-jobs", "Backups")
            .RequireAuthorization(IdentityPolicies.Admin);

        group.MapPost("", StartAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Starts a backup package generation job");

        group.MapGet("/{id:int}", GetAsync)
            .WithSummary("Returns the status of a backup job");

        group.MapPost("/{id:int}/cancel", CancelAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Requests cancellation of a backup job");
    }

    private static async Task<IResult> StartAsync(
        JobService jobs,
        ICurrentUser currentUser,
        IOptions<DatabaseOptions> databaseOptions,
        CancellationToken cancellationToken)
    {
        EnsurePostgres(databaseOptions.Value);

        var status = await jobs.EnqueueAsync(
            BackupJobHandler.JobType,
            parameters: null,
            currentUser.UserId,
            cancellationToken);

        return TypedResults.Accepted($"/api/backup-jobs/{status.Id}", status);
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

    private static void EnsurePostgres(DatabaseOptions options)
    {
        var provider = DatabaseProviderParser.Parse(options.Provider!);
        if (provider != DatabaseProvider.Postgres)
        {
            throw JobProblem.Unprocessable(
                "Backup generation requires the PostgreSQL provider.");
        }
    }
}
