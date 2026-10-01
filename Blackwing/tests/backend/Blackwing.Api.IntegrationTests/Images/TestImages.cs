using System.Buffers.Binary;
using System.Text;
using SkiaSharp;

namespace Blackwing.Api.IntegrationTests.Images;

/// <summary>
/// Builds test images in memory, so no binary fixture is ever committed to the repository.
/// </summary>
/// <remarks>
/// Every image has a red square in its top-left corner and a blue-ish background, which is what
/// lets a test tell whether an orientation was applied: the square travels with the pixels, so
/// its final corner reveals exactly what rotation the pipeline performed. The <c>seed</c> varies
/// the pixels, and therefore the hash, for tests that need two different photos.
/// </remarks>
internal static class TestImages
{
    /// <summary>Side of the red marker, in pixels of the stored (unrotated) image.</summary>
    public static int MarkerSize(int width, int height) => Math.Max(4, Math.Min(width, height) / 6);

    public static byte[] Jpeg(int width = 64, int height = 48, int seed = 0) =>
        Encode(width, height, seed, SKEncodedImageFormat.Jpeg, quality: 95);

    public static byte[] Png(int width = 64, int height = 48, int seed = 0) =>
        Encode(width, height, seed, SKEncodedImageFormat.Png, quality: 100);

    public static byte[] Webp(int width = 64, int height = 48, int seed = 0) =>
        Encode(width, height, seed, SKEncodedImageFormat.Webp, quality: 95);

    /// <summary>
    /// A JPEG carrying an EXIF block with an orientation and, optionally, a capture date. The
    /// block is assembled by hand and spliced in right behind the start-of-image marker; the
    /// layout is fully specified by the TIFF 6.0 and EXIF standards and is about a hundred bytes.
    /// </summary>
    /// <param name="width">Width of the stored image, before any orientation is applied.</param>
    /// <param name="height">Height of the stored image, before any orientation is applied.</param>
    /// <param name="orientation">EXIF orientation, 1 to 8.</param>
    /// <param name="dateTimeOriginal">The capture date to record, or <see langword="null"/> for none.</param>
    /// <param name="seed">Varies the pixels.</param>
    public static byte[] JpegWithExif(
        int width,
        int height,
        int orientation,
        DateTime? dateTimeOriginal = null,
        int seed = 0)
    {
        var jpeg = Jpeg(width, height, seed);
        var exif = BuildExifSegment(orientation, dateTimeOriginal);

        // SOI (FF D8) first, then the new APP1 segment, then everything that was after the SOI.
        var result = new byte[jpeg.Length + exif.Length];
        jpeg.AsSpan(0, 2).CopyTo(result);
        exif.CopyTo(result, 2);
        jpeg.AsSpan(2).CopyTo(result.AsSpan(2 + exif.Length));
        return result;
    }

    /// <summary>
    /// A well-formed JPEG start followed by garbage: the magic numbers pass, the decoder does not.
    /// </summary>
    public static byte[] CorruptJpeg() =>
    [
        0xff, 0xd8, 0xff, 0xe0, 0x00, 0x10, 0x4a, 0x46, 0x49, 0x46, 0x00, 0x01,
        .. Encoding.ASCII.GetBytes("this is not really image data"),
    ];

    /// <summary>Plain text, for the "extension says image, content says otherwise" case.</summary>
    public static byte[] NotAnImage() => Encoding.ASCII.GetBytes("This file only pretends to be a picture.");

    /// <summary>
    /// A seekable stream of <paramref name="length"/> zero bytes that materializes nothing. Being
    /// seekable matters: it lets <see cref="StreamContent"/> declare a Content-Length.
    /// </summary>
    public static Stream Endless(long length) => new ZeroStream(length);

    /// <summary>The size the decoder reports, which for a JPEG ignores EXIF orientation.</summary>
    public static (int Width, int Height) CodecSize(byte[] encoded)
    {
        using var stream = new MemoryStream(encoded);
        using var codec = SKCodec.Create(stream) ?? throw new InvalidOperationException("Not an image.");
        return (codec.Info.Width, codec.Info.Height);
    }

    /// <summary>Decodes an image and returns its pixel at the given position.</summary>
    public static SKColor PixelAt(byte[] encoded, int x, int y)
    {
        using var bitmap = SKBitmap.Decode(encoded) ?? throw new InvalidOperationException("Not an image.");
        return bitmap.GetPixel(x, y);
    }

    public static bool IsRed(SKColor color) => color is { Red: > 200, Green: < 90, Blue: < 90 };

