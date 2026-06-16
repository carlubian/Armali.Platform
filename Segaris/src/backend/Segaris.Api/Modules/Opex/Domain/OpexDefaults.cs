using Segaris.Shared.Authorization;

namespace Segaris.Api.Modules.Opex.Domain;

/// <summary>Frozen creation defaults that do not depend on persisted catalogs.</summary>
internal static class OpexDefaults
{
    public const string HouseholdTimeZoneId = "Europe/Madrid";
    public const OpexMovementType MovementType = OpexMovementType.Expense;
    public const OpexContractStatus Status = OpexContractStatus.Planning;
    public const OpexExpectedFrequency ExpectedFrequency = OpexExpectedFrequency.None;
    public const RecordVisibility Visibility = RecordVisibility.Public;
    public const decimal OccurrenceAmount = 0.00m;

    public static DateOnly OccurrenceDate(DateTimeOffset now)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(HouseholdTimeZoneId);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, timeZone).DateTime);
    }
}
