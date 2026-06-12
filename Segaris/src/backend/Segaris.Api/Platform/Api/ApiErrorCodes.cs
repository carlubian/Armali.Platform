using Segaris.Shared.Api;

namespace Segaris.Api.Platform.Api;

internal static class ApiErrorCodes
{
    public static readonly ErrorCode BadRequest = new("request.invalid");
    public static readonly ErrorCode Unauthorized = new("authentication.required");
    public static readonly ErrorCode Forbidden = new("authorization.forbidden");
    public static readonly ErrorCode NotFound = new("resource.not_found");
    public static readonly ErrorCode Conflict = new("resource.conflict");
    public static readonly ErrorCode Unprocessable = new("request.unprocessable");
    public static readonly ErrorCode RequestTooLarge = new("request.too_large");
    public static readonly ErrorCode Unexpected = new("server.unexpected");
    public static readonly ErrorCode Unavailable = new("server.unavailable");

    public static ErrorCode ForStatus(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => BadRequest,
        StatusCodes.Status401Unauthorized => Unauthorized,
        StatusCodes.Status403Forbidden => Forbidden,
        StatusCodes.Status404NotFound => NotFound,
        StatusCodes.Status409Conflict => Conflict,
        StatusCodes.Status413PayloadTooLarge => RequestTooLarge,
        StatusCodes.Status422UnprocessableEntity => Unprocessable,
        StatusCodes.Status503ServiceUnavailable => Unavailable,
        _ => Unexpected,
    };
}
