using Segaris.Api.Modules.Mood.Domain;

namespace Segaris.UnitTests;

public sealed class MoodPeriodTests
{
    [Fact]
    public void Scale_order_is_frozen()
    {
        Assert.Equal(["Year", "Semester", "Quarter", "Month"], Enum.GetNames<MoodDashboardScale>());
    }

    [Theory]
    [InlineData("year", "Year")]
    [InlineData("Semester", "Semester")]
    [InlineData("QUARTER", "Quarter")]
    [InlineData(" month ", "Month")]
    public void Scale_parsing_is_case_insensitive_and_trims(string value, string expected)
    {
        Assert.True(MoodPeriod.TryParseScale(value, out var scale));
        Assert.Equal(expected, scale.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("weekly")]
    [InlineData("4")]
    public void Scale_parsing_rejects_unknown_values(string? value)
    {
        Assert.False(MoodPeriod.TryParseScale(value, out _));
    }

    [Theory]
    [InlineData("Year", "2026", "2026-01-01", "2026-12-31")]
    [InlineData("Semester", "2026-S1", "2026-01-01", "2026-06-30")]
    [InlineData("Semester", "2026-S2", "2026-07-01", "2026-12-31")]
    [InlineData("Quarter", "2026-Q1", "2026-01-01", "2026-03-31")]
    [InlineData("Quarter", "2026-Q3", "2026-07-01", "2026-09-30")]
    [InlineData("Quarter", "2026-Q4", "2026-10-01", "2026-12-31")]
    [InlineData("Month", "2026-01", "2026-01-01", "2026-01-31")]
    [InlineData("Month", "2026-12", "2026-12-01", "2026-12-31")]
    public void Period_tokens_resolve_to_strict_inclusive_boundaries(
        string scaleName,
        string token,
        string start,
        string end)
    {
        var scale = ParseScale(scaleName);
        Assert.True(MoodPeriod.TryParse(scale, token, out var period));
        Assert.Equal(DateOnly.Parse(start), period.Start);
        Assert.Equal(DateOnly.Parse(end), period.End);
        Assert.Equal(token, period.Token);
    }

    [Fact]
    public void February_boundary_respects_leap_years()
    {
        Assert.True(MoodPeriod.TryParse(MoodDashboardScale.Month, "2024-02", out var leap));
        Assert.Equal(new DateOnly(2024, 2, 29), leap.End);

        Assert.True(MoodPeriod.TryParse(MoodDashboardScale.Month, "2026-02", out var common));
        Assert.Equal(new DateOnly(2026, 2, 28), common.End);
    }

    [Theory]
    [InlineData("Year", "2026", "2025", "2027")]
    [InlineData("Semester", "2026-S1", "2025-S2", "2026-S2")]
    [InlineData("Semester", "2026-S2", "2026-S1", "2027-S1")]
    [InlineData("Quarter", "2026-Q1", "2025-Q4", "2026-Q2")]
    [InlineData("Quarter", "2026-Q4", "2026-Q3", "2027-Q1")]
    [InlineData("Month", "2026-01", "2025-12", "2026-02")]
    [InlineData("Month", "2026-12", "2026-11", "2027-01")]
    public void Previous_and_next_roll_over_year_boundaries(
        string scaleName,
        string token,
        string previous,
        string next)
    {
        var scale = ParseScale(scaleName);
        Assert.True(MoodPeriod.TryParse(scale, token, out var period));
        Assert.Equal(previous, period.Previous.Token);
        Assert.Equal(next, period.Next.Token);
    }

    [Theory]
    [InlineData("Year", "2026-03-15", "2026")]
    [InlineData("Semester", "2026-03-15", "2026-S1")]
    [InlineData("Semester", "2026-09-15", "2026-S2")]
    [InlineData("Quarter", "2026-05-15", "2026-Q2")]
    [InlineData("Month", "2026-07-15", "2026-07")]
    public void Current_resolves_the_period_containing_today(
        string scaleName,
        string today,
        string expectedToken)
    {
        var period = MoodPeriod.Current(ParseScale(scaleName), DateOnly.Parse(today));
        Assert.Equal(expectedToken, period.Token);
    }

    [Theory]
    [InlineData("Year", "26")]
    [InlineData("Year", "2026-Q1")]
    [InlineData("Semester", "2026-S0")]
    [InlineData("Semester", "2026-S3")]
    [InlineData("Quarter", "2026-Q5")]
    [InlineData("Quarter", "2026-S1")]
    [InlineData("Month", "2026-00")]
    [InlineData("Month", "2026-13")]
    [InlineData("Month", "2026-1")]
    [InlineData("Month", "")]
    public void Invalid_tokens_are_rejected(string scaleName, string token)
    {
        Assert.False(MoodPeriod.TryParse(ParseScale(scaleName), token, out _));
    }

    [Fact]
    public void Round_trip_token_parsing_is_stable_across_navigation()
    {
        Assert.True(MoodPeriod.TryParse(MoodDashboardScale.Month, "2026-11", out var period));
        var navigated = period.Next.Next; // 2027-01

        Assert.True(MoodPeriod.TryParse(MoodDashboardScale.Month, navigated.Token, out var reparsed));
        Assert.Equal(navigated.Start, reparsed.Start);
        Assert.Equal(navigated.End, reparsed.End);
        Assert.Equal("2027-01", navigated.Token);
    }

    [Theory]
    [InlineData("2026-06-15", "2026-06-15", "2026-06-21")]
    [InlineData("2026-06-18", "2026-06-15", "2026-06-21")]
    [InlineData("2026-06-21", "2026-06-15", "2026-06-21")]
    public void Madrid_week_helpers_resolve_monday_to_sunday_ranges(
        string date,
        string expectedStart,
        string expectedEnd)
    {
        var civilDate = DateOnly.Parse(date);
        var start = MoodWeek.StartOfWeek(civilDate);
        var end = MoodWeek.EndOfWeek(civilDate);

        Assert.Equal(DateOnly.Parse(expectedStart), start);
        Assert.Equal(DateOnly.Parse(expectedEnd), end);
        Assert.True(MoodWeek.IsMondayToSunday(start, end));
    }

    private static MoodDashboardScale ParseScale(string scaleName)
    {
        Assert.True(MoodPeriod.TryParseScale(scaleName, out var scale));
        return scale;
    }
}
