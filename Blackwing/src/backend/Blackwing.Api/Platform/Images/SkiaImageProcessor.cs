using Blackwing.Api.Configuration;
using Blackwing.Api.Platform.Api;
using Blackwing.Shared.Images;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace Blackwing.Api.Platform.Images;

/// <summary>
/// The only place that knows SkiaSharp: decodes an image, applies its EXIF orientation and
/// encodes the WebP derivatives.
/// </summary>
/// <remarks>
/// Skia's memory is native, so the garbage collector cannot see it. Every codec, bitmap, image
/// and data object here sits in a <c>using</c>; a leak would show up weeks later as a container
/// that grows until the kernel kills it.
/// </remarks>
internal sealed class SkiaImageProcessor(IOptions<ImageOptions> options) : IImageProcessor
{
    /// <summary>
    /// Decoding allocates width x height x 4 bytes of native memory. This bounds what a hostile
    /// or absurd file can make the process ask for; real photographs stay far below it.
    /// </summary>
    private const long MaximumPixels = 300L * 1000 * 1000;

    private readonly ImageOptions _options = options.Value;

    public ProcessedImage Process(Stream content)
    {
        ArgumentNullException.ThrowIfNull(content);

        content.Position = 0;
        using var codec = SKCodec.Create(content) ?? throw Undecodable();

        var rawWidth = codec.Info.Width;
        var rawHeight = codec.Info.Height;
        if (rawWidth <= 0 || rawHeight <= 0 || (long)rawWidth * rawHeight > MaximumPixels)
        {
            throw Undecodable();
        }

        // The origin says how the sensor was held. Rotations by a quarter turn swap the axes, and
        // those swapped dimensions are the ones a person would call the image's size.
        var origin = codec.EncodedOrigin;
        var swapsAxes = SwapsAxes(origin);
        var width = swapsAxes ? rawHeight : rawWidth;
        var height = swapsAxes ? rawWidth : rawHeight;

        var derivatives = new Dictionary<ImageVariant, byte[]>
        {
            [ImageVariant.Thumbnail] = Render(codec, origin, width, height, _options.ThumbnailLongestEdge, _options.ThumbnailQuality),
            [ImageVariant.Preview] = Render(codec, origin, width, height, _options.PreviewLongestEdge, _options.PreviewQuality),
        };

        return new ProcessedImage(width, height, derivatives);
    }

    /// <summary>
    /// Produces one derivative: decode (scaled down by the codec when it can, which bounds the
    /// peak of native memory on large originals), orient, shrink to the target and encode.
    /// Nothing is ever scaled up: an original smaller than the target keeps its native size.
    /// </summary>
    private static byte[] Render(
        SKCodec codec,
        SKEncodedOrigin origin,
        int logicalWidth,
        int logicalHeight,
        int longestEdge,
        int quality)
    {
        var logicalLongest = Math.Max(logicalWidth, logicalHeight);
        var targetScale = logicalLongest <= longestEdge ? 1f : (float)longestEdge / logicalLongest;

        var decodeSize = ChooseDecodeSize(codec, targetScale, longestEdge);

        using var raw = Decode(codec, decodeSize);

        // Orient hands back the very same bitmap when there is nothing to rotate, so only a
        // bitmap it created is disposed here; the decoded one is disposed by its own using.
        var oriented = Orient(raw, origin);
        try
        {
            return Shrink(oriented, longestEdge, quality);
        }
        finally
        {
            if (!ReferenceEquals(oriented, raw))
            {
                oriented.Dispose();
            }
        }
    }

