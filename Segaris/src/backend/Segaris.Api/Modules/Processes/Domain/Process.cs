using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Processes.Domain;

/// <summary>The editable own fields of a process, independent of its steps and audit metadata.</summary>
/// <remarks>
/// The derived status and the <c>Cancelled</c> override are intentionally absent: the
/// status is computed from the steps and never accepted from the client, and the override
/// is toggled through the dedicated cancel/reopen actions rather than a field update.
/// </remarks>
internal sealed record ProcessValues(
    string? Name,
    int CategoryId,
    DateOnly? DueDate,
    string? Notes,
    RecordVisibility Visibility);

/// <summary>
/// A sequential procedure the household carries out step by step in order. It owns its
/// required <see cref="ProcessCategory"/> reference, an optional global due date, optional
/// notes, the manual terminal <see cref="IsCancelled"/> override, its visibility, and an
/// ordered list of steps (stored as separate <see cref="Step"/> rows). The status is
/// derived from the steps and is never stored; only the <see cref="IsCancelled"/> override
/// is persisted.
/// </summary>
internal sealed class Process
{
    private Process()
    {
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int CategoryId { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public string? Notes { get; private set; }
    public bool IsCancelled { get; private set; }
    public RecordVisibility Visibility { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int UpdatedBy { get; private set; }

    public static Process Create(ProcessValues values, UserId creatorId, DateTimeOffset now)
    {
        ProcessesValidation.EnsureUtc(now);
        var process = new Process
        {
            CreatedAt = now,
            CreatedBy = creatorId.Value,
            UpdatedAt = now,
            UpdatedBy = creatorId.Value,
        };
        process.Apply(values, creatorId, now, isCreation: true);
        return process;
    }

    public void Update(ProcessValues values, UserId actorId, DateTimeOffset now) =>
        Apply(values, actorId, now, isCreation: false);

    /// <summary>Sets the manual terminal override; it takes precedence over the derived status.</summary>
    public void Cancel(UserId actorId, DateTimeOffset now)
    {
        ProcessesValidation.EnsureUtc(now);
        IsCancelled = true;
        StampModification(actorId, now);
    }

    /// <summary>Clears the override, returning the process to its derived status.</summary>
    public void Reopen(UserId actorId, DateTimeOffset now)
    {
        ProcessesValidation.EnsureUtc(now);
        IsCancelled = false;
        StampModification(actorId, now);
    }

    /// <summary>Re-points the required category during a Configuration migration.</summary>
    internal void ReplaceCategory(int categoryId, UserId actorId, DateTimeOffset now)
    {
        ProcessesValidation.EnsureUtc(now);
        ProcessesValidation.EnsurePositiveIdentifier(categoryId, "Category identifier");
        CategoryId = categoryId;
        StampModification(actorId, now);
    }

    private void Apply(ProcessValues values, UserId actorId, DateTimeOffset now, bool isCreation)
    {
        ArgumentNullException.ThrowIfNull(values);
        ProcessesValidation.EnsureUtc(now);

        var name = ProcessesValidation.ValidateName(values.Name);
        var notes = ProcessesValidation.ValidateNotes(values.Notes);
        ProcessesValidation.EnsureKnownVisibility(values.Visibility);
        ProcessesValidation.EnsurePositiveIdentifier(values.CategoryId, "Category identifier");

        // Public records collaborate (any user may edit) but only the creator may change
        // a process's visibility, mirroring the platform visibility baseline.
        if (!isCreation && values.Visibility != Visibility && actorId.Value != CreatedBy)
        {
            throw new ProcessesValidationException(
                "Only the creator may change process visibility.",
                ProcessesValidationReason.VisibilityForbidden);
        }

        Name = name;
        CategoryId = values.CategoryId;
        DueDate = values.DueDate;
        Notes = notes;
        Visibility = values.Visibility;
        StampModification(actorId, now);
    }

    private void StampModification(UserId actorId, DateTimeOffset now)
    {
        UpdatedAt = now;
        UpdatedBy = actorId.Value;
    }
}
