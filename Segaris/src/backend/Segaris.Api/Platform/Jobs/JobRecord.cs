namespace Segaris.Api.Platform.Jobs;

/// <summary>
/// The persistent record for one background job. PostgreSQL is the source of truth for
/// claiming, status transitions, and recovery. The record stores only safe diagnostic
/// summaries; serialized exceptions, stack traces, and secrets never enter it.
/// </summary>
internal sealed class JobRecord
{
    public int Id { get; set; }

    /// <summary>Stable job-type code that selects the owning handler, for example "backup".</summary>
    public string JobType { get; set; } = null!;

    public JobState State { get; set; }

    /// <summary>
    /// Holds the exclusivity key while the job is active and is cleared on any terminal
    /// state. A unique index over this column lets PostgreSQL and SQLite both reject a
    /// second active job for the same key, for example a second backup.
    /// </summary>
    public string? ActiveExclusivityKey { get; set; }

    /// <summary>Versioned, typed parameters required by the handler. Never contains secrets.</summary>
    public string? Parameters { get; set; }

    public int? Progress { get; set; }

    public string? ProgressCode { get; set; }

    /// <summary>Safe completion metadata, for example the produced package filename.</summary>
    public string? ResultReference { get; set; }

    public string? ResultCode { get; set; }

    public string? FailureCode { get; set; }

    public string? TraceId { get; set; }

    public int? CreatedBy { get; set; }

    public bool CancellationRequested { get; set; }

    public DateTimeOffset? CancellationRequestedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
