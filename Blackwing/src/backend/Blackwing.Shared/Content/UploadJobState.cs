namespace Blackwing.Shared.Content;

/// <summary>
/// Lifecycle of one queued upload. The values are stored as <see cref="int"/>.
/// </summary>
public enum UploadJobState
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Duplicate = 4,
}
