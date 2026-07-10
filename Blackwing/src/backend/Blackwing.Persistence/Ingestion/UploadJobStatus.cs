namespace Blackwing.Persistence.Ingestion;

/// <summary>The lifecycle of a single staged upload as it moves through ingestion.</summary>
public enum UploadJobStatus
{
    /// <summary>Staged and waiting for the worker to pick it up.</summary>
    Pending,
    /// <summary>Claimed by the worker and being turned into an image.</summary>
    Processing,
    /// <summary>Turned into a pending-review image; terminal and successful.</summary>
    Completed,
    /// <summary>Processing failed; terminal unless the failure is recoverable and the job is retried.</summary>
    Failed,
    /// <summary>The bytes already exist for this owner; nothing new was created. Terminal.</summary>
    Duplicate,
}
