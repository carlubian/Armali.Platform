using System.Diagnostics;
using Blackwing.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Blackwing.Api.Platform.Persistence;

internal static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Applies pending migrations at startup so a fresh deployment is operable without a manual
    /// step. A failure here is fatal by design: serving requests against a schema that does not
    /// match the model would corrupt data quietly instead of failing loudly.
    /// </summary>
    public static async Task MigrateBlackwingDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<BlackwingDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<BlackwingDbContext>>();
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Applying Blackwing database migrations.");
        try
        {
            await database.Database.MigrateAsync(cancellationToken);
            logger.LogInformation(
                "Blackwing database migrations completed in {ElapsedMilliseconds} ms.",
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception exception)
        {
            logger.LogCritical(
                exception,
                "Blackwing database migration failed after {ElapsedMilliseconds} ms.",
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
