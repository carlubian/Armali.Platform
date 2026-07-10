namespace Blackwing.Shared.Storage;

/// <summary>The image container formats Blackwing accepts on upload.</summary>
public enum DetectedImageFormat
{
    Jpeg,
    Png,
    WebP,
}

/// <summary>
/// Content sniffing for uploads. The multipart content-type header is only a hint
/// and can lie; the real format is decided from the leading magic bytes so a
/// mislabelled or hostile file is rejected before it reaches the processor. This is
/// a cheap gate — the image processor decodes the bytes and remains authoritative.
/// </summary>
public static class ImageFormatDetector
{
    /// <summary>Bytes needed to recognise every accepted format (WebP checks up to offset 12).</summary>
    public const int HeaderBytes = 12;

    /// <summary>Recognises an accepted format from a file header, or returns <c>null</c> when unsupported.</summary>
    public static DetectedImageFormat? Detect(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return DetectedImageFormat.Jpeg;

        if (header.Length >= 8
            && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
            && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
            return DetectedImageFormat.Png;

        if (header.Length >= 12
            && header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F'
            && header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P')
            return DetectedImageFormat.WebP;

        return null;
    }

    /// <summary>The canonical content type stored for an accepted format.</summary>
    public static string ContentType(DetectedImageFormat format) => format switch
    {
        DetectedImageFormat.Jpeg => "image/jpeg",
        DetectedImageFormat.Png => "image/png",
        DetectedImageFormat.WebP => "image/webp",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown image format."),
    };
}
