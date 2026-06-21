namespace Segaris.Api.Modules.Processes.Domain;

/// <summary>
/// The execution state of a single step. A required step may only be
/// <see cref="Pending"/> or <see cref="Completed"/>; only an optional step may enter
/// <see cref="Skipped"/>. Resolved steps (<see cref="Completed"/> or
/// <see cref="Skipped"/>) always form a contiguous prefix of the sequence; see
/// <see cref="ProcessExecution"/> for the frontier and contiguity rules.
/// </summary>
internal enum StepExecutionState
{
    Pending,
    Completed,
    Skipped,
}
