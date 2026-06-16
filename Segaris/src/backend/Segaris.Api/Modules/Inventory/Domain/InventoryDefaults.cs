using Segaris.Shared.Authorization;

namespace Segaris.Api.Modules.Inventory.Domain;

/// <summary>Frozen creation defaults that do not depend on persisted catalogs.</summary>
internal static class InventoryDefaults
{
    public const string HouseholdTimeZoneId = "Europe/Madrid";
    public const InventoryItemStatus ItemStatus = InventoryItemStatus.Candidate;
    public const InventoryOrderStatus OrderStatus = InventoryOrderStatus.Planning;
    public const RecordVisibility Visibility = RecordVisibility.Public;
    public const decimal CurrentStock = 0.00m;
    public const decimal MinimumStock = 0.00m;
    public const int ExpectedReceiptOffsetDays = 7;

    public static DateOnly OrderDate(DateTimeOffset now) => Today(now);

    public static DateOnly ExpectedReceiptDate(DateTimeOffset now) =>
        Today(now).AddDays(ExpectedReceiptOffsetDays);

    private static DateOnly Today(DateTimeOffset now)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(HouseholdTimeZoneId);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, timeZone).DateTime);
    }
}
