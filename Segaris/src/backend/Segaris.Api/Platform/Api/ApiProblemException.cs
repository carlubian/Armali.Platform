using Segaris.Shared.Api;

namespace Segaris.Api.Platform.Api;

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
