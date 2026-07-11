namespace Blackwing.Shared.Storage;

/// <summary>
/// Port for the private image store. Delivery never exposes files as public
/// static content; every read is mediated by an authorized endpoint that has
/// already verified ownership. Implementations address bytes by the SHA-256 of
/// the original and separate them physically per user (defense in depth).
/// </summary>
public interface IImageStore
{
    /// <summary>Persists one derivative's bytes at its content-addressed path, creating the shard tree as needed.</summary>
    Task SaveAsync(Guid ownerUserId, string sha256, ImageDerivative derivative, Stream content, CancellationToken cancellationToken = default);

    /// <summary>Opens one derivative for streaming, or returns <c>null</c> when it is not stored.</summary>
    Task<Stream?> OpenReadAsync(Guid ownerUserId, string sha256, ImageDerivative derivative, CancellationToken cancellationToken = default);

    /// <summary>Reports whether a derivative is present on disk.</summary>
    Task<bool> ExistsAsync(Guid ownerUserId, string sha256, ImageDerivative derivative, CancellationToken cancellationToken = default);

    /// <summary>Removes every derivative of an image and prunes now-empty shard directories. Missing files are ignored.</summary>
    Task DeleteAllAsync(Guid ownerUserId, string sha256, CancellationToken cancellationToken = default);
}
