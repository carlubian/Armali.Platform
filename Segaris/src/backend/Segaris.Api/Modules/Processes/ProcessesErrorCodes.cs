using Segaris.Shared.Api;

namespace Segaris.Api.Modules.Processes;

/// <summary>Stable machine-readable Processes failures.</summary>
internal static class ProcessesErrorCodes
{
    public static readonly ErrorCode ProcessNotFound = new("processes.process.not_found");
    public static readonly ErrorCode ProcessValidation = new("processes.process.validation");
    public static readonly ErrorCode ProcessVisibilityForbidden = new("processes.process.visibility_forbidden");
    public static readonly ErrorCode UnknownCategory = new("processes.process.unknown_category");

    public static readonly ErrorCode StepNotFound = new("processes.step.not_found");
    public static readonly ErrorCode StepValidation = new("processes.step.validation");

    // Restructure that breaks the resolved-prefix contiguity invariant.
    public static readonly ErrorCode StepContiguityViolation = new("processes.step.contiguity_violation");

    // Completing, skipping, or undoing a step out of frontier order.
    public static readonly ErrorCode StepFrontierViolation = new("processes.step.frontier_violation");

    // Skipping a required (non-optional) step.
    public static readonly ErrorCode StepNotOptional = new("processes.step.not_optional");

    public static readonly ErrorCode AttachmentNotFound = new("processes.attachment.not_found");
    public static readonly ErrorCode AttachmentInvalid = new("processes.attachment.invalid");

    public static readonly ErrorCode CategoryNotFound = new("processes.category.not_found");
    public static readonly ErrorCode CategoryValidation = new("processes.category.validation");
    public static readonly ErrorCode CategoryDuplicateName = new("processes.category.duplicate_name");
    public static readonly ErrorCode CategoryRequiredNotEmpty = new("processes.category.required_not_empty");

    // A referenced category may only be replaced, never cleared, before deletion.
    public static readonly ErrorCode CategoryReferenced = new("processes.category.referenced");
    public static readonly ErrorCode CategoryInvalidReplacement = new("processes.category.invalid_replacement");
    public static readonly ErrorCode CategoryMigrationConflict = new("processes.category.migration_conflict");
}
