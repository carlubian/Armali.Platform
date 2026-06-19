namespace Segaris.Api.Modules.Maintenance.Seeding;

internal static class MaintenanceSeedExtensions
{
    public static async Task SeedMaintenanceAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<MaintenanceSeeder>().SeedAsync(cancellationToken);
    }
}
