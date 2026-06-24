namespace Segaris.Api.Modules.Calendar.Contracts;

internal sealed record CalendarEntryResponse(
    string Id,
    string SourceModule,
    string SourceType,
    string VisualFamily,
    string Title,
    string? Subtitle,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsAllDay,
    string? Status,
    string? TargetRoute);

internal sealed record CalendarDailyNoteResponse(
    int Id,
    DateOnly Date,
    string? Title,
    string Body,
    string Visibility,
    int CreatedById,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    int? UpdatedById,
    string? UpdatedByName,
    DateTimeOffset? UpdatedAt);
