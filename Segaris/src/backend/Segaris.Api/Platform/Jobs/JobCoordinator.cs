using System.Collections.Concurrent;

namespace Segaris.Api.Platform.Jobs;

/// <summary>
/// In-process coordination between the API and the background worker for the single
/// backend instance. It wakes the worker promptly when a job is enqueued and signals
/// cancellation to a handler that is currently running in this process. It is not a
/// source of truth: PostgreSQL still owns claiming, state, and recovery.
/// </summary>
internal sealed class JobCoordinator : IDisposable
{
    private readonly SemaphoreSlim signal = new(0);
    private readonly ConcurrentDictionary<int, CancellationTokenSource> running = new();

    /// <summary>Wakes the worker because a new job is waiting.</summary>
    public void NotifyEnqueued()
    {
        if (signal.CurrentCount == 0)
        {
            signal.Release();
        }
    }

    /// <summary>Waits for a wake-up signal or until the poll interval elapses.</summary>
    public async Task WaitForWorkAsync(TimeSpan pollInterval, CancellationToken cancellationToken)
    {
        try
        {
            await signal.WaitAsync(pollInterval, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    public CancellationTokenSource TrackRunning(int jobId, CancellationToken linkedToken)
    {
        var source = CancellationTokenSource.CreateLinkedTokenSource(linkedToken);
        running[jobId] = source;
        return source;
    }

    public void Untrack(int jobId)
    {
        if (running.TryRemove(jobId, out var source))
        {
            source.Dispose();
        }
    }

    /// <summary>
    /// Signals cancellation to a job running in this process. Returns false when the job
    /// is not currently executing here, in which case the persisted request flag is the
    /// only mechanism and the worker honors it before or between steps.
    /// </summary>
    public bool TryCancelRunning(int jobId)
    {
        if (running.TryGetValue(jobId, out var source))
        {
            source.Cancel();
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        signal.Dispose();
        foreach (var source in running.Values)
        {
            source.Dispose();
        }

        running.Clear();
    }
}
