using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Segaris.Persistence;

namespace Segaris.Api.Persistence;

internal static class DatabaseMigrationExtensions
{
    public static async Task MigrateSegarisDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SegarisDbContext>>();
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Applying Segaris database migrations.");
        try
        {
            await database.Database.MigrateAsync(cancellationToken);
            logger.LogInformation(
                "Segaris database migrations completed in {ElapsedMilliseconds} ms.",
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception exception)
        {
            logger.LogCritical(
                exception,
                "Segaris database migration failed after {ElapsedMilliseconds} ms.",
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
