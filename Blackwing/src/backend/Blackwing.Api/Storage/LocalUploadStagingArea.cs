using System.Security.Cryptography;
using Blackwing.Api.Configuration;
using Blackwing.Shared.Storage;
using Microsoft.Extensions.Options;

namespace Blackwing.Api.Storage;

/// <summary>
/// Local-filesystem adapter for <see cref="IUploadStagingArea"/>. Each staged file
/// lives at <c>{stagingRoot}/{userId}/{token}</c>, is streamed (never buffered
/// wholesale), and its SHA-256 is computed incrementally as the bytes pass through.
/// </summary>
public sealed class LocalUploadStagingArea(IOptions<BlackwingOptions> options) : IUploadStagingArea
{
    private const int StreamBufferBytes = 81920;
    private readonly string root = ResolveRoot(options.Value.Storage.StagingPath);

    public async Task<StagingResult> StageAsync(Guid ownerUserId, Stream content, long maxBytes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        var token = Guid.NewGuid().ToString("N");
        var path = ResolvePath(ownerUserId, token);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var header = new byte[ImageFormatDetector.HeaderBytes];
        var headerFilled = 0;
        long total = 0;
        var buffer = new byte[StreamBufferBytes];

        try
        {
            await using (var destination = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, StreamBufferBytes, useAsync: true))
            {
                int read;
                while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    total += read;
                    if (total > maxBytes)
                    {
                        await destination.DisposeAsync();
                        Discard(ownerUserId, token);
                        return StagingResult.TooLarge();
                    }

                    hash.AppendData(buffer, 0, read);
                    if (headerFilled < header.Length)
                    {
                        var copy = Math.Min(read, header.Length - headerFilled);
                        Array.Copy(buffer, 0, header, headerFilled, copy);
                        headerFilled += copy;
                    }

                    await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
            }
        }
        catch
        {
            Discard(ownerUserId, token);
            throw;
        }

        if (total == 0)
        {
            Discard(ownerUserId, token);
            return StagingResult.TooLarge(); // An empty file carries no image; treat it as rejected.
        }

        var sha256 = Convert.ToHexStringLower(hash.GetHashAndReset());
        var capturedHeader = headerFilled == header.Length ? header : header[..headerFilled];
        return StagingResult.Staged(token, sha256, total, capturedHeader);
    }

    public Task<Stream?> OpenReadAsync(Guid ownerUserId, string token, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(ownerUserId, token);
        if (!File.Exists(path)) return Task.FromResult<Stream?>(null);
        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, StreamBufferBytes, useAsync: true);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DiscardAsync(Guid ownerUserId, string token, CancellationToken cancellationToken = default)
    {
        Discard(ownerUserId, token);
        return Task.CompletedTask;
    }

    private void Discard(Guid ownerUserId, string token)
    {
        var path = ResolvePath(ownerUserId, token);
        if (File.Exists(path)) File.Delete(path);
        var ownerDirectory = Path.GetDirectoryName(path)!;
        if (Directory.Exists(ownerDirectory) && !Directory.EnumerateFileSystemEntries(ownerDirectory).Any())
            Directory.Delete(ownerDirectory);
    }

    private string ResolvePath(Guid ownerUserId, string token)
    {
        if (string.IsNullOrWhiteSpace(token) || !token.All(Uri.IsHexDigit))
            throw new ArgumentException("A staging token must be hexadecimal.", nameof(token));
        return Path.Combine(root, ownerUserId.ToString("N"), token);
    }

    private static string ResolveRoot(string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath)) throw new InvalidOperationException("Blackwing:Storage:StagingPath is required.");
        return Path.GetFullPath(configuredPath);
    }
}
