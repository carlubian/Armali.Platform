using System.Diagnostics.CodeAnalysis;
using Segaris.Shared.Authorization;

namespace Segaris.Api.Modules.Projects.Domain;

internal static class ProjectsValidation
{
    public const int NameMaximumLength = ProjectsDefaults.NameMaximumLength;
    public const int CodeLength = ProjectsDefaults.CodeLength;

    public static string ValidateName(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > NameMaximumLength)
        {
            throw new ProjectsValidationException(
                $"Name is required and may contain at most {NameMaximumLength} characters.");
        }

        return trimmed;
    }

    public static string ValidateCode(string? value)
    {
        var trimmed = value?.Trim();
        if (trimmed is null
            || trimmed.Length != CodeLength
            || !trimmed.All(static character => character is >= 'A' and <= 'Z'))
        {
            throw new ProjectsValidationException(
                $"Code is required and must be exactly {CodeLength} uppercase ASCII letters.",
                ProjectsValidationReason.InvalidCode);
        }

        return trimmed;
    }

    public static void EnsureKnownStatusAndVisibility(ProjectStatus status, RecordVisibility visibility)
    {
        if (!Enum.IsDefined(status) || !Enum.IsDefined(visibility))
        {
            throw new ProjectsValidationException("Status or visibility is invalid.");
        }
    }

    public static void EnsurePositiveIdentifier(int value, string field)
    {
        if (value <= 0)
        {
            throw new ProjectsValidationException($"{field} must be positive.");
        }
    }

    public static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ProjectsValidationException("Technical timestamps must use UTC.");
        }
    }
}

internal enum ProjectsValidationReason
{
    Validation,
    InvalidCode,
    VisibilityForbidden,
}

[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Domain failures carry a stable reason.")]
internal sealed class ProjectsValidationException(
    string message,
    ProjectsValidationReason reason = ProjectsValidationReason.Validation) : Exception(message)
{
    public ProjectsValidationReason Reason { get; } = reason;
}
