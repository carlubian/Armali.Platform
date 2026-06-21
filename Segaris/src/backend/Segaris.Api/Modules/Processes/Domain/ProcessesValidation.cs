using System.Diagnostics.CodeAnalysis;
using Segaris.Shared.Authorization;

namespace Segaris.Api.Modules.Processes.Domain;

/// <summary>
/// Domain limits and validation rules for processes, steps, and the Processes-owned
/// <c>ProcessCategory</c> catalogue. The field limits mirror
/// <see cref="ProcessesDefaults"/> and are re-exposed here so the entities validate
/// without depending on presentation defaults. The catalogue name limit matches the
/// established module-owned catalogue convention.
/// </summary>
internal static class ProcessesValidation
{
    public const int NameMaximumLength = ProcessesDefaults.NameMaximumLength;
    public const int NotesMaximumLength = ProcessesDefaults.NotesMaximumLength;
    public const int StepDescriptionMaximumLength = ProcessesDefaults.StepDescriptionMaximumLength;
    public const int StepNotesMaximumLength = ProcessesDefaults.StepNotesMaximumLength;
    public const int CategoryNameMaximumLength = 100;

    public static string ValidateName(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > NameMaximumLength)
        {
            throw new ProcessesValidationException(
                $"Name is required and may contain at most {NameMaximumLength} characters.");
        }

        return trimmed;
    }

    public static string ValidateStepDescription(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > StepDescriptionMaximumLength)
        {
            throw new ProcessesValidationException(
                $"Step description is required and may contain at most {StepDescriptionMaximumLength} characters.",
                ProcessesValidationReason.StepValidation);
        }

        return trimmed;
    }

    public static string? ValidateNotes(string? value) => ValidateOptionalText(value, NotesMaximumLength);

    public static string? ValidateStepNotes(string? value) =>
        ValidateOptionalText(value, StepNotesMaximumLength, ProcessesValidationReason.StepValidation);

    public static void EnsureKnownVisibility(RecordVisibility visibility)
    {
        if (!Enum.IsDefined(visibility))
        {
            throw new ProcessesValidationException("Visibility is invalid.");
        }
    }

    public static void EnsureKnownExecutionState(StepExecutionState state)
    {
        if (!Enum.IsDefined(state))
        {
            throw new ProcessesValidationException(
                "Execution state is invalid.",
                ProcessesValidationReason.StepValidation);
        }
    }

    public static void EnsurePositiveIdentifier(int value, string field)
    {
        if (value <= 0)
        {
            throw new ProcessesValidationException($"{field} must be positive.");
        }
    }

    public static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ProcessesValidationException("Technical timestamps must use UTC.");
        }
    }

    private static string? ValidateOptionalText(
        string? value,
        int maximumLength,
        ProcessesValidationReason reason = ProcessesValidationReason.Validation)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maximumLength)
        {
            throw new ProcessesValidationException(
                $"Notes may contain at most {maximumLength} characters.",
                reason);
        }

        return trimmed.Length == 0 ? null : trimmed;
    }
}

/// <summary>
/// Distinguishes the Processes domain failures so the HTTP surface can map each one to
/// its frozen <see cref="ProcessesErrorCodes"/> value.
/// </summary>
internal enum ProcessesValidationReason
{
    /// <summary>A required string, length, or enum rule on the process failed.</summary>
    Validation,

    /// <summary>A required string, length, or enum rule on a step failed.</summary>
    StepValidation,

    /// <summary>The step does not exist inside the accessible process.</summary>
    StepNotFound,

    /// <summary>The referenced process category does not exist.</summary>
    UnknownCategory,

    /// <summary>A visibility change would violate ownership or private-isolation rules.</summary>
    VisibilityForbidden,

    /// <summary>A step was skipped while it was not optional.</summary>
    StepNotOptional,

    /// <summary>A frontier rule (complete, skip, or undo order) was violated.</summary>
    FrontierViolation,

    /// <summary>A restructure broke the resolved-prefix contiguity invariant.</summary>
    ContiguityViolation,
}

[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Domain failures carry a stable reason.")]
internal sealed class ProcessesValidationException(
    string message,
    ProcessesValidationReason reason = ProcessesValidationReason.Validation) : Exception(message)
{
    public ProcessesValidationReason Reason { get; } = reason;
}
