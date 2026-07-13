namespace Segaris.Api.Modules.Wellness.Domain;

internal sealed class WellnessValidationException(string message, string? field = null) : Exception(message)
{
    public string? Field { get; } = field;
}

internal static class WellnessValidation
{
    public static string ValidateTaskName(string? value, string field = "name")
    {
        var display = value?.Trim();
        if (string.IsNullOrWhiteSpace(display) || display.Length > WellnessDefaults.TaskNameMaximumLength)
        {
            throw new WellnessValidationException(
                $"Task name is required and may contain at most {WellnessDefaults.TaskNameMaximumLength} characters.",
                field);
        }

        return display;
    }

    public static WellnessCategory ValidateCategory(WellnessCategory category)
    {
        if (!Enum.IsDefined(category))
        {
            throw new WellnessValidationException("Category is invalid.", "category");
        }

        return category;
    }

    public static WellnessCategory ParseCategory(string? value)
    {
        if (!Enum.TryParse<WellnessCategory>(value, ignoreCase: false, out var category)
            || !Enum.IsDefined(category))
        {
            throw new WellnessValidationException("Category is invalid.", "category");
        }

        return category;
    }

    public static int? ValidateScore(int? score)
    {
        if (score is < WellnessDefaults.MinimumScore or > WellnessDefaults.MaximumScore)
        {
            throw new WellnessValidationException(
                $"Score must be between {WellnessDefaults.MinimumScore} and {WellnessDefaults.MaximumScore}.",
                "score");
        }

        return score;
    }

    public static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new WellnessValidationException("Technical timestamps must use UTC.");
        }
    }

    public static void EnsurePositiveIdentifier(int value, string field)
    {
        if (value <= 0)
        {
            throw new WellnessValidationException($"{field} must be positive.", field);
        }
    }
}
