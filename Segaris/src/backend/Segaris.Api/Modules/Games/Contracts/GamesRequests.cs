namespace Segaris.Api.Modules.Games.Contracts;

// Game catalogue. A game carries a required name and a fixed platform, so the
// module-owned catalogue does not reuse the shared name-only catalogue request.
internal sealed record CreateGameRequest(
    string? Name,
    string? Platform);

internal sealed record UpdateGameRequest(
    string? Name,
    string? Platform);

// Playthrough. Start month and year are stored as two integers, never a synthetic
// date. Tags are free-text and normalized on save.
internal sealed record CreatePlaythroughRequest(
    string? Name,
    int GameId,
    int? StartYear,
    int? StartMonth,
    string? Status,
    IReadOnlyList<string>? Tags,
    string? Visibility);

internal sealed record UpdatePlaythroughRequest(
    string? Name,
    int GameId,
    int? StartYear,
    int? StartMonth,
    string? Status,
    IReadOnlyList<string>? Tags,
    string? Visibility);

// Section. A section carries a required name and a fixed palette colour token.
internal sealed record CreateSectionRequest(
    string? Name,
    string? Color);

internal sealed record UpdateSectionRequest(
    string? Name,
    string? Color);

/// <summary>
/// Dedicated reorder payload for a playthrough's sections. The identifiers list is
/// the complete set of the playthrough's section identifiers in the desired order.
/// </summary>
internal sealed record SectionOrderRequest(
    IReadOnlyList<int> SectionIds);

// Goal. Text is free-form; goals keep their creation order and cannot be reordered.
internal sealed record CreateGoalRequest(
    string? Text);

internal sealed record UpdateGoalRequest(
    string? Text);

/// <summary>Quick completion toggle for a single goal; never changes its order.</summary>
internal sealed record GoalCompletionRequest(
    bool Completed);
