namespace Segaris.Api.Modules.Processes.Seeding;

internal static class ProcessesSeedExtensions
{
    public static async Task SeedProcessesAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ProcessesSeeder>().SeedAsync(cancellationToken);
    }
}
