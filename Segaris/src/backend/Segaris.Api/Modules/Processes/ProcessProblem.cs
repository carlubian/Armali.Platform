using Segaris.Api.Modules.Processes.Domain;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Processes;

/// <summary>Translates process failures into stable HTTP problem responses.</summary>
internal static class ProcessProblem
{
    public static ApiProblemException NotFound() => new(
        StatusCodes.Status404NotFound,
        ProcessesErrorCodes.ProcessNotFound,
        "Process not found.");

    public static ApiProblemException From(ProcessesValidationException exception) =>
        exception.Reason switch
        {
            ProcessesValidationReason.UnknownCategory => new(
                StatusCodes.Status400BadRequest,
                ProcessesErrorCodes.UnknownCategory,
                "Process validation failed.",
                errors: Errors("categoryId", exception.Message)),
            ProcessesValidationReason.VisibilityForbidden => new(
                StatusCodes.Status403Forbidden,
                ProcessesErrorCodes.ProcessVisibilityForbidden,
                "Process visibility change is forbidden.",
                errors: Errors("visibility", exception.Message)),
            _ => Validation(exception.Message),
        };

    public static ApiProblemException Validation(string message) => new(
        StatusCodes.Status400BadRequest,
        ProcessesErrorCodes.ProcessValidation,
        "Process validation failed.",
        errors: Errors("process", message));

    private static Dictionary<string, string[]> Errors(string field, string message) =>
        new(StringComparer.Ordinal)
        {
            [field] = [message],
        };
}
