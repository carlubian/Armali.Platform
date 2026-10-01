using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using ExtractorReader = MetadataExtractor.ImageMetadataReader;

namespace Blackwing.Api.Platform.Images;

/// <summary>
/// Reads the capture date out of an image's EXIF block. Wraps MetadataExtractor so the rest of
/// the code never has to know about it.
/// </summary>
internal static class ImageMetadataReader
{
    /// <summary>
    /// The moment the photo was taken, or <see langword="null"/> when the file carries no usable
    /// date. Looks, in order, at <c>DateTimeOriginal</c>, <c>DateTimeDigitized</c> and the
    /// general <c>DateTime</c> of the main image; the first valid one wins.
    /// </summary>
    /// <remarks>
    /// EXIF has no time zone, so the value is read as UTC and stored as such. That is
    /// deterministic, and ordering photos is the only thing the date is used for. A file without
    /// EXIF, or with a damaged block, yields <see langword="null"/> instead of an error: an
    /// unreadable date must never be the reason a photo cannot be uploaded. The stream is left at
    /// position zero.
    /// </remarks>
    public static DateTimeOffset? ReadCapturedAt(Stream content)
    {
        ArgumentNullException.ThrowIfNull(content);

        content.Position = 0;
        try
        {
            var directories = ExtractorReader.ReadMetadata(content);

            var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            if (TryRead(subIfd, ExifDirectoryBase.TagDateTimeOriginal, out var captured)
                || TryRead(subIfd, ExifDirectoryBase.TagDateTimeDigitized, out captured))
            {
                return captured;
            }

            var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            return TryRead(ifd0, ExifDirectoryBase.TagDateTime, out captured) ? captured : null;
        }
        catch (Exception exception) when (exception is ImageProcessingException or IOException)
        {
            return null;
        }
        finally
        {
            content.Position = 0;
        }
    }

    private static bool TryRead(MetadataExtractor.Directory? directory, int tag, out DateTimeOffset value)
    {
        value = default;
        if (directory is null || !directory.TryGetDateTime(tag, out var dateTime))
        {
            return false;
        }

        value = new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified), TimeSpan.Zero);
        return true;
    }
}
