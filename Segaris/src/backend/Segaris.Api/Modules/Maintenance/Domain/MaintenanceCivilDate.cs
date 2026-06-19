using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Maintenance.Domain;

/// <summary>
/// Resolves the household civil date used by Maintenance. Task dates are stored as
/// civil dates, so the system-managed completion date and the attention window are
/// evaluated in <c>Europe/Madrid</c>.
/// </summary>
internal static class MaintenanceCivilDate
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
