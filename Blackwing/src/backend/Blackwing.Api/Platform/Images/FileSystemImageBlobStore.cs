using System.Buffers;
using System.Security.Cryptography;
using Blackwing.Api.Platform.Api;
using Blackwing.Api.Platform.Storage;
using Blackwing.Shared.Identity;
using Blackwing.Shared.Images;

namespace Blackwing.Api.Platform.Images;

/// <summary>
/// Stores images on the local volume, addressed by the SHA-256 of their content and fanned out
/// two levels deep per account.
/// </summary>
/// <remarks>
/// Layout, relative to the root of the image volume:
/// <code>
/// {ownerUserId}/{hash[0..2]}/{hash[2..4]}/{hash}                 original, bytes untouched
/// {ownerUserId}/{hash[0..2]}/{hash[2..4]}/{hash}.thumb.webp
/// {ownerUserId}/{hash[0..2]}/{hash[2..4]}/{hash}.preview.webp
/// .staging/{stagingId}.upload
/// </code>
/// Two hexadecimal characters per level keep any directory far below tens of thousands of
/// entries at the corpus size this application is built for. The original is stored without an
/// extension and without any transformation, so a download is byte for byte what was uploaded.
/// </remarks>
internal sealed class FileSystemImageBlobStore(
    BlackwingStoragePaths paths,
    ILogger<FileSystemImageBlobStore> logger) : IImageBlobStore
{
    private const int BufferSize = 64 * 1024;
    private const string StagingDirectoryName = ".staging";
    private const string StagingExtension = ".upload";
    private const int HashLength = 64;
    private const int StagingIdLength = 32;

    /// <summary>
    /// Computes the path of a variant relative to the volume root. Pure, so it can be tested
    /// without touching disk. The hash must be 64 lowercase hexadecimal characters; anything else
    /// is rejected, which also rules out path traversal through a crafted value.
    /// </summary>
    internal static string GetRelativePath(UserId owner, string contentHash, ImageVariant variant)
    {
        EnsureValidHash(contentHash);

        var fileName = variant switch
        {
            ImageVariant.Original => contentHash,
            ImageVariant.Thumbnail => $"{contentHash}.thumb.webp",
            ImageVariant.Preview => $"{contentHash}.preview.webp",
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unknown image variant."),
        };

        return Path.Combine(owner.ToString(), contentHash[..2], contentHash[2..4], fileName);
    }

    public async Task<StagedImage> StageAsync(
        Stream content,
        long maximumBytes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        var stagingId = Guid.NewGuid().ToString("N");
        var path = GetStagingPath(stagingId);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var (hash, total) = await WriteBoundedAsync(content, path, maximumBytes, cancellationToken);
            return new StagedImage(stagingId, hash, total);
        }
        catch (ApiProblemException)
        {
            TryDelete(path, "staged upload");
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            TryDelete(path, "staged upload");
            logger.LogError(exception, "Staging an image upload failed.");
            throw StorageUnavailable();
        }
        catch (OperationCanceledException)
        {
            TryDelete(path, "staged upload");
            throw;
        }
    }

    public Task<Stream> OpenStagedAsync(StagedImage staged, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(staged);

        try
        {
            return Task.FromResult<Stream>(OpenRead(GetStagingPath(staged.StagingId)));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogError(exception, "A staged image upload could not be opened.");
            throw StorageUnavailable();
        }
    }

    public async Task PromoteAsync(
        UserId owner,
        StagedImage staged,
        IReadOnlyDictionary<ImageVariant, byte[]> derivatives,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(staged);
        ArgumentNullException.ThrowIfNull(derivatives);

        var originalPath = GetFullPath(owner, staged.ContentHash, ImageVariant.Original);
        var written = new List<string>();

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(originalPath)!);

            foreach (var (variant, bytes) in derivatives)
            {
                if (variant == ImageVariant.Original)
                {
                    throw new ArgumentException("The original is promoted from staging, not supplied as a derivative.", nameof(derivatives));
                }

                var derivativePath = GetFullPath(owner, staged.ContentHash, variant);
                await WriteAtomicallyAsync(derivativePath, bytes, cancellationToken);
                written.Add(derivativePath);
            }

            // The name is the hash of the content, so replacing an existing file replaces it with
            // identical bytes. That makes a leftover from an interrupted earlier attempt harmless
            // instead of a reason to fail.
            File.Move(GetStagingPath(staged.StagingId), originalPath, overwrite: true);
            written.Add(originalPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            RemoveAll(written);
            logger.LogError(exception, "Promoting an image to its final place failed.");
            throw StorageUnavailable();
        }
        catch
        {
            RemoveAll(written);
            throw;
        }
    }

    public Task DiscardStagedAsync(StagedImage staged, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(staged);

        TryDelete(GetStagingPath(staged.StagingId), "staged upload");
        return Task.CompletedTask;
    }

    public async Task<ImageBlob?> OpenReadAsync(
        UserId owner,
        string contentHash,
        ImageVariant variant,
        CancellationToken cancellationToken)
    {
        var path = GetFullPath(owner, contentHash, variant);

        FileStream stream;
        try
        {
            stream = OpenRead(path);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogError(exception, "A stored image variant could not be opened.");
            throw StorageUnavailable();
        }

        try
        {
            var contentType = variant == ImageVariant.Original
                ? await DetectOriginalContentTypeAsync(stream, cancellationToken)
                : "image/webp";

            return new ImageBlob(stream, stream.Length, contentType);
        }
        catch
        {
            await stream.DisposeAsync();
            throw;
        }
    }

    public Task DeleteAsync(UserId owner, string contentHash, CancellationToken cancellationToken)
    {
        foreach (var variant in Enum.GetValues<ImageVariant>())
        {
            var path = GetFullPath(owner, contentHash, variant);
            try
            {
                File.Delete(path);
            }
            catch (DirectoryNotFoundException)
            {
                // Nothing was ever stored for this variant. Deleting is idempotent by design.
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                logger.LogError(exception, "A stored image variant could not be deleted.");
                throw StorageUnavailable();
            }
        }

        return Task.CompletedTask;
    }

    private static void EnsureValidHash(string contentHash)
    {
        ArgumentNullException.ThrowIfNull(contentHash);

        if (contentHash.Length != HashLength
            || !contentHash.All(character => character is (>= '0' and <= '9') or (>= 'a' and <= 'f')))
        {
            throw new ArgumentException(
                "A content hash is 64 lowercase hexadecimal characters.",
                nameof(contentHash));
        }
    }

    private static void EnsureValidStagingId(string stagingId)
    {
        ArgumentNullException.ThrowIfNull(stagingId);

        if (stagingId.Length != StagingIdLength
            || !stagingId.All(character => character is (>= '0' and <= '9') or (>= 'a' and <= 'f')))
        {
            throw new ArgumentException("A staging identifier is 32 lowercase hexadecimal characters.", nameof(stagingId));
        }
    }

    private static async Task<(string Hash, long Length)> WriteBoundedAsync(
        Stream source,
        string destinationPath,
        long maximumBytes,
        CancellationToken cancellationToken)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);

        try
        {
            await using var destination = new FileStream(
                destinationPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                BufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            long total = 0;
            while (true)
            {
                var read = await source.ReadAsync(buffer.AsMemory(0, BufferSize), cancellationToken);
                if (read == 0)
                {
                    break;
                }

                total += read;

                // Give up the moment the limit is crossed. The rest of the upload is never read,
                // let alone buffered: the whole point of staging to disk is that no file ever
                // sits whole in managed memory.
                if (total > maximumBytes)
                {
                    throw new ApiProblemException(
                        StatusCodes.Status413PayloadTooLarge,
                        ImageErrorCodes.TooLarge,
                        "The image is too large.",
                        $"Images may not exceed {maximumBytes} bytes.");
                }

                hash.AppendData(buffer, 0, read);
                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            await destination.FlushAsync(cancellationToken);
            return (Convert.ToHexStringLower(hash.GetHashAndReset()), total);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static async Task WriteAtomicallyAsync(string path, byte[] bytes, CancellationToken cancellationToken)
    {
        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllBytesAsync(temporaryPath, bytes, cancellationToken);
            File.Move(temporaryPath, path, overwrite: true);
        }
        catch
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // The original failure is the one worth reporting.
            }

            throw;
        }
    }

    private static async Task<string> DetectOriginalContentTypeAsync(
        FileStream stream,
        CancellationToken cancellationToken)
    {
        var header = new byte[12];
        var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        stream.Position = 0;

        return ImagePolicy.DetectContentType(header.AsSpan(0, read)) ?? "application/octet-stream";
    }

    private static FileStream OpenRead(string path) => new(
        path,
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read,
        BufferSize,
        FileOptions.Asynchronous | FileOptions.SequentialScan);

    private static ApiProblemException StorageUnavailable() => new(
        StatusCodes.Status503ServiceUnavailable,
        ImageErrorCodes.StorageUnavailable,
        "The image storage is unavailable.");

    private string GetFullPath(UserId owner, string contentHash, ImageVariant variant) =>
        Path.Combine(paths.Images, GetRelativePath(owner, contentHash, variant));

    private string GetStagingPath(string stagingId)
    {
        EnsureValidStagingId(stagingId);
        return Path.Combine(paths.Images, StagingDirectoryName, $"{stagingId}{StagingExtension}");
    }

    private void RemoveAll(IEnumerable<string> files)
    {
        foreach (var file in files)
        {
            TryDelete(file, "partially promoted image file");
        }
    }

    private void TryDelete(string path, string purpose)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Could not remove {Purpose} at {Path}.", purpose, path);
        }
    }
}
