namespace Segaris.Api.Modules.Capex.Seeding;

internal static class CapexSeedExtensions
{
    public static async Task SeedCapexAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<CapexSeeder>().SeedAsync(cancellationToken);
    }
}
