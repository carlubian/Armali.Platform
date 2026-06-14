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
    [Fact]
    public async Task Seeder_is_idempotent_and_preserves_stable_identifiers()
    {
        await using var fixture = await CatalogFixture.CreateAsync();
        var seeder = new ConfigurationSeeder(fixture.Database, fixture.Clock);

        await seeder.SeedAsync(CancellationToken.None);
        var original = await fixture.Database.Set<SegarisSupplier>()
            .AsNoTracking()
            .ToDictionaryAsync(entity => entity.Code, entity => entity.Id);

        fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddDays(1);
        await seeder.SeedAsync(CancellationToken.None);
        var repeated = await fixture.Database.Set<SegarisSupplier>()
            .AsNoTracking()
            .ToDictionaryAsync(entity => entity.Code, entity => entity.Id);

        Assert.Equal(original, repeated);
        Assert.Equal(ConfigurationCatalog.Suppliers.Count, repeated.Count);
        Assert.Equal(ConfigurationCatalog.CostCenters.Count, await fixture.Database.Set<SegarisCostCenter>().CountAsync());
        Assert.Equal(ConfigurationCatalog.Currencies.Count, await fixture.Database.Set<SegarisCurrency>().CountAsync());
    }

    [Fact]
    public async Task Seeder_restores_the_canonical_name_for_an_existing_code()
    {
        await using var fixture = await CatalogFixture.CreateAsync();
        var seeder = new ConfigurationSeeder(fixture.Database, fixture.Clock);
        await seeder.SeedAsync(CancellationToken.None);
        var supplier = await fixture.Database.Set<SegarisSupplier>()
            .SingleAsync(entity => entity.Code == ConfigurationCatalog.SupplierCodes.Amazon);
        var id = supplier.Id;
        supplier.Name = "Changed";
        await fixture.Database.SaveChangesAsync();

        fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddHours(1);
        await seeder.SeedAsync(CancellationToken.None);

        Assert.Equal(id, supplier.Id);
        Assert.Equal("Amazon", supplier.Name);
        Assert.Equal(fixture.Clock.UtcNow, supplier.UpdatedAt);
    }

    [Fact]
    public async Task Catalog_reader_lists_bounded_models_and_validates_identifiers()
    {
        await using var fixture = await CatalogFixture.CreateAsync();
        await new ConfigurationSeeder(fixture.Database, fixture.Clock)
            .SeedAsync(CancellationToken.None);
        var catalog = new ConfigurationCatalogService(fixture.Database);

        var suppliers = await catalog.ListSuppliersAsync(CancellationToken.None);
        var costCenters = await catalog.ListCostCentersAsync(CancellationToken.None);
        var currencies = await catalog.ListCurrenciesAsync(CancellationToken.None);

        Assert.Equal(ConfigurationCatalog.Suppliers.Count, suppliers.Count);
        Assert.Equal(ConfigurationCatalog.CostCenters.Count, costCenters.Count);
        Assert.Equal(ConfigurationCatalog.Currencies.Count, currencies.Count);
        Assert.True(await catalog.SupplierExistsAsync(suppliers[0].Id, CancellationToken.None));
        Assert.True(await catalog.CostCenterExistsAsync(costCenters[0].Id, CancellationToken.None));
        Assert.True(await catalog.CurrencyExistsAsync(currencies[0].Id, CancellationToken.None));
        Assert.False(await catalog.SupplierExistsAsync(int.MaxValue, CancellationToken.None));
        Assert.False(await catalog.CostCenterExistsAsync(int.MaxValue, CancellationToken.None));
        Assert.False(await catalog.CurrencyExistsAsync(int.MaxValue, CancellationToken.None));
    }

    private sealed class CatalogFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private CatalogFixture(
            SqliteConnection connection,
            SegarisDbContext database,
            MutableClock clock)
        {
            this.connection = connection;
            Database = database;
            Clock = clock;
        }

        public SegarisDbContext Database { get; }

        public MutableClock Clock { get; }

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
