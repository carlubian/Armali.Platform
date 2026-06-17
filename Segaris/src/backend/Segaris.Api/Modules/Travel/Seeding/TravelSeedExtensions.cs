namespace Segaris.Api.Modules.Travel.Seeding;

internal static class TravelSeedExtensions
{
    public static async Task SeedTravelAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TravelSeeder>().SeedAsync(cancellationToken);
    }
}
