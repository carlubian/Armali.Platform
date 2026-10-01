using Blackwing.Api.IntegrationTests.Images;
using Blackwing.Api.Platform.Images;

namespace Blackwing.UnitTests.Images;

/// <summary>
/// Capture-date extraction. The cases that matter are the ones where there is nothing to read:
/// an unreadable date must never be the reason a photo cannot be uploaded.
/// </summary>
public sealed class ImageMetadataReaderTests
{
    [Fact]
    public void The_capture_date_is_read_from_date_time_original_and_taken_as_utc()
    {
        var jpeg = TestImages.JpegWithExif(64, 48, orientation: 1, new DateTime(2021, 7, 15, 10, 30, 45));
        using var stream = new MemoryStream(jpeg);

        var captured = ImageMetadataReader.ReadCapturedAt(stream);

        Assert.Equal(new DateTimeOffset(2021, 7, 15, 10, 30, 45, TimeSpan.Zero), captured);
        Assert.Equal(TimeSpan.Zero, captured!.Value.Offset);
    }

    [Fact]
    public void The_stream_is_left_at_the_start_so_the_next_reader_can_use_it()
    {
        var jpeg = TestImages.JpegWithExif(64, 48, orientation: 1, new DateTime(2020, 1, 2, 3, 4, 5));
        using var stream = new MemoryStream(jpeg);

        ImageMetadataReader.ReadCapturedAt(stream);

        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public void A_jpeg_with_exif_but_no_date_has_no_capture_date()
    {
        using var stream = new MemoryStream(TestImages.JpegWithExif(64, 48, orientation: 6, dateTimeOriginal: null));

        Assert.Null(ImageMetadataReader.ReadCapturedAt(stream));
    }

    [Theory]
    [MemberData(nameof(ImagesWithoutExif))]
    public void An_image_without_exif_has_no_capture_date(string format, byte[] encoded)
    {
        using var stream = new MemoryStream(encoded);

        Assert.True(ImageMetadataReader.ReadCapturedAt(stream) is null, $"{format} reported a date.");
    }

    [Fact]
    public void Bytes_that_are_not_an_image_yield_null_instead_of_an_exception()
    {
        using var stream = new MemoryStream(TestImages.NotAnImage());

        Assert.Null(ImageMetadataReader.ReadCapturedAt(stream));
        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public void A_damaged_jpeg_yields_null_instead_of_an_exception()
    {
        using var stream = new MemoryStream(TestImages.CorruptJpeg());

        Assert.Null(ImageMetadataReader.ReadCapturedAt(stream));
    }

    public static TheoryData<string, byte[]> ImagesWithoutExif() => new()
    {
        { "jpeg", TestImages.Jpeg() },
        { "png", TestImages.Png() },
        { "webp", TestImages.Webp() },
    };
}
