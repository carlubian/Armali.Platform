using Blackwing.Shared.Ownership;

namespace Blackwing.Persistence.Ingestion;

/// <summary>
/// One staged file waiting to become an image. The upload endpoint stages the
/// bytes (computing the SHA-256) and records a job; a background worker claims it,
/// generates derivatives and creates the <see cref="Gallery.Image"/>. The job keeps
/// the batch responsive — the request returns as soon as staging finishes — and it
/// carries per-item progress and diagnostics so a failure never blocks the rest.
/// </summary>
public sealed class UploadJob : IOwnedEntity
{
    private UploadJob()
    {
    }

    public Guid Id { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string DeclaredContentType { get; private set; } = string.Empty;
    public long Bytes { get; private set; }
    public string Sha256 { get; private set; } = string.Empty;

    /// <summary>Opaque handle to the staged bytes in the staging area; never a public path.</summary>
    public string StagingToken { get; private set; } = string.Empty;

    public UploadJobStatus Status { get; private set; }

    /// <summary>The created image once the job completes; <c>null</c> otherwise.</summary>
    public Guid? ImageId { get; private set; }

    public string? FailureCode { get; private set; }
    public string? FailureMessage { get; private set; }

    /// <summary>How many times the worker has begun processing this job.</summary>
    public int AttemptCount { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsTerminal => Status is UploadJobStatus.Completed or UploadJobStatus.Duplicate
        || (Status is UploadJobStatus.Failed && !UploadFailureCodes.IsRecoverable(FailureCode));

    public static UploadJob Create(Guid ownerUserId, string originalFileName, string declaredContentType, long bytes, string sha256, string stagingToken, DateTimeOffset now)
    {
        if (ownerUserId == Guid.Empty) throw new ArgumentException("An owner is required.", nameof(ownerUserId));
        EnsureUtc(now, nameof(now));
        return new UploadJob
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            OriginalFileName = Clamp(Required(originalFileName, nameof(originalFileName)), FileNameMaxLength),
            DeclaredContentType = Clamp(Required(declaredContentType, nameof(declaredContentType)), ContentTypeMaxLength),
            Bytes = Positive(bytes, nameof(bytes)),
            Sha256 = NormalizeSha256(sha256),
            StagingToken = Required(stagingToken, nameof(stagingToken)),
            Status = UploadJobStatus.Pending,
            AttemptCount = 0,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>Records a successful ingestion into the given image; terminal.</summary>
    public void Complete(Guid imageId, DateTimeOffset now)
    {
        if (imageId == Guid.Empty) throw new ArgumentException("An image id is required.", nameof(imageId));
        EnsureUtc(now, nameof(now));
        Status = UploadJobStatus.Completed;
        ImageId = imageId;
        FailureCode = null;
        FailureMessage = null;
        UpdatedAt = now;
    }

    /// <summary>Records that the bytes already exist for this owner; terminal, nothing was created.</summary>
    public void MarkDuplicate(DateTimeOffset now)
    {
        EnsureUtc(now, nameof(now));
        Status = UploadJobStatus.Duplicate;
        FailureCode = null;
        FailureMessage = null;
        UpdatedAt = now;
    }

    /// <summary>Records a processing failure with a frozen code and a short diagnostic message.</summary>
    public void Fail(string code, string? message, DateTimeOffset now)
    {
        EnsureUtc(now, nameof(now));
        Status = UploadJobStatus.Failed;
        FailureCode = Required(code, nameof(code));
        FailureMessage = message is null ? null : Clamp(message, FailureMessageMaxLength);
        UpdatedAt = now;
    }

    /// <summary>
    /// Returns the job to the queue. Used by an explicit retry of a recoverable
    /// failure and by the startup sweep that recovers jobs left mid-flight by a
    /// crash. Clears the previous failure; the attempt count is bumped when the
    /// worker next claims it. Refuses to reopen a successful or duplicate job.
    /// </summary>
    public void Requeue(DateTimeOffset now)
    {
        EnsureUtc(now, nameof(now));
        if (Status is UploadJobStatus.Completed or UploadJobStatus.Duplicate)
            throw new InvalidOperationException("A completed or duplicate job cannot be requeued.");
        Status = UploadJobStatus.Pending;
        FailureCode = null;
        FailureMessage = null;
        UpdatedAt = now;
    }

    public const int FileNameMaxLength = 260;
    public const int ContentTypeMaxLength = 100;
    public const int FailureMessageMaxLength = 500;

    private static string NormalizeSha256(string value)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.Length != 64 || !normalized.All(Uri.IsHexDigit))
            throw new ArgumentException("A SHA-256 must be 64 hexadecimal characters.", nameof(value));
        return normalized;
    }

    private static string Required(string value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException($"{name} is required.", name) : value.Trim();

    private static string Clamp(string value, int max) => value.Length <= max ? value : value[..max];

    private static long Positive(long value, string name) =>
        value > 0 ? value : throw new ArgumentOutOfRangeException(name, value, $"{name} must be positive.");

    private static void EnsureUtc(DateTimeOffset value, string name)
    {
        if (value.Offset != TimeSpan.Zero) throw new ArgumentException($"{name} must be UTC.", name);
    }
}
