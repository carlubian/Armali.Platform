namespace Blackwing.Persistence.Ingestion;

/// <summary>
/// Frozen diagnostic codes for a failed upload job. Recoverable failures leave the
/// staged bytes in place so the owner can retry safely; a permanent failure means
/// retrying cannot help (the file is not a decodable image) and the staged bytes
/// are discarded.
/// </summary>
public static class UploadFailureCodes
{
    /// <summary>The staged bytes are not a decodable JPEG, PNG or WebP. Permanent.</summary>
    public const string InvalidImage = "invalid_image";

    /// <summary>An unexpected error occurred while turning the image into derivatives. Recoverable.</summary>
    public const string ProcessingError = "processing_error";

    /// <summary>Writing the original or a derivative to the image store failed. Recoverable.</summary>
    public const string StorageError = "storage_error";

    /// <summary>The staged bytes are gone (never uploaded, or already cleaned up). Permanent.</summary>
    public const string StagingMissing = "staging_missing";

    /// <summary>Reports whether a failure code can be safely retried (its staged bytes are kept).</summary>
    public static bool IsRecoverable(string? code) => code switch
    {
        ProcessingError or StorageError => true,
        _ => false,
    };
}
