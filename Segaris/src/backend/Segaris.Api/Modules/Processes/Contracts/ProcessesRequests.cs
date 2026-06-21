namespace Segaris.Api.Modules.Processes.Contracts;

/// <summary>
/// Frozen request contract for <c>POST /api/processes</c>. <see cref="Visibility"/> is
/// the fixed platform visibility vocabulary. <see cref="CategoryId"/> is the required
/// database-assigned category identifier. <see cref="DueDate"/> is the optional global
/// civil due date with no past/future boundary. The status is system-derived from the
/// steps and is never accepted from the client; the terminal <c>Cancelled</c> override
/// is toggled through the dedicated cancel/reopen routes. A process may be created with
/// zero steps.
/// </summary>
internal sealed record CreateProcessRequest(
    string? Name,
    int CategoryId,
    DateOnly? DueDate,
    string? Notes,
    string? Visibility);

/// <summary>
/// Frozen request contract for <c>PUT /api/processes/{processId}</c>. The update fully
/// replaces the process's own editable fields in one transaction. It does not touch the
/// step list (restructured through the steps route) nor the <c>Cancelled</c> override.
/// </summary>
internal sealed record UpdateProcessRequest(
    string? Name,
    int CategoryId,
    DateOnly? DueDate,
    string? Notes,
    string? Visibility);

/// <summary>
/// A single entry in a step-list restructure. <see cref="Id"/> identifies an existing
/// step whose execution state is preserved across the restructure; it is
/// <see langword="null"/> for a newly added step (which starts <c>Pending</c>). The
/// item's position in <see cref="UpdateStepListRequest.Steps"/> defines its
/// <c>SortOrder</c>. The execution state is never accepted from the client — it is
/// preserved by step identity and re-validated against the contiguity invariant.
/// </summary>
internal sealed record StepListItemRequest(
    int? Id,
    string? Description,
    DateOnly? DueDate,
    string? Notes,
    bool IsOptional);

/// <summary>
/// Frozen request contract for <c>PUT /api/processes/{processId}/steps</c>: the full
/// collection of steps in their intended order. The restructure adds, removes,
/// reorders, renames, and changes due date, notes, and the optional flag while
/// preserving each surviving step's execution state by identity.
/// </summary>
internal sealed record UpdateStepListRequest(
    IReadOnlyList<StepListItemRequest> Steps);

/// <summary>
/// Frozen administrative request contract for the module-owned <c>ProcessCategory</c>
/// catalogue, presented through Configuration.
/// </summary>
internal sealed record ProcessCategoryRequest(string? Name);
