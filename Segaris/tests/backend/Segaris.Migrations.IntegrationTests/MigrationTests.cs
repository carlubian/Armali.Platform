using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Segaris.Api.Persistence;

namespace Segaris.Migrations.IntegrationTests;

public sealed class MigrationTests
{
    [Fact]
    public async Task Sqlite_migrations_create_a_fresh_database()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"segaris-{Guid.NewGuid():N}.db");

        try
        {
            await using (var database = CreateContext("Sqlite", $"Data Source={databasePath}"))
            {
                await database.Database.MigrateAsync();

                var appliedMigrations = await database.Database.GetAppliedMigrationsAsync();
                Assert.Contains(
                    appliedMigrations,
                    migration => migration.EndsWith("_InitialPersistenceFoundation"));
                Assert.True(File.Exists(databasePath));
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }

    [Fact]
    public void Provider_histories_have_correlated_migration_names()
    {
        using var sqlite = CreateContext("Sqlite", "Data Source=:memory:");
        using var postgres = CreateContext(
            "Postgres",
            "Host=localhost;Database=segaris_test;Username=postgres;Password=postgres");

        var sqliteNames = sqlite.Database.GetMigrations().Select(LogicalName).ToArray();
        var postgresNames = postgres.Database.GetMigrations().Select(LogicalName).ToArray();

        Assert.Equal(sqliteNames, postgresNames);
        Assert.Contains("InitialPersistenceFoundation", sqliteNames);
        Assert.Contains("AttachmentStorage", sqliteNames);
        Assert.Contains("IdentityProfile", sqliteNames);
    }

    [Fact]
    public async Task Sqlite_upgrades_from_the_current_schema()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"segaris-upgrade-{Guid.NewGuid():N}.db");
        try
        {
            await using var database = CreateContext("Sqlite", $"Data Source={databasePath}");
            var previousMigration = database.Database.GetMigrations()
                .Single(migration => migration.EndsWith("_BackgroundJobs", StringComparison.Ordinal));
            var migrator = database.GetService<IMigrator>();

            await migrator.MigrateAsync(previousMigration);
            await migrator.MigrateAsync();

            var applied = await database.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, migration => migration.EndsWith("_IdentityProfile"));
            await database.Database.OpenConnectionAsync();
            await using var command = database.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM identity_users WHERE Language = 'en-GB'";
            Assert.Equal(0L, (long)(await command.ExecuteScalarAsync())!);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task Persistence_rejects_non_utc_technical_timestamps()
    {
        await using var database = CreateContext("Sqlite", "Data Source=:memory:");
        database.Set<Segaris.Api.Platform.Persistence.PersistenceCompatibilityRecord>().Add(new()
        {
            Name = "Non-UTC fixture",
            CivilDate = new DateOnly(2026, 6, 12),
            Amount = 1m,
            CurrencyCode = "EUR",
            CreatedAt = new DateTimeOffset(2026, 6, 12, 10, 0, 0, TimeSpan.FromHours(2)),
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => database.SaveChangesAsync());

        Assert.Contains("must be UTC", exception.Message);
    }

    private static Segaris.Persistence.SegarisDbContext CreateContext(
        string provider,
        string connectionString)
    {
        return new SegarisDesignTimeDbContextFactory().CreateDbContext(
        [
            "--provider",
            provider,
            "--connection",
            connectionString,
        ]);
    }

    private static string LogicalName(string migrationName)
    {
        var separator = migrationName.IndexOf('_', StringComparison.Ordinal);
        return separator < 0 ? migrationName : migrationName[(separator + 1)..];
    }
}
