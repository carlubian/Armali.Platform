namespace Segaris.Api.Modules.Mood.Domain;

/// <summary>Frozen creation defaults and validation bounds for Mood entries.</summary>
internal static class MoodDefaults
{
    public const string HouseholdTimeZoneId = "Europe/Madrid";

    /// <summary>Inclusive lower bound for <c>Score</c>.</summary>
    public const int ScoreMinimum = 1;

    /// <summary>Inclusive upper bound for <c>Score</c>.</summary>
    public const int ScoreMaximum = 5;

    /// <summary>Maximum length of the optional notes field.</summary>
    public const int NotesMaxLength = 1000;

    /// <summary>The household civil "today" used as the new-entry and dashboard default.</summary>
    public static DateOnly Today(DateTimeOffset utcNow)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(HouseholdTimeZoneId);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utcNow, zone).DateTime);
    }
}
