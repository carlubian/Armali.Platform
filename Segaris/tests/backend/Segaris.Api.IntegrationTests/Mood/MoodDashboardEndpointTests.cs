using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Mood.Contracts;
using Segaris.Api.Modules.Mood.Domain;

namespace Segaris.Api.IntegrationTests.Mood;

public sealed class MoodDashboardEndpointTests
{
    [Fact]
    public async Task Dashboard_requires_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync(
            MoodRequests.DashboardPeriodPath("year", "2026"),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Year_dashboard_aggregates_score_by_day_and_month_and_criteria_distribution()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        // Two entries on one Monday (Jan), one Tuesday (Mar), one Wednesday (Jul).
        await CreateAsync(client, csrf, new DateOnly(2026, 1, 5), score: 2,
            MoodEnergy.Low, MoodAlignment.Negative, MoodDirection.Harmony, MoodSource.Internal);
        await CreateAsync(client, csrf, new DateOnly(2026, 1, 5), score: 4,
            MoodEnergy.High, MoodAlignment.Positive, MoodDirection.Offensive, MoodSource.External);
        await CreateAsync(client, csrf, new DateOnly(2026, 3, 10), score: 5,
            MoodEnergy.Medium, MoodAlignment.Medium, MoodDirection.Defensive, MoodSource.Internal);
        await CreateAsync(client, csrf, new DateOnly(2026, 7, 15), score: 1,
            MoodEnergy.Low, MoodAlignment.Negative, MoodDirection.Stability, MoodSource.External);

        // Entries just outside the year must not contribute.
        await CreateAsync(client, csrf, new DateOnly(2025, 12, 31), score: 5);
        await CreateAsync(client, csrf, new DateOnly(2027, 1, 1), score: 5);

        var dashboard = await client.GetFromJsonAsync<MoodDashboardResponse>(
            MoodRequests.DashboardPeriodPath("year", "2026"),
            CancellationToken.None);

        Assert.NotNull(dashboard);
        Assert.Equal("Year", dashboard.Scale);
        Assert.Equal("2026", dashboard.Period);
        Assert.Equal(new DateOnly(2026, 1, 1), dashboard.From);
        Assert.Equal(new DateOnly(2026, 12, 31), dashboard.To);
        Assert.Equal("2025", dashboard.PreviousPeriod);
        Assert.Equal("2027", dashboard.NextPeriod);
        Assert.Equal("Month", dashboard.BucketGranularity);
        Assert.Equal(4, dashboard.EntryCount);

        // Score by day of week, Monday-first, missing days null.
        Assert.Equal(7, dashboard.ScoreByDayOfWeek.Count);
        Assert.Equal("Monday", dashboard.ScoreByDayOfWeek[0].DayOfWeek);
        Assert.Equal("Sunday", dashboard.ScoreByDayOfWeek[6].DayOfWeek);
        var monday = dashboard.ScoreByDayOfWeek[0];
        Assert.Equal(2, monday.MinScore);
        Assert.Equal(3.0d, monday.AverageScore);
        Assert.Equal(4, monday.MaxScore);
        Assert.Equal(5.0d, Day(dashboard, "Tuesday").AverageScore);
        Assert.Equal(1.0d, Day(dashboard, "Wednesday").AverageScore);
        Assert.Null(Day(dashboard, "Thursday").AverageScore);
        Assert.Null(Day(dashboard, "Sunday").MinScore);

        // Twelve month buckets, Jan/Mar/Jul populated, others null.
        Assert.Equal(12, dashboard.Buckets.Count);
        Assert.Equal("2026-01", dashboard.Buckets[0].Key);
        Assert.Equal("2026-12", dashboard.Buckets[11].Key);
        var january = Bucket(dashboard, "2026-01");
        Assert.Equal(new DateOnly(2026, 1, 1), january.Start);
        Assert.Equal(new DateOnly(2026, 1, 31), january.End);
        Assert.Equal(2, january.MinScore);
        Assert.Equal(3.0d, january.AverageScore);
        Assert.Equal(4, january.MaxScore);
        Assert.Equal(5.0d, Bucket(dashboard, "2026-03").AverageScore);
        Assert.Equal(1.0d, Bucket(dashboard, "2026-07").AverageScore);
        Assert.Null(Bucket(dashboard, "2026-02").AverageScore);

        // Whole-period criteria distribution counts every enum value.
        Assert.Equal(2, Count(dashboard.Distribution.Energy, "Low"));
        Assert.Equal(1, Count(dashboard.Distribution.Energy, "Medium"));
        Assert.Equal(1, Count(dashboard.Distribution.Energy, "High"));
        Assert.Equal(2, Count(dashboard.Distribution.Alignment, "Negative"));
        Assert.Equal(1, Count(dashboard.Distribution.Direction, "Harmony"));
        Assert.Equal(1, Count(dashboard.Distribution.Direction, "Offensive"));
        Assert.Equal(1, Count(dashboard.Distribution.Direction, "Stability"));
        Assert.Equal(2, Count(dashboard.Distribution.Source, "Internal"));
        Assert.Equal(2, Count(dashboard.Distribution.Source, "External"));

        // Per-bucket evolution distribution for January.
        Assert.Equal(1, Count(january.Distribution.Energy, "Low"));
        Assert.Equal(0, Count(january.Distribution.Energy, "Medium"));
        Assert.Equal(1, Count(january.Distribution.Energy, "High"));
        Assert.Equal(0, Count(Bucket(dashboard, "2026-02").Distribution.Source, "Internal"));
    }

