using System.Text.RegularExpressions;

namespace Segaris.Api.Modules.Clothes.Domain;

/// <summary>Domain limits and value rules shared by the Clothes Waves.</summary>
internal static partial class ClothesValidation
{
    public const int GarmentNameMaximumLength = ClothesDefaults.NameMaximumLength;
    public const int SizeMaximumLength = ClothesDefaults.SizeMaximumLength;
    public const int NotesMaximumLength = ClothesDefaults.NotesMaximumLength;
    public const int CategoryNameMaximumLength = 100;
    public const int ColorNameMaximumLength = 100;

    /// <summary>The stored length of a canonical <c>#RRGGBB</c> colour value.</summary>
    public const int ColorValueLength = 7;

    public static string ValidateGarmentName(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > GarmentNameMaximumLength)
        {
            throw new ClothesValidationException(
                $"Name is required and may contain at most {GarmentNameMaximumLength} characters.");
        }

        return trimmed;
    }

    public static string? ValidateSize(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > SizeMaximumLength)
        {
            throw new ClothesValidationException(
                $"Size may contain at most {SizeMaximumLength} characters.");
        }

        return trimmed.Length == 0 ? null : trimmed;
    }

    public static string? ValidateNotes(string? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value.Length > NotesMaximumLength)
        {
            throw new ClothesValidationException(
                $"Notes may contain at most {NotesMaximumLength} characters.");
        }

        return value.Length == 0 ? null : value;
    }

    /// <summary>
    /// Rejects any care axis whose value is set but not defined for that axis. Each
    /// axis is independently optional, so a <see langword="null"/> value is always
    /// valid.
    /// </summary>
    public static void ValidateCareAxes(
        WashingCare? washing,
        DryingCare? drying,
        IroningCare? ironing,
        DryCleaningCare? dryCleaning)
    {
        if ((washing is { } w && !Enum.IsDefined(w))
            || (drying is { } d && !Enum.IsDefined(d))
            || (ironing is { } i && !Enum.IsDefined(i))
            || (dryCleaning is { } c && !Enum.IsDefined(c)))
        {
            throw new ClothesValidationException("A care value is invalid for its axis.");
        }
    }

    /// <summary>
    /// Validates and canonicalizes a colour value as a <c>#RRGGBB</c> hex string,
    /// returning the upper-case form with a leading <c>#</c>. Shorthand, alpha, and
    /// named colours are rejected.
    /// </summary>
    public static string ValidateColorValue(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed) || !HexColorValue().IsMatch(trimmed))
        {
            throw new ClothesValidationException(
                "A colour value is required and must be a #RRGGBB hex string.");
        }

        return trimmed.ToUpperInvariant();
    }

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexColorValue();
}

/// <summary>
/// Distinguishes the Clothes domain failures so the HTTP surface can map each one to
/// its frozen <see cref="ClothesErrorCodes"/> value.
/// </summary>
internal enum ClothesValidationReason
{
    /// <summary>A required string, length, enum, or colour rule failed.</summary>
    Validation,

    /// <summary>A referenced category or colour does not exist.</summary>
    CatalogReference,

    /// <summary>A visibility change would violate ownership or public-record privacy rules.</summary>
    VisibilityForbidden,
}

internal sealed class ClothesValidationException(
    string message,
    ClothesValidationReason reason = ClothesValidationReason.Validation) : Exception(message)
{
    public ClothesValidationReason Reason { get; } = reason;
}
