namespace Segaris.Api.Platform.Jobs;

/// <summary>
/// The shared lifecycle states for a persistent background job. Modules may expose a
/// smaller user-facing vocabulary where some internal states need no separate presentation.
/// </summary>
internal enum JobState
{
    /// <summary>Accepted and waiting for execution.</summary>
    Queued,

    /// <summary>Claimed by the current backend process.</summary>
    Running,

    /// <summary>Completed and any result was published successfully.</summary>
    Succeeded,

    /// <summary>Ended with a handled or unexpected failure.</summary>
    Failed,

    /// <summary>A user or system request has asked a cooperative handler to stop.</summary>
    CancellationRequested,

    /// <summary>Stopped at a safe cancellation boundary.</summary>
    Cancelled,

    /// <summary>Execution was lost because the process stopped before recording completion.</summary>
    Interrupted,
}

internal static class JobStates
{
    /// <summary>
    /// States in which a job still owns its exclusivity key and may still execute or
    /// be executing. A terminal state releases the key.
    /// </summary>
    public static readonly IReadOnlyCollection<JobState> Active =
    [
        JobState.Queued,
        JobState.Running,
        JobState.CancellationRequested,
    ];

    public static bool IsTerminal(JobState state) => state is
        JobState.Succeeded or
        JobState.Failed or
        JobState.Cancelled or
        JobState.Interrupted;
}
