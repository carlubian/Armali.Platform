using Segaris.Api.Platform.Api;
using Segaris.Api.Modules.Projects.Domain;

namespace Segaris.Api.Modules.Projects;

internal static class ProjectsStructureProblem
{
    public static ApiProblemException ProgramNotFound() => new(
        StatusCodes.Status404NotFound,
        ProjectsErrorCodes.ProgramNotFound,
        "Project program not found.");

    public static ApiProblemException AxisNotFound() => new(
        StatusCodes.Status404NotFound,
        ProjectsErrorCodes.AxisNotFound,
        "Project axis not found.");

    public static ApiProblemException ProgramValidation(string field, string message) => new(
        StatusCodes.Status400BadRequest,
        ProjectsErrorCodes.ProgramValidation,
        "Project program validation failed.",
        errors: new Dictionary<string, string[]> { [field] = [message] });

    public static ApiProblemException AxisValidation(string field, string message) => new(
        StatusCodes.Status400BadRequest,
        ProjectsErrorCodes.AxisValidation,
        "Project axis validation failed.",
        errors: new Dictionary<string, string[]> { [field] = [message] });

    public static ApiProblemException ProgramDuplicateCode() => new(
        StatusCodes.Status409Conflict,
        ProjectsErrorCodes.ProgramDuplicateCode,
        "Project program code already exists.");

    public static ApiProblemException AxisDuplicateCode() => new(
        StatusCodes.Status409Conflict,
        ProjectsErrorCodes.AxisDuplicateCode,
        "Project axis code already exists.");

    public static ApiProblemException ReassignmentRequired() => new(
        StatusCodes.Status409Conflict,
        ProjectsErrorCodes.ReassignmentRequired,
        "The structural node has children and requires reassignment before deletion.");

    public static ApiProblemException NoCompatibleTarget() => new(
        StatusCodes.Status409Conflict,
        ProjectsErrorCodes.NoCompatibleTarget,
        "No compatible reassignment target exists.");

    public static ApiProblemException InvalidReassignmentTarget() => new(
        StatusCodes.Status400BadRequest,
        ProjectsErrorCodes.InvalidReassignmentTarget,
        "The reassignment target is invalid.");

    public static ApiProblemException FromProgramValidation(ProjectsValidationException exception) =>
        exception.Reason == ProjectsValidationReason.InvalidCode
            ? ProgramValidation("code", exception.Message)
            : ProgramValidation("name", exception.Message);

    public static ApiProblemException FromAxisValidation(ProjectsValidationException exception) =>
        exception.Reason == ProjectsValidationReason.InvalidCode
            ? AxisValidation("code", exception.Message)
            : AxisValidation("name", exception.Message);
}
