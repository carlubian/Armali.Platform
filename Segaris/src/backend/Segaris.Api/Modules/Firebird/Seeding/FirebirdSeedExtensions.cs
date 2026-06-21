namespace Segaris.Api.Modules.Firebird.Seeding;

internal static class FirebirdSeedExtensions
{
    public static async Task SeedFirebirdAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<FirebirdSeeder>().SeedAsync(cancellationToken);
    }
}

