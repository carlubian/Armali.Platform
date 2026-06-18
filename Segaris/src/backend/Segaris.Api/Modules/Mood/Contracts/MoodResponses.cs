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

/// <summary>
/// Score minimum, average, and maximum for one weekday slot of the selected period.
/// All three values are null when the selected period has no entries on that weekday.
/// <see cref="DayOfWeek"/> is the stable enum name (<c>Monday</c>..<c>Sunday</c>);
/// the dashboard orders the seven slots Monday-first.
/// </summary>
internal sealed record MoodScoreByDayResponse(
    string DayOfWeek,
    int? MinScore,
    double? AverageScore,
    int? MaxScore);

/// <summary>Count of entries carrying one fixed criterion value, including zeros.</summary>
internal sealed record MoodValueCountResponse(string Value, int Count);

/// <summary>
/// Per-criterion value distribution for a scope (the whole period or a single
/// bucket). Each list contains every enum value in declared order, so absent values
/// surface as a zero count rather than a missing axis category.
/// </summary>
internal sealed record MoodCriteriaDistributionResponse(
    IReadOnlyList<MoodValueCountResponse> Energy,
    IReadOnlyList<MoodValueCountResponse> Alignment,
    IReadOnlyList<MoodValueCountResponse> Direction,
    IReadOnlyList<MoodValueCountResponse> Source);

/// <summary>
/// A single dashboard time bucket carrying both the score min/average/max summary
/// and the criteria distribution used for evolution charts. Buckets are calendar
/// months for the Year, Semester, and Quarter scales and Monday-to-Sunday weeks for
/// the Month scale. <see cref="Key"/> is the stable bucket token (<c>2026-07</c> for
/// months, the week's Monday <c>2026-07-06</c> for weeks). <see cref="Start"/> and
/// <see cref="End"/> are the bucket's natural civil bounds; only entries inside the
/// selected period contribute, so edge weeks aggregate just their in-period days.
/// </summary>
internal sealed record MoodBucketResponse(
    string Key,
    DateOnly Start,
    DateOnly End,
    int? MinScore,
    double? AverageScore,
    int? MaxScore,
    MoodCriteriaDistributionResponse Distribution);

/// <summary>
/// Owner-only strict-period dashboard payload. Every aggregate covers only the
/// current user's entries whose <c>EntryDate</c> falls inside the selected calendar
/// period. <see cref="BucketGranularity"/> is <c>Month</c> for Year, Semester, and
/// Quarter scales and <c>Week</c> for the Month scale, matching <see cref="Buckets"/>.
/// </summary>
internal sealed record MoodDashboardResponse(
    string Scale,
    string Period,
    DateOnly From,
    DateOnly To,
    string PreviousPeriod,
    string NextPeriod,
    string BucketGranularity,
    int EntryCount,
    IReadOnlyList<MoodScoreByDayResponse> ScoreByDayOfWeek,
    MoodCriteriaDistributionResponse Distribution,
    IReadOnlyList<MoodBucketResponse> Buckets);
