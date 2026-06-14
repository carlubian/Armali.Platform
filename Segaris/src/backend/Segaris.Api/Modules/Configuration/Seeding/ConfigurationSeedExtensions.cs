namespace Segaris.Api.Modules.Configuration.Seeding;

internal static class ConfigurationSeedExtensions
{
    public static async Task SeedConfigurationAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<ConfigurationSeeder>();
        await seeder.SeedAsync(cancellationToken);
    }
}
