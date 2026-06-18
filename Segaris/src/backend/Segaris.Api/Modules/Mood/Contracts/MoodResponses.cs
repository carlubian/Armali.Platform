namespace Segaris.Api.Modules.Mood.Contracts;

/// <summary>
/// Read model for a single mood entry. Criteria are projected as their stable enum
/// names and the derived emotion is resolved from the code-backed matrix. Notes are
/// included for the entry dialog; the weekly log does not render them inline.
/// </summary>
internal sealed record MoodEntryResponse(
    int Id,
    DateOnly EntryDate,
    int Score,
    string Energy,
    string Alignment,
    string Direction,
    string Source,
    string DerivedEmotion,
    string? Notes,
    int CreatedById,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    int? UpdatedById,
    string? UpdatedByName,
    DateTimeOffset? UpdatedAt);

/// <summary>Average mood score for one civil date in a weekly log range.</summary>
internal sealed record MoodDailyAverageResponse(DateOnly EntryDate, double? AverageScore);

/// <summary>
/// Owner-only weekly log payload. Entries preserve deterministic insertion order
/// within each day, while daily averages include missing days as null values.
/// </summary>
internal sealed record MoodEntryListResponse(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<MoodEntryResponse> Entries,
    IReadOnlyList<MoodDailyAverageResponse> DailyAverages);

/// <summary>
/// Fixed criteria vocabularies and derived-emotion codes the frontend translates
/// through the Mood i18next namespace. The lists never expose another user's data.
/// </summary>
internal sealed record MoodOptionsResponse(
    IReadOnlyList<string> Energies,
    IReadOnlyList<string> Alignments,
    IReadOnlyList<string> Directions,
    IReadOnlyList<string> Sources,
    IReadOnlyList<string> Emotions);
