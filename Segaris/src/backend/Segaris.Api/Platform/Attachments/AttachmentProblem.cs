using Segaris.Api.Platform.Api;

namespace Segaris.Api.Platform.Attachments;

internal static class AttachmentProblem
{
    public static ApiProblemException Invalid(string field, string message) => new(
        StatusCodes.Status400BadRequest,
        ApiErrorCodes.BadRequest,
        "The attachment is invalid.",
        errors: new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [field] = [message],
        });

    public static ApiProblemException StorageUnavailable() => new(
        StatusCodes.Status503ServiceUnavailable,
        ApiErrorCodes.Unavailable,
        "Attachment storage is unavailable.");
}
