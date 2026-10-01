using Blackwing.Shared.Api;

namespace Blackwing.Api.Platform.Images;

/// <summary>Error codes of the image module. Platform-wide ones live in <c>ApiErrorCodes</c>.</summary>
internal static class ImageErrorCodes
{
    public static readonly ErrorCode UnsupportedFormat = new("image.format_unsupported");
    public static readonly ErrorCode ContentMismatch = new("image.content_mismatch");
    public static readonly ErrorCode TooLarge = new("image.too_large");
    public static readonly ErrorCode Duplicate = new("image.duplicate");
    public static readonly ErrorCode Undecodable = new("image.undecodable");
    public static readonly ErrorCode StorageUnavailable = new("image.storage_unavailable");
}
