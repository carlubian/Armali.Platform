using Blackwing.Api.Configuration;
using Blackwing.Shared.Storage;
using Microsoft.Extensions.Options;

namespace Blackwing.Api.Storage;

/// <summary>
/// Local-filesystem adapter for <see cref="IImageStore"/>. Bytes are streamed to
/// and from a content-addressed, per-user tree rooted at the configured images
/// path; nothing is buffered wholesale in memory.
/// </summary>
public sealed class LocalImageStore(IOptions<BlackwingOptions> options) : IImageStore
{
    private const int StreamBufferBytes = 81920;
    private readonly string root = ResolveRoot(options.Value.Storage.ImagesPath);

    public async Task SaveAsync(Guid ownerUserId, string sha256, ImageDerivative derivative, Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        var path = ImageStoragePath.Resolve(root, ownerUserId, sha256, derivative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var destination = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, StreamBufferBytes, useAsync: true);
        await content.CopyToAsync(destination, StreamBufferBytes, cancellationToken);
    }

    public Task<Stream?> OpenReadAsync(Guid ownerUserId, string sha256, ImageDerivative derivative, CancellationToken cancellationToken = default)
    {
        var path = ImageStoragePath.Resolve(root, ownerUserId, sha256, derivative);
        if (!File.Exists(path)) return Task.FromResult<Stream?>(null);
        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, StreamBufferBytes, useAsync: true);
        return Task.FromResult<Stream?>(stream);
    }

    public Task<bool> ExistsAsync(Guid ownerUserId, string sha256, ImageDerivative derivative, CancellationToken cancellationToken = default) =>
        Task.FromResult(File.Exists(ImageStoragePath.Resolve(root, ownerUserId, sha256, derivative)));

    public Task DeleteAllAsync(Guid ownerUserId, string sha256, CancellationToken cancellationToken = default)
    {
        foreach (var derivative in Enum.GetValues<ImageDerivative>())
        {
            var path = ImageStoragePath.Resolve(root, ownerUserId, sha256, derivative);
            if (File.Exists(path)) File.Delete(path);
        }

        // Prune shard directories left empty by this deletion, deepest first.
        var shard = Path.Combine(root, ImageStoragePath.OwnerSegment(ownerUserId), ImageStoragePath.ShardSegment(sha256));
        PruneIfEmpty(shard);
        PruneIfEmpty(Path.GetDirectoryName(shard)!);
        return Task.CompletedTask;
    }

    private static void PruneIfEmpty(string directory)
    {
        if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
            Directory.Delete(directory);
    }

    private static string ResolveRoot(string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath)) throw new InvalidOperationException("Blackwing:Storage:ImagesPath is required.");
        return Path.GetFullPath(configuredPath);
    }
}
