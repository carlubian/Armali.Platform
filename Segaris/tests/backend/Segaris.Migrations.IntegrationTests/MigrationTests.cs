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
                Assert.Contains(
                    appliedMigrations,
                    migration => migration.EndsWith("_ProjectsRisks"));
                Assert.Contains(
                    appliedMigrations,
                    migration => migration.EndsWith("_ProcessesDomainPersistence"));
                Assert.Contains(
                    appliedMigrations,
                    migration => migration.EndsWith("_FirebirdDomainPersistence"));
                Assert.Contains(
                    appliedMigrations,
                    migration => migration.EndsWith("_RecipesDomainPersistence"));
                Assert.Contains(
                    appliedMigrations,
                    migration => migration.EndsWith("_DestinationsDomainPersistence"));
                Assert.Contains(
                    appliedMigrations,
                    migration => migration.EndsWith("_TravelDestinationReference"));
                Assert.Contains(
                    appliedMigrations,
                    migration => migration.EndsWith("_HealthDomainPersistence"));
                Assert.Contains(
                    appliedMigrations,
                    migration => migration.EndsWith("_CalendarDailyNotes"));
                Assert.Contains(
                    appliedMigrations,
                    migration => migration.EndsWith("_GamesDomainPersistence"));
                Assert.Contains(
                    appliedMigrations,
                    migration => migration.EndsWith("_WellnessDomainPersistence"));
                Assert.Contains(
                    appliedMigrations,
                    migration => migration.EndsWith("_MoodScoreZeroScale"));
                Assert.Contains(
                    appliedMigrations,
                    migration => migration.EndsWith("_MoodIntentCriterion"));
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
                // Programs, axes, projects, activities, risks, and the shared number allocator.
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'projects_%'";
                Assert.Equal(6L, (long)(await command.ExecuteScalarAsync())!);
                // Processes plus the steps and category catalog tables.
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'processes_%'";
                Assert.Equal(3L, (long)(await command.ExecuteScalarAsync())!);
                // People, usernames, interactions, and the category and platform catalogs.
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'firebird_%'";
                Assert.Equal(5L, (long)(await command.ExecuteScalarAsync())!);
                // Recipes, ingredients, steps, menus, menu slots, and the category catalog.
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'recipe_%'";
                Assert.Equal(6L, (long)(await command.ExecuteScalarAsync())!);
                // Destinations, places, and the destination and place category catalogs.
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND (name = 'destinations' OR name LIKE 'destination_%' OR name = 'place_categories')";
                Assert.Equal(4L, (long)(await command.ExecuteScalarAsync())!);
                // Diseases, medicines, the disease-medicine join, and the disease and
                // medicine category catalogs.
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'health_%'";
                Assert.Equal(5L, (long)(await command.ExecuteScalarAsync())!);
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'calendar_%'";
                Assert.Equal(1L, (long)(await command.ExecuteScalarAsync())!);
                // Games, playthroughs, playthrough tags, sections, and goals.
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'games_%'";
                Assert.Equal(5L, (long)(await command.ExecuteScalarAsync())!);
                // Catalogue tasks, user days, and selected day-task snapshots.
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'wellness_%'";
                Assert.Equal(3L, (long)(await command.ExecuteScalarAsync())!);
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
        Assert.Contains("ProjectsRisks", sqliteNames);
        Assert.Contains("ProcessesDomainPersistence", sqliteNames);
        Assert.Contains("FirebirdDomainPersistence", sqliteNames);
        Assert.Contains("RecipesDomainPersistence", sqliteNames);
        Assert.Contains("DestinationsDomainPersistence", sqliteNames);
        Assert.Contains("TravelDestinationReference", sqliteNames);
        Assert.Contains("HealthDomainPersistence", sqliteNames);
        Assert.Contains("MedicinePrimaryAttachment", sqliteNames);
        Assert.Contains("CalendarDailyNotes", sqliteNames);
        Assert.Contains("CurrencyExchangeRateToEur", sqliteNames);
        Assert.Contains("GamesDomainPersistence", sqliteNames);
        Assert.Contains("WellnessDomainPersistence", sqliteNames);
        Assert.Contains("MoodScoreZeroScale", sqliteNames);
        Assert.Contains("MoodIntentCriterion", sqliteNames);
    }

    [Fact]
    public void Mood_intent_criterion_is_the_current_tail()
    {
        // The Mood Intent criterion replaces Direction and Source after the 0-5
        // score scale re-mapping, which still reads the Direction column.
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
            Assert.Equal("MoodIntentCriterion", migrations[^1]);
            Assert.Equal("MoodScoreZeroScale", migrations[^2]);
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

    [Fact]
    public async Task Sqlite_upgrade_backfills_seeded_currency_exchange_rates()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"segaris-rates-{Guid.NewGuid():N}.db");
        try
        {
            await using var database = CreateContext("Sqlite", $"Data Source={databasePath}");
            var baseline = database.Database.GetMigrations()
                .Single(migration => migration.EndsWith("_CalendarDailyNotes", StringComparison.Ordinal));
            var migrator = database.GetService<IMigrator>();

            // Apply the pre-Wave-1 schema (currencies without an exchange rate) and
            // insert two seeded currencies and one administrator-created currency.
            await migrator.MigrateAsync(baseline);
            await database.Database.OpenConnectionAsync();
            await ExecuteAsync(database,
                "INSERT INTO configuration_currencies (\"Code\", \"NormalizedCode\", \"Name\", \"NormalizedName\", \"SortOrder\", \"CreatedAt\", \"UpdatedAt\") " +
                "VALUES ('EUR', 'EUR', 'Euro', 'EURO', 0, '2026-01-01 00:00:00+00:00', '2026-01-01 00:00:00+00:00'), " +
                "('USD', 'USD', 'US Dollar', 'US DOLLAR', 1, '2026-01-01 00:00:00+00:00', '2026-01-01 00:00:00+00:00'), " +
                "('JPY', 'JPY', 'Japanese Yen', 'JAPANESE YEN', 2, '2026-01-01 00:00:00+00:00', '2026-01-01 00:00:00+00:00');");

            // Upgrade to the exchange-rate model.
            await migrator.MigrateAsync();

            await using var command = database.Database.GetDbConnection().CreateCommand();
            command.CommandText =
                "SELECT \"NormalizedCode\", \"ExchangeRateToEur\" FROM configuration_currencies ORDER BY \"SortOrder\"";
            await using var reader = await command.ExecuteReaderAsync();

            Assert.True(await reader.ReadAsync());
            Assert.Equal("EUR", reader.GetString(0));
            Assert.Equal(1m, reader.GetDecimal(1));
            Assert.True(await reader.ReadAsync());
            Assert.Equal("USD", reader.GetString(0));
            Assert.Equal(0.92m, reader.GetDecimal(1));
            // The administrator-created currency keeps a null rate until one is set.
            Assert.True(await reader.ReadAsync());
            Assert.Equal("JPY", reader.GetString(0));
            Assert.True(await reader.IsDBNullAsync(1));
            Assert.False(await reader.ReadAsync());
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task Sqlite_upgrade_remaps_mood_scores_onto_the_zero_based_scale()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"segaris-mood-scores-{Guid.NewGuid():N}.db");
        try
        {
            await using var database = CreateContext("Sqlite", $"Data Source={databasePath}");
            var baseline = database.Database.GetMigrations()
                .Single(migration => migration.EndsWith("_WellnessDomainPersistence", StringComparison.Ordinal));
            var migrator = database.GetService<IMigrator>();

            // Apply the 1-5 schema and insert one entry per re-mapping rule. Foreign
            // keys are relaxed so the fixture does not need an identity user.
            await migrator.MigrateAsync(baseline);
            await database.Database.OpenConnectionAsync();
            await ExecuteAsync(database, "PRAGMA foreign_keys = OFF;");
            (int Score, string Alignment, string Direction, int Expected)[] cases =
            [
                (1, "Negative", "Defensive", 1),
                (2, "Positive", "Harmony", 1),
                (2, "Negative", "Offensive", 1),
                (3, "Negative", "Harmony", 2),
                (3, "Negative", "Stability", 2),
                (3, "Medium", "Offensive", 2),
                (3, "Medium", "Defensive", 2),
                (3, "Medium", "Harmony", 3),
                (3, "Medium", "Stability", 3),
                (3, "Positive", "Offensive", 3),
                (3, "Positive", "Defensive", 3),
                (4, "Negative", "Offensive", 4),
                (5, "Medium", "Defensive", 5),
            ];
            foreach (var (score, alignment, direction, _) in cases)
            {
                await ExecuteAsync(database,
                    "INSERT INTO mood_entries (\"EntryDate\", \"Score\", \"Energy\", \"Alignment\", \"Direction\", \"Source\", \"CreatedAt\", \"CreatedBy\") " +
                    $"VALUES ('2026-06-18', {score}, 'Medium', '{alignment}', '{direction}', 'Internal', '2026-06-18 00:00:00+00:00', 1);");
            }

            // Stop at the score migration: later migrations drop the Direction
            // column and, with foreign keys back on, reject the userless fixture.
            await migrator.MigrateAsync(database.Database.GetMigrations()
                .Single(migration => migration.EndsWith("_MoodScoreZeroScale", StringComparison.Ordinal)));

            await using var command = database.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT \"Score\" FROM mood_entries ORDER BY \"Id\"";
            var migrated = new List<int>();
            await using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    migrated.Add(reader.GetInt32(0));
                }
            }

            Assert.Equal(cases.Select(row => row.Expected).ToArray(), migrated);

            // The widened constraint now accepts the new zero score.
            await ExecuteAsync(database, "UPDATE mood_entries SET \"Score\" = 0 WHERE \"Id\" = 1;");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task Sqlite_upgrade_converts_mood_direction_and_source_into_intent()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"segaris-mood-intent-{Guid.NewGuid():N}.db");
        try
        {
            await using var database = CreateContext("Sqlite", $"Data Source={databasePath}");
            var baseline = database.Database.GetMigrations()
                .Single(migration => migration.EndsWith("_MoodScoreZeroScale", StringComparison.Ordinal));
            var migrator = database.GetService<IMigrator>();

            // Apply the four-criteria schema and insert one entry for every previous
            // combination. Foreign keys are relaxed so the fixture does not need an
            // identity user.
            await migrator.MigrateAsync(baseline);
            await database.Database.OpenConnectionAsync();
            await ExecuteAsync(database, "PRAGMA foreign_keys = OFF;");
            string[] energies = ["High", "Medium", "Low"];
            string[] alignments = ["Positive", "Medium", "Negative"];
            string[] directions = ["Harmony", "Offensive", "Defensive", "Stability"];
            string[] sources = ["Internal", "External"];
            foreach (var energy in energies)
            {
                foreach (var alignment in alignments)
                {
                    foreach (var direction in directions)
                    {
                        foreach (var source in sources)
                        {
                            await ExecuteAsync(database,
                                "INSERT INTO mood_entries (\"EntryDate\", \"Score\", \"Energy\", \"Alignment\", \"Direction\", \"Source\", \"Notes\", \"CreatedAt\", \"CreatedBy\") " +
                                $"VALUES ('2026-06-18', 3, '{energy}', '{alignment}', '{direction}', '{source}', '{energy}/{alignment}/{direction}/{source}', '2026-06-18 00:00:00+00:00', 1);");
                        }
                    }
                }
            }

            await migrator.MigrateAsync();

            // Every entry survives with its Energy and Alignment and a known Intent.
            var migrated = new Dictionary<string, (string Energy, string Alignment, string Intent)>();
            await using var command = database.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT \"Notes\", \"Energy\", \"Alignment\", \"Intent\" FROM mood_entries";
            await using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    migrated.Add(reader.GetString(0), (reader.GetString(1), reader.GetString(2), reader.GetString(3)));
                }
            }

            Assert.Equal(72, migrated.Count);
            Assert.All(migrated, row =>
            {
                var parts = row.Key.Split('/');
                Assert.Equal(parts[0], row.Value.Energy);
                Assert.Equal(parts[1], row.Value.Alignment);
                Assert.Contains(row.Value.Intent, new[] { "Stay", "Defend", "Attack", "Rebuild", "Explore" });
            });

            (string Energy, string Alignment, string Direction, string Source, string Expected)[] sample =
            [
                ("High", "Positive", "Harmony", "Internal", "Stay"),
                ("High", "Positive", "Offensive", "Internal", "Explore"),
                ("High", "Positive", "Offensive", "External", "Attack"),
                ("High", "Negative", "Harmony", "Internal", "Rebuild"),
                ("High", "Negative", "Harmony", "External", "Attack"),
                ("Medium", "Medium", "Harmony", "External", "Stay"),
                ("Medium", "Medium", "Defensive", "Internal", "Defend"),
                ("Medium", "Negative", "Defensive", "Internal", "Rebuild"),
                ("Low", "Positive", "Defensive", "External", "Stay"),
                ("Low", "Medium", "Offensive", "Internal", "Attack"),
                ("Low", "Negative", "Stability", "External", "Rebuild"),
            ];
            foreach (var (energy, alignment, direction, source, expected) in sample)
            {
                Assert.Equal(expected, migrated[$"{energy}/{alignment}/{direction}/{source}"].Intent);
            }

            // The old criteria columns are gone and Intent is constrained.
            command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('mood_entries') WHERE name IN ('Direction', 'Source')";
            Assert.Equal(0L, (long)(await command.ExecuteScalarAsync())!);
            await Assert.ThrowsAsync<SqliteException>(() =>
                ExecuteAsync(database, "UPDATE mood_entries SET \"Intent\" = 'Harmony' WHERE \"Id\" = 1;"));

            // Rolling back restores the previous columns with constraint-valid values.
            // The upgrade's table rebuild re-enabled foreign keys, so relax them again.
            await ExecuteAsync(database, "PRAGMA foreign_keys = OFF;");
            await migrator.MigrateAsync(baseline);
            command.CommandText =
                "SELECT COUNT(*) FROM mood_entries WHERE \"Direction\" IN ('Harmony', 'Defensive', 'Offensive', 'Stability') AND \"Source\" = 'Internal'";
            Assert.Equal(72L, (long)(await command.ExecuteScalarAsync())!);
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
            Assert.Contains(applied, migration => migration.EndsWith("_ProjectsRisks"));
            Assert.Contains(applied, migration => migration.EndsWith("_ProcessesDomainPersistence"));
            Assert.Contains(applied, migration => migration.EndsWith("_FirebirdDomainPersistence"));
            Assert.Contains(applied, migration => migration.EndsWith("_RecipesDomainPersistence"));
            Assert.Contains(applied, migration => migration.EndsWith("_DestinationsDomainPersistence"));
            Assert.Contains(applied, migration => migration.EndsWith("_TravelDestinationReference"));
            Assert.Contains(applied, migration => migration.EndsWith("_HealthDomainPersistence"));
            Assert.Contains(applied, migration => migration.EndsWith("_GamesDomainPersistence"));
            Assert.Contains(applied, migration => migration.EndsWith("_WellnessDomainPersistence"));
            Assert.Contains(applied, migration => migration.EndsWith("_MoodScoreZeroScale"));
            Assert.Contains(applied, migration => migration.EndsWith("_MoodIntentCriterion"));
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
            Assert.Equal(6L, (long)(await command.ExecuteScalarAsync())!);
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'processes_%'";
            Assert.Equal(3L, (long)(await command.ExecuteScalarAsync())!);
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'firebird_%'";
            Assert.Equal(5L, (long)(await command.ExecuteScalarAsync())!);
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'games_%'";
            Assert.Equal(5L, (long)(await command.ExecuteScalarAsync())!);
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'wellness_%'";
            Assert.Equal(3L, (long)(await command.ExecuteScalarAsync())!);
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
