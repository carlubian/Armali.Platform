using Segaris.Shared.Api;

namespace Segaris.Api.Modules.Mood;

/// <summary>Stable machine-readable Mood failures.</summary>
internal static class MoodErrorCodes
{
    /// <summary>
    /// An entry does not exist or is not owned by the current user. Missing and
    /// inaccessible entries share this code so private existence is not disclosed.
    /// </summary>
    public static readonly ErrorCode EntryNotFound = new("mood.entry.not_found");

    /// <summary>The submitted entry failed score, criteria, date, or notes validation.</summary>
    public static readonly ErrorCode EntryValidation = new("mood.entry.validation");

    /// <summary>The requested entry date range is missing or malformed.</summary>
    public static readonly ErrorCode RangeValidation = new("mood.range.validation");

    /// <summary>The requested dashboard scale or period token is unknown or malformed.</summary>
    public static readonly ErrorCode PeriodValidation = new("mood.period.validation");
}
