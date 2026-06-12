using Microsoft.EntityFrameworkCore;
using Segaris.Persistence;
using Segaris.Shared.Time;

namespace Segaris.Api.Platform.Jobs;

/// <summary>
/// The single-instance background worker. It recovers interrupted jobs at startup, then
/// claims queued jobs one at a time through an atomic database transition, runs each
/// handler in its own dependency-injection scope, and records the terminal state. PostgreSQL
/// owns claiming so a queued job cannot be claimed twice even if the topology changes later.
/// </summary>
internal sealed class JobWorker(
    IServiceScopeFactory scopeFactory,
    JobCoordinator coordinator,
    JobTypeRegistry registry,
    IClock clock,
    ILogger<JobWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverInterruptedJobsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await coordinator.WaitForWorkAsync(PollInterval, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var claimed = await ClaimNextAsync(stoppingToken);
                if (claimed is null)
                {
                    break;
                }

                await RunAsync(claimed, stoppingToken);
            }
        }
    }

    private async Task RecoverInterruptedJobsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
            var now = clock.UtcNow;
            var recovered = await dbContext.Set<JobRecord>()
                .Where(job => job.State == JobState.Running
                    || job.State == JobState.CancellationRequested)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(job => job.State, JobState.Interrupted)
                        .SetProperty(job => job.ActiveExclusivityKey, (string?)null)
                        .SetProperty(job => job.FailureCode, "interrupted")
                        .SetProperty(job => job.CompletedAt, now)
                        .SetProperty(job => job.UpdatedAt, now),
                    cancellationToken);
            if (recovered > 0)
            {
                logger.LogWarning(
                    "Marked {Count} job(s) left running by a previous process as interrupted.",
                    recovered);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Interrupted-job recovery failed at startup.");
        }
    }

    private async Task<ClaimedJob?> ClaimNextAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();

        while (!cancellationToken.IsCancellationRequested)
        {
            var next = await dbContext.Set<JobRecord>()
                .AsNoTracking()
                .Where(job => job.State == JobState.Queued)
                .OrderBy(job => job.Id)
                .Select(job => new
                {
                    job.Id,
                    job.JobType,
                    job.Parameters,
                    job.CancellationRequested,
                })
                .FirstOrDefaultAsync(cancellationToken);
            if (next is null)
            {
                return null;
            }

            var now = clock.UtcNow;
            if (next.CancellationRequested)
            {
                await dbContext.Set<JobRecord>()
                    .Where(job => job.Id == next.Id && job.State == JobState.Queued)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(job => job.State, JobState.Cancelled)
                            .SetProperty(job => job.ActiveExclusivityKey, (string?)null)
                            .SetProperty(job => job.CompletedAt, now)
                            .SetProperty(job => job.UpdatedAt, now),
                        cancellationToken);
                continue;
            }

            var claimed = await dbContext.Set<JobRecord>()
                .Where(job => job.Id == next.Id && job.State == JobState.Queued)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(job => job.State, JobState.Running)
                        .SetProperty(job => job.StartedAt, now)
                        .SetProperty(job => job.UpdatedAt, now),
                    cancellationToken);
            if (claimed == 1)
            {
                return new ClaimedJob(next.Id, next.JobType, next.Parameters);
            }
        }

        return null;
    }

    private async Task RunAsync(ClaimedJob job, CancellationToken stoppingToken)
    {
        var registration = registry.Get(job.JobType);
        using var cancellation = coordinator.TrackRunning(job.Id, stoppingToken);
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var handler = (IJobHandler)scope.ServiceProvider.GetRequiredService(registration.HandlerType);
            var context = new JobExecutionContext(job.Id, job.JobType, job.Parameters, ReportProgressAsync);
            var result = await handler.ExecuteAsync(context, cancellation.Token);
            await CompleteAsync(job.Id, JobState.Succeeded, result, failureCode: null);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Graceful shutdown. Leave the record running; startup recovery in the next
            // process marks it interrupted so partial output is never reported as success.
            logger.LogInformation("Job {JobId} was interrupted by backend shutdown.", job.Id);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Job {JobId} stopped after a cancellation request.", job.Id);
            await CompleteAsync(job.Id, JobState.Cancelled, JobResult.None, failureCode: null);
        }
        catch (JobFailureException failure)
        {
            logger.LogError(
                failure,
                "Job {JobId} of type {JobType} failed with code {FailureCode}.",
                job.Id,
                job.JobType,
                failure.FailureCode);
            await CompleteAsync(job.Id, JobState.Failed, JobResult.None, failure.FailureCode);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Job {JobId} of type {JobType} failed.", job.Id, job.JobType);
            await CompleteAsync(job.Id, JobState.Failed, JobResult.None, failureCode: "unexpected");
        }
        finally
        {
            coordinator.Untrack(job.Id);
        }
    }

    private async Task CompleteAsync(int id, JobState state, JobResult result, string? failureCode)
    {
        // Completion bookkeeping must finish even during shutdown, so it does not observe
        // the stopping token.
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
            var record = await dbContext.Set<JobRecord>().SingleOrDefaultAsync(job => job.Id == id);
            if (record is null)
            {
                return;
            }

            JobStateMachine.EnsureCanTransition(record.State, state);
            var now = clock.UtcNow;
            record.State = state;
            record.ActiveExclusivityKey = null;
            record.ResultReference = result.ResultReference;
            record.ResultCode = result.ResultCode;
            record.FailureCode = failureCode;
            record.CompletedAt = now;
            record.UpdatedAt = now;
            if (state == JobState.Succeeded)
            {
                record.Progress = 100;
            }

            await dbContext.SaveChangesAsync();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Recording the terminal state for job {JobId} failed.", id);
        }
    }

    private async Task ReportProgressAsync(int id, int percent, string? code, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
            var now = clock.UtcNow;
            await dbContext.Set<JobRecord>()
                .Where(job => job.Id == id)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(job => job.Progress, percent)
                        .SetProperty(job => job.ProgressCode, code)
                        .SetProperty(job => job.UpdatedAt, now),
                    cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Progress update for job {JobId} could not be persisted.", id);
        }
    }

    private sealed record ClaimedJob(int Id, string JobType, string? Parameters);
}
