using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Configuration.Seeding;
using Segaris.Persistence;
using Segaris.Shared.Time;

namespace Segaris.UnitTests;

public sealed class ConfigurationCatalogTests
{
    [Theory]
    [InlineData("  euro  ", "EURO")]
    [InlineData("El Corte Inglés", "EL CORTE INGLÉS")]
    [InlineData("Food  &  Dining", "FOOD  &  DINING")]
    public void Normalization_trims_and_folds_invariant_without_collapsing_inner_whitespace(
        string value,
        string expected)
    {
        Assert.Equal(expected, CatalogNormalization.Normalize(value));
    }

    [Fact]
    public async Task Seeding_inserts_initial_values_once_with_deterministic_order_and_normalization()
    {
        await using var fixture = await CatalogFixture.CreateAsync();
        await fixture.SeedAsync();

        var suppliers = await fixture.Database.Set<SegarisSupplier>()
            .AsNoTracking().OrderBy(entity => entity.SortOrder).ToArrayAsync();

        Assert.Equal(ConfigurationCatalog.Suppliers.Count, suppliers.Length);
        Assert.Equal(
            ConfigurationCatalog.Suppliers.Select(seed => seed.Name).ToArray(),
            suppliers.Select(entity => entity.Name).ToArray());
        Assert.Equal(
            Enumerable.Range(0, suppliers.Length).ToArray(),
            suppliers.Select(entity => entity.SortOrder).ToArray());
        Assert.All(suppliers, entity =>
            Assert.Equal(CatalogNormalization.Normalize(entity.Name), entity.NormalizedName));

        var currencies = await fixture.Database.Set<SegarisCurrency>()
            .AsNoTracking().OrderBy(entity => entity.SortOrder).ToArrayAsync();
        Assert.All(currencies, entity =>
            Assert.Equal(CatalogNormalization.Normalize(entity.Code), entity.NormalizedCode));
        Assert.Equal("EUR", currencies[0].Code);
    }

    [Fact]
    public async Task Seeding_is_idempotent_and_preserves_identifiers()
    {
        await using var fixture = await CatalogFixture.CreateAsync();
        await fixture.SeedAsync();
        var original = await fixture.Database.Set<SegarisSupplier>()
            .AsNoTracking().ToDictionaryAsync(entity => entity.Name, entity => entity.Id);

        fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddDays(1);
        await fixture.SeedAsync();
        var repeated = await fixture.Database.Set<SegarisSupplier>()
            .AsNoTracking().ToDictionaryAsync(entity => entity.Name, entity => entity.Id);

        Assert.Equal(original, repeated);
        Assert.Equal(ConfigurationCatalog.Suppliers.Count, repeated.Count);
        // The Configuration seeder marks its three catalogs; the Capex-owned
        // category catalog is marked by the Capex seeder, which this fixture omits.
        var markers = await fixture.Database.Set<SegarisCatalogInitialization>()
            .Select(marker => marker.CatalogKey).ToArrayAsync();
        Assert.Equal(
            new[]
            {
                ConfigurationInitializationKeys.Suppliers,
                ConfigurationInitializationKeys.CostCenters,
                ConfigurationInitializationKeys.Currencies,
            }.OrderBy(key => key),
            markers.OrderBy(key => key));
    }

    [Fact]
    public async Task A_catalog_that_already_has_rows_is_marked_without_seeding()
    {
        await using var fixture = await CatalogFixture.CreateAsync();
        fixture.Database.Add(new SegarisSupplier
        {
            Name = "Custom",
            NormalizedName = CatalogNormalization.Normalize("Custom"),
            SortOrder = 0,
            CreatedAt = fixture.Clock.UtcNow,
            UpdatedAt = fixture.Clock.UtcNow,
        });
        await fixture.Database.SaveChangesAsync();

        await fixture.SeedAsync();

        var suppliers = await fixture.Database.Set<SegarisSupplier>().AsNoTracking().ToArrayAsync();
        var supplier = Assert.Single(suppliers);
        Assert.Equal("Custom", supplier.Name);
        Assert.True(await fixture.Database.Set<SegarisCatalogInitialization>()
            .AnyAsync(marker => marker.CatalogKey == ConfigurationInitializationKeys.Suppliers));
        // The other catalogs were empty, so they still receive their initial values.
        Assert.Equal(
            ConfigurationCatalog.CostCenters.Count,
            await fixture.Database.Set<SegarisCostCenter>().CountAsync());
    }

