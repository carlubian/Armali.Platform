using Blackwing.Shared.Identity;

namespace Blackwing.Shared.Images;

/// <summary>
/// Content-addressed storage for image files. The physical layout is an implementation detail
/// behind this interface so it can evolve; the database stores the hash and the metadata, never a
/// path.
/// </summary>
public interface IImageBlobStore
{
    /// <summary>
    /// Writes <paramref name="content"/> to staging while hashing it, and gives up as soon as it
    /// exceeds <paramref name="maximumBytes"/>. The content is never buffered whole in memory.
    /// </summary>
    Task<StagedImage> StageAsync(Stream content, long maximumBytes, CancellationToken cancellationToken);

    /// <summary>Opens a staged upload for reading, positioned at the start.</summary>
    Task<Stream> OpenStagedAsync(StagedImage staged, CancellationToken cancellationToken);

    /// <summary>
    /// Moves the staged original to its final place and writes the already encoded derivatives
    /// next to it. If anything fails midway, whatever was already written is removed first.
    /// </summary>
    Task PromoteAsync(
        UserId owner,
        StagedImage staged,
        IReadOnlyDictionary<ImageVariant, byte[]> derivatives,
        CancellationToken cancellationToken);

    /// <summary>Removes a staged upload that will not be promoted. Tolerates it being gone.</summary>
    Task DiscardStagedAsync(StagedImage staged, CancellationToken cancellationToken);

    /// <summary>Opens one stored variant, or returns <see langword="null"/> when it is not there.</summary>
    Task<ImageBlob?> OpenReadAsync(
        UserId owner,
        string contentHash,
        ImageVariant variant,
        CancellationToken cancellationToken);

    /// <summary>Deletes all three variants of an image. Tolerates any of them being absent.</summary>
    Task DeleteAsync(UserId owner, string contentHash, CancellationToken cancellationToken);
}
