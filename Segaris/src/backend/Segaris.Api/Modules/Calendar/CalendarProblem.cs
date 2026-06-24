using Segaris.Api.Modules.Calendar.Domain;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Calendar;

internal static class CalendarProblem
{
    public static ApiProblemException NoteNotFound() => new(
        StatusCodes.Status404NotFound,
        CalendarErrorCodes.NoteNotFound,
        "Calendar note not found.");

    public static ApiProblemException From(CalendarValidationException exception) =>
        exception.Reason switch
        {
            CalendarValidationReason.VisibilityForbidden => new(
                StatusCodes.Status403Forbidden,
                CalendarErrorCodes.NoteVisibilityForbidden,
                "Calendar note visibility change is forbidden.",
                errors: Errors("visibility", exception.Message)),
            CalendarValidationReason.Date => Validation("date", exception.Message),
            CalendarValidationReason.Title => Validation("title", exception.Message),
            CalendarValidationReason.Body => Validation("body", exception.Message),
            CalendarValidationReason.Visibility => Validation("visibility", exception.Message),
            _ => Validation("note", exception.Message),
        };

    public static ApiProblemException RangeInvalid(string field, string message) => new(
        StatusCodes.Status400BadRequest,
        CalendarErrorCodes.NoteValidation,
        "The Calendar note range is invalid.",
        errors: Errors(field, message));

    public static ApiProblemException Validation(string field, string message) => new(
        StatusCodes.Status400BadRequest,
        CalendarErrorCodes.NoteValidation,
        "Calendar note validation failed.",
        errors: Errors(field, message));

    private static Dictionary<string, string[]> Errors(string field, string message) =>
        new(StringComparer.Ordinal)
        {
            [field] = [message],
        };
}
