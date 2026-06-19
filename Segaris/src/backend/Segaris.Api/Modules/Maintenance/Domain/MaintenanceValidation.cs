namespace Segaris.Api.Modules.Maintenance.Domain;

/// <summary>
/// Domain limits and validation rules for maintenance tasks and the Maintenance-owned
/// <c>MaintenanceType</c> catalogue. The task field limits mirror
/// <see cref="MaintenanceDefaults"/> and are re-exposed here so the entity validates
/// without depending on presentation defaults. The catalogue name limit matches the
/// established module-owned catalogue convention.
/// </summary>
internal static class MaintenanceValidation
{
    public const int TitleMaximumLength = MaintenanceDefaults.TitleMaximumLength;
    public const int NotesMaximumLength = MaintenanceDefaults.NotesMaximumLength;
    public const int TypeNameMaximumLength = 100;

    public static string ValidateTitle(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > TitleMaximumLength)
        {
            throw new MaintenanceValidationException(
                $"Title is required and may contain at most {TitleMaximumLength} characters.");
        }

        return trimmed;
    }

    public static string? ValidateNotes(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > NotesMaximumLength)
        {
            throw new MaintenanceValidationException(
                $"Notes may contain at most {NotesMaximumLength} characters.");
        }

        return trimmed.Length == 0 ? null : trimmed;
    }
}

/// <summary>
/// Distinguishes the Maintenance domain failures so the HTTP surface can map each one
/// to its frozen <see cref="MaintenanceErrorCodes"/> value.
/// </summary>
internal enum MaintenanceValidationReason
{
    /// <summary>A required string, length, or enum rule failed.</summary>
    Validation,

    /// <summary>The referenced maintenance type does not exist.</summary>
    UnknownType,

    /// <summary>The referenced asset is missing or inaccessible.</summary>
    AssetReference,

    /// <summary>The asset link violates the public/private visibility rule.</summary>
    AssetVisibilityForbidden,

    /// <summary>A visibility change would violate ownership or private-isolation rules.</summary>
    VisibilityForbidden,
}

internal sealed class MaintenanceValidationException(
    string message,
    MaintenanceValidationReason reason = MaintenanceValidationReason.Validation) : Exception(message)
{
    public MaintenanceValidationReason Reason { get; } = reason;
}
