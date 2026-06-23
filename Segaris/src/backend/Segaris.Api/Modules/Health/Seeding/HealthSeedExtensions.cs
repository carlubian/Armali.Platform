namespace Segaris.Api.Modules.Health.Seeding;

internal static class HealthSeedExtensions
{
    public static async Task SeedHealthAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<HealthSeeder>().SeedAsync(cancellationToken);
    }
}
