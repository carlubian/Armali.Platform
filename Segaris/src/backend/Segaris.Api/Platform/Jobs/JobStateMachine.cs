namespace Segaris.Api.Platform.Jobs;

/// <summary>
/// Validates job lifecycle transitions centrally so handlers and the worker cannot move
/// a job into an impossible state.
/// </summary>
internal static class JobStateMachine
{
    private static readonly Dictionary<JobState, JobState[]> AllowedTransitions = new()
    {
        [JobState.Queued] =
        [
            JobState.Running,
            JobState.CancellationRequested,
            JobState.Cancelled,
            JobState.Interrupted,
        ],
        [JobState.Running] =
        [
            JobState.Succeeded,
            JobState.Failed,
            JobState.CancellationRequested,
            JobState.Cancelled,
            JobState.Interrupted,
        ],
        [JobState.CancellationRequested] =
        [
            JobState.Succeeded,
            JobState.Failed,
            JobState.Cancelled,
            JobState.Interrupted,
        ],
        [JobState.Succeeded] = [],
        [JobState.Failed] = [],
        [JobState.Cancelled] = [],
        [JobState.Interrupted] = [],
    };

    public static bool CanTransition(JobState from, JobState to) =>
        AllowedTransitions.TryGetValue(from, out var targets)
        && Array.IndexOf(targets, to) >= 0;

    public static void EnsureCanTransition(JobState from, JobState to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidOperationException(
                $"A job cannot transition from {from} to {to}.");
        }
    }
}
