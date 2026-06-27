using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Belfalas.Persistence.Seeding;

/// <summary>
/// Startup helpers that bring the SQLite database up to the latest migration and seed
/// the reference content (world templates). Seeding is idempotent and carries no game
/// logic; runtime state is created when eras are authored in later waves.
/// </summary>
public static class BelfalasDatabaseExtensions
{
    /// <summary>Applies pending migrations and seeds reference data.</summary>
    public static async Task MigrateAndSeedBelfalasAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<BelfalasDbContext>();

        EnsureDatabaseDirectoryExists(database.Database.GetConnectionString());
        await database.Database.MigrateAsync(cancellationToken);
        await SeedReferenceDataAsync(database, cancellationToken);
    }

    /// <summary>Seeds reference data (the default world template) when it is absent.</summary>
    public static async Task SeedReferenceDataAsync(
        BelfalasDbContext database,
        CancellationToken cancellationToken = default)
    {
        var defaultExists = await database.WorldTemplates
            .AnyAsync(template => template.Id == WorldTemplateSeed.DefaultTemplateId, cancellationToken);
        if (defaultExists)
        {
            return;
        }

        database.WorldTemplates.Add(WorldTemplateSeed.CreateDefault());
        await database.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Creates the directory holding the SQLite file when it does not yet exist.</summary>
    private static void EnsureDatabaseDirectoryExists(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
