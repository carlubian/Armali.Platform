using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Capex.Domain;

/// <summary>
/// Resolves the household civil "today" used by <c>DueDate</c> comparisons such as
/// the launcher attention rule. <c>DueDate</c> is a civil date, so "today" is
/// evaluated in the application's <c>Europe/Madrid</c> household time zone rather
/// than by converting the date through UTC.
/// </summary>
internal static class CapexCivilDate
{
    private static readonly TimeZoneInfo Household =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");

    public static DateOnly Today(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        var local = TimeZoneInfo.ConvertTime(clock.UtcNow, Household);
        return DateOnly.FromDateTime(local.Date);
    }
}
