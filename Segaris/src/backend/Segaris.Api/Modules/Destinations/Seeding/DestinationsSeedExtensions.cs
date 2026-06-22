namespace Segaris.Api.Modules.Destinations.Seeding;

internal static class DestinationsSeedExtensions
{
    public static async Task SeedDestinationsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DestinationsSeeder>().SeedAsync(cancellationToken);
    }
}