    [Fact]
    public async Task Month_dashboard_buckets_entries_into_monday_to_sunday_weeks()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        // March 2026 starts on a Sunday; the leading week's Monday is 2026-02-23.
        await CreateAsync(client, csrf, new DateOnly(2026, 3, 1), score: 1); // week 2026-02-23
        await CreateAsync(client, csrf, new DateOnly(2026, 3, 2), score: 3); // week 2026-03-02
        await CreateAsync(client, csrf, new DateOnly(2026, 3, 15), score: 5); // week 2026-03-09

        var dashboard = await client.GetFromJsonAsync<MoodDashboardResponse>(
            MoodRequests.DashboardPeriodPath("month", "2026-03"),
            CancellationToken.None);

        Assert.NotNull(dashboard);
        Assert.Equal("Month", dashboard.Scale);
        Assert.Equal("2026-03", dashboard.Period);
        Assert.Equal("2026-02", dashboard.PreviousPeriod);
        Assert.Equal("2026-04", dashboard.NextPeriod);
        Assert.Equal("Week", dashboard.BucketGranularity);
        Assert.Equal(3, dashboard.EntryCount);

        Assert.Equal(
            ["2026-02-23", "2026-03-02", "2026-03-09", "2026-03-16", "2026-03-23", "2026-03-30"],
            dashboard.Buckets.Select(bucket => bucket.Key).ToArray());
        var leading = Bucket(dashboard, "2026-02-23");
        Assert.Equal(new DateOnly(2026, 2, 23), leading.Start);
        Assert.Equal(new DateOnly(2026, 3, 1), leading.End);
        Assert.Equal(1.0d, leading.AverageScore);
        Assert.Equal(3.0d, Bucket(dashboard, "2026-03-02").AverageScore);
        Assert.Equal(5.0d, Bucket(dashboard, "2026-03-09").AverageScore);
        Assert.Null(Bucket(dashboard, "2026-03-16").AverageScore);
    }

    [Fact]
    public async Task No_data_and_future_periods_return_empty_scaffolding_without_errors()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        var dashboard = await client.GetFromJsonAsync<MoodDashboardResponse>(
            MoodRequests.DashboardPeriodPath("year", "2099"),
            CancellationToken.None);

        Assert.NotNull(dashboard);
        Assert.Equal(0, dashboard.EntryCount);
        Assert.Equal(7, dashboard.ScoreByDayOfWeek.Count);
        Assert.All(dashboard.ScoreByDayOfWeek, day =>
        {
            Assert.Null(day.MinScore);
            Assert.Null(day.AverageScore);
            Assert.Null(day.MaxScore);
        });
        Assert.Equal(12, dashboard.Buckets.Count);
        Assert.All(dashboard.Buckets, bucket => Assert.Null(bucket.AverageScore));
        Assert.Equal(3, dashboard.Distribution.Energy.Count);
        Assert.All(dashboard.Distribution.Energy, value => Assert.Equal(0, value.Count));
        Assert.Equal(4, dashboard.Distribution.Direction.Count);
    }

    [Fact]
    public async Task Dashboard_defaults_to_the_current_year_when_scale_and_period_are_omitted()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var today = MoodDefaults.Today(DateTimeOffset.UtcNow);

        var dashboard = await client.GetFromJsonAsync<MoodDashboardResponse>(
            MoodRequests.DashboardPath,
            CancellationToken.None);

        Assert.NotNull(dashboard);
        Assert.Equal("Year", dashboard.Scale);
        Assert.Equal("Month", dashboard.BucketGranularity);
        Assert.True(dashboard.From <= today && today <= dashboard.To);
        Assert.Equal(new DateOnly(today.Year, 1, 1), dashboard.From);
        Assert.Equal(new DateOnly(today.Year, 12, 31), dashboard.To);
    }

    [Fact]
    public async Task Dashboard_only_aggregates_the_current_users_entries()
    {
        using var server = new CapexTestServer();
        await server.CreateUserAsync("mood-trends", "MoodTrendsPass123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "mood-trends", "MoodTrendsPass123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);
        await CreateAsync(member, memberCsrf, new DateOnly(2026, 4, 6), score: 2);

        using var admin = await server.CreateAuthenticatedClientAsync();
        var adminCsrf = await CapexTestServer.GetCsrfTokenAsync(admin);
        await CreateAsync(admin, adminCsrf, new DateOnly(2026, 4, 6), score: 5);
        await CreateAsync(admin, adminCsrf, new DateOnly(2026, 4, 7), score: 5);

        var memberDashboard = await member.GetFromJsonAsync<MoodDashboardResponse>(
            MoodRequests.DashboardPeriodPath("year", "2026"),
            CancellationToken.None);
        var adminDashboard = await admin.GetFromJsonAsync<MoodDashboardResponse>(
            MoodRequests.DashboardPeriodPath("year", "2026"),
            CancellationToken.None);

        Assert.NotNull(memberDashboard);
        Assert.Equal(1, memberDashboard.EntryCount);
        Assert.Equal(2.0d, Bucket(memberDashboard, "2026-04").AverageScore);
        Assert.NotNull(adminDashboard);
        Assert.Equal(2, adminDashboard.EntryCount);
        Assert.Equal(5.0d, Bucket(adminDashboard, "2026-04").AverageScore);
    }

    [Theory]
    [InlineData("weekly", "2026")]
    [InlineData("year", "2026-Q1")]
    [InlineData("quarter", "2026-S1")]
    [InlineData("month", "2026-13")]
    public async Task Dashboard_rejects_unknown_scale_or_malformed_period(string scale, string period)
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        using var response = await client.GetAsync(
            MoodRequests.DashboardPeriodPath(scale, period),
            CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("mood.period.validation", problem!.Code);
    }

    private static MoodScoreByDayResponse Day(MoodDashboardResponse dashboard, string dayOfWeek) =>
        dashboard.ScoreByDayOfWeek.Single(day => day.DayOfWeek == dayOfWeek);

    private static MoodBucketResponse Bucket(MoodDashboardResponse dashboard, string key) =>
        dashboard.Buckets.Single(bucket => bucket.Key == key);

    private static int Count(IEnumerable<MoodValueCountResponse> values, string value) =>
        values.Single(item => item.Value == value).Count;

    private static async Task CreateAsync(
        HttpClient client,
        string csrf,
        DateOnly entryDate,
        int score = 3,
        MoodEnergy energy = MoodEnergy.Medium,
        MoodAlignment alignment = MoodAlignment.Medium,
        MoodDirection direction = MoodDirection.Harmony,
        MoodSource source = MoodSource.Internal)
    {
        var request = MoodRequests.ValidEntry(entryDate, score, energy, alignment, direction, source);
        using var response = await CapexApi.PostJsonAsync(client, MoodRequests.EntriesPath, request, csrf);
        response.EnsureSuccessStatusCode();
    }

    private sealed record ProblemPayload(string? Code);
}
