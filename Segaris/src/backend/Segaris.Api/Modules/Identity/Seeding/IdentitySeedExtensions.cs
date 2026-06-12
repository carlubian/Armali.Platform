namespace Segaris.Api.Modules.Identity.Seeding;

internal static class IdentitySeedExtensions
{
    public static async Task SeedIdentityAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await seeder.SeedAsync(cancellationToken);
    }
}
