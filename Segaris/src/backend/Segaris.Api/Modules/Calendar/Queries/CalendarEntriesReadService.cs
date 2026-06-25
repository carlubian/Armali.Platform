using Segaris.Api.Modules.Calendar.Contracts;
using Segaris.Api.Modules.Calendar.Projection;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Calendar.Queries;

internal sealed class CalendarEntriesReadService(
    CalendarDailyNoteReadService notes,
    IEnumerable<ICalendarProjectionProvider> projectionProviders)
{
    public async Task<IReadOnlyList<CalendarEntryResponse>> ListEntriesAsync(
        CalendarEntriesFilter filter,
        UserId viewer,
        CancellationToken cancellationToken)
    {
        var entries = new List<NormalizedCalendarProjection>();

        if (ShouldInclude(CalendarSourceModules.Calendar, CalendarVisualFamilies.Note, filter))
        {
            var noteEntries = await notes.ListNotesAsync(filter.From, filter.To, viewer, cancellationToken);
            entries.AddRange(noteEntries.Select(ToProjection));
        }

        foreach (var provider in projectionProviders)
        {
            if (filter.SourceModules.Count > 0 && !filter.SourceModules.Contains(provider.SourceModule))
            {
                continue;
            }

            var projections = await provider.ListAsync(filter.From, filter.To, viewer, cancellationToken);
            entries.AddRange(projections);
        }

        return entries
            .Where(entry => MatchesFilter(entry, filter))
            .OrderBy(entry => entry.StartDate)
            .ThenBy(entry => entry.EndDate ?? entry.StartDate)
            .ThenBy(entry => entry.VisualFamily, StringComparer.Ordinal)
            .ThenBy(entry => entry.SourceModule, StringComparer.Ordinal)
            .ThenBy(entry => entry.SourceType, StringComparer.Ordinal)
            .ThenBy(entry => entry.Id, StringComparer.Ordinal)
            .Select(ToResponse)
            .ToArray();
    }

    private static bool ShouldInclude(
        string sourceModule,
        string visualFamily,
        CalendarEntriesFilter filter) =>
        (filter.SourceModules.Count == 0 || filter.SourceModules.Contains(sourceModule)) &&
        (filter.VisualFamilies.Count == 0 || filter.VisualFamilies.Contains(visualFamily));

    private static bool MatchesFilter(NormalizedCalendarProjection entry, CalendarEntriesFilter filter) =>
        entry.StartDate <= filter.To &&
        (entry.EndDate ?? entry.StartDate) >= filter.From &&
        ShouldInclude(entry.SourceModule, entry.VisualFamily, filter);

    private static NormalizedCalendarProjection ToProjection(CalendarDailyNoteResponse note) => new(
        $"calendar:note:{note.Id}",
        CalendarSourceModules.Calendar,
        CalendarSourceTypes.DailyNote,
        CalendarVisualFamilies.Note,
        string.IsNullOrWhiteSpace(note.Title) ? "Daily note" : note.Title,
        note.Body,
        note.Date,
        null,
        true,
        note.Visibility,
        $"/calendar?day={note.Date:yyyy-MM-dd}&noteId={note.Id}");

    private static CalendarEntryResponse ToResponse(NormalizedCalendarProjection entry) => new(
        entry.Id,
        entry.SourceModule,
        entry.SourceType,
        entry.VisualFamily,
        entry.Title,
        entry.Subtitle,
        entry.StartDate,
        entry.EndDate,
        entry.IsAllDay,
        entry.Status,
        entry.TargetRoute);
}
