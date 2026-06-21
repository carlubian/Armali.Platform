using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Processes.Domain;

/// <summary>
/// Resolves the household civil date used by Processes attention rules. Process
/// and step due dates are stored as civil dates, so "today" is evaluated in
/// <c>Europe/Madrid</c>.
/// </summary>
internal static class ProcessCivilDate
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
