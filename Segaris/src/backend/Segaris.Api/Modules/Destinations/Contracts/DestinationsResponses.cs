namespace Segaris.Api.Modules.Destinations.Contracts;

internal sealed record DestinationCategoryResponse(int Id, string Name, int SortOrder);

internal sealed record PlaceCategoryResponse(int Id, string Name, int SortOrder);

internal sealed record DestinationThumbnailResponse(
    string? AttachmentId,
    string? Url,
    string Source);

internal sealed record DestinationAttachmentResponse(
    string Id,
    string FileName,
    string ContentType,
    long Size,
    int CreatedById,
    DateTimeOffset CreatedAt,
    bool IsPrimary);

internal sealed record DestinationSummaryResponse(
    int Id,
    string Name,
    int CategoryId,
    string CategoryName,
    string? Country,
    bool IsSchengenArea,
    decimal? AveragePlaceRating,
    int RatedPlaceCount,
    string Visibility,
    DestinationThumbnailResponse Thumbnail,
    int CreatorId,
    string CreatorName);

internal sealed record DestinationResponse(
    int Id,
    string Name,
    int CategoryId,
    string CategoryName,
    string? Country,
    string? EntryRequirements,
    bool IsSchengenArea,
    string? Notes,
    decimal? AveragePlaceRating,
    int RatedPlaceCount,
    string Visibility,
    DestinationThumbnailResponse Thumbnail,
    IReadOnlyList<DestinationAttachmentResponse> Attachments,
    int CreatedById,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    int? UpdatedById,
    string? UpdatedByName,
    DateTimeOffset? UpdatedAt);

internal sealed record PlaceSummaryResponse(
    int Id,
    int DestinationId,
    string Name,
    int CategoryId,
    string CategoryName,
    int? Rating,
    string? Review,
    string? Address,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
