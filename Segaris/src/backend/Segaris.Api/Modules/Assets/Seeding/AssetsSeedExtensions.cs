namespace Segaris.Api.Modules.Assets.Seeding;

internal static class AssetsSeedExtensions
{
    public static async Task SeedAssetsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AssetsSeeder>().SeedAsync(cancellationToken);
    }
}
