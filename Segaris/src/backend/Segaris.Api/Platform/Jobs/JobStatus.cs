namespace Segaris.Api.Platform.Jobs;

/// <summary>
/// The safe public projection of a job. It exposes lifecycle and progress information and a
/// reference to any produced output, but never parameters, private payloads, or secrets.
/// </summary>
internal sealed record JobStatus(
    int Id,
    string JobType,
    string State,
    int? Progress,
    string? ProgressCode,
    string? ResultReference,
    string? ResultCode,
    string? FailureCode,
    bool CancellationRequested,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt)
{
    public static JobStatus FromRecord(JobRecord record) => new(
        record.Id,
        record.JobType,
        record.State.ToString(),
        record.Progress,
        record.ProgressCode,
        record.ResultReference,
        record.ResultCode,
        record.FailureCode,
        record.CancellationRequested,
        record.CreatedAt,
        record.StartedAt,
        record.CompletedAt);
}
