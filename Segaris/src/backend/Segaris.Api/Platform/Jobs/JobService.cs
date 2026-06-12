using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Platform.Jobs;

/// <summary>
/// The application-facing entry point for persistent background jobs. It enqueues work,
/// enforces per-type exclusivity, exposes safe status, and records cooperative cancellation
/// requests. PostgreSQL remains the source of truth for claiming and execution.
/// </summary>
internal sealed class JobService(
    SegarisDbContext dbContext,
    JobTypeRegistry registry,
    JobCoordinator coordinator,
    IClock clock)
{
    public async Task<JobStatus> EnqueueAsync(
        string jobType,
        string? parameters,
        UserId? createdBy,
        CancellationToken cancellationToken)
    {
        var registration = registry.Get(jobType);

        if (registration.ExclusivityKey is not null)
        {
            var active = await ActiveJobIdAsync(registration.ExclusivityKey, cancellationToken);
            if (active is not null)
            {
                throw JobProblem.AlreadyActive(jobType, active.Value);
            }
        }

        var now = clock.UtcNow;
        var record = new JobRecord
        {
            JobType = jobType,
            State = JobState.Queued,
            ActiveExclusivityKey = registration.ExclusivityKey,
            Parameters = parameters,
            CreatedBy = createdBy?.Value,
            TraceId = Activity.Current?.Id,
            CreatedAt = now,
            UpdatedAt = now,
        };

        dbContext.Add(record);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (registration.ExclusivityKey is not null)
        {
            // The unique active-key index is the race-safe backstop for two concurrent
            // starts. Re-read so the conflict is reported with the surviving job.
            var active = await ActiveJobIdAsync(registration.ExclusivityKey, cancellationToken);
            if (active is not null)
            {
                throw JobProblem.AlreadyActive(jobType, active.Value);
            }

            throw;
        }

        coordinator.NotifyEnqueued();
        return JobStatus.FromRecord(record);
    }

    public async Task<JobStatus?> GetAsync(int id, CancellationToken cancellationToken)
    {
        var record = await dbContext.Set<JobRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(job => job.Id == id, cancellationToken);
        return record is null ? null : JobStatus.FromRecord(record);
    }

    /// <summary>
    /// Records a cooperative cancellation request. A queued job is left for the worker to
    /// stop before claiming; a running job is moved to <see cref="JobState.CancellationRequested"/>
    /// and signaled in-process. Returns the resulting status, or null when the job is absent.
    /// </summary>
    public async Task<JobStatus?> RequestCancellationAsync(int id, CancellationToken cancellationToken)
    {
        var record = await dbContext.Set<JobRecord>()
            .SingleOrDefaultAsync(job => job.Id == id, cancellationToken);
        if (record is null)
        {
            return null;
        }

        if (JobStates.IsTerminal(record.State) || record.CancellationRequested)
        {
            return JobStatus.FromRecord(record);
        }

        var now = clock.UtcNow;
        record.CancellationRequested = true;
        record.CancellationRequestedAt = now;
        record.UpdatedAt = now;
        if (record.State == JobState.Running)
        {
            JobStateMachine.EnsureCanTransition(record.State, JobState.CancellationRequested);
            record.State = JobState.CancellationRequested;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        coordinator.TryCancelRunning(id);
        return JobStatus.FromRecord(record);
    }

    private async Task<int?> ActiveJobIdAsync(string exclusivityKey, CancellationToken cancellationToken)
    {
        var id = await dbContext.Set<JobRecord>()
            .AsNoTracking()
            .Where(job => job.ActiveExclusivityKey == exclusivityKey)
            .OrderBy(job => job.Id)
            .Select(job => (int?)job.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return id;
    }
}
