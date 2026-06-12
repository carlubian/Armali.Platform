using Segaris.Api.Modules.Identity.Seeding;

namespace Segaris.Api.Persistence;

internal sealed class DevelopmentDatabaseSeeder(IdentitySeeder identitySeeder)
{
    public Task SeedAsync(CancellationToken cancellationToken)
    {
        // Platform-owned foundation data only. Business demonstration data belongs to
        // module-specific development seeders and is not inserted by default.
        return identitySeeder.SeedAsync(cancellationToken);
    }
}
