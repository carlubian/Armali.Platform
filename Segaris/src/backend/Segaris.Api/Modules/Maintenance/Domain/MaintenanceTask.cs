using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Maintenance.Domain;

/// <summary>The editable fields of a maintenance task, independent of audit metadata.</summary>
/// <remarks>
/// <see cref="CompletedDate"/> is intentionally absent: it is system-managed and is
/// derived from the status transition rather than accepted from the client.
/// </remarks>
internal sealed record MaintenanceTaskValues(
    string Title,
    int MaintenanceTypeId,
    MaintenanceStatus Status,
    MaintenancePriority Priority,
    DateOnly? DueDate,
    string? Notes,
    int? AssetId,
    RecordVisibility Visibility);

/// <summary>
/// A single repair or maintenance task. It owns its required <see cref="MaintenanceType"/>
/// reference, its descriptive status and priority, an optional due date, optional notes,
/// an optional opaque <see cref="AssetId"/> reference (resolved live through the Assets
/// read contract, never a foreign key), its visibility, and a system-managed
/// <see cref="CompletedDate"/>. Entering <see cref="MaintenanceStatus.Completed"/> sets
/// the completion date to the household civil date; leaving it clears the completion
/// date. The task carries no cost, recurrence, labour, or parts.
/// </summary>
internal sealed class MaintenanceTask
{
    private MaintenanceTask()
    {
    }

    public int Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public int MaintenanceTypeId { get; private set; }
    public MaintenanceStatus Status { get; private set; }
    public MaintenancePriority Priority { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DateOnly? CompletedDate { get; private set; }
    public string? Notes { get; private set; }
    public int? AssetId { get; private set; }
    public RecordVisibility Visibility { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int UpdatedBy { get; private set; }

    /// <param name="now">The UTC technical timestamp recorded in the audit metadata.</param>
    /// <param name="today">The household civil date (<c>Europe/Madrid</c>) used to stamp the completion date.</param>
    public static MaintenanceTask Create(MaintenanceTaskValues values, UserId creatorId, DateTimeOffset now, DateOnly today)
    {
        EnsureUtc(now);
        var task = new MaintenanceTask
        {
            CreatedAt = now,
            CreatedBy = creatorId.Value,
            UpdatedAt = now,
            UpdatedBy = creatorId.Value,
        };
        task.Apply(values, creatorId, now, today, isCreation: true);
        return task;
    }

    public void Update(MaintenanceTaskValues values, UserId actorId, DateTimeOffset now, DateOnly today)
    {
        Apply(values, actorId, now, today, isCreation: false);
    }

    /// <summary>Re-points the required type during a Configuration migration.</summary>
    internal void ReplaceType(int maintenanceTypeId, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (maintenanceTypeId <= 0)
        {
            throw new MaintenanceValidationException("Catalogue identifiers must be positive.");
        }

        MaintenanceTypeId = maintenanceTypeId;
        StampModification(actorId, now);
    }

    /// <summary>Re-points the optional live asset reference during Assets deletion.</summary>
    internal void ReplaceAsset(int assetId, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (assetId <= 0)
        {
            throw new MaintenanceValidationException("Asset identifiers must be positive.");
        }

        AssetId = assetId;
        StampModification(actorId, now);
    }

    private void Apply(MaintenanceTaskValues values, UserId actorId, DateTimeOffset now, DateOnly today, bool isCreation)
    {
        ArgumentNullException.ThrowIfNull(values);
        EnsureUtc(now);

        var title = MaintenanceValidation.ValidateTitle(values.Title);
        var notes = MaintenanceValidation.ValidateNotes(values.Notes);
        if (!Enum.IsDefined(values.Status) || !Enum.IsDefined(values.Priority) || !Enum.IsDefined(values.Visibility))
        {
            throw new MaintenanceValidationException("Status, priority, or visibility is invalid.");
        }

        if (values.MaintenanceTypeId <= 0)
        {
            throw new MaintenanceValidationException("Catalogue identifiers must be positive.");
        }

        if (values.AssetId is <= 0)
        {
            throw new MaintenanceValidationException("Asset identifiers must be positive.");
        }

        // Public records collaborate (any user may edit) but only the creator may
        // change a task's visibility, mirroring the platform visibility baseline.
        if (!isCreation && values.Visibility != Visibility && actorId.Value != CreatedBy)
        {
            throw new MaintenanceValidationException(
                "Only the creator may change task visibility.",
                MaintenanceValidationReason.VisibilityForbidden);
        }

        var wasCompleted = !isCreation && Status == MaintenanceStatus.Completed;

        Title = title;
        MaintenanceTypeId = values.MaintenanceTypeId;
        Status = values.Status;
        Priority = values.Priority;
        DueDate = values.DueDate;
        Notes = notes;
        AssetId = values.AssetId;
        Visibility = values.Visibility;

        // The completion date is system-managed: it is stamped when a task enters
        // Completed, preserved while it stays Completed, and cleared otherwise.
        if (Status == MaintenanceStatus.Completed)
        {
            if (!wasCompleted)
            {
                CompletedDate = today;
            }
        }
        else
        {
            CompletedDate = null;
        }

        StampModification(actorId, now);
    }

    private void StampModification(UserId actorId, DateTimeOffset now)
    {
        UpdatedAt = now;
        UpdatedBy = actorId.Value;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new MaintenanceValidationException("Technical timestamps must use UTC.");
        }
    }
}
