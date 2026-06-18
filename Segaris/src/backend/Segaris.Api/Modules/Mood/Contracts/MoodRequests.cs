namespace Segaris.Api.Modules.Mood.Contracts;

/// <summary>
/// Create payload for a current-user mood entry. The four criteria are carried as
/// nullable strings so a missing or unknown value surfaces as a stable
/// <c>mood.entry.validation</c> failure rather than a deserialization error. The
/// derived emotion is never accepted on input because the module calculates it.
/// </summary>
internal sealed record CreateMoodEntryRequest(
    DateOnly EntryDate,
    int Score,
    string? Energy,
    string? Alignment,
    string? Direction,
    string? Source,
    string? Notes);

/// <summary>Update payload for a current-user mood entry. Shares the create shape.</summary>
internal sealed record UpdateMoodEntryRequest(
    DateOnly EntryDate,
    int Score,
    string? Energy,
    string? Alignment,
    string? Direction,
    string? Source,
    string? Notes);
