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
}
