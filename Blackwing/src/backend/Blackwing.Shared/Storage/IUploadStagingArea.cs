namespace Blackwing.Shared.Storage;

/// <summary>Why staging a file stopped.</summary>
public enum StagingOutcome
{
    /// <summary>The bytes were fully written and hashed.</summary>
    Staged,
    /// <summary>The stream exceeded the per-file size limit; the partial file was discarded.</summary>
    TooLarge,
}

/// <summary>
/// The result of streaming one uploaded file into the staging area. On success it
/// carries the opaque <see cref="Token"/> the worker uses to reopen the bytes, the
/// streamed <see cref="Sha256"/> and byte count, and the leading <see cref="Header"/>
/// captured for cheap format sniffing.
/// </summary>
public sealed record StagingResult(StagingOutcome Outcome, string Token, string Sha256, long Bytes, byte[] Header)
{
    public static StagingResult TooLarge() => new(StagingOutcome.TooLarge, string.Empty, string.Empty, 0, []);
    public static StagingResult Staged(string token, string sha256, long bytes, byte[] header) =>
        new(StagingOutcome.Staged, token, sha256, bytes, header);
}

/// <summary>
/// A transient, per-user holding area for uploaded bytes between the request and the
/// worker. Files are addressed by an opaque token (never a public path) and are
/// removed once the worker succeeds, the upload duplicates an existing image, or the
/// owner discards a permanent failure. It is deliberately separate from the durable,
/// content-addressed image store so a half-finished or failed upload never pollutes it.
/// </summary>
public interface IUploadStagingArea
{
    /// <summary>
    /// Streams <paramref name="content"/> to a new staged file for the owner while
    /// computing its SHA-256, stopping and discarding the partial file if it exceeds
    /// <paramref name="maxBytes"/>. Nothing is buffered wholesale in memory.
    /// </summary>
    Task<StagingResult> StageAsync(Guid ownerUserId, Stream content, long maxBytes, CancellationToken cancellationToken = default);

    /// <summary>Opens the staged bytes for streaming, or returns <c>null</c> when the token is unknown.</summary>
    Task<Stream?> OpenReadAsync(Guid ownerUserId, string token, CancellationToken cancellationToken = default);

    /// <summary>Removes the staged file and prunes the owner's staging directory when empty. A missing file is ignored.</summary>
    Task DiscardAsync(Guid ownerUserId, string token, CancellationToken cancellationToken = default);
}
