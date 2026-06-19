using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Assets.Domain;

/// <summary>
/// Resolves the household civil date used by Assets attention rules. Asset dates
/// are stored as civil dates, so "today" is evaluated in Europe/Madrid.
/// </summary>
internal static class AssetsCivilDate
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
