namespace Segaris.Api.Modules.Mood.Domain;

/// <summary>Europe/Madrid civil-week helpers for the Mood weekly log.</summary>
internal static class MoodWeek
{
    public const int DaysPerWeek = 7;

    public static DateOnly StartOfWeek(DateOnly date)
    {
        var offset = ((int)date.DayOfWeek + 6) % DaysPerWeek;
        return date.AddDays(-offset);
    }

    public static DateOnly EndOfWeek(DateOnly date) => StartOfWeek(date).AddDays(DaysPerWeek - 1);

    public static bool IsMondayToSunday(DateOnly from, DateOnly to) =>
        from.DayOfWeek == DayOfWeek.Monday && to == from.AddDays(DaysPerWeek - 1);
}
