namespace Segaris.Api.Modules.Assets.Domain;

/// <summary>
/// Domain limits and normalization rules for assets and the Assets-owned catalogs.
/// The asset field limits mirror <see cref="AssetDefaults"/> and are re-exposed here
/// so the entity validates without depending on presentation defaults. The catalog
/// name limit matches the established module-owned catalog convention.
/// </summary>
internal static class AssetValidation
{
    public const int NameMaximumLength = AssetDefaults.NameMaximumLength;
    public const int CodeMaximumLength = AssetDefaults.CodeMaximumLength;
    public const int BrandModelMaximumLength = AssetDefaults.BrandModelMaximumLength;
    public const int SerialNumberMaximumLength = AssetDefaults.SerialNumberMaximumLength;
    public const int NotesMaximumLength = AssetDefaults.NotesMaximumLength;
    public const int CategoryNameMaximumLength = 100;
    public const int LocationNameMaximumLength = 100;

    public static string ValidateName(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > NameMaximumLength)
        {
            throw new AssetValidationException(
                $"Name is required and may contain at most {NameMaximumLength} characters.");
        }

        return trimmed;
    }

    /// <summary>
    /// Validates the optional, case-insensitively unique household code. A blank code
    /// is treated as absent; a present code is trimmed, bounded, and normalized to its
    /// upper-case form for case-insensitive uniqueness.
    /// </summary>
    public static (string? Display, string? Normalized) ValidateCode(string? value)
    {
        if (value is null)
        {
            return (null, null);
        }

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return (null, null);
        }

        if (trimmed.Length > CodeMaximumLength)
        {
            throw new AssetValidationException(
                $"Code may contain at most {CodeMaximumLength} characters.");
        }

        return (trimmed, NormalizeCode(trimmed));
    }

    public static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();

    public static string? ValidateOptionalText(string? value, int maximumLength, string field)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maximumLength)
        {
            throw new AssetValidationException(
                $"{field} may contain at most {maximumLength} characters.");
        }

        return trimmed.Length == 0 ? null : trimmed;
    }

    public static string? ValidateNotes(string? value) =>
        ValidateOptionalText(value, NotesMaximumLength, "Notes");
}

/// <summary>
/// Distinguishes the Assets domain failures so the HTTP surface can map each one to
/// its frozen <see cref="AssetsErrorCodes"/> value.
/// </summary>
internal enum AssetValidationReason
{
    /// <summary>A required string, length, or enum rule failed.</summary>
    Validation,

    /// <summary>A code collided with another asset's code, case-insensitively.</summary>
    DuplicateCode,

    /// <summary>A referenced category or location does not exist.</summary>
    CatalogReference,

    /// <summary>A visibility change would violate ownership or private-isolation rules.</summary>
    VisibilityForbidden,
}

internal sealed class AssetValidationException(
    string message,
    AssetValidationReason reason = AssetValidationReason.Validation) : Exception(message)
{
    public AssetValidationReason Reason { get; } = reason;
}
