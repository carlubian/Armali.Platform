using Segaris.Api.Modules.Mood.Domain;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Mood;

internal static class MoodProblem
{
    public static ApiProblemException From(MoodValidationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new ApiProblemException(
            StatusCodes.Status400BadRequest,
            MoodErrorCodes.EntryValidation,
            "The mood entry is invalid.",
            exception.Message);
    }

    public static ApiProblemException EntryNotFound() => new(
        StatusCodes.Status404NotFound,
        MoodErrorCodes.EntryNotFound,
        "Mood entry not found.");

    public static ApiProblemException RangeInvalid(
        string field,
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null) => new(
        StatusCodes.Status400BadRequest,
        MoodErrorCodes.RangeValidation,
        "The mood entry range is invalid.",
        errors: errors ?? new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [field] = [message],
        });

    public static ApiProblemException PeriodInvalid(
        string field,
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null) => new(
        StatusCodes.Status400BadRequest,
        MoodErrorCodes.PeriodValidation,
        "The mood dashboard period is invalid.",
        errors: errors ?? new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [field] = [message],
        });
}
