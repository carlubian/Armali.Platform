namespace Segaris.Api.Modules.Processes.Contracts;

internal sealed record ProcessCategoryResponse(int Id, string Name, int SortOrder);

internal sealed record ProcessAttachmentResponse(
    string Id,
    string FileName,
    string ContentType,
    long Size,
    int CreatedById,
    DateTimeOffset CreatedAt);

/// <summary>
/// Frozen step projection. <see cref="State"/> is one of the
/// <see cref="Domain.StepExecutionState"/> names. Steps carry no attachments and no
/// system-managed completion date in the initial release.
/// </summary>
internal sealed record StepResponse(
    int Id,
    string Description,
    DateOnly? DueDate,
    string? Notes,
    bool IsOptional,
    string State,
    int SortOrder);

/// <summary>
/// Frozen table-row projection. <see cref="Status"/> is the derived status, or
/// <c>Cancelled</c> when the override is set. <see cref="ResolvedStepCount"/> over
/// <see cref="TotalStepCount"/> is the step progress, and <see cref="EffectiveDueDate"/>
/// is the global due date when set, otherwise the next pending step's due date.
/// </summary>
internal sealed record ProcessSummaryResponse(
    int Id,
    string Name,
    int CategoryId,
    string CategoryName,
    string Status,
    bool IsCancelled,
    int ResolvedStepCount,
    int TotalStepCount,
    DateOnly? EffectiveDueDate,
    string Visibility,
    int CreatorId,
    string CreatorName);

/// <summary>
/// Frozen process detail projection. <see cref="Status"/> is system-derived (or
/// <c>Cancelled</c>) and never accepted from the client. <see cref="DueDate"/> is the
/// global due date; <see cref="EffectiveDueDate"/> is the value used for sorting and
/// attention. <see cref="NextPendingStepId"/> is the frontier step, or
/// <see langword="null"/> when the process has no pending step. Process attachments have
/// no primary image.
/// </summary>
internal sealed record ProcessResponse(
    int Id,
    string Name,
    int CategoryId,
    string CategoryName,
    string Status,
    bool IsCancelled,
    DateOnly? DueDate,
    DateOnly? EffectiveDueDate,
    string? Notes,
    int ResolvedStepCount,
    int TotalStepCount,
    int? NextPendingStepId,
    string Visibility,
    IReadOnlyList<StepResponse> Steps,
    IReadOnlyList<ProcessAttachmentResponse> Attachments,
    int CreatedById,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    int? UpdatedById,
    string? UpdatedByName,
    DateTimeOffset? UpdatedAt);
