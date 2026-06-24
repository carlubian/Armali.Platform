namespace Segaris.Api.Modules.Calendar.Contracts;

internal sealed record UpsertCalendarDailyNoteRequest(
    DateOnly Date,
    string? Title,
    string Body,
    string Visibility);
