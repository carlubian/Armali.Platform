using Blackwing.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Blackwing.Api.Persistence;

internal static class DatabaseMigrationExtensions
{
    public static async Task MigrateBlackwingDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<BlackwingDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<BlackwingDbContext>>();
        logger.LogInformation("Applying Blackwing database migrations.");
        await database.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Blackwing database migrations completed.");
    }
}
