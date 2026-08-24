using Blackwing.Api.Configuration;
using Microsoft.Extensions.Options;

namespace Blackwing.Api.Platform.Storage;

/// <summary>
/// Resolves the configured storage roots to absolute paths. Nothing writes into them yet; this
/// phase only validates them and makes sure the directories exist.
/// </summary>
internal sealed class BlackwingStoragePaths
{
    public BlackwingStoragePaths(IOptions<StorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var storage = options.Value;
        Images = Path.GetFullPath(storage.ImagesPath!);
        DataProtectionKeys = string.IsNullOrWhiteSpace(storage.DataProtectionKeysPath)
            ? null
            : Path.GetFullPath(storage.DataProtectionKeysPath);
    }

    /// <summary>Root of the image volume.</summary>
    public string Images { get; }

    /// <summary>Data Protection key ring directory, or <see langword="null"/> when not configured.</summary>
    public string? DataProtectionKeys { get; }

    public void EnsureCreated()
    {
        Directory.CreateDirectory(Images);
        if (DataProtectionKeys is not null)
        {
            Directory.CreateDirectory(DataProtectionKeys);
        }
    }
}
