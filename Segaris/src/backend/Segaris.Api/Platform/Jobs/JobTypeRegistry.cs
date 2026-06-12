namespace Segaris.Api.Platform.Jobs;

/// <summary>
/// Declares one job type and the handler that runs it. Modules register these as singletons
/// so the API can validate exclusivity at enqueue time without resolving the scoped handler.
/// </summary>
internal sealed record JobTypeRegistration(string JobType, string? ExclusivityKey, Type HandlerType);

/// <summary>
/// The known job types for this backend instance, built from the registered
/// <see cref="JobTypeRegistration"/> values. Duplicate job types fail fast.
/// </summary>
internal sealed class JobTypeRegistry
{
    private readonly Dictionary<string, JobTypeRegistration> registrations;

    public JobTypeRegistry(IEnumerable<JobTypeRegistration> registrations)
    {
        this.registrations = new Dictionary<string, JobTypeRegistration>(StringComparer.Ordinal);
        foreach (var registration in registrations)
        {
            if (!this.registrations.TryAdd(registration.JobType, registration))
            {
                throw new InvalidOperationException(
                    $"The job type '{registration.JobType}' is registered more than once.");
            }
        }
    }

    public JobTypeRegistration Get(string jobType)
    {
        if (!registrations.TryGetValue(jobType, out var registration))
        {
            throw new InvalidOperationException($"No handler is registered for job type '{jobType}'.");
        }

        return registration;
    }

    public bool TryGet(string jobType, out JobTypeRegistration registration) =>
        registrations.TryGetValue(jobType, out registration!);
}
