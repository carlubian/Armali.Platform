using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Mood.Contracts;
using Segaris.Api.Modules.Mood.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Mood.Queries;

internal sealed class MoodReadService(SegarisDbContext database)
{
    public async Task<MoodEntryListResponse> ListEntriesAsync(
        DateOnly from,
        DateOnly to,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var query = database.Set<MoodEntry>()
            .AsNoTracking()
            .Where(entry => entry.CreatedBy == userId.Value)
            .Where(entry => entry.EntryDate >= from && entry.EntryDate <= to)
            .OrderBy(entry => entry.EntryDate)
            .ThenBy(entry => entry.Id);
        var rows = await ProjectEntryRows(query).ToArrayAsync(cancellationToken);
        var entries = rows.Select(ToResponse).ToArray();

        var averageRows = await database.Set<MoodEntry>()
            .AsNoTracking()
            .Where(entry => entry.CreatedBy == userId.Value)
            .Where(entry => entry.EntryDate >= from && entry.EntryDate <= to)
            .GroupBy(entry => entry.EntryDate)
            .Select(group => new DailyAverageRow(group.Key, group.Average(entry => (double)entry.Score)))
            .ToArrayAsync(cancellationToken);
        var averagesByDate = averageRows.ToDictionary(row => row.EntryDate, row => row.AverageScore);

        var dailyAverages = EachDate(from, to)
            .Select(date => new MoodDailyAverageResponse(
                date,
                averagesByDate.TryGetValue(date, out var average) ? average : null))
            .ToArray();

        return new MoodEntryListResponse(from, to, entries, dailyAverages);
    }

    public async Task<MoodEntryResponse?> GetEntryAsync(
        int entryId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var query = database.Set<MoodEntry>()
            .AsNoTracking()
            .Where(entry => entry.Id == entryId && entry.CreatedBy == userId.Value);
        var row = await ProjectEntryRows(query).FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : ToResponse(row);
    }

    public MoodOptionsResponse GetOptions() => new(
        MoodCriteriaCatalog.Energies,
        MoodCriteriaCatalog.Alignments,
        MoodCriteriaCatalog.Directions,
        MoodCriteriaCatalog.Sources,
        MoodCriteriaCatalog.Emotions);

    private IQueryable<MoodEntryRow> ProjectEntryRows(IQueryable<MoodEntry> entries) =>
        entries.Select(entry => new MoodEntryRow(
            entry.Id,
            entry.EntryDate,
            entry.Score,
            entry.Energy,
            entry.Alignment,
            entry.Direction,
            entry.Source,
            entry.Notes,
            entry.CreatedBy,
            database.Set<SegarisUser>()
                .Where(user => user.Id == entry.CreatedBy)
                .Select(user => user.DisplayName)
                .First(),
            entry.CreatedAt,
            entry.UpdatedBy,
            database.Set<SegarisUser>()
                .Where(user => user.Id == entry.UpdatedBy)
                .Select(user => user.DisplayName)
                .FirstOrDefault(),
            entry.UpdatedAt));

    private static MoodEntryResponse ToResponse(MoodEntryRow row) => new(
        row.Id,
        row.EntryDate,
        row.Score,
        row.Energy.ToString(),
        row.Alignment.ToString(),
        row.Direction.ToString(),
        row.Source.ToString(),
        MoodDerivedEmotionMatrix.Resolve(row.Energy, row.Alignment, row.Direction, row.Source),
        row.Notes,
        row.CreatedBy,
        row.CreatedByName,
        row.CreatedAt,
        row.UpdatedBy,
        row.UpdatedByName,
        row.UpdatedAt);

    private static IEnumerable<DateOnly> EachDate(DateOnly from, DateOnly to)
    {
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            yield return date;
        }
    }

    private sealed record DailyAverageRow(DateOnly EntryDate, double AverageScore);

    private sealed record MoodEntryRow(
        int Id,
        DateOnly EntryDate,
        int Score,
        MoodEnergy Energy,
        MoodAlignment Alignment,
        MoodDirection Direction,
        MoodSource Source,
        string? Notes,
        int CreatedBy,
        string CreatedByName,
        DateTimeOffset CreatedAt,
        int? UpdatedBy,
        string? UpdatedByName,
        DateTimeOffset? UpdatedAt);
}
