using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Calendar.Contracts;
using Segaris.Api.Modules.Calendar.Domain;
using Segaris.Api.Modules.Identity;
using Segaris.Persistence;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Calendar.Queries;

internal sealed class CalendarDailyNoteReadService(SegarisDbContext database)
{
    public async Task<IReadOnlyList<CalendarDailyNoteResponse>> ListNotesAsync(
        DateOnly from,
        DateOnly to,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var query = database.Set<CalendarDailyNote>()
            .AsNoTracking()
            .Where(CalendarDailyNotePolicies.AccessibleTo(userId))
            .Where(note => note.Date >= from && note.Date <= to)
            .OrderBy(note => note.Date)
            .ThenBy(note => note.Id);

        var rows = await ProjectRows(query).ToArrayAsync(cancellationToken);
        return rows.Select(ToResponse).ToArray();
    }

    public async Task<CalendarDailyNoteResponse?> GetNoteAsync(
        int noteId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var query = database.Set<CalendarDailyNote>()
            .AsNoTracking()
            .Where(CalendarDailyNotePolicies.AccessibleTo(userId))
            .Where(note => note.Id == noteId);

        var row = await ProjectRows(query).FirstOrDefaultAsync(cancellationToken);
        return row is null ? null : ToResponse(row);
    }

    private IQueryable<CalendarDailyNoteRow> ProjectRows(IQueryable<CalendarDailyNote> notes) =>
        notes.Select(note => new CalendarDailyNoteRow(
            note.Id,
            note.Date,
            note.Title,
            note.Body,
            note.Visibility.ToString(),
            note.CreatedBy,
            database.Set<SegarisUser>()
                .Where(user => user.Id == note.CreatedBy)
                .Select(user => user.DisplayName)
                .First(),
            note.CreatedAt,
            note.UpdatedBy,
            database.Set<SegarisUser>()
                .Where(user => user.Id == note.UpdatedBy)
                .Select(user => user.DisplayName)
                .First(),
            note.UpdatedAt));

    private static CalendarDailyNoteResponse ToResponse(CalendarDailyNoteRow row) => new(
        row.Id,
        row.Date,
        row.Title,
        row.Body,
        row.Visibility,
        row.CreatedById,
        row.CreatedByName,
        row.CreatedAt,
        row.UpdatedById,
        row.UpdatedByName,
        row.UpdatedAt);

    private sealed record CalendarDailyNoteRow(
        int Id,
        DateOnly Date,
        string? Title,
        string Body,
        string Visibility,
        int CreatedById,
        string CreatedByName,
        DateTimeOffset CreatedAt,
        int UpdatedById,
        string UpdatedByName,
        DateTimeOffset UpdatedAt);
}