    private static byte[] Shrink(SKBitmap oriented, int longestEdge, int quality)
    {
        // The decode size may already be smaller than the original, so the shrink is measured
        // against what was actually decoded, not against the original.
        var orientedLongest = Math.Max(oriented.Width, oriented.Height);
        if (orientedLongest <= longestEdge)
        {
            return Encode(oriented, quality);
        }

        var ratio = (double)longestEdge / orientedLongest;
        var targetWidth = Math.Max(1, (int)Math.Round(oriented.Width * ratio));
        var targetHeight = Math.Max(1, (int)Math.Round(oriented.Height * ratio));

        using var resized = oriented.Resize(
            oriented.Info.WithSize(targetWidth, targetHeight),
            new SKSamplingOptions(SKCubicResampler.Mitchell))
            ?? throw Undecodable();

        return Encode(resized, quality);
    }

    /// <summary>
    /// Asks the codec for a native reduced size (JPEG can decode at 1/2, 1/4 or 1/8 cheaply) and
    /// uses it only if it is still at least as large as the target; otherwise decodes in full.
    /// </summary>
    private static SKSizeI ChooseDecodeSize(SKCodec codec, float targetScale, int longestEdge)
    {
        var full = new SKSizeI(codec.Info.Width, codec.Info.Height);
        if (targetScale >= 1f)
        {
            return full;
        }

        var scaled = codec.GetScaledDimensions(targetScale);
        return scaled.Width > 0
            && scaled.Height > 0
            && Math.Max(scaled.Width, scaled.Height) >= longestEdge
            ? scaled
            : full;
    }

    private static SKBitmap Decode(SKCodec codec, SKSizeI size)
    {
        var info = new SKImageInfo(size.Width, size.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        var bitmap = new SKBitmap(info);

        SKCodecResult result;
        try
        {
            result = codec.GetPixels(info, bitmap.GetPixels());
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }

        // A truncated file decodes only partly and reports IncompleteInput. Keeping half a photo
        // silently would be worse than refusing it.
        if (result != SKCodecResult.Success)
        {
            bitmap.Dispose();
            throw Undecodable();
        }

        return bitmap;
    }

    /// <summary>
    /// Returns a bitmap in upright orientation, or <paramref name="source"/> itself when it is
    /// already upright. The matrices map each pixel of the stored image to its place in the
    /// upright one, following the eight EXIF orientations.
    /// </summary>
    private static SKBitmap Orient(SKBitmap source, SKEncodedOrigin origin)
    {
        if (origin is SKEncodedOrigin.TopLeft or SKEncodedOrigin.Default)
        {
            return source;
        }

        float w = source.Width;
        float h = source.Height;

        var swapsAxes = SwapsAxes(origin);
        var target = new SKBitmap(new SKImageInfo(
            swapsAxes ? source.Height : source.Width,
            swapsAxes ? source.Width : source.Height,
            source.ColorType,
            source.AlphaType));

        // x' = scaleX * x + skewX * y + transX,  y' = skewY * x + scaleY * y + transY
        var matrix = origin switch
        {
            SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1),
            SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1),
            SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1),
            SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightTop => new SKMatrix(0, -1, h, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, h, -1, 0, w, 0, 0, 1),
            SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, w, 0, 0, 1),
            _ => SKMatrix.Identity,
        };

        using var canvas = new SKCanvas(target);
        canvas.SetMatrix(matrix);

        // Quarter turns and flips land every pixel exactly on another pixel, so nearest-neighbour
        // sampling is lossless here.
        canvas.DrawBitmap(source, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest));
        return target;
    }

    private static byte[] Encode(SKBitmap bitmap, int quality)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Webp, quality) ?? throw Undecodable();
        return data.ToArray();
    }

    private static bool SwapsAxes(SKEncodedOrigin origin) => origin
        is SKEncodedOrigin.LeftTop
        or SKEncodedOrigin.RightTop
        or SKEncodedOrigin.RightBottom
        or SKEncodedOrigin.LeftBottom;

    private static ApiProblemException Undecodable() => new(
        StatusCodes.Status400BadRequest,
        ImageErrorCodes.Undecodable,
        "The image could not be decoded.",
        "The file looks like an image but its content is damaged or unsupported.");
}
