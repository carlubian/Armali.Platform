using System.Security.Cryptography;
using Blackwing.Api.Configuration;
using Blackwing.Api.Platform.Api;
using Blackwing.Api.Platform.Images;
using Blackwing.Api.Platform.Storage;
using Blackwing.Shared.Identity;
using Blackwing.Shared.Images;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Blackwing.UnitTests.Images;

/// <summary>
/// The blob store against a real temporary directory. No database is involved: the store only
/// knows hashes and bytes, which is precisely what keeps it replaceable.
/// </summary>
public sealed class FileSystemImageBlobStoreTests : IDisposable
{
    private static readonly UserId Owner = new(3);

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"blackwing-store-{Guid.NewGuid():N}");
    private readonly FileSystemImageBlobStore _store;

    public FileSystemImageBlobStoreTests()
    {
        var paths = new BlackwingStoragePaths(Options.Create(new StorageOptions { ImagesPath = _root }));
        _store = new FileSystemImageBlobStore(paths, NullLogger<FileSystemImageBlobStore>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task Staging_records_the_size_and_the_lowercase_sha256_of_what_was_written()
    {
        var content = Pattern(200_000);

        var staged = await _store.StageAsync(new MemoryStream(content), 1024 * 1024, CancellationToken.None);

        Assert.Equal(content.Length, staged.ByteSize);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(content)), staged.ContentHash);
        Assert.Equal(64, staged.ContentHash.Length);
        Assert.Equal(content, await ReadAllAsync(await _store.OpenStagedAsync(staged, CancellationToken.None)));
    }

    [Fact]
    public async Task Staging_gives_up_at_the_limit_without_reading_the_rest_and_leaves_nothing_behind()
    {
        await using var source = new GeneratedStream(length: 50L * 1024 * 1024);
        const long Limit = 200 * 1024;

        var problem = await Assert.ThrowsAsync<ApiProblemException>(
            () => _store.StageAsync(source, Limit, CancellationToken.None));

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, problem.StatusCode);
        Assert.Equal(ImageErrorCodes.TooLarge, problem.Code);

        // One more buffer than the limit at most: the rest of the 50 MB was never touched.
        Assert.True(source.BytesRead <= Limit + 64 * 1024, $"Read {source.BytesRead} bytes.");
        Assert.Empty(StagingFiles());
    }

    [Fact]
    public async Task A_file_exactly_at_the_limit_is_accepted()
    {
        var content = Pattern(4096);

        var staged = await _store.StageAsync(new MemoryStream(content), content.Length, CancellationToken.None);

        Assert.Equal(content.Length, staged.ByteSize);
    }

    [Fact]
    public async Task Promoting_places_the_three_variants_under_the_owner_and_fan_out()
    {
        var original = Jpeg(1000);
        var staged = await _store.StageAsync(new MemoryStream(original), 1024 * 1024, CancellationToken.None);
        var thumbnail = Pattern(100);
        var preview = Pattern(300);

        await _store.PromoteAsync(Owner, staged, Derivatives(thumbnail, preview), CancellationToken.None);

        var directory = Path.Combine(_root, "3", staged.ContentHash[..2], staged.ContentHash[2..4]);
        Assert.Equal(original, await File.ReadAllBytesAsync(Path.Combine(directory, staged.ContentHash)));
        Assert.Equal(thumbnail, await File.ReadAllBytesAsync(Path.Combine(directory, $"{staged.ContentHash}.thumb.webp")));
        Assert.Equal(preview, await File.ReadAllBytesAsync(Path.Combine(directory, $"{staged.ContentHash}.preview.webp")));
        Assert.Empty(StagingFiles());
        Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
    }

    [Fact]
    public async Task Each_variant_reads_back_with_its_length_and_content_type()
    {
        var original = Jpeg(1000);
        var staged = await _store.StageAsync(new MemoryStream(original), 1024 * 1024, CancellationToken.None);
        await _store.PromoteAsync(Owner, staged, Derivatives(Pattern(100), Pattern(300)), CancellationToken.None);

        await using var originalBlob = await _store.OpenReadAsync(Owner, staged.ContentHash, ImageVariant.Original, CancellationToken.None);
        await using var thumbnailBlob = await _store.OpenReadAsync(Owner, staged.ContentHash, ImageVariant.Thumbnail, CancellationToken.None);
        await using var previewBlob = await _store.OpenReadAsync(Owner, staged.ContentHash, ImageVariant.Preview, CancellationToken.None);

        Assert.NotNull(originalBlob);
        Assert.NotNull(thumbnailBlob);
        Assert.NotNull(previewBlob);
        Assert.Equal("image/jpeg", originalBlob.ContentType);
        Assert.Equal(original.Length, originalBlob.Length);
        Assert.Equal(0, originalBlob.Content.Position);
        Assert.Equal("image/webp", thumbnailBlob.ContentType);
        Assert.Equal(100, thumbnailBlob.Length);
        Assert.Equal("image/webp", previewBlob.ContentType);
        Assert.Equal(300, previewBlob.Length);
    }

    [Fact]
    public async Task Another_owner_cannot_reach_the_same_hash()
    {
        var staged = await _store.StageAsync(new MemoryStream(Jpeg(500)), 1024 * 1024, CancellationToken.None);
        await _store.PromoteAsync(Owner, staged, Derivatives(Pattern(10), Pattern(20)), CancellationToken.None);

        var other = await _store.OpenReadAsync(new UserId(4), staged.ContentHash, ImageVariant.Original, CancellationToken.None);

        Assert.Null(other);
    }

    [Fact]
    public async Task Reading_something_that_was_never_stored_answers_null()
    {
        var hash = new string('a', 64);

        var blob = await _store.OpenReadAsync(Owner, hash, ImageVariant.Preview, CancellationToken.None);

        Assert.Null(blob);
    }

    [Fact]
    public async Task Deleting_removes_all_three_variants_and_is_idempotent()
    {
        var staged = await _store.StageAsync(new MemoryStream(Jpeg(500)), 1024 * 1024, CancellationToken.None);
        await _store.PromoteAsync(Owner, staged, Derivatives(Pattern(10), Pattern(20)), CancellationToken.None);

        await _store.DeleteAsync(Owner, staged.ContentHash, CancellationToken.None);
        await _store.DeleteAsync(Owner, staged.ContentHash, CancellationToken.None);

        foreach (var variant in Enum.GetValues<ImageVariant>())
        {
            Assert.Null(await _store.OpenReadAsync(Owner, staged.ContentHash, variant, CancellationToken.None));
        }
    }

    [Fact]
    public async Task Deleting_something_that_never_existed_does_not_throw()
    {
        await _store.DeleteAsync(Owner, new string('b', 64), CancellationToken.None);
    }

    [Fact]
    public async Task Discarding_a_staged_upload_removes_it_and_tolerates_it_already_being_gone()
    {
        var staged = await _store.StageAsync(new MemoryStream(Pattern(100)), 1024, CancellationToken.None);
        Assert.Single(StagingFiles());

        await _store.DiscardStagedAsync(staged, CancellationToken.None);
        await _store.DiscardStagedAsync(staged, CancellationToken.None);

        Assert.Empty(StagingFiles());
    }

    [Fact]
    public async Task Promoting_over_the_leftovers_of_an_interrupted_attempt_succeeds()
    {
        var original = Jpeg(500);
        var first = await _store.StageAsync(new MemoryStream(original), 1024 * 1024, CancellationToken.None);
        await _store.PromoteAsync(Owner, first, Derivatives(Pattern(10), Pattern(20)), CancellationToken.None);

        // The same content again, as after a crash between the files and the database row.
        var second = await _store.StageAsync(new MemoryStream(original), 1024 * 1024, CancellationToken.None);
        await _store.PromoteAsync(Owner, second, Derivatives(Pattern(10), Pattern(20)), CancellationToken.None);

        await using var blob = await _store.OpenReadAsync(Owner, second.ContentHash, ImageVariant.Original, CancellationToken.None);
        Assert.NotNull(blob);
        Assert.Equal(original.Length, blob.Length);
    }

    [Fact]
    public async Task A_staging_identifier_that_is_not_a_guid_cannot_name_a_path()
    {
        var forged = new StagedImage("..\\..\\secret", new string('a', 64), 1);

        await Assert.ThrowsAsync<ArgumentException>(() => _store.OpenStagedAsync(forged, CancellationToken.None));
    }

    private static Dictionary<ImageVariant, byte[]> Derivatives(byte[] thumbnail, byte[] preview) => new()
    {
        [ImageVariant.Thumbnail] = thumbnail,
        [ImageVariant.Preview] = preview,
    };

    private string[] StagingFiles()
    {
        var staging = Path.Combine(_root, ".staging");
        return Directory.Exists(staging) ? Directory.GetFiles(staging) : [];
    }

    private static byte[] Pattern(int length)
    {
        var bytes = new byte[length];
        for (var index = 0; index < length; index++)
        {
            bytes[index] = (byte)(index % 251);
        }

        return bytes;
    }

    /// <summary>A pattern that starts like a JPEG, so the original's content type can be sniffed.</summary>
    private static byte[] Jpeg(int length)
    {
        var bytes = Pattern(length);
        bytes[0] = 0xff;
        bytes[1] = 0xd8;
        bytes[2] = 0xff;
        return bytes;
    }

    private static async Task<byte[]> ReadAllAsync(Stream stream)
    {
        await using (stream)
        {
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            return buffer.ToArray();
        }
    }

    /// <summary>
    /// A read-only stream of <paramref name="length"/> zero bytes that materializes nothing and
    /// counts how much was actually consumed.
    /// </summary>
    private sealed class GeneratedStream(long length) : Stream
    {
        private long _position;

        public long BytesRead => _position;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => length;

        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            var count = (int)Math.Min(buffer.Length, length - _position);
            buffer[..count].Clear();
            _position += count;
            return count;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Read(buffer.Span));

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
