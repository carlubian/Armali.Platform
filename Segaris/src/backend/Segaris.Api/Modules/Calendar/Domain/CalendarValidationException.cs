namespace Segaris.Api.Modules.Calendar.Domain;

internal enum CalendarValidationReason
{
    General,
    Date,
    Title,
    Body,
    Visibility,
    VisibilityForbidden,
}

internal sealed class CalendarValidationException(
    string message,
    CalendarValidationReason reason = CalendarValidationReason.General)
    : Exception(message)
{
    public CalendarValidationReason Reason { get; } = reason;
}
