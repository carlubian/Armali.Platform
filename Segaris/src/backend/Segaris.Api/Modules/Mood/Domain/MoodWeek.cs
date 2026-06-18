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

    /// <summary>
    /// The Monday of every Monday-to-Sunday week that overlaps the inclusive
    /// <paramref name="from"/>..<paramref name="to"/> range, in ascending order. The
    /// first and last weeks may extend beyond the range when it does not align to a
    /// week boundary, which is how the Month dashboard scale buckets its weeks.
    /// </summary>
    public static IEnumerable<DateOnly> WeekStarts(DateOnly from, DateOnly to)
    {
        for (var monday = StartOfWeek(from); monday <= to; monday = monday.AddDays(DaysPerWeek))
        {
            yield return monday;
        }
    }
}
