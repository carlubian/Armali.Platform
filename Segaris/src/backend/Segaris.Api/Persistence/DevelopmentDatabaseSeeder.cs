using Segaris.Api.Modules.Configuration.Seeding;
using Segaris.Api.Modules.Identity.Seeding;

namespace Segaris.Api.Persistence;

internal sealed class DevelopmentDatabaseSeeder(
    IdentitySeeder identitySeeder,
    ConfigurationSeeder configurationSeeder)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await identitySeeder.SeedAsync(cancellationToken);
        await configurationSeeder.SeedAsync(cancellationToken);
    }
}
