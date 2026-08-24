using Blackwing.Shared.Api;

namespace Blackwing.Api.Platform.Api;

/// <summary>
/// The single way an endpoint reports a failure. <see cref="ApiExceptionHandler"/> turns it into
/// an RFC 9457 <c>application/problem+json</c> response with a stable error code.
/// </summary>
internal sealed class ApiProblemException : Exception
{
    public ApiProblemException(
        int statusCode,
        ErrorCode code,
        string title,
        string? detail = null,
        IReadOnlyDictionary<string, string[]>? errors = null)
        : base(detail ?? title)
    {
        StatusCode = statusCode;
        Code = code;
        Title = title;
        Detail = detail;
        Errors = errors;
    }

    public int StatusCode { get; }

    public ErrorCode Code { get; }

    public string Title { get; }

    public string? Detail { get; }

    public IReadOnlyDictionary<string, string[]>? Errors { get; }

    public static ApiProblemException NotFound() => new(
        StatusCodes.Status404NotFound,
        ApiErrorCodes.NotFound,
        "The requested resource was not found.");
}
