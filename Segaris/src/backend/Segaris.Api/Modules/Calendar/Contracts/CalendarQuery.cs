namespace Segaris.Api.Modules.Calendar.Contracts;

internal sealed record CalendarEntriesFilter(
    DateOnly From,
    DateOnly To,
    IReadOnlySet<string> SourceModules,
    IReadOnlySet<string> VisualFamilies);

internal static class CalendarEntriesQuery
{
    public const int MaximumRangeDays = 366;

    public static CalendarEntriesFilter Create(
        DateOnly from,
        DateOnly to,
        IEnumerable<string>? sourceModules = null,
        IEnumerable<string>? visualFamilies = null)
    {
        if (to < from)
        {
            throw new ArgumentException("Calendar range end must be on or after the start.", nameof(to));
        }

        if ((to.DayNumber - from.DayNumber) + 1 > MaximumRangeDays)
        {
            throw new ArgumentOutOfRangeException(nameof(to), "Calendar ranges may not exceed one calendar year.");
        }

        var sources = ToFilterSet(sourceModules, CalendarSourceModules.AllowedFilters, nameof(sourceModules));
        var families = ToFilterSet(visualFamilies, CalendarVisualFamilies.AllowedFilters, nameof(visualFamilies));

        return new CalendarEntriesFilter(from, to, sources, families);
    }

    private static IReadOnlySet<string> ToFilterSet(
        IEnumerable<string>? values,
        IReadOnlySet<string> allowed,
        string parameterName)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values ?? [])
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var normalized = value.Trim();
            if (!allowed.Contains(normalized))
            {
                throw new ArgumentException($"Unsupported Calendar filter value '{normalized}'.", parameterName);
            }

            set.Add(normalized);
        }

        return set;
    }
}
