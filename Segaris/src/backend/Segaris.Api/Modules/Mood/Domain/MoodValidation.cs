using Segaris.Api.Modules.Mood.Contracts;

namespace Segaris.Api.Modules.Mood.Domain;

internal static class MoodValidation
{
    public static int ValidateScore(int value)
    {
        if (value is < MoodDefaults.ScoreMinimum or > MoodDefaults.ScoreMaximum)
        {
            throw new MoodValidationException(
                $"Score must be between {MoodDefaults.ScoreMinimum} and {MoodDefaults.ScoreMaximum}.",
                MoodValidationReason.Score);
        }

        return value;
    }

    public static string? ValidateNotes(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (trimmed.Length > MoodDefaults.NotesMaxLength)
        {
            throw new MoodValidationException(
                $"Notes may contain at most {MoodDefaults.NotesMaxLength} characters.",
                MoodValidationReason.Notes);
        }

        return trimmed;
    }

    public static MoodEnergy ValidateEnergy(MoodEnergy value) => ValidateEnum(value);

    public static MoodAlignment ValidateAlignment(MoodAlignment value) => ValidateEnum(value);

    public static MoodDirection ValidateDirection(MoodDirection value) => ValidateEnum(value);

    public static MoodSource ValidateSource(MoodSource value) => ValidateEnum(value);

    private static TEnum ValidateEnum<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new MoodValidationException(
                $"{typeof(TEnum).Name} has an unknown value.",
                MoodValidationReason.Criteria);
        }

        return value;
    }
}

internal enum MoodValidationReason
{
    Validation,
    Score,
    Notes,
    Criteria,
}

internal sealed class MoodValidationException(
    string message,
    MoodValidationReason reason = MoodValidationReason.Validation) : Exception(message)
{
    public MoodValidationReason Reason { get; } = reason;
}
