namespace Segaris.Api.Modules.Destinations.Domain;

/// <summary>Shared validation rules for the Destinations domain entities.</summary>
internal static class DestinationsValidation
{
    public static string ValidateName(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > DestinationsDefaults.NameMaximumLength)
        {
            throw new DestinationsValidationException(
                $"Name is required and may contain at most {DestinationsDefaults.NameMaximumLength} characters.");
        }

        return trimmed;
    }

    public static string? ValidateCountry(string? value) =>
        ValidateOptionalTrimmed(value, DestinationsDefaults.CountryMaximumLength, "Country");

    public static string? ValidateEntryRequirements(string? value) =>
        ValidateOptionalText(value, DestinationsDefaults.EntryRequirementsMaximumLength, "Entry requirements");

    public static string? ValidateNotes(string? value) =>
        ValidateOptionalText(value, DestinationsDefaults.NotesMaximumLength, "Notes");

    public static string ValidatePlaceName(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > DestinationsDefaults.PlaceNameMaximumLength)
        {
            throw new DestinationsValidationException(
                $"Place name is required and may contain at most {DestinationsDefaults.PlaceNameMaximumLength} characters.");
        }

        return trimmed;
    }

    public static int? ValidateRating(int? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value < DestinationsDefaults.MinimumPlaceRating || value > DestinationsDefaults.MaximumPlaceRating)
        {
            throw new DestinationsValidationException(
                $"Rating must be between {DestinationsDefaults.MinimumPlaceRating} and {DestinationsDefaults.MaximumPlaceRating}.");
        }

        return value;
    }

    public static string? ValidateReview(string? value) =>
        ValidateOptionalText(value, DestinationsDefaults.PlaceReviewMaximumLength, "Review");

    public static string? ValidateAddress(string? value) =>
        ValidateOptionalTrimmed(value, DestinationsDefaults.PlaceAddressMaximumLength, "Address");

    public static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DestinationsValidationException("Technical timestamps must use UTC.");
        }
    }

    public static void EnsurePositiveIdentifier(int value, string label)
    {
        if (value <= 0)
        {
            throw new DestinationsValidationException($"{label} must be positive.");
        }
    }

    private static string? ValidateOptionalTrimmed(string? value, int maximumLength, string label)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maximumLength)
        {
            throw new DestinationsValidationException(
                $"{label} may contain at most {maximumLength} characters.");
        }

        return trimmed.Length == 0 ? null : trimmed;
    }

    private static string? ValidateOptionalText(string? value, int maximumLength, string label)
    {
        if (value is null)
        {
            return null;
        }

        if (value.Length > maximumLength)
        {
            throw new DestinationsValidationException(
                $"{label} may contain at most {maximumLength} characters.");
        }

        return value.Length == 0 ? null : value;
    }
}

/// <summary>
/// Distinguishes the Destinations domain failures so the HTTP surface can map each one
/// to its frozen <see cref="DestinationsErrorCodes"/> value.
/// </summary>
internal enum DestinationsValidationReason
{
    /// <summary>A required string, length, enum, or bound rule failed.</summary>
    Validation,

    /// <summary>A referenced category does not exist or has become invalid.</summary>
    CatalogReference,

    /// <summary>A visibility change would violate ownership or privacy rules.</summary>
    VisibilityForbidden,
}

internal sealed class DestinationsValidationException(
    string message,
    DestinationsValidationReason reason = DestinationsValidationReason.Validation) : Exception(message)
{
    public DestinationsValidationReason Reason { get; } = reason;
}
