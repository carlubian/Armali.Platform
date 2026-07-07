namespace Segaris.Api.Modules.Games.Contracts;

/// <summary>A module-owned catalogue game surfaced through Configuration.</summary>
internal sealed record GameResponse(int Id, string Name, string Platform, int SortOrder);

/// <summary>
/// Derived, never-persisted progress projection. A section or playthrough with no
/// goals exposes zero completed and zero total goals.
/// </summary>
internal sealed record ProgressResponse(int CompletedGoals, int TotalGoals);

/// <summary>Card projection of an accessible playthrough for the collection view.</summary>
internal sealed record PlaythroughSummaryResponse(
    int Id,
    string Name,
    int GameId,
    string GameName,
    string Platform,
    string Status,
    int StartYear,
    int StartMonth,
    IReadOnlyList<string> Tags,
    ProgressResponse Progress,
    string Visibility,
    int CreatorId,
    string CreatorName);

/// <summary>Detail projection of a single accessible playthrough.</summary>
internal sealed record PlaythroughResponse(
    int Id,
    string Name,
    int GameId,
    string GameName,
    string Platform,
    string Status,
    int StartYear,
    int StartMonth,
    IReadOnlyList<string> Tags,
    ProgressResponse Progress,
    string Visibility,
    int CreatorId,
    string CreatorName,
    DateTimeOffset CreatedAt,
    int? UpdatedById,
    string? UpdatedByName,
    DateTimeOffset? UpdatedAt);

/// <summary>A playthrough section with its derived, never-persisted progress.</summary>
internal sealed record SectionResponse(
    int Id,
    string Name,
    string Color,
    int SortOrder,
    ProgressResponse Progress);

/// <summary>A section goal in permanent creation order.</summary>
internal sealed record GoalResponse(
    int Id,
    string Text,
    bool Completed,
    int Position);
