using Segaris.Api.Modules.Projects.Domain;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Projects;

internal static class ProjectsProblem
{
    public static ApiProblemException ProjectNotFound() => new(
        StatusCodes.Status404NotFound,
        ProjectsErrorCodes.ProjectNotFound,
        "The requested project was not found.");

    public static ApiProblemException ActivityNotFound() => new(
        StatusCodes.Status404NotFound,
        ProjectsErrorCodes.ActivityNotFound,
        "The requested activity was not found.");

    public static ApiProblemException RiskNotFound() => new(
        StatusCodes.Status404NotFound,
        ProjectsErrorCodes.RiskNotFound,
        "The requested project risk was not found.");

    public static ApiProblemException AttachmentNotFound() => new(
        StatusCodes.Status404NotFound,
        ProjectsErrorCodes.AttachmentNotFound,
        "The requested project attachment was not found.");

    public static ApiProblemException AttachmentInvalid(
        string field,
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null) => new(
        StatusCodes.Status400BadRequest,
        ProjectsErrorCodes.AttachmentInvalid,
        "The attachment is invalid.",
        errors: errors ?? Errors(field, message));

    public static ApiProblemException FromProjectValidation(ProjectsValidationException exception) =>
        exception.Reason == ProjectsValidationReason.VisibilityForbidden
            ? new ApiProblemException(
                StatusCodes.Status403Forbidden,
                ProjectsErrorCodes.ProjectVisibilityForbidden,
                exception.Message)
            : new ApiProblemException(
                StatusCodes.Status400BadRequest,
                ProjectsErrorCodes.ProjectValidation,
                exception.Message);

    public static ApiProblemException FromActivityValidation(ProjectsValidationException exception) =>
        exception.Reason == ProjectsValidationReason.VisibilityForbidden
            ? new ApiProblemException(
                StatusCodes.Status403Forbidden,
                ProjectsErrorCodes.ActivityVisibilityForbidden,
                exception.Message)
            : new ApiProblemException(
                StatusCodes.Status400BadRequest,
                ProjectsErrorCodes.ActivityValidation,
                exception.Message);

    public static ApiProblemException FromRiskValidation(ProjectsValidationException exception) => new(
        StatusCodes.Status400BadRequest,
        ProjectsErrorCodes.RiskValidation,
        exception.Message);

    private static Dictionary<string, string[]> Errors(string field, string message) =>
        new(StringComparer.Ordinal)
        {
            [field] = [message],
        };
}
