namespace Segaris.Api.Platform.Jobs;

/// <summary>
/// A controllable handler used only by Testing-environment probe endpoints to exercise the
/// generic job lifecycle (success, failure, cancellation, claiming, and recovery) without a
/// real domain or PostgreSQL. It is inert in other environments because no endpoint enqueues it.
/// </summary>
internal sealed class ProbeJobHandler : IJobHandler
{
    public const string JobType = "probe";
    public const string ExclusivityKey = "probe";

    public async Task<JobResult> ExecuteAsync(
        JobExecutionContext context,
        CancellationToken cancellationToken)
    {
        await context.ReportProgressAsync(25, "started", cancellationToken);
        switch (context.Parameters)
        {
            case "fail":
                throw new JobFailureException("probe_failed", "The probe job failed on request.");
            case "block":
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();
                return JobResult.None;
            default:
                await context.ReportProgressAsync(75, "working", cancellationToken);
                return new JobResult("probe-result", "probe_succeeded");
        }
    }
}
