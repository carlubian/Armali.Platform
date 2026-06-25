using Segaris.Shared.Authorization;

namespace Segaris.Api.Modules.Calendar.Domain;

internal static class CalendarValidation
{
    public static DateOnly ValidateDate(DateOnly? value)
    {
        if (value is null)
        {
            throw new CalendarValidationException(
                "Daily note date is required.",
                CalendarValidationReason.Date);
        }

        return value.Value;
    }

    public static string? ValidateTitle(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var title = value.Trim();
        if (title.Length > CalendarDefaults.TitleMaximumLength)
        {
            throw new CalendarValidationException(
                $"Daily note titles may not exceed {CalendarDefaults.TitleMaximumLength} characters.",
                CalendarValidationReason.Title);
        }

        return title;
    }

    public static string ValidateBody(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new CalendarValidationException(
                "Daily note body is required.",
                CalendarValidationReason.Body);
        }

        var body = value.Trim();
        if (body.Length > CalendarDefaults.BodyMaximumLength)
        {
            throw new CalendarValidationException(
                $"Daily note body may not exceed {CalendarDefaults.BodyMaximumLength} characters.",
                CalendarValidationReason.Body);
        }

        return body;
    }

    public static RecordVisibility ValidateVisibility(RecordVisibility visibility)
    {
        if (!Enum.IsDefined(visibility))
        {
            throw new CalendarValidationException(
                "Daily note visibility is invalid.",
                CalendarValidationReason.Visibility);
        }

        return visibility;
    }
}
