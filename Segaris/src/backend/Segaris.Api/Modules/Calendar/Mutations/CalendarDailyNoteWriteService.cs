using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Calendar.Contracts;
using Segaris.Api.Modules.Calendar.Domain;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Calendar.Mutations;

internal sealed class CalendarDailyNoteWriteService(SegarisDbContext database, IClock clock)
{
    public async Task<int> CreateAsync(
        UpsertCalendarDailyNoteRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var note = CalendarDailyNote.Create(
            Map(request, defaultVisibility: RecordVisibility.Private),
            actorId,
            clock.UtcNow);
        database.Add(note);
        await database.SaveChangesAsync(cancellationToken);
        return note.Id;
    }

    public async Task<bool> UpdateAsync(
        int noteId,
        UpsertCalendarDailyNoteRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var note = await database.Set<CalendarDailyNote>()
            .Where(CalendarDailyNotePolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == noteId)
            .FirstOrDefaultAsync(cancellationToken);
        if (note is null)
        {
            return false;
        }

        var values = Map(request, note.Visibility);
        if (values.Visibility != note.Visibility && !CalendarDailyNotePolicies.CanChangeVisibility(note, actorId))
        {
            throw new CalendarValidationException(
                "Only the creator may change note visibility.",
                CalendarValidationReason.VisibilityForbidden);
        }

        note.Update(values, actorId, clock.UtcNow);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        int noteId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var note = await database.Set<CalendarDailyNote>()
            .Where(CalendarDailyNotePolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == noteId)
            .FirstOrDefaultAsync(cancellationToken);
        if (note is null)
        {
            return false;
        }

        database.Remove(note);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static CalendarDailyNoteValues Map(
        UpsertCalendarDailyNoteRequest request,
        RecordVisibility defaultVisibility) => new(
            CalendarValidation.ValidateDate(request.Date),
            request.Title,
            request.Body,
            ParseVisibility(request.Visibility, defaultVisibility));

    private static RecordVisibility ParseVisibility(string? value, RecordVisibility defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (Enum.TryParse<RecordVisibility>(value.Trim(), ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new CalendarValidationException(
            "Daily note visibility is not a recognized value.",
            CalendarValidationReason.Visibility);
    }
}
