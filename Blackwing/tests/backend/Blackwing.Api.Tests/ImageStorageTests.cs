using System.Text;
using Blackwing.Api.Configuration;
using Blackwing.Api.Storage;
using Blackwing.Shared.Storage;
using Microsoft.Extensions.Options;

namespace Blackwing.Api.Tests;

public sealed class ImageStorageTests
{
    private const string Sha = "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789";

    [Fact]
    public void Path_is_content_addressed_sharded_and_separated_per_user()
    {
        var owner = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var original = ImageStoragePath.Resolve("/data/images", owner, Sha, ImageDerivative.Original);
        var preview = ImageStoragePath.Resolve("/data/images", owner, Sha, ImageDerivative.Preview);
        var thumbnail = ImageStoragePath.Resolve("/data/images", owner, Sha, ImageDerivative.Thumbnail);

        Assert.EndsWith(Path.Combine("11111111111111111111111111111111", "ab", "cd", $"{Sha}.orig"), original);
        Assert.EndsWith($"{Sha}.preview.webp", preview);
        Assert.EndsWith($"{Sha}.thumb.webp", thumbnail);
    }

    [Fact]
    public void Path_rejects_a_hash_that_is_not_64_hex_characters() =>
        Assert.Throws<ArgumentException>(() => ImageStoragePath.Resolve("/data/images", Guid.NewGuid(), "short", ImageDerivative.Original));

    [Fact]
    public async Task Store_round_trips_derivatives_and_isolates_owners()
    {
        var root = NewTempRoot();
        try
        {
            var store = CreateStore(root);
            var alice = Guid.NewGuid();
            var bob = Guid.NewGuid();

            await store.SaveAsync(alice, Sha, ImageDerivative.Thumbnail, Stream("alice-thumb"));
            await store.SaveAsync(alice, Sha, ImageDerivative.Original, Stream("alice-orig"));

            Assert.True(await store.ExistsAsync(alice, Sha, ImageDerivative.Thumbnail));
            Assert.False(await store.ExistsAsync(alice, Sha, ImageDerivative.Preview));
            // The same hash for a different owner is a physically independent copy.
            Assert.False(await store.ExistsAsync(bob, Sha, ImageDerivative.Thumbnail));

            await using var read = await store.OpenReadAsync(alice, Sha, ImageDerivative.Thumbnail);
            Assert.NotNull(read);
            using var reader = new StreamReader(read!);
            Assert.Equal("alice-thumb", await reader.ReadToEndAsync());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Deleting_removes_every_derivative_and_prunes_empty_shards()
    {
        var root = NewTempRoot();
        try
        {
            var store = CreateStore(root);
            var owner = Guid.NewGuid();
            await store.SaveAsync(owner, Sha, ImageDerivative.Original, Stream("o"));
            await store.SaveAsync(owner, Sha, ImageDerivative.Preview, Stream("p"));
            await store.SaveAsync(owner, Sha, ImageDerivative.Thumbnail, Stream("t"));

            await store.DeleteAllAsync(owner, Sha);

            Assert.False(await store.ExistsAsync(owner, Sha, ImageDerivative.Original));
            Assert.False(await store.ExistsAsync(owner, Sha, ImageDerivative.Preview));
            Assert.False(await store.ExistsAsync(owner, Sha, ImageDerivative.Thumbnail));
            // The emptied shard directories are pruned.
            var shard = Path.Combine(root, ImageStoragePath.OwnerSegment(owner), "ab");
            Assert.False(Directory.Exists(shard));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Deleting_a_missing_image_is_a_no_op()
    {
        var root = NewTempRoot();
        try
        {
            await CreateStore(root).DeleteAllAsync(Guid.NewGuid(), Sha);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static LocalImageStore CreateStore(string root) =>
        new(Options.Create(new BlackwingOptions { Storage = new StorageOptions { ImagesPath = root } }));

    private static string NewTempRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "blackwing-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static MemoryStream Stream(string content) => new(Encoding.UTF8.GetBytes(content));
}
