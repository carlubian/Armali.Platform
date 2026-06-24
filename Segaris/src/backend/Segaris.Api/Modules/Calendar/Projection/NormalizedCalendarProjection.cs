namespace Segaris.Api.Modules.Calendar.Projection;

internal sealed record NormalizedCalendarProjection(
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
