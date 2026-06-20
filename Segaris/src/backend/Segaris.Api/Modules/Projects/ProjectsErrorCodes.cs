using Segaris.Shared.Api;

namespace Segaris.Api.Modules.Projects;

/// <summary>Stable machine-readable Projects failures.</summary>
internal static class ProjectsErrorCodes
{
    public static readonly ErrorCode ProjectNotFound = new("projects.project.not_found");
    public static readonly ErrorCode ProjectValidation = new("projects.project.validation");
    public static readonly ErrorCode ProjectVisibilityForbidden = new("projects.project.visibility_forbidden");

    public static readonly ErrorCode ActivityNotFound = new("projects.activity.not_found");
    public static readonly ErrorCode ActivityValidation = new("projects.activity.validation");
    public static readonly ErrorCode ActivityVisibilityForbidden = new("projects.activity.visibility_forbidden");

    public static readonly ErrorCode RiskNotFound = new("projects.risk.not_found");
    public static readonly ErrorCode RiskValidation = new("projects.risk.validation");

    public static readonly ErrorCode AttachmentNotFound = new("projects.attachment.not_found");
    public static readonly ErrorCode AttachmentInvalid = new("projects.attachment.invalid");

    public static readonly ErrorCode ProgramNotFound = new("projects.program.not_found");
    public static readonly ErrorCode ProgramValidation = new("projects.program.validation");
    public static readonly ErrorCode ProgramDuplicateCode = new("projects.program.duplicate_code");

    public static readonly ErrorCode AxisNotFound = new("projects.axis.not_found");
    public static readonly ErrorCode AxisValidation = new("projects.axis.validation");
    public static readonly ErrorCode AxisDuplicateCode = new("projects.axis.duplicate_code");

    // Reassignment-on-delete for non-empty programs and axes.
    public static readonly ErrorCode ReassignmentRequired = new("projects.structure.reassignment_required");
    public static readonly ErrorCode NoCompatibleTarget = new("projects.structure.no_compatible_target");
    public static readonly ErrorCode InvalidReassignmentTarget = new("projects.structure.invalid_target");
}
