namespace Segaris.Api.Modules.Health.Contracts;

internal sealed record DiseaseCategoryResponse(int Id, string Name, int SortOrder);

internal sealed record MedicineCategoryResponse(int Id, string Name, int SortOrder);

internal sealed record MedicineThumbnailResponse(
    string? AttachmentId,
    string? Url,
    string Source);

internal sealed record DiseaseSummaryResponse(
    int Id,
    string Name,
    int CategoryId,
    string CategoryName,
    string Visibility,
    int AssociatedMedicineCount,
    int CreatorId,
    string CreatorName);

internal sealed record DiseaseResponse(
    int Id,
    string Name,
    int CategoryId,
    string CategoryName,
    string? Symptoms,
    int? AverageDurationDays,
    string? Notes,
    string Visibility,
    int CreatorId,
    string CreatorName,
    DateTimeOffset CreatedAt,
    int? UpdatedById,
    string? UpdatedByName,
    DateTimeOffset? UpdatedAt);

internal sealed record MedicineSummaryResponse(
    int Id,
    string Name,
    int CategoryId,
    string CategoryName,
    bool RequiresPrescription,
    int? InventoryItemId,
    string? InventoryItemName,
    string Visibility,
    MedicineThumbnailResponse Thumbnail,
    int CreatorId,
    string CreatorName);

internal sealed record MedicineResponse(
    int Id,
    string Name,
    int CategoryId,
    string CategoryName,
    string? Posology,
    bool RequiresPrescription,
    int? InventoryItemId,
    string? InventoryItemName,
    string? Notes,
    string Visibility,
    IReadOnlyList<MedicineAttachmentResponse> Attachments,
    int CreatorId,
    string CreatorName,
    DateTimeOffset CreatedAt,
    int? UpdatedById,
    string? UpdatedByName,
    DateTimeOffset? UpdatedAt);

internal sealed record MedicineAttachmentResponse(
    string Id,
    string FileName,
    string ContentType,
    long Size,
    int CreatedById,
    DateTimeOffset CreatedAt,
    bool IsPrimary);
