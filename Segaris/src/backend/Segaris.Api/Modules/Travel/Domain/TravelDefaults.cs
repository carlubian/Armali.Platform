using Segaris.Shared.Authorization;

namespace Segaris.Api.Modules.Travel.Domain;

internal static class TravelDefaults
{
    public const string HouseholdTimeZoneId = "Europe/Madrid";

    public static TravelTripStatus TripStatus => TravelTripStatus.Planned;

    public static RecordVisibility Visibility => RecordVisibility.Public;

    public static decimal ExpenseAmount => 0.00m;

    public static DateOnly Today(DateTimeOffset utcNow)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(HouseholdTimeZoneId);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utcNow, zone).DateTime);
    }
}
