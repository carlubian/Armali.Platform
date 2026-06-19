namespace Segaris.Api.Modules.Maintenance.Contracts;

internal sealed record MaintenanceTypeResponse(int Id, string Name, int SortOrder);

internal sealed record MaintenanceTaskAttachmentResponse(
    string Id,
    string FileName,
    string ContentType,
    long Size,
    int CreatedById,
    DateTimeOffset CreatedAt);

/// <summary>
/// Frozen table-row projection. <see cref="AssetId"/> and <see cref="AssetName"/>
/// carry the optional live Assets reference: the name is resolved through the Assets
/// read contract and is <see langword="null"/> when the asset is not resolvable for
/// the viewer, so the client can show a neutral placeholder.
/// </summary>
internal sealed record MaintenanceTaskSummaryResponse(
    int Id,
    string Title,
    int MaintenanceTypeId,
    string MaintenanceTypeName,
    string Status,
    string Priority,
    int? AssetId,
    string? AssetName,
    DateOnly? DueDate,
    string Visibility,
    int CreatorId,
    string CreatorName);

/// <summary>
/// Frozen task detail projection. <see cref="CompletedDate"/> is system-managed and
/// is set only while the task is <c>Completed</c>. Maintenance attachments have no
/// primary image.
/// </summary>
internal sealed record MaintenanceTaskResponse(
    int Id,
    string Title,
    int MaintenanceTypeId,
    string MaintenanceTypeName,
    string Status,
    string Priority,
    int? AssetId,
    string? AssetName,
    DateOnly? DueDate,
    DateOnly? CompletedDate,
    string? Notes,
    string Visibility,
    IReadOnlyList<MaintenanceTaskAttachmentResponse> Attachments,
    int CreatedById,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    int? UpdatedById,
    string? UpdatedByName,
    DateTimeOffset? UpdatedAt);
