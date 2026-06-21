using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Firebird.Domain;

/// <summary>Europe/Madrid civil-date helpers for Firebird records.</summary>
internal static class FirebirdCivilDate
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
