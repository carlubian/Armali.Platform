using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Calendar.Domain;

internal sealed record CalendarDailyNoteValues(
    DateOnly Date,
    string? Title,
    string? Body,
    RecordVisibility Visibility);

internal sealed class CalendarDailyNote
{
    private CalendarDailyNote()
    {
    }

    public int Id { get; private set; }
    public DateOnly Date { get; private set; }
    public string? Title { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public RecordVisibility Visibility { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int UpdatedBy { get; private set; }

    public static CalendarDailyNote Create(
        CalendarDailyNoteValues values,
        UserId creatorId,
        DateTimeOffset now)
    {
        EnsureUtc(now);
        var note = new CalendarDailyNote
        {
            CreatedAt = now,
            CreatedBy = creatorId.Value,
            UpdatedAt = now,
            UpdatedBy = creatorId.Value,
        };
        note.Apply(values, creatorId, now, isCreation: true);
        return note;
    }

    public void Update(CalendarDailyNoteValues values, UserId actorId, DateTimeOffset now)
    {
        Apply(values, actorId, now, isCreation: false);
    }

    private void Apply(
        CalendarDailyNoteValues values,
        UserId actorId,
        DateTimeOffset now,
        bool isCreation)
    {
        EnsureUtc(now);
        var visibility = CalendarValidation.ValidateVisibility(values.Visibility);
        if (!isCreation && visibility != Visibility && actorId.Value != CreatedBy)
        {
            throw new CalendarValidationException(
                "Only the creator may change note visibility.",
                CalendarValidationReason.VisibilityForbidden);
        }

        Date = values.Date;
        Title = CalendarValidation.ValidateTitle(values.Title);
        Body = CalendarValidation.ValidateBody(values.Body);
        Visibility = visibility;
        UpdatedAt = now;
        UpdatedBy = actorId.Value;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new CalendarValidationException("Technical timestamps must use UTC.");
        }
    }
}
