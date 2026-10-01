using Blackwing.Api.Configuration;
using Blackwing.Api.IntegrationTests.Images;
using Blackwing.Api.Platform.Api;
using Blackwing.Api.Platform.Images;
using Blackwing.Shared.Images;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Blackwing.UnitTests.Images;

/// <summary>
/// The imaging code against real encoded pictures. The one thing that cannot be faked is the
/// native library, so these run it for real; no database or container is involved.
/// </summary>
public sealed class SkiaImageProcessorTests
{
    private readonly SkiaImageProcessor _processor = new(Options.Create(new ImageOptions()));

    [Fact]
    public void A_large_image_gets_derivatives_whose_longest_edge_is_the_configured_one()
    {
        var processed = Process(TestImages.Jpeg(width: 3200, height: 1600));

        Assert.Equal((3200, 1600), (processed.Width, processed.Height));
        Assert.Equal((400, 200), Size(processed, ImageVariant.Thumbnail));
        Assert.Equal((1600, 800), Size(processed, ImageVariant.Preview));
    }

    [Fact]
    public void A_portrait_image_is_measured_on_its_height()
    {
        var processed = Process(TestImages.Jpeg(width: 1500, height: 3000));

        Assert.Equal((200, 400), Size(processed, ImageVariant.Thumbnail));
        Assert.Equal((800, 1600), Size(processed, ImageVariant.Preview));
    }

    [Fact]
    public void An_image_smaller_than_the_target_is_never_scaled_up()
    {
        var processed = Process(TestImages.Jpeg(width: 100, height: 80));

        Assert.Equal((100, 80), (processed.Width, processed.Height));
        Assert.Equal((100, 80), Size(processed, ImageVariant.Thumbnail));
        Assert.Equal((100, 80), Size(processed, ImageVariant.Preview));
    }

    [Fact]
    public void An_image_between_the_two_targets_is_shrunk_for_the_thumbnail_only()
    {
        var processed = Process(TestImages.Jpeg(width: 900, height: 600));

        Assert.Equal((400, 267), Size(processed, ImageVariant.Thumbnail));
        Assert.Equal((900, 600), Size(processed, ImageVariant.Preview));
    }

    [Fact]
    public void A_very_thin_image_never_shrinks_to_zero_pixels()
    {
        var processed = Process(TestImages.Png(width: 5000, height: 20));

        var (width, height) = Size(processed, ImageVariant.Thumbnail);
        Assert.Equal(400, width);
        Assert.True(height >= 1);
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public void Every_accepted_format_decodes_and_produces_webp(string format, byte[] encoded)
    {
        var processed = Process(encoded);

        Assert.Equal((64, 48), (processed.Width, processed.Height));
        foreach (var variant in new[] { ImageVariant.Thumbnail, ImageVariant.Preview })
        {
            Assert.True(
                ImagePolicy.DetectContentType(processed.Derivatives[variant]) == "image/webp",
                $"The {variant} of a {format} is not WebP.");
        }
    }

    /// <summary>
    /// The eight EXIF orientations. The red marker starts in the top-left of the stored image, so
    /// where it ends up in the derivative says which transformation was applied.
    /// </summary>
    [Theory]
    [InlineData(1, false, Corner.TopLeft)]
    [InlineData(2, false, Corner.TopRight)]
    [InlineData(3, false, Corner.BottomRight)]
    [InlineData(4, false, Corner.BottomLeft)]
    [InlineData(5, true, Corner.TopLeft)]
    [InlineData(6, true, Corner.TopRight)]
    [InlineData(7, true, Corner.BottomRight)]
    [InlineData(8, true, Corner.BottomLeft)]
    public void The_exif_orientation_is_applied_to_the_dimensions_and_to_the_derivatives(
        int orientation,
        bool swapsAxes,
        Corner expectedMarker)
    {
        const int StoredWidth = 120;
        const int StoredHeight = 60;
        var encoded = TestImages.JpegWithExif(StoredWidth, StoredHeight, orientation);

        var processed = Process(encoded);

        var (expectedWidth, expectedHeight) = swapsAxes ? (StoredHeight, StoredWidth) : (StoredWidth, StoredHeight);
        Assert.Equal((expectedWidth, expectedHeight), (processed.Width, processed.Height));
        Assert.Equal((expectedWidth, expectedHeight), Size(processed, ImageVariant.Preview));

        var marker = TestImages.MarkerSize(StoredWidth, StoredHeight);
        var preview = processed.Derivatives[ImageVariant.Preview];
        foreach (var corner in Enum.GetValues<Corner>())
        {
            var (x, y) = CornerCentre(corner, expectedWidth, expectedHeight, marker);
            Assert.Equal(corner == expectedMarker, TestImages.IsRed(TestImages.PixelAt(preview, x, y)));
        }
    }

    [Fact]
    public void The_original_is_untouched_by_processing_so_it_still_reports_its_stored_size()
    {
        var encoded = TestImages.JpegWithExif(200, 100, orientation: 6);

        Process(encoded);

        // The processor only reads. The bytes handed to it are the bytes that stay on disk, and
        // a decoder that ignores EXIF still sees the stored, unrotated size.
        Assert.Equal((200, 100), TestImages.CodecSize(encoded));
    }

    [Fact]
    public void Content_with_valid_magic_numbers_but_a_damaged_body_is_reported_as_undecodable()
    {
        var problem = Assert.Throws<ApiProblemException>(() => Process(TestImages.CorruptJpeg()));

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Equal(ImageErrorCodes.Undecodable, problem.Code);
    }

    [Fact]
    public void A_truncated_image_is_refused_rather_than_kept_half_decoded()
    {
        var whole = TestImages.Jpeg(width: 800, height: 600);
        var truncated = whole[..(whole.Length / 2)];

        var problem = Assert.Throws<ApiProblemException>(() => Process(truncated));

        Assert.Equal(ImageErrorCodes.Undecodable, problem.Code);
    }

    [Fact]
    public void Custom_targets_from_configuration_are_honoured()
    {
        var processor = new SkiaImageProcessor(Options.Create(new ImageOptions
        {
            ThumbnailLongestEdge = 100,
            PreviewLongestEdge = 300,
        }));

        using var stream = new MemoryStream(TestImages.Jpeg(width: 1000, height: 500));
        var processed = processor.Process(stream);

        Assert.Equal((100, 50), Size(processed, ImageVariant.Thumbnail));
        Assert.Equal((300, 150), Size(processed, ImageVariant.Preview));
    }

    public static TheoryData<string, byte[]> Formats() => new()
    {
        { "jpeg", TestImages.Jpeg() },
        { "png", TestImages.Png() },
        { "webp", TestImages.Webp() },
    };

    public enum Corner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
    }

    private ProcessedImage Process(byte[] encoded)
    {
        using var stream = new MemoryStream(encoded);
        return _processor.Process(stream);
    }

    private static (int Width, int Height) Size(ProcessedImage processed, ImageVariant variant) =>
        TestImages.CodecSize(processed.Derivatives[variant]);

    /// <summary>
    /// A pixel well inside a corner's marker area, in the coordinates of the upright image. The
    /// marker keeps its pixel size, so it is located by that size and not by a proportion.
    /// </summary>
    private static (int X, int Y) CornerCentre(Corner corner, int width, int height, int marker)
    {
        var inset = marker / 2;
        return corner switch
        {
            Corner.TopLeft => (inset, inset),
            Corner.TopRight => (width - 1 - inset, inset),
            Corner.BottomLeft => (inset, height - 1 - inset),
            _ => (width - 1 - inset, height - 1 - inset),
        };
    }
}
