namespace Segaris.Api.Modules.Inventory.Seeding;

internal static class InventorySeedExtensions
{
    public static async Task SeedInventoryAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<InventorySeeder>().SeedAsync(cancellationToken);
    }
}
