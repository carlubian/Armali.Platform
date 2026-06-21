namespace Segaris.Api.Modules.Firebird.Contracts;

internal sealed record PersonCategoryResponse(int Id, string Name, int SortOrder);

internal sealed record UsernamePlatformResponse(int Id, string Name, int SortOrder);

internal sealed record PersonAvatarResponse(
    string? AttachmentId,
    string? Url,
    string Source);

internal sealed record PersonSummaryResponse(
    int Id,
    string Name,
    int CategoryId,
    string CategoryName,
    string Status,
    int? BirthdayMonth,
    int? BirthdayDay,
    string Visibility,
    PersonAvatarResponse Avatar,
    int CreatorId,
    string CreatorName);

internal sealed record UsernameResponse(
    int Id,
    int PlatformId,
    string PlatformName,
    string Handle,
    string? Notes);

internal sealed record InteractionResponse(
    int Id,
    DateOnly Date,
    string Description);

internal sealed record PersonResponse(
    int Id,
    string Name,
    int CategoryId,
    string CategoryName,
    string Status,
    int? BirthdayMonth,
    int? BirthdayDay,
    string? Notes,
    string Visibility,
    PersonAvatarResponse Avatar,
    IReadOnlyList<UsernameResponse> Usernames,
    IReadOnlyList<InteractionResponse> Interactions,
    int CreatedById,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    int? UpdatedById,
    string? UpdatedByName,
    DateTimeOffset? UpdatedAt);
