namespace Segaris.Api.Platform.Jobs;

internal static class JobServiceCollectionExtensions
{
    /// <summary>
    /// Registers the shared persistent background-job infrastructure. Individual job types
    /// register their own <see cref="JobTypeRegistration"/> and scoped handler.
    /// </summary>
    public static IServiceCollection AddSegarisJobs(this IServiceCollection services)
    {
        services.AddSingleton<JobCoordinator>();
        services.AddSingleton<JobTypeRegistry>();
        services.AddScoped<JobService>();
        services.AddHostedService<JobWorker>();

        // The probe handler is a test fixture: it is registered so the worker can resolve it,
        // but only Testing-environment endpoints enqueue it, so it stays inert elsewhere.
        services.AddScoped<ProbeJobHandler>();
        services.AddSingleton(new JobTypeRegistration(
            ProbeJobHandler.JobType,
            ProbeJobHandler.ExclusivityKey,
            typeof(ProbeJobHandler)));
        return services;
    }
}
