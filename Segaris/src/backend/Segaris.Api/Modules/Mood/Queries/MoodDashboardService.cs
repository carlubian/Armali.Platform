using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Mood.Contracts;
using Segaris.Api.Modules.Mood.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Mood.Queries;

/// <summary>
/// Builds the owner-only strict-period dashboard aggregates. The selected period's
/// entries are read once with a minimal projection and aggregated in memory, which
/// keeps weekday, month, and Monday-week bucketing and the arithmetic average
/// identical across SQLite and PostgreSQL.
/// </summary>
internal sealed class MoodDashboardService(SegarisDbContext database)
{
    private static readonly DayOfWeek[] WeekdayOrder =
    [
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday,
        DayOfWeek.Sunday,
    ];

    public async Task<MoodDashboardResponse> GetDashboardAsync(
        MoodPeriod period,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var rows = await database.Set<MoodEntry>()
            .AsNoTracking()
            .Where(entry => entry.CreatedBy == userId.Value)
            .Where(entry => entry.EntryDate >= period.Start && entry.EntryDate <= period.End)
            .Select(entry => new Row(
                entry.EntryDate,
                entry.Score,
                entry.Energy,
                entry.Alignment,
                entry.Direction,
                entry.Source))
            .ToArrayAsync(cancellationToken);

        var byWeek = period.Scale == MoodDashboardScale.Month;

        return new MoodDashboardResponse(
            period.Scale.ToString(),
            period.Token,
            period.Start,
            period.End,
            period.Previous.Token,
            period.Next.Token,
            byWeek ? "Week" : "Month",
            rows.Length,
            BuildScoreByDayOfWeek(rows),
            BuildDistribution(rows),
            BuildBuckets(period, rows, byWeek));
    }

    private static IReadOnlyList<MoodScoreByDayResponse> BuildScoreByDayOfWeek(IReadOnlyList<Row> rows)
    {
        var byDay = rows.ToLookup(row => row.EntryDate.DayOfWeek);
        return WeekdayOrder
            .Select(day =>
            {
                var (min, average, max) = ScoreStats(byDay[day]);
                return new MoodScoreByDayResponse(day.ToString(), min, average, max);
            })
            .ToArray();
    }

    private static IReadOnlyList<MoodBucketResponse> BuildBuckets(
        MoodPeriod period,
        IReadOnlyList<Row> rows,
        bool byWeek)
    {
        var buckets = byWeek ? EnumerateWeeks(period) : EnumerateMonths(period);
        var assigned = rows.ToLookup(row => byWeek
            ? MoodWeek.StartOfWeek(row.EntryDate)
            : new DateOnly(row.EntryDate.Year, row.EntryDate.Month, 1));

        return buckets
            .Select(bucket =>
            {
                var bucketRows = assigned[bucket.Start].ToArray();
                var (min, average, max) = ScoreStats(bucketRows);
                return new MoodBucketResponse(
                    bucket.Key,
                    bucket.Start,
                    bucket.End,
                    min,
                    average,
                    max,
                    BuildDistribution(bucketRows));
            })
            .ToArray();
    }

    private static IEnumerable<Bucket> EnumerateMonths(MoodPeriod period)
    {
        for (var month = new DateOnly(period.Start.Year, period.Start.Month, 1);
            month <= period.End;
            month = month.AddMonths(1))
        {
            var end = new DateOnly(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
            yield return new Bucket(
                $"{month.Year:D4}-{month.Month:D2}",
                month,
                end);
        }
    }

    private static IEnumerable<Bucket> EnumerateWeeks(MoodPeriod period) =>
        MoodWeek.WeekStarts(period.Start, period.End)
            .Select(monday => new Bucket(
                monday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                monday,
                monday.AddDays(MoodWeek.DaysPerWeek - 1)));

    private static MoodCriteriaDistributionResponse BuildDistribution(IReadOnlyList<Row> rows) => new(
        Distribution(rows, row => row.Energy),
        Distribution(rows, row => row.Alignment),
        Distribution(rows, row => row.Direction),
        Distribution(rows, row => row.Source));

    private static IReadOnlyList<MoodValueCountResponse> Distribution<TEnum>(
        IReadOnlyList<Row> rows,
        Func<Row, TEnum> selector)
        where TEnum : struct, Enum
    {
        var counts = rows
            .GroupBy(selector)
            .ToDictionary(group => group.Key, group => group.Count());

        return Enum.GetValues<TEnum>()
            .Select(value => new MoodValueCountResponse(
                value.ToString(),
                counts.TryGetValue(value, out var count) ? count : 0))
            .ToArray();
    }

    private static (int? Min, double? Average, int? Max) ScoreStats(IEnumerable<Row> rows)
    {
        var scores = rows.Select(row => row.Score).ToArray();
        return scores.Length == 0
            ? (null, null, null)
            : (scores.Min(), scores.Average(), scores.Max());
    }

    private sealed record Bucket(string Key, DateOnly Start, DateOnly End);

    private sealed record Row(
        DateOnly EntryDate,
        int Score,
        MoodEnergy Energy,
        MoodAlignment Alignment,
        MoodDirection Direction,
        MoodSource Source);
}
