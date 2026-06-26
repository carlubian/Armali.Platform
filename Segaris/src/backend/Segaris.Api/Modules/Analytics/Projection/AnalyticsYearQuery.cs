using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Analytics.Projection;

internal sealed record AnalyticsYearQuery(int SelectedYear, int PreviousYear)
{
    public const int MinimumYear = 2000;
    public const int MaximumYear = 2100;
    public const string HouseholdTimeZoneId = "Europe/Madrid";

    private static readonly TimeZoneInfo Household =
        TimeZoneInfo.FindSystemTimeZoneById(HouseholdTimeZoneId);

    public static AnalyticsYearQuery Create(int year)
    {
        if (year is < MinimumYear or > MaximumYear)
        {
            throw new ArgumentOutOfRangeException(nameof(year), year, "Analytics year is outside the supported range.");
        }

        return new(year, year - 1);
    }

    public static AnalyticsYearQuery Parse(string? value, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (string.IsNullOrWhiteSpace(value))
        {
            return Current(clock);
        }

        if (!int.TryParse(value, out var year))
        {
            throw new ArgumentException("Analytics year must be a four-digit number.", nameof(value));
        }

        return Create(year);
    }

    public static AnalyticsYearQuery Current(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        var local = TimeZoneInfo.ConvertTime(clock.UtcNow, Household);
        return Create(local.Year);
    }

    public (DateOnly From, DateOnly To) SelectedYearRange() =>
        (new DateOnly(SelectedYear, 1, 1), new DateOnly(SelectedYear, 12, 31));

    public (DateOnly From, DateOnly To) PreviousYearRange() =>
        (new DateOnly(PreviousYear, 1, 1), new DateOnly(PreviousYear, 12, 31));
}
