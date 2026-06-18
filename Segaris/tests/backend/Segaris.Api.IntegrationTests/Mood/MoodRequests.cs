using Segaris.Api.Modules.Mood.Contracts;

namespace Segaris.Api.IntegrationTests.Mood;

/// <summary>
/// Shared Mood request and route helpers for the owner-only entry, options, and
/// dashboard tests added from Wave 2 onward. Wave 0 only freezes the surface; no
/// endpoints respond yet.
/// </summary>
internal static class MoodRequests
{
    public const string EntriesPath = "/api/mood/entries";
    public const string OptionsPath = "/api/mood/options";
    public const string DashboardPath = "/api/mood/dashboard";

    public static string EntryPath(int entryId) => $"{EntriesPath}/{entryId}";

    public static string EntryRangePath(DateOnly from, DateOnly to) =>
        $"{EntriesPath}?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";

    public static string DashboardPeriodPath(string scale, string period) =>
        $"{DashboardPath}?scale={scale}&period={period}";

    public static CreateMoodEntryRequest ValidEntry(
        DateOnly entryDate,
        int score = 3,
        MoodEnergy energy = MoodEnergy.Medium,
        MoodAlignment alignment = MoodAlignment.Medium,
        MoodDirection direction = MoodDirection.Harmony,
        MoodSource source = MoodSource.Internal,
        string? notes = null) =>
        new(
            entryDate,
            score,
            energy.ToString(),
            alignment.ToString(),
            direction.ToString(),
            source.ToString(),
            notes);
}
