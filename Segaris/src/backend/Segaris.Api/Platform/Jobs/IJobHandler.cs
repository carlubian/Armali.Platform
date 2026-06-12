namespace Segaris.Api.Platform.Jobs;

/// <summary>
/// One handler implements the behavior for a single job type. Handlers are registered by
/// their owning module through <see cref="JobTypeRegistration"/> and resolved per job
/// through a dedicated dependency-injection scope.
/// </summary>
internal interface IJobHandler
{
    Task<JobResult> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken);
}

/// <summary>
/// The safe completion metadata a handler returns. The reference points to module-owned
/// output, for example a relative package filename; it never contains secrets or private
/// payloads.
/// </summary>
internal sealed record JobResult(string? ResultReference = null, string? ResultCode = null)
{
    public static readonly JobResult None = new();
}
