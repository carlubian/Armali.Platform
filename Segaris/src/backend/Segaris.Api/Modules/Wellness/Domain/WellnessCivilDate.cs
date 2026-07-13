using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Wellness.Domain;

/// <summary>Europe/Madrid civil-date helpers for Wellness daily generation.</summary>
internal static class WellnessCivilDate
{
    public const string HouseholdTimeZoneId = "Europe/Madrid";

    private static readonly TimeZoneInfo Household =
        TimeZoneInfo.FindSystemTimeZoneById(HouseholdTimeZoneId);

    public static DateOnly Today(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        var local = TimeZoneInfo.ConvertTime(clock.UtcNow, Household);
        return DateOnly.FromDateTime(local.Date);
    }
}
