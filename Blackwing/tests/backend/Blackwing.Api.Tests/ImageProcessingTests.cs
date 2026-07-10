using Blackwing.Api.Configuration;
using Blackwing.Api.Ingestion;
using Blackwing.Shared.Storage;
using ImageMagick;
using Microsoft.Extensions.Options;

namespace Blackwing.Api.Tests;

/// <summary>
/// Exercises the Magick.NET pipeline directly (no database), so the native library,
/// WebP encoding and resize policy are validated everywhere the suite runs.
/// </summary>
public sealed class ImageProcessingTests
{
    private static ImageProcessingService CreateProcessor(int previewEdge = 1600, int thumbEdge = 400) =>
        new(Options.Create(new BlackwingOptions
        {
            Storage = new StorageOptions { ImagesPath = "unused", StagingPath = "unused" },
            Ingestion = new IngestionOptions { PreviewMaxEdge = previewEdge, ThumbnailMaxEdge = thumbEdge },
        }));

    [Fact]
    public void Reads_dimensions_and_produces_webp_derivatives_bounded_by_the_configured_edges()
    {
        var processor = CreateProcessor(previewEdge: 800, thumbEdge: 200);
        using var source = new MemoryStream(CreateImage(MagickFormat.Jpeg, 2000, 1500));

        var result = processor.Process(source);

        Assert.Equal(DetectedImageFormat.Jpeg, result.Format);
        Assert.Equal(2000, result.Width);
        Assert.Equal(1500, result.Height);

        AssertWebpWithin(result.Preview, 800);
        AssertWebpWithin(result.Thumbnail, 200);
    }

    [Fact]
    public void Does_not_upscale_an_image_smaller_than_the_derivative_edges()
    {
        var processor = CreateProcessor(previewEdge: 1600, thumbEdge: 400);
        using var source = new MemoryStream(CreateImage(MagickFormat.Png, 120, 90));

        var result = processor.Process(source);

        // A small original keeps its size in both derivatives rather than being blown up.
        using var preview = new MagickImage(result.Preview);
        Assert.Equal(120u, preview.Width);
        Assert.Equal(90u, preview.Height);
    }

    [Fact]
    public void Rejects_bytes_that_are_not_a_decodable_image()
    {
        var processor = CreateProcessor();
        using var source = new MemoryStream("plainly not an image"u8.ToArray());
        Assert.Throws<InvalidImageException>(() => processor.Process(source));
    }

    private static void AssertWebpWithin(byte[] bytes, int maxEdge)
    {
        Assert.Equal(DetectedImageFormat.WebP, ImageFormatDetector.Detect(bytes));
        using var image = new MagickImage(bytes);
        Assert.True(image.Width <= (uint)maxEdge && image.Height <= (uint)maxEdge,
            $"Derivative {image.Width}x{image.Height} exceeds the {maxEdge}px bound.");
        Assert.True(image.Width == (uint)maxEdge || image.Height == (uint)maxEdge,
            "The longest edge should meet the bound for a downscaled image.");
    }

    private static byte[] CreateImage(MagickFormat format, uint width, uint height)
    {
        using var image = new MagickImage(MagickColors.SteelBlue, width, height);
        image.Format = format;
        return image.ToByteArray();
    }
}
