namespace Segaris.Api.Modules.Processes.Domain;

/// <summary>
/// A single step reduced to the two fields the sequential-execution rules depend on:
/// its execution <see cref="State"/> and whether it <see cref="IsOptional"/>. The pure
/// frontier, contiguity, and derived-status functions operate over an ordered list of
/// these snapshots so they remain testable without persistence.
/// </summary>
internal readonly record struct StepSnapshot(StepExecutionState State, bool IsOptional);

/// <summary>
/// The frozen Processes execution contract: the wire status vocabulary plus the
/// documented frontier, contiguity, and derived-status rules that govern sequential
/// execution. Wave 0 freezes this contract; Wave 1 adds the pure, unit-testable
/// functions (<see cref="DeriveStatus"/>, <see cref="FrontierIndex"/>, and
/// <see cref="ResolvedFormContiguousPrefix"/>) that implement the derivation and the
/// invariants. The rules are enforced by the backend regardless of the client.
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

    /// <summary>
    /// A step is <em>resolved</em> when it is <see cref="StepExecutionState.Completed"/>
    /// or <see cref="StepExecutionState.Skipped"/>; only an optional step may be skipped.
    /// </summary>
    public static bool IsResolved(StepExecutionState state) =>
        state is StepExecutionState.Completed or StepExecutionState.Skipped;

    /// <summary>
    /// The index of the frontier — the first step that is not resolved — or
    /// <see langword="null"/> when the process is empty or every step is resolved (the
    /// frontier has reached the end of the sequence). The result is only meaningful when
    /// the resolved steps form a contiguous prefix; see
    /// <see cref="ResolvedFormContiguousPrefix"/>.
    /// </summary>
    public static int? FrontierIndex(IReadOnlyList<StepSnapshot> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        for (var index = 0; index < steps.Count; index++)
        {
            if (!IsResolved(steps[index].State))
            {
                return index;
            }
        }

        return null;
    }

    /// <summary>
    /// Derives the process status from its ordered steps:
    /// <see cref="ProcessDerivedStatus.NotStarted"/> when no step is resolved (including
    /// an empty process), <see cref="ProcessDerivedStatus.Completed"/> when the sequence
    /// is non-empty and every step is resolved, and
    /// <see cref="ProcessDerivedStatus.InProgress"/> otherwise. The manual terminal
    /// <see cref="CancelledStatusName"/> override is applied above this derived value and
    /// is not represented here.
    /// </summary>
    public static ProcessDerivedStatus DeriveStatus(IReadOnlyList<StepSnapshot> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        if (steps.Count == 0)
        {
            return ProcessDerivedStatus.NotStarted;
        }

        var frontier = FrontierIndex(steps);
        if (frontier is null)
        {
            return ProcessDerivedStatus.Completed;
        }

        return frontier.Value == 0
            ? ProcessDerivedStatus.NotStarted
            : ProcessDerivedStatus.InProgress;
    }

    /// <summary>
    /// The number of resolved (<see cref="StepExecutionState.Completed"/> or
    /// <see cref="StepExecutionState.Skipped"/>) steps, used for the step-progress
    /// projection (<c>resolved / total</c>).
    /// </summary>
    public static int ResolvedCount(IReadOnlyList<StepSnapshot> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        var count = 0;
        foreach (var step in steps)
        {
            if (IsResolved(step.State))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Confirms the contiguity invariant: the resolved steps form a contiguous prefix at
    /// the front of the sequence, so no resolved step appears after a pending step. A
    /// <see cref="StepExecutionState.Skipped"/> step must also be optional.
    /// </summary>
    public static bool ResolvedFormContiguousPrefix(IReadOnlyList<StepSnapshot> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        var seenPending = false;
        foreach (var step in steps)
        {
            if (step.State == StepExecutionState.Skipped && !step.IsOptional)
            {
                return false;
            }

            if (IsResolved(step.State))
            {
                if (seenPending)
                {
                    return false;
                }
            }
            else
            {
                seenPending = true;
            }
        }

        return true;
    }
}
