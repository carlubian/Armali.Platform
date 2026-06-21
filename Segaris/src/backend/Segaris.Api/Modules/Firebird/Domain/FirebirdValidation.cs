using System.Diagnostics.CodeAnalysis;
using Segaris.Shared.Authorization;

namespace Segaris.Api.Modules.Firebird.Domain;

internal static class FirebirdValidation
{
    public const int NameMaximumLength = FirebirdDefaults.NameMaximumLength;
    public const int NotesMaximumLength = FirebirdDefaults.NotesMaximumLength;
    public const int UsernameHandleMaximumLength = FirebirdDefaults.UsernameHandleMaximumLength;
    public const int UsernameNotesMaximumLength = FirebirdDefaults.UsernameNotesMaximumLength;
    public const int InteractionDescriptionMaximumLength = FirebirdDefaults.InteractionDescriptionMaximumLength;
    public const int CatalogNameMaximumLength = FirebirdDefaults.CatalogNameMaximumLength;

    public static string ValidateName(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > NameMaximumLength)
        {
            throw new FirebirdValidationException(
                $"Name is required and may contain at most {NameMaximumLength} characters.");
        }

        return trimmed;
    }

    public static string ValidateUsernameHandle(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > UsernameHandleMaximumLength)
        {
            throw new FirebirdValidationException(
                $"Username handle is required and may contain at most {UsernameHandleMaximumLength} characters.",
                FirebirdValidationReason.UsernameValidation);
        }

        return trimmed;
    }

    public static string ValidateInteractionDescription(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > InteractionDescriptionMaximumLength)
        {
            throw new FirebirdValidationException(
                $"Interaction description is required and may contain at most {InteractionDescriptionMaximumLength} characters.",
                FirebirdValidationReason.InteractionValidation);
        }

        return trimmed;
    }

    public static string? ValidateNotes(string? value) => ValidateOptionalText(value, NotesMaximumLength);

    public static string? ValidateUsernameNotes(string? value) =>
        ValidateOptionalText(value, UsernameNotesMaximumLength, FirebirdValidationReason.UsernameValidation);

    public static void EnsureKnownStatusAndVisibility(PersonStatus status, RecordVisibility visibility)
    {
        if (!Enum.IsDefined(status) || !Enum.IsDefined(visibility))
        {
            throw new FirebirdValidationException("Status or visibility is invalid.");
        }
    }

    public static void EnsurePositiveIdentifier(int value, string field, FirebirdValidationReason reason = FirebirdValidationReason.Validation)
    {
        if (value <= 0)
        {
            throw new FirebirdValidationException($"{field} must be positive.", reason);
        }
    }

    public static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new FirebirdValidationException("Technical timestamps must use UTC.");
        }
    }

    public static void EnsureNotFuture(DateOnly value, DateOnly today)
    {
        if (value > today)
        {
            throw new FirebirdValidationException(
                "Interaction date cannot be in the future.",
                FirebirdValidationReason.InteractionValidation);
        }
    }

    private static string? ValidateOptionalText(
        string? value,
        int maximumLength,
        FirebirdValidationReason reason = FirebirdValidationReason.Validation)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maximumLength)
        {
            throw new FirebirdValidationException(
                $"Text may contain at most {maximumLength} characters.",
                reason);
        }

        return trimmed.Length == 0 ? null : trimmed;
    }
}

internal enum FirebirdValidationReason
{
    Validation,
    UsernameValidation,
    InteractionValidation,
    UnknownCatalogReference,
    VisibilityForbidden,
}

[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Domain failures carry a stable reason.")]
internal sealed class FirebirdValidationException(
    string message,
    FirebirdValidationReason reason = FirebirdValidationReason.Validation) : Exception(message)
{
    public FirebirdValidationReason Reason { get; } = reason;
}

