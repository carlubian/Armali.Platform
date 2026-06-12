namespace Segaris.Api.Platform.Jobs;

/// <summary>
/// The restricted surface a handler receives while it runs. It exposes the job identity,
/// its typed parameters, and a best-effort way to publish safe progress without granting
/// the handler direct access to lifecycle transitions.
/// </summary>
internal sealed class JobExecutionContext(
    int jobId,
    string jobType,
    string? parameters,
    Func<int, int, string?, CancellationToken, Task> reportProgress)
{
    public int JobId { get; } = jobId;

    public string JobType { get; } = jobType;

    public string? Parameters { get; } = parameters;

    /// <summary>
    /// Records a safe progress value (0-100) and optional message code. Progress is
    /// best-effort and must never carry secrets or private payloads.
    /// </summary>
    public Task ReportProgressAsync(int percent, string? code, CancellationToken cancellationToken)
    {
        var bounded = Math.Clamp(percent, 0, 100);
        return reportProgress(JobId, bounded, code, cancellationToken);
    }
}
