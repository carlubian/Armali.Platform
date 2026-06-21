namespace Segaris.Api.Modules.Processes.Domain;

/// <summary>
/// The frozen Processes execution contract: the wire status vocabulary plus the
/// documented frontier, contiguity, and derived-status rules that govern sequential
/// execution. Wave 0 freezes this contract; the pure, unit-testable functions that
/// implement the derivation and the invariants are added in Wave 1. The rules are
/// enforced by the backend regardless of the client.
/// </summary>
/// <remarks>
/// <para>
/// <b>Frontier.</b> The frontier is the first step that is neither
/// <see cref="StepExecutionState.Completed"/> nor <see cref="StepExecutionState.Skipped"/>.
/// Only the frontier step may be completed, and only an optional frontier step may be
/// skipped. Only the most recently resolved step (the last resolved step before the
/// frontier) may be undone, returning it to <see cref="StepExecutionState.Pending"/>.
/// </para>
/// <para>
/// <b>Contiguity invariant.</b> The resolved steps always form a contiguous prefix at
/// the front of the sequence. A new or pending step may not be inserted inside the
/// resolved prefix; it must be placed at or after the frontier. Reordering within the
/// resolved prefix or within the pending tail is allowed, and removing a resolved step
/// shrinks the prefix. Any arrangement that would leave a resolved step after a pending
/// step is rejected.
/// </para>
/// <para>
/// <b>Derived status.</b> <see cref="ProcessDerivedStatus.NotStarted"/> when no step is
/// resolved (including an empty process), <see cref="ProcessDerivedStatus.InProgress"/>
/// when at least one step is resolved but not every required step is completed, and
/// <see cref="ProcessDerivedStatus.Completed"/> when the frontier has reached the end of
/// a non-empty sequence. The manual terminal <see cref="CancelledStatusName"/> override
/// takes precedence over the derived value, removes the process from launcher attention,
/// and is cleared explicitly to return the process to its derived status.
/// </para>
/// </remarks>
internal static class ProcessExecution
{
    /// <summary>The wire name of the manual terminal override; it is not a derived value.</summary>
    public const string CancelledStatusName = "Cancelled";

    /// <summary>
    /// The full frozen status vocabulary surfaced on the wire: the three derived values
    /// (<see cref="ProcessDerivedStatus"/>) followed by the <see cref="CancelledStatusName"/>
    /// override. Used to allow-list the table status filter.
    /// </summary>
    public static readonly IReadOnlyList<string> StatusNames =
    [
        nameof(ProcessDerivedStatus.NotStarted),
        nameof(ProcessDerivedStatus.InProgress),
        nameof(ProcessDerivedStatus.Completed),
        CancelledStatusName,
    ];
}
