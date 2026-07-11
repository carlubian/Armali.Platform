using Blackwing.Api.Ingestion;
using Blackwing.Shared.Storage;

namespace Blackwing.Api.Tests;

public sealed class UploadValidationTests
{
    [Fact]
    public void Detects_jpeg_png_and_webp_from_their_magic_bytes()
    {
        Assert.Equal(DetectedImageFormat.Jpeg, ImageFormatDetector.Detect([0xFF, 0xD8, 0xFF, 0xE0, 0x00]));
        Assert.Equal(DetectedImageFormat.Png, ImageFormatDetector.Detect([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]));
        Assert.Equal(DetectedImageFormat.WebP, ImageFormatDetector.Detect("RIFF\0\0\0\0WEBP"u8.ToArray()));
    }

    [Fact]
    public void Rejects_non_image_and_truncated_headers()
    {
        Assert.Null(ImageFormatDetector.Detect("not an image at all"u8.ToArray()));
        Assert.Null(ImageFormatDetector.Detect([0xFF, 0xD8])); // Truncated JPEG signature.
        Assert.Null(ImageFormatDetector.Detect([]));
        // RIFF container that is not WebP (e.g. WAV) is refused.
        Assert.Null(ImageFormatDetector.Detect("RIFF\0\0\0\0WAVE"u8.ToArray()));
    }

    [Theory]
    [InlineData("image/jpeg", DetectedImageFormat.Jpeg)]
    [InlineData("image/png", DetectedImageFormat.Png)]
    [InlineData("image/webp", DetectedImageFormat.WebP)]
    public void Maps_each_format_to_its_canonical_content_type(string expected, DetectedImageFormat format) =>
        Assert.Equal(expected, ImageFormatDetector.ContentType(format));

    [Fact]
    public void Parses_a_valid_exif_capture_date_as_utc()
    {
        var parsed = ImageProcessingService.ParseExifDate("2024:05:17 08:30:45");
        Assert.Equal(new DateTimeOffset(2024, 5, 17, 8, 30, 45, TimeSpan.Zero), parsed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a date")]
    [InlineData("0000:00:00 00:00:00")] // The camera placeholder for "unknown".
    public void Rejects_missing_or_placeholder_exif_dates(string? raw) =>
        Assert.Null(ImageProcessingService.ParseExifDate(raw));
}
