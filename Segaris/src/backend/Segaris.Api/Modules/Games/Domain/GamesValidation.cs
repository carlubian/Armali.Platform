using Segaris.Shared.Authorization;

namespace Segaris.Api.Modules.Games.Domain;

internal enum GamesValidationReason
{
    Validation,
    UnknownGame,
    VisibilityForbidden,
}

internal sealed class GamesValidationException(
    string message,
    GamesValidationReason reason = GamesValidationReason.Validation,
    string? field = null) : Exception(message)
{
    public GamesValidationReason Reason { get; } = reason;
    public string? Field { get; } = field;
}

internal static class GamesValidation
{
    public static string ValidateName(string? value, string field = "name")
    {
        var display = value?.Trim();
        if (string.IsNullOrWhiteSpace(display) || display.Length > GamesDefaults.NameMaximumLength)
        {
            throw new GamesValidationException(
                $"Name is required and may contain at most {GamesDefaults.NameMaximumLength} characters.",
                field: field);
        }

        return display;
    }

    public static string ValidateGoalText(string? value)
    {
        var display = value?.Trim();
        if (string.IsNullOrWhiteSpace(display) || display.Length > GamesDefaults.GoalTextMaximumLength)
        {
            throw new GamesValidationException(
                $"Goal text is required and may contain at most {GamesDefaults.GoalTextMaximumLength} characters.",
                field: "text");
        }

        return display;
    }

    public static int ValidateStartYear(int? value)
    {
        if (value is < GamesDefaults.MinimumStartYear or > GamesDefaults.MaximumStartYear)
        {
            throw new GamesValidationException(
                $"Start year must be between {GamesDefaults.MinimumStartYear} and {GamesDefaults.MaximumStartYear}.",
                field: "startYear");
        }

        return value ?? GamesDefaults.MinimumStartYear;
    }

    public static int ValidateStartMonth(int? value)
    {
        if (value is < GamesDefaults.MinimumStartMonth or > GamesDefaults.MaximumStartMonth)
        {
            throw new GamesValidationException(
                $"Start month must be between {GamesDefaults.MinimumStartMonth} and {GamesDefaults.MaximumStartMonth}.",
                field: "startMonth");
        }

        return value ?? GamesDefaults.MinimumStartMonth;
    }

    public static string NormalizeName(string value) => value.Trim().ToUpperInvariant();

    public static GamePlatform ValidatePlatform(string? value)
    {
        if (!Enum.TryParse<GamePlatform>(value, ignoreCase: false, out var platform)
            || !Enum.IsDefined(platform))
        {
            throw new GamesValidationException("Platform is invalid.", field: "platform");
        }

        return platform;
    }

    public static PlaythroughStatus ValidateStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return GamesDefaults.Status;
        }

        if (!Enum.TryParse<PlaythroughStatus>(value, ignoreCase: false, out var status)
            || !Enum.IsDefined(status))
        {
            throw new GamesValidationException("Status is invalid.", field: "status");
        }

        return status;
    }

    public static SectionColor ValidateColor(string? value)
    {
        if (!Enum.TryParse<SectionColor>(value, ignoreCase: false, out var color)
            || !Enum.IsDefined(color))
        {
            throw new GamesValidationException("Section colour is invalid.", field: "color");
        }

        return color;
    }

    public static RecordVisibility ValidateVisibility(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return GamesDefaults.Visibility;
        }

        if (!Enum.TryParse<RecordVisibility>(value, ignoreCase: false, out var visibility)
            || !Enum.IsDefined(visibility))
        {
            throw new GamesValidationException("Visibility is invalid.", field: "visibility");
        }

        return visibility;
    }

    public static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Technical timestamps must be UTC.", nameof(value));
        }
    }

    public static void EnsurePositiveIdentifier(int value, string field)
    {
        if (value <= 0)
        {
            throw new GamesValidationException($"{field} must be positive.", field: field);
        }
    }
}
