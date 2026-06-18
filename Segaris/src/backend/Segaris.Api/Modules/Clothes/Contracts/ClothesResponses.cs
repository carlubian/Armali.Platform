namespace Segaris.Api.Modules.Clothes.Contracts;

internal sealed record ClothingCategoryResponse(int Id, string Name, int SortOrder);

internal sealed record ClothingColorResponse(
    int Id,
    string Name,
    string ColorValue,
    int SortOrder);

internal sealed record ClothesAttachmentResponse(
    string Id,
    string FileName,
    string ContentType,
    long Size,
    int CreatedById,
    DateTimeOffset CreatedAt,
    bool IsPrimary);

internal sealed record ClothesThumbnailResponse(
    string? AttachmentId,
    string? Url,
    string Source);

internal sealed record ClothesGarmentSummaryResponse(
    int Id,
    string Name,
    int CategoryId,
    string CategoryName,
    string Status,
    string? Size,
    IReadOnlyList<ClothingColorResponse> Colors,
    string? WashingCare,
    string? DryingCare,
    string? IroningCare,
    string? DryCleaningCare,
    string Visibility,
    ClothesThumbnailResponse Thumbnail,
    int CreatorId,
    string CreatorName);

internal sealed record ClothesGarmentResponse(
    int Id,
    string Name,
    int CategoryId,
    string CategoryName,
    string Status,
    string? Size,
    IReadOnlyList<ClothingColorResponse> Colors,
    string? WashingCare,
    string? DryingCare,
    string? IroningCare,
    string? DryCleaningCare,
    string? Notes,
    string Visibility,
    ClothesThumbnailResponse Thumbnail,
    IReadOnlyList<ClothesAttachmentResponse> Attachments,
    int CreatedById,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    int? UpdatedById,
    string? UpdatedByName,
    DateTimeOffset? UpdatedAt);
