using System.Security.Cryptography;
using System.Text;
using Blackwing.Api.Configuration;
using Blackwing.Api.Storage;
using Blackwing.Shared.Storage;
using Microsoft.Extensions.Options;

namespace Blackwing.Api.Tests;

public sealed class UploadStagingTests
{
    [Fact]
    public async Task Staging_streams_bytes_hashes_them_and_captures_the_header()
    {
        var root = NewTempRoot();
        try
        {
            var staging = CreateStaging(root);
            var owner = Guid.NewGuid();
            var payload = Encoding.UTF8.GetBytes("the original picture bytes");
            var expectedSha = Convert.ToHexStringLower(SHA256.HashData(payload));

            var result = await staging.StageAsync(owner, new MemoryStream(payload), maxBytes: 1024);

            Assert.Equal(StagingOutcome.Staged, result.Outcome);
            Assert.Equal(expectedSha, result.Sha256);
            Assert.Equal(payload.Length, result.Bytes);
            Assert.Equal(payload[..ImageFormatDetector.HeaderBytes], result.Header);

            await using var readBack = await staging.OpenReadAsync(owner, result.Token);
            Assert.NotNull(readBack);
            using var reader = new StreamReader(readBack!);
            Assert.Equal("the original picture bytes", await reader.ReadToEndAsync());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Staging_stops_and_discards_a_file_over_the_limit()
    {
        var root = NewTempRoot();
        try
        {
            var staging = CreateStaging(root);
            var owner = Guid.NewGuid();
            var oversize = new byte[4096];

            var result = await staging.StageAsync(owner, new MemoryStream(oversize), maxBytes: 1024);

            Assert.Equal(StagingOutcome.TooLarge, result.Outcome);
            Assert.Empty(result.Token);
            // Nothing is left behind for the owner.
            Assert.False(Directory.Exists(Path.Combine(root, owner.ToString("N"))));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Staging_treats_an_empty_stream_as_rejected()
    {
        var root = NewTempRoot();
        try
        {
            var result = await CreateStaging(root).StageAsync(Guid.NewGuid(), new MemoryStream([]), maxBytes: 1024);
            Assert.Equal(StagingOutcome.TooLarge, result.Outcome);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Discarding_removes_the_staged_file()
    {
        var root = NewTempRoot();
        try
        {
            var staging = CreateStaging(root);
            var owner = Guid.NewGuid();
            var result = await staging.StageAsync(owner, new MemoryStream([1, 2, 3, 4]), maxBytes: 1024);

            await staging.DiscardAsync(owner, result.Token);

            Assert.Null(await staging.OpenReadAsync(owner, result.Token));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static LocalUploadStagingArea CreateStaging(string root) =>
        new(Options.Create(new BlackwingOptions { Storage = new StorageOptions { ImagesPath = root, StagingPath = root } }));

    private static string NewTempRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "blackwing-staging-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
