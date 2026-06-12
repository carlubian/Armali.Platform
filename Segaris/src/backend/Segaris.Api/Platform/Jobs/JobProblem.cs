using Segaris.Api.Platform.Api;

namespace Segaris.Api.Platform.Jobs;

internal static class JobProblem
{
    /// <summary>
    /// A conflicting start request for an exclusive job type. The active job identifier is
    /// disclosed because the caller is already authorized to start the operation.
    /// </summary>
    public static ApiProblemException AlreadyActive(string jobType, int activeJobId) => new(
        StatusCodes.Status409Conflict,
        ApiErrorCodes.Conflict,
        $"A {jobType} job is already queued or running.",
        errors: new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["activeJobId"] = [activeJobId.ToString(System.Globalization.CultureInfo.InvariantCulture)],
        });

    public static ApiProblemException Unprocessable(string detail) => new(
        StatusCodes.Status422UnprocessableEntity,
        ApiErrorCodes.Unprocessable,
        detail);
}
