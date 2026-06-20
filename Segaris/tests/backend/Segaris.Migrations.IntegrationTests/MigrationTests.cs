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
                Assert.Contains(
                    appliedMigrations,
                    migration => migration.EndsWith("_ConfigurationFoundation"));
                Assert.Contains(
                    appliedMigrations,
                    migration => migration.EndsWith("_CapexDomainPersistence"));
                Assert.Contains(
                    appliedMigrations,
                    migration => migration.EndsWith("_OpexDomainPersistence"));
                Assert.Contains(
                    appliedMigrations,
                    migration => migration.EndsWith("_InventoryDomainPersistence"));
                Assert.Contains(
                    appliedMigrations,
                    migration => migration.EndsWith("_TravelDomainPersistence"));
                Assert.Contains(
                    appliedMigrations,
                    migration => migration.EndsWith("_MoodDomainPersistence"));
                Assert.Contains(
                    appliedMigrations,
                    migration => migration.EndsWith("_ClothesDomainPersistence"));
                Assert.Contains(
                    appliedMigrations,
                    migration => migration.EndsWith("_AssetsDomainPersistence"));
                Assert.Contains(
                    appliedMigrations,
                    migration => migration.EndsWith("_MaintenanceDomainPersistence"));
                Assert.Contains(
                    appliedMigrations,
                    migration => migration.EndsWith("_ProjectsDomainPersistence"));
                Assert.True(File.Exists(databasePath));

                await database.Database.OpenConnectionAsync();
                await using var command = database.Database.GetDbConnection().CreateCommand();
                // Three catalog tables plus the one-time initialization table.
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'configuration_%'";
                Assert.Equal(4L, (long)(await command.ExecuteScalarAsync())!);
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'capex_%'";
                Assert.Equal(3L, (long)(await command.ExecuteScalarAsync())!);
                // Contracts, occurrences, and the Opex-owned category catalog.
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'opex_%'";
                Assert.Equal(3L, (long)(await command.ExecuteScalarAsync())!);
                // Items, item-suppliers, orders, order lines, and the category and
                // location catalogs.
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'inventory_%'";
                Assert.Equal(6L, (long)(await command.ExecuteScalarAsync())!);
                // Trips, itinerary entries, expenses, and the trip-type and
                // expense-category catalogs.
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'travel_%'";
                Assert.Equal(5L, (long)(await command.ExecuteScalarAsync())!);
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'mood_%'";
                Assert.Equal(1L, (long)(await command.ExecuteScalarAsync())!);
                // Garments, the garment-colour join, and the category and colour catalogs.
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND (name LIKE 'clothes_%' OR name LIKE 'clothing_%')";
                Assert.Equal(4L, (long)(await command.ExecuteScalarAsync())!);
                // Assets plus the category and location catalogs.
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND (name = 'assets' OR name LIKE 'asset_%')";
                Assert.Equal(3L, (long)(await command.ExecuteScalarAsync())!);
                // Maintenance tasks plus the type catalog.
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'maintenance_%'";
                Assert.Equal(2L, (long)(await command.ExecuteScalarAsync())!);
                // Programs, axes, projects, activities, and the shared number allocator.
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'projects_%'";
                Assert.Equal(5L, (long)(await command.ExecuteScalarAsync())!);
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
        Assert.Contains("ConfigurationFoundation", sqliteNames);
        Assert.Contains("CapexDomainPersistence", sqliteNames);
        Assert.Contains("OpexDomainPersistence", sqliteNames);
        Assert.Contains("InventoryDomainPersistence", sqliteNames);
        Assert.Contains("TravelDomainPersistence", sqliteNames);
        Assert.Contains("MoodDomainPersistence", sqliteNames);
        Assert.Contains("ClothesDomainPersistence", sqliteNames);
        Assert.Contains("AssetsDomainPersistence", sqliteNames);
        Assert.Contains("MaintenanceDomainPersistence", sqliteNames);
        Assert.Contains("ProjectsDomainPersistence", sqliteNames);
    }

    [Fact]
    public void Projects_domain_persistence_is_the_current_tail_after_the_maintenance_model()
    {
        // The Projects Wave 1 structural model and shared number allocator are the
        // current tail and migrate from the MaintenanceDomainPersistence model.
        using var sqlite = CreateContext("Sqlite", "Data Source=:memory:");
        using var postgres = CreateContext(
            "Postgres",
            "Host=localhost;Database=segaris_test;Username=postgres;Password=postgres");

        foreach (var migrations in new[]
        {
            sqlite.Database.GetMigrations().Select(LogicalName).ToArray(),
            postgres.Database.GetMigrations().Select(LogicalName).ToArray(),
        })
        {
            Assert.Equal("ProjectsDomainPersistence", migrations[^1]);
            Assert.Equal("MaintenanceDomainPersistence", migrations[^2]);
        }
    }

    [Fact]
    public async Task Sqlite_upgrade_backfills_normalization_order_and_initialization_markers()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"segaris-upgrade-data-{Guid.NewGuid():N}.db");
        try
        {
            await using var database = CreateContext("Sqlite", $"Data Source={databasePath}");
            var baseline = database.Database.GetMigrations()
                .Single(migration => migration.EndsWith("_CapexDomainPersistence", StringComparison.Ordinal));
            var migrator = database.GetService<IMigrator>();

            // Apply the pre-Wave-1 schema and insert catalog rows the old way, with
            // codes and without normalized/order columns.
            await migrator.MigrateAsync(baseline);
            await database.Database.OpenConnectionAsync();
            await ExecuteAsync(database,
                "INSERT INTO configuration_suppliers (\"Code\", \"Name\", \"CreatedAt\", \"UpdatedAt\") " +
                "VALUES ('AMAZON', 'Amazon', '2026-01-01 00:00:00+00:00', '2026-01-01 00:00:00+00:00'), " +
                "('CARREFOUR', 'Carrefour', '2026-01-01 00:00:00+00:00', '2026-01-01 00:00:00+00:00');");
            await ExecuteAsync(database,
                "INSERT INTO configuration_cost_centers (\"Code\", \"Name\", \"CreatedAt\", \"UpdatedAt\") " +
                "VALUES ('HOUSEHOLD', 'Household', '2026-01-01 00:00:00+00:00', '2026-01-01 00:00:00+00:00');");
            await ExecuteAsync(database,
                "INSERT INTO configuration_currencies (\"Code\", \"Name\", \"CreatedAt\", \"UpdatedAt\") " +
                "VALUES ('EUR', 'Euro', '2026-01-01 00:00:00+00:00', '2026-01-01 00:00:00+00:00');");
            await ExecuteAsync(database,
                "INSERT INTO capex_categories (\"Code\", \"Name\", \"CreatedAt\", \"UpdatedAt\") " +
                "VALUES ('FURNITURE', 'Furniture', '2026-01-01 00:00:00+00:00', '2026-01-01 00:00:00+00:00');");

            // Upgrade to the catalog model.
            await migrator.MigrateAsync();

            await using var command = database.Database.GetDbConnection().CreateCommand();

            // Code columns are gone; normalized names and zero-based order are backfilled.
            command.CommandText =
                "SELECT \"NormalizedName\", \"SortOrder\" FROM configuration_suppliers ORDER BY \"Id\"";
            await using (var reader = await command.ExecuteReaderAsync())
            {
                Assert.True(await reader.ReadAsync());
                Assert.Equal("AMAZON", reader.GetString(0));
                Assert.Equal(0, reader.GetInt32(1));
                Assert.True(await reader.ReadAsync());
                Assert.Equal("CARREFOUR", reader.GetString(0));
                Assert.Equal(1, reader.GetInt32(1));
                Assert.False(await reader.ReadAsync());
            }

            command.CommandText = "SELECT \"NormalizedCode\" FROM configuration_currencies";
            Assert.Equal("EUR", (string)(await command.ExecuteScalarAsync())!);

            // Every populated catalog is marked initialized so startup never reseeds it.
            command.CommandText = "SELECT COUNT(*) FROM configuration_catalog_initializations";
            Assert.Equal(4L, (long)(await command.ExecuteScalarAsync())!);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }

    private static async Task ExecuteAsync(
        Segaris.Persistence.SegarisDbContext database,
        string sql)
    {
        await using var command = database.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task Sqlite_upgrades_from_the_current_schema()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"segaris-upgrade-{Guid.NewGuid():N}.db");
        try
        {
            await using var database = CreateContext("Sqlite", $"Data Source={databasePath}");
            var previousMigration = database.Database.GetMigrations()
                .Single(migration => migration.EndsWith("_ConfigurationFoundation", StringComparison.Ordinal));
            var migrator = database.GetService<IMigrator>();

            await migrator.MigrateAsync(previousMigration);
            await migrator.MigrateAsync();

            var applied = await database.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, migration => migration.EndsWith("_ConfigurationFoundation"));
            Assert.Contains(applied, migration => migration.EndsWith("_CapexDomainPersistence"));
            Assert.Contains(applied, migration => migration.EndsWith("_CatalogModelAndInitialization"));
            Assert.Contains(applied, migration => migration.EndsWith("_OpexDomainPersistence"));
            Assert.Contains(applied, migration => migration.EndsWith("_InventoryDomainPersistence"));
            Assert.Contains(applied, migration => migration.EndsWith("_TravelDomainPersistence"));
            Assert.Contains(applied, migration => migration.EndsWith("_MoodDomainPersistence"));
            Assert.Contains(applied, migration => migration.EndsWith("_ClothesDomainPersistence"));
            Assert.Contains(applied, migration => migration.EndsWith("_AssetsDomainPersistence"));
            Assert.Contains(applied, migration => migration.EndsWith("_MaintenanceDomainPersistence"));
            Assert.Contains(applied, migration => migration.EndsWith("_ProjectsDomainPersistence"));
            await database.Database.OpenConnectionAsync();
            await using var command = database.Database.GetDbConnection().CreateCommand();
            // Three catalog tables plus the one-time initialization table.
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'configuration_%'";
            Assert.Equal(4L, (long)(await command.ExecuteScalarAsync())!);
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'capex_%'";
            Assert.Equal(3L, (long)(await command.ExecuteScalarAsync())!);
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'opex_%'";
            Assert.Equal(3L, (long)(await command.ExecuteScalarAsync())!);
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'inventory_%'";
            Assert.Equal(6L, (long)(await command.ExecuteScalarAsync())!);
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'travel_%'";
            Assert.Equal(5L, (long)(await command.ExecuteScalarAsync())!);
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'mood_%'";
            Assert.Equal(1L, (long)(await command.ExecuteScalarAsync())!);
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND (name LIKE 'clothes_%' OR name LIKE 'clothing_%')";
            Assert.Equal(4L, (long)(await command.ExecuteScalarAsync())!);
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND (name = 'assets' OR name LIKE 'asset_%')";
            Assert.Equal(3L, (long)(await command.ExecuteScalarAsync())!);
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'maintenance_%'";
            Assert.Equal(2L, (long)(await command.ExecuteScalarAsync())!);
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'projects_%'";
            Assert.Equal(5L, (long)(await command.ExecuteScalarAsync())!);
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
