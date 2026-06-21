namespace Segaris.Api.Modules.Processes.Domain;

/// <summary>
/// The derived process status computed by the backend from a process's steps. It is
/// never accepted from the client. The manual terminal <c>Cancelled</c> override is
/// stored separately as a flag and takes precedence over this derived value; see
/// <see cref="ProcessExecution"/> for the full wire vocabulary and the derivation rule.
/// </summary>
internal enum ProcessDerivedStatus
{
    /// <summary>No step is resolved (the frontier is the first step, or there are no steps).</summary>
    NotStarted,

    /// <summary>At least one step is resolved but not every required step is yet completed.</summary>
    InProgress,

    /// <summary>
    /// Every required step is completed and every optional step is completed or skipped
    /// (the frontier has reached the end of a non-empty sequence).
    /// </summary>
    Completed,
}
