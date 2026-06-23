namespace Segaris.Api.Modules.Health.Domain;

/// <summary>Domain validation helpers shared across the Health module Waves.</summary>
internal static class HealthValidation
{
    public static string ValidateName(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > HealthDefaults.NameMaximumLength)
        {
            throw new HealthValidationException(
                $"Name is required and may contain at most {HealthDefaults.NameMaximumLength} characters.");
        }

        return trimmed;
    }

    public static string? ValidateSymptoms(string? value) =>
        ValidateOptionalText(value, HealthDefaults.SymptomsMaximumLength, "Symptoms");

    public static string? ValidatePosology(string? value) =>
        ValidateOptionalText(value, HealthDefaults.PosologyMaximumLength, "Posology");

    public static string? ValidateNotes(string? value) =>
        ValidateOptionalText(value, HealthDefaults.NotesMaximumLength, "Notes");

    public static int? ValidateAverageDurationDays(int? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value < HealthDefaults.MinimumAverageDurationDays || value > HealthDefaults.MaximumAverageDurationDays)
        {
            throw new HealthValidationException(
                $"Average duration must be between {HealthDefaults.MinimumAverageDurationDays} and "
                + $"{HealthDefaults.MaximumAverageDurationDays} days.");
        }

        return value;
    }

    private static string? ValidateOptionalText(string? value, int maximumLength, string field)
    {
        if (value is null)
        {
            return null;
        }

        if (value.Length > maximumLength)
        {
            throw new HealthValidationException(
                $"{field} may contain at most {maximumLength} characters.");
        }

        return value.Length == 0 ? null : value;
    }
}

/// <summary>
/// Distinguishes the Health domain failures so the HTTP surface can map each one to
/// its frozen <see cref="HealthErrorCodes"/> value.
/// </summary>
internal enum HealthValidationReason
{
    /// <summary>A required string, length, enum, or bound rule failed.</summary>
    Validation,

    /// <summary>A referenced category does not exist or has become invalid.</summary>
    CatalogReference,

    /// <summary>A visibility change would violate ownership or privacy rules.</summary>
    VisibilityForbidden,

    /// <summary>A referenced Inventory item does not exist or is not accessible.</summary>
    ItemNotAccessible,

    /// <summary>An Inventory item reference would violate medicine visibility.</summary>
    ItemVisibilityForbidden,

    /// <summary>An association endpoint is not accessible to the acting user.</summary>
    AssociationNotAccessible,

    /// <summary>An association would violate the visibility rule on creation.</summary>
    AssociationVisibilityForbidden,

    /// <summary>Publishing a record is blocked by a non-public association.</summary>
    AssociationPublishBlocked,
}

internal sealed class HealthValidationException(
    string message,
    HealthValidationReason reason = HealthValidationReason.Validation) : Exception(message)
{
    public HealthValidationReason Reason { get; } = reason;
}