    [Fact]
    public async Task A_deliberately_emptied_catalog_is_never_restored()
    {
        await using var fixture = await CatalogFixture.CreateAsync();
        await fixture.SeedAsync();
        fixture.Database.Set<SegarisSupplier>()
            .RemoveRange(await fixture.Database.Set<SegarisSupplier>().ToArrayAsync());
        await fixture.Database.SaveChangesAsync();

        await fixture.SeedAsync();

        Assert.Empty(await fixture.Database.Set<SegarisSupplier>().AsNoTracking().ToArrayAsync());
    }

    [Fact]
    public async Task Reader_lists_bounded_models_in_sort_order_and_validates_identifiers()
    {
        await using var fixture = await CatalogFixture.CreateAsync();
        await fixture.SeedAsync();
        var catalog = new ConfigurationCatalogService(fixture.Database);

        var suppliers = await catalog.ListSuppliersAsync(CancellationToken.None);
        var currencies = await catalog.ListCurrenciesAsync(CancellationToken.None);

        Assert.Equal(
            ConfigurationCatalog.Suppliers.Select(seed => seed.Name).ToArray(),
            suppliers.Select(item => item.Name).ToArray());
        Assert.Equal(0, suppliers[0].SortOrder);
        Assert.Equal("EUR", currencies[0].Code);
        Assert.True(await catalog.SupplierExistsAsync(suppliers[0].Id, CancellationToken.None));
        Assert.False(await catalog.SupplierExistsAsync(int.MaxValue, CancellationToken.None));
    }

    [Fact]
    public async Task A_case_insensitive_duplicate_name_violates_the_unique_index()
    {
        await using var fixture = await CatalogFixture.CreateAsync();
        fixture.Database.Add(NewSupplier(fixture, "Café"));
        await fixture.Database.SaveChangesAsync();

        fixture.Database.Add(NewSupplier(fixture, "café"));

        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Database.SaveChangesAsync());
    }

    [Fact]
    public async Task A_case_insensitive_duplicate_currency_code_violates_the_unique_index()
    {
        await using var fixture = await CatalogFixture.CreateAsync();
        fixture.Database.Add(NewCurrency(fixture, "abc", "Alpha"));
        await fixture.Database.SaveChangesAsync();

        fixture.Database.Add(NewCurrency(fixture, "ABC", "Beta"));

        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Database.SaveChangesAsync());
    }

    private static SegarisSupplier NewSupplier(CatalogFixture fixture, string name) => new()
    {
        Name = name,
        NormalizedName = CatalogNormalization.Normalize(name),
        SortOrder = 0,
        CreatedAt = fixture.Clock.UtcNow,
        UpdatedAt = fixture.Clock.UtcNow,
    };

    private static SegarisCurrency NewCurrency(CatalogFixture fixture, string code, string name) => new()
    {
        Code = code.ToUpperInvariant(),
        NormalizedCode = CatalogNormalization.Normalize(code),
        Name = name,
        NormalizedName = CatalogNormalization.Normalize(name),
        SortOrder = 0,
        CreatedAt = fixture.Clock.UtcNow,
        UpdatedAt = fixture.Clock.UtcNow,
    };

    private sealed class CatalogFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private CatalogFixture(SqliteConnection connection, SegarisDbContext database, MutableClock clock)
        {
            this.connection = connection;
            Database = database;
            Clock = clock;
        }

        public SegarisDbContext Database { get; }

        public MutableClock Clock { get; }

        public Task SeedAsync() =>
            new ConfigurationSeeder(Database, new CatalogInitializer(Database, Clock))
                .SeedAsync(CancellationToken.None);

        public static async Task<CatalogFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<SegarisDbContext>()
                .UseSqlite(connection)
                .Options;
            var database = new SegarisDbContext(options, [new ConfigurationModelContributor()]);
            await database.Database.EnsureCreatedAsync();
            return new CatalogFixture(
                connection,
                database,
                new MutableClock { UtcNow = new DateTimeOffset(2026, 6, 14, 10, 0, 0, TimeSpan.Zero) });
        }

        public async ValueTask DisposeAsync()
        {
            await Database.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    private sealed class MutableClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}
