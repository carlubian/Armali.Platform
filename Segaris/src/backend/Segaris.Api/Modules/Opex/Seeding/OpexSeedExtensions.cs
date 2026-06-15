namespace Segaris.Api.Modules.Opex.Seeding;

internal static class OpexSeedExtensions
{
    public static async Task SeedOpexAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<OpexSeeder>().SeedAsync(cancellationToken);
    }
}