    private static byte[] Encode(int width, int height, int seed, SKEncodedImageFormat format, int quality)
    {
        using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque));
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(new SKColor((byte)(20 + (seed % 50)), (byte)(90 + (seed % 60)), (byte)(180 + (seed % 40))));

            using var paint = new SKPaint();

            // A white square somewhere near the middle whose position depends on the seed, so two
            // seeds give two different hashes without ever touching the marker corner.
            paint.Color = SKColors.White;
            var size = Math.Max(2, Math.Min(width, height) / 8);
            var x = (width / 2) + (seed % Math.Max(1, (width / 4) + 1));
            var y = (height / 2) + (seed % Math.Max(1, (height / 4) + 1));
            canvas.DrawRect(x, y, size, size, paint);

            paint.Color = new SKColor(255, 0, 0);
            var marker = MarkerSize(width, height);
            canvas.DrawRect(0, 0, marker, marker, paint);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, quality) ?? throw new InvalidOperationException("Encoding failed.");
        return data.ToArray();
    }

    private static byte[] BuildExifSegment(int orientation, DateTime? dateTimeOriginal)
    {
        // TIFF block, big endian ("MM"). Offsets are relative to the start of this block.
        //   0   header (8 bytes): "MM", 0x002A, offset of IFD0 = 8
        //   8   IFD0: count, entries, next-IFD offset
        //   ..  Exif IFD, when there is a date
        //   ..  the date string
        var hasDate = dateTimeOriginal is not null;
        var ifd0Entries = hasDate ? 2 : 1;
        var ifd0Size = 2 + (ifd0Entries * 12) + 4;
        var exifIfdOffset = 8 + ifd0Size;
        var exifIfdSize = hasDate ? 2 + 12 + 4 : 0;
        var dateOffset = exifIfdOffset + exifIfdSize;
        var dateLength = hasDate ? 20 : 0;
        var tiffLength = dateOffset + dateLength;

        var tiff = new byte[tiffLength];
        var span = tiff.AsSpan();

        "MM"u8.CopyTo(span);
        BinaryPrimitives.WriteUInt16BigEndian(span[2..], 0x002A);
        BinaryPrimitives.WriteUInt32BigEndian(span[4..], 8);

        var cursor = 8;
        BinaryPrimitives.WriteUInt16BigEndian(span[cursor..], (ushort)ifd0Entries);
        cursor += 2;

        // Orientation: tag 0x0112, type SHORT (3), count 1. A SHORT sits in the first two bytes
        // of the four-byte value field.
        WriteEntry(span, ref cursor, tag: 0x0112, type: 3, count: 1, valueOrOffset: (uint)orientation << 16);

        if (hasDate)
        {
            // Pointer to the Exif sub-IFD: tag 0x8769, type LONG (4), count 1.
            WriteEntry(span, ref cursor, tag: 0x8769, type: 4, count: 1, valueOrOffset: (uint)exifIfdOffset);
        }

        BinaryPrimitives.WriteUInt32BigEndian(span[cursor..], 0); // no next IFD
        cursor += 4;

        if (hasDate)
        {
            BinaryPrimitives.WriteUInt16BigEndian(span[cursor..], 1);
            cursor += 2;

            // DateTimeOriginal: tag 0x9003, type ASCII (2), count 20 including the terminator.
            WriteEntry(span, ref cursor, tag: 0x9003, type: 2, count: 20, valueOrOffset: (uint)dateOffset);

            BinaryPrimitives.WriteUInt32BigEndian(span[cursor..], 0);
            cursor += 4;

            var text = dateTimeOriginal!.Value.ToString("yyyy':'MM':'dd HH':'mm':'ss", System.Globalization.CultureInfo.InvariantCulture);
            Encoding.ASCII.GetBytes(text).CopyTo(span[cursor..]); // the 20th byte stays zero
        }

        // APP1 segment: marker, a length that counts itself, the "Exif\0\0" identifier, the block.
        var payloadLength = 6 + tiff.Length;
        var segment = new byte[2 + 2 + payloadLength];
        segment[0] = 0xff;
        segment[1] = 0xe1;
        BinaryPrimitives.WriteUInt16BigEndian(segment.AsSpan(2), (ushort)(2 + payloadLength));
        "Exif\0\0"u8.CopyTo(segment.AsSpan(4));
        tiff.CopyTo(segment, 10);
        return segment;
    }

    private static void WriteEntry(Span<byte> span, ref int cursor, ushort tag, ushort type, uint count, uint valueOrOffset)
    {
        BinaryPrimitives.WriteUInt16BigEndian(span[cursor..], tag);
        BinaryPrimitives.WriteUInt16BigEndian(span[(cursor + 2)..], type);
        BinaryPrimitives.WriteUInt32BigEndian(span[(cursor + 4)..], count);
        BinaryPrimitives.WriteUInt32BigEndian(span[(cursor + 8)..], valueOrOffset);
        cursor += 12;
    }

    private sealed class ZeroStream(long length) : Stream
    {
        private long _position;

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => length;

        public override long Position
        {
            get => _position;
            set => _position = value;
        }

        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            var count = (int)Math.Max(0, Math.Min(buffer.Length, length - _position));
            buffer[..count].Clear();
            _position += count;
            return count;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Read(buffer.Span));

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            _position = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => _position + offset,
                _ => length + offset,
            };
            return _position;
        }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
