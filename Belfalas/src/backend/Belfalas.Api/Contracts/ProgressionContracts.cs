namespace Belfalas.Api.Contracts;

/// <summary>The live progression of a single area: its level, XP, and position within the level.</summary>
public sealed record AreaProgressResponse(
    Guid AreaId,
    string AreaName,
    int Order,
    int Level,
    int Xp,
    int XpPerLevel,
    int XpIntoLevel,
    int XpForNextLevel,
    int MaxLevel,
    bool IsComplete);

/// <summary>
/// Headline progression for the active era: the global level (average of area levels) and
/// the per-area breakdown.
/// </summary>
public sealed record ProgressionSummaryResponse(
    Guid EraId,
    string EraName,
    double GlobalLevel,
    int MaxLevel,
    IReadOnlyList<AreaProgressResponse> Areas);
