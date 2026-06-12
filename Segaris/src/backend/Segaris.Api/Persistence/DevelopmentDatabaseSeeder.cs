namespace Segaris.Api.Persistence;

internal sealed class DevelopmentDatabaseSeeder
{
    public Task SeedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
