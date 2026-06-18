namespace Segaris.Api.Modules.Clothes.Seeding;

internal static class ClothesSeedExtensions
{
    public static async Task SeedClothesAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ClothesSeeder>().SeedAsync(cancellationToken);
    }
}
