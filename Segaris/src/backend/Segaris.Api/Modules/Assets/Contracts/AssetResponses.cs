namespace Segaris.Api.Modules.Assets.Contracts;

internal sealed record AssetCategoryResponse(int Id, string Name, int SortOrder);

internal sealed record AssetLocationResponse(int Id, string Name, int SortOrder);

internal sealed record AssetAttachmentResponse(
    string Id,
    string FileName,
    string ContentType,
    long Size,
    int CreatedById,
    DateTimeOffset CreatedAt,
    bool IsPrimary);

internal sealed record AssetThumbnailResponse(
    string? AttachmentId,
    string? Url,
    string Source);

internal sealed record AssetSummaryResponse(
    int Id,
    string Name,
    string? Code,
    int CategoryId,
    string CategoryName,
    int LocationId,
    string LocationName,
    string Status,
    DateOnly? ExpectedEndOfLifeDate,
    string Visibility,
    AssetThumbnailResponse Thumbnail,
    int CreatorId,
    string CreatorName);

internal sealed record AssetResponse(
    int Id,
    string Name,
    string? Code,
    int CategoryId,
    string CategoryName,
    int LocationId,
    string LocationName,
    string Status,
    string? BrandModel,
    string? SerialNumber,
    DateOnly? AcquisitionDate,
    DateOnly? ExpectedEndOfLifeDate,
    string? Notes,
    string Visibility,
    AssetThumbnailResponse Thumbnail,
    IReadOnlyList<AssetAttachmentResponse> Attachments,
    int CreatedById,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    int? UpdatedById,
    string? UpdatedByName,
    DateTimeOffset? UpdatedAt);
