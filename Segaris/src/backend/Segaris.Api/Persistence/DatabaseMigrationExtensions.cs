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
        await database.Database.MigrateAsync(cancellationToken);
    }
}
