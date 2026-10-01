using Blackwing.Api.Platform.Api;

namespace Blackwing.Api.Platform.Images;

/// <summary>
/// Decides what counts as an acceptable image upload: three formats, recognised by their leading
/// bytes rather than by what the client claims.
/// </summary>
/// <remarks>
/// Ported from the image part of Segaris' <c>AttachmentPolicy</c>, keeping only JPEG, PNG and WebP.
/// The extension picks the expected format, but trust sits in the bytes: a file whose extension
/// and content disagree is rejected.
/// </remarks>
internal static class ImagePolicy
{
    public const int MaximumFileNameLength = 255;

    /// <summary>Enough leading bytes to recognise every accepted format.</summary>
    private const int HeaderLength = 12;

    private static readonly byte[] JpegSignature = [0xff, 0xd8, 0xff];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];

    private static readonly Dictionary<string, string> ContentTypesByExtension =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp",
        };

    /// <summary>
    /// Strips surrounding whitespace and checks the name: no path, no control characters, at
    /// most 255 characters and an allowed extension.
    /// </summary>
    public static string NormalizeAndValidateFileName(string? fileName)
    {
        var trimmed = fileName?.Trim() ?? string.Empty;
        var normalized = Path.GetFileName(trimmed);

        if (normalized.Length == 0
            || normalized.Length > MaximumFileNameLength
            || !string.Equals(trimmed, normalized, StringComparison.Ordinal)
            || normalized.Contains('/', StringComparison.Ordinal)
            || normalized.Contains('\\', StringComparison.Ordinal)
            || normalized.Any(char.IsControl))
        {
            throw new ApiProblemException(
                StatusCodes.Status400BadRequest,
                ApiErrorCodes.BadRequest,
                "One or more request values are invalid.",
                errors: new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    ["fileName"] = ["The file name is invalid."],
                });
        }

        if (!ContentTypesByExtension.ContainsKey(Path.GetExtension(normalized)))
        {
            throw new ApiProblemException(
                StatusCodes.Status400BadRequest,
                ImageErrorCodes.UnsupportedFormat,
                "The image format is not supported.",
                "Only JPEG, PNG and WebP images are accepted.");
        }

        return normalized;
    }

    /// <summary>
    /// The content type that corresponds to the extension of an already validated file name.
    /// </summary>
    public static string ResolveContentType(string fileName) =>
        ContentTypesByExtension[Path.GetExtension(fileName)];

    /// <summary>
    /// Rejects a content type declared by the client that does not match the file extension. A
    /// client that declares nothing is fine: the bytes are what is trusted.
    /// </summary>
    public static void EnsureDeclaredContentTypeMatches(string fileName, string? declaredContentType)
    {
        if (string.IsNullOrWhiteSpace(declaredContentType))
        {
            return;
        }

        var declared = declaredContentType.Split(';', 2)[0].Trim();
        if (!string.Equals(declared, ResolveContentType(fileName), StringComparison.OrdinalIgnoreCase))
        {
            throw ContentMismatch();
        }
    }

    /// <summary>
    /// Checks the leading bytes against the format the extension promises. The stream is left at
    /// position zero whether it validates or not.
    /// </summary>
    public static void ValidateContent(string fileName, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        stream.Position = 0;
        try
        {
            Span<byte> header = stackalloc byte[HeaderLength];
            var read = stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);

            var detected = DetectContentType(header[..read]);
            if (!string.Equals(detected, ResolveContentType(fileName), StringComparison.Ordinal))
            {
                throw ContentMismatch();
            }
        }
        finally
        {
            stream.Position = 0;
        }
    }

    /// <summary>
    /// Recognises an accepted format from its leading bytes, or returns <see langword="null"/>.
    /// </summary>
    public static string? DetectContentType(ReadOnlySpan<byte> header)
    {
        if (header.StartsWith(JpegSignature))
        {
            return "image/jpeg";
        }

        if (header.StartsWith(PngSignature))
        {
            return "image/png";
        }

        if (header.Length >= HeaderLength
            && header[..4].SequenceEqual("RIFF"u8)
            && header[8..12].SequenceEqual("WEBP"u8))
        {
            return "image/webp";
        }

        return null;
    }

    private static ApiProblemException ContentMismatch() => new(
        StatusCodes.Status400BadRequest,
        ImageErrorCodes.ContentMismatch,
        "The file content does not match its declared format.");
}
