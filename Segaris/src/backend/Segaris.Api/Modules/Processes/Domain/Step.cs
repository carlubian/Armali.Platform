using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Processes.Domain;

/// <summary>The editable fields of a step, independent of its execution state and audit metadata.</summary>
/// <remarks>
/// The execution state is intentionally absent: it is preserved by step identity across a
/// restructure and advanced only through the frontier actions, never set directly from a
/// field update.
/// </remarks>
internal sealed record StepValues(
    string? Description,
    DateOnly? DueDate,
    string? Notes,
    bool IsOptional);

/// <summary>
/// A single step of a <see cref="Process"/>, executed strictly in <see cref="SortOrder"/>
/// sequence. It carries a required description, an optional civil due date, optional notes,
/// the <see cref="IsOptional"/> skippable flag, and its <see cref="State"/>. A new step
/// starts <see cref="StepExecutionState.Pending"/>; only an optional step may be skipped.
/// Steps inherit their owning process's visibility and authorization and carry no
/// attachments and no system-managed completion date in the initial release.
/// </summary>
internal sealed class Step
{
    private Step()
    {
    }

    public int Id { get; private set; }
    public int ProcessId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateOnly? DueDate { get; private set; }
    public string? Notes { get; private set; }
    public bool IsOptional { get; private set; }
    public StepExecutionState State { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int UpdatedBy { get; private set; }

    /// <summary>The two fields the sequential-execution rules depend on.</summary>
    public StepSnapshot Snapshot => new(State, IsOptional);

    public static Step Create(int processId, StepValues values, int sortOrder, UserId creatorId, DateTimeOffset now)
    {
        ProcessesValidation.EnsureUtc(now);
        ProcessesValidation.EnsurePositiveIdentifier(processId, "Process identifier");
        var step = new Step
        {
            ProcessId = processId,
            State = StepExecutionState.Pending,
            SortOrder = sortOrder,
            CreatedAt = now,
            CreatedBy = creatorId.Value,
            UpdatedAt = now,
            UpdatedBy = creatorId.Value,
        };
        step.Apply(values, sortOrder, creatorId, now);
        return step;
    }

    /// <summary>Updates the editable fields and position during a restructure, preserving the execution state.</summary>
    public void Update(StepValues values, int sortOrder, UserId actorId, DateTimeOffset now) =>
        Apply(values, sortOrder, actorId, now);

    public void Reposition(int sortOrder, UserId actorId, DateTimeOffset now)
    {
        ProcessesValidation.EnsureUtc(now);
        SortOrder = sortOrder;
        StampModification(actorId, now);
    }

    /// <summary>Marks the step <see cref="StepExecutionState.Completed"/>. Frontier order is enforced by the caller.</summary>
    public void Complete(UserId actorId, DateTimeOffset now) =>
        Transition(StepExecutionState.Completed, actorId, now);

    /// <summary>Marks the step <see cref="StepExecutionState.Skipped"/>; only valid for an optional step.</summary>
    public void Skip(UserId actorId, DateTimeOffset now) =>
        Transition(StepExecutionState.Skipped, actorId, now);

    /// <summary>Returns a resolved step to <see cref="StepExecutionState.Pending"/>.</summary>
    public void Reopen(UserId actorId, DateTimeOffset now) =>
        Transition(StepExecutionState.Pending, actorId, now);

    private void Transition(StepExecutionState state, UserId actorId, DateTimeOffset now)
    {
        ProcessesValidation.EnsureUtc(now);
        if (state == StepExecutionState.Skipped && !IsOptional)
        {
            throw new ProcessesValidationException(
                "Only an optional step may be skipped.",
                ProcessesValidationReason.StepNotOptional);
        }

        State = state;
        StampModification(actorId, now);
    }

    private void Apply(StepValues values, int sortOrder, UserId actorId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(values);
        ProcessesValidation.EnsureUtc(now);

        var description = ProcessesValidation.ValidateStepDescription(values.Description);
        var notes = ProcessesValidation.ValidateStepNotes(values.Notes);

        // A skipped step that loses its optional flag would violate the execution-state
        // invariant, so clearing the flag is rejected while the step is skipped.
        if (State == StepExecutionState.Skipped && !values.IsOptional)
        {
            throw new ProcessesValidationException(
                "A skipped step must remain optional.",
                ProcessesValidationReason.StepNotOptional);
        }

        Description = description;
        DueDate = values.DueDate;
        Notes = notes;
        IsOptional = values.IsOptional;
        SortOrder = sortOrder;
        StampModification(actorId, now);
    }

    private void StampModification(UserId actorId, DateTimeOffset now)
    {
        UpdatedAt = now;
        UpdatedBy = actorId.Value;
    }
}
