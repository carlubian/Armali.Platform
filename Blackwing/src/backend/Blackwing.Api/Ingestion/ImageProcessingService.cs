using Blackwing.Api.Configuration;
using Blackwing.Shared.Storage;
using ImageMagick;
using Microsoft.Extensions.Options;

namespace Blackwing.Api.Ingestion;

/// <summary>
/// Thrown when staged bytes cannot be decoded as an image. Permanent: a retry
/// cannot help, so the staged bytes are discarded and the job fails for good.
/// </summary>
public sealed class InvalidImageException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>The metadata read from an image plus its two generated WebP derivatives.</summary>
public sealed record ProcessedImage(
    DetectedImageFormat Format,
    int Width,
    int Height,
    DateTimeOffset? CapturedAt,
    byte[] Preview,
    byte[] Thumbnail);

/// <summary>
/// Turns a decoded image into the two derivatives Blackwing serves and reads the
/// facts the gallery needs. Everything is normalised to its EXIF orientation so the
/// derivatives are upright, and both are re-encoded to WebP. The untouched original
/// is stored separately by the worker. Magick.NET (ImageMagick, Apache-2.0) is the
/// engine; resource limits cap the memory any single decode may use.
/// </summary>
public sealed class ImageProcessingService
{
    private readonly IngestionOptions options;

    public ImageProcessingService(IOptions<BlackwingOptions> options)
    {
        this.options = options.Value.Ingestion;
        // Bound the memory a single decode may consume so a crafted or huge file
        // cannot exhaust the process; ImageMagick spills to disk past the limit.
        ResourceLimits.Memory = (ulong)this.options.ProcessingMemoryLimitBytes;
    }

    /// <summary>
    /// Reads dimensions, EXIF capture date and orientation from the staged bytes and
    /// produces an upright preview and thumbnail as WebP. Throws
    /// <see cref="InvalidImageException"/> when the bytes are not a decodable image.
    /// </summary>
    public ProcessedImage Process(Stream source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();

        MagickImage image;
        try
        {
            image = new MagickImage(source);
        }
        catch (MagickException exception)
        {
            throw new InvalidImageException("The uploaded file is not a decodable image.", exception);
        }

        using (image)
        {
            var format = MapFormat(image.Format)
                ?? throw new InvalidImageException($"Unsupported decoded format '{image.Format}'.");

            var capturedAt = ReadCaptureDate(image);

            // Rotate pixels upright and drop the now-misleading orientation tag so the
            // recorded dimensions and every derivative agree on a single orientation.
            image.AutoOrient();
            var width = (int)image.Width;
            var height = (int)image.Height;

            cancellationToken.ThrowIfCancellationRequested();
            var preview = Encode(image, options.PreviewMaxEdge);
            var thumbnail = Encode(image, options.ThumbnailMaxEdge);

            return new ProcessedImage(format, width, height, capturedAt, preview, thumbnail);
        }
    }

    private byte[] Encode(MagickImage image, int maxEdge)
    {
        using var derivative = (MagickImage)image.Clone();
        // Only ever shrink: a small original keeps its size rather than being upscaled.
        if (derivative.Width > (uint)maxEdge || derivative.Height > (uint)maxEdge)
            derivative.Resize(new MagickGeometry((uint)maxEdge, (uint)maxEdge) { IgnoreAspectRatio = false, Greater = true, Less = false });
        derivative.Strip(); // Derivatives carry no EXIF/metadata.
        derivative.Format = MagickFormat.WebP;
        derivative.Quality = (uint)options.WebpQuality;
        return derivative.ToByteArray();
    }

    private static DateTimeOffset? ReadCaptureDate(MagickImage image)
    {
        var exif = image.GetExifProfile();
        if (exif is null) return null;

        var value = FirstDateValue(exif, ExifTag.DateTimeOriginal)
            ?? FirstDateValue(exif, ExifTag.DateTimeDigitized)
            ?? FirstDateValue(exif, ExifTag.DateTime);
        return value;
    }

    private static DateTimeOffset? FirstDateValue(IExifProfile exif, ExifTag<string> tag)
    {
        var raw = exif.GetValue(tag)?.Value;
        return ParseExifDate(raw);
    }

    /// <summary>
    /// Parses the EXIF date format "yyyy:MM:dd HH:mm:ss". EXIF carries no time zone,
    /// so the reading is treated as UTC — an approximation, but consistent and never
    /// wrong by more than the local offset for ordering purposes.
    /// </summary>
    internal static DateTimeOffset? ParseExifDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (!DateTime.TryParseExact(raw.Trim(), "yyyy:MM:dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsed))
            return null;
        if (parsed.Year < 1900) return null; // Reject placeholder "0000:00:00" style values.
        return new DateTimeOffset(parsed, TimeSpan.Zero);
    }

    private static DetectedImageFormat? MapFormat(MagickFormat format) => format switch
    {
        MagickFormat.Jpeg or MagickFormat.Jpg => DetectedImageFormat.Jpeg,
        MagickFormat.Png or MagickFormat.Png00 or MagickFormat.Png8 or MagickFormat.Png24 or MagickFormat.Png32 or MagickFormat.Png48 or MagickFormat.Png64 => DetectedImageFormat.Png,
        MagickFormat.WebP => DetectedImageFormat.WebP,
        _ => null,
    };
}
