using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Configuration.Seeding;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Persistence;
using Segaris.Api.Modules.Inventory;
using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Api.Modules.Inventory.Mutations;
using Segaris.Api.Modules.Inventory.Persistence;
using Segaris.Api.Modules.Inventory.Seeding;
using Segaris.Api.Platform.Api;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.UnitTests;

public sealed class InventoryDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 16, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Item_trims_name_stamps_audit_and_associates_suppliers()
    {
        var item = InventoryItem.Create(
            Values() with { Name = " Olive oil ", SupplierIds = [3, 7] }, new UserId(1), Now);

        Assert.Equal("Olive oil", item.Name);
        Assert.Equal(InventoryItemStatus.Candidate, item.Status);
        Assert.Equal(1, item.CreatedBy);
        Assert.Equal(1, item.UpdatedBy);
        Assert.Equal(Now, item.CreatedAt);
        Assert.Equal(new[] { 3, 7 }, item.Suppliers.Select(association => association.SupplierId).OrderBy(id => id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Item_rejects_a_blank_name(string name)
    {
        Assert.Throws<InventoryValidationException>(() =>
            InventoryItem.Create(Values() with { Name = name }, new UserId(1), Now));
    }

    [Fact]
    public void Item_rejects_an_overlong_name()
    {
        var name = new string('a', InventoryValidation.ItemNameMaximumLength + 1);
        Assert.Throws<InventoryValidationException>(() =>
            InventoryItem.Create(Values() with { Name = name }, new UserId(1), Now));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1.001)]
    public void Item_rejects_negative_or_overprecise_stock(decimal stock)
    {
        Assert.Throws<InventoryValidationException>(() =>
            InventoryItem.Create(Values() with { CurrentStock = stock }, new UserId(1), Now));
        Assert.Throws<InventoryValidationException>(() =>
            InventoryItem.Create(Values() with { MinimumStock = stock }, new UserId(1), Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Item_rejects_nonpositive_catalog_identifiers(int value)
    {
        Assert.Throws<InventoryValidationException>(() =>
            InventoryItem.Create(Values() with { CategoryId = value }, new UserId(1), Now));
        Assert.Throws<InventoryValidationException>(() =>
            InventoryItem.Create(Values() with { LocationId = value }, new UserId(1), Now));
    }

    [Fact]
    public void Item_requires_at_least_one_supplier()
    {
        var error = Assert.Throws<InventoryValidationException>(() =>
            InventoryItem.Create(Values() with { SupplierIds = [] }, new UserId(1), Now));
        Assert.Equal(InventoryValidationReason.SupplierRequired, error.Reason);
    }

    [Fact]
    public void Item_update_reconciles_the_supplier_association_set()
    {
        var item = InventoryItem.Create(Values() with { SupplierIds = [3, 7] }, new UserId(1), Now);

        item.Update(Values() with { SupplierIds = [7, 9] }, new UserId(2), Now.AddMinutes(1));

        Assert.Equal(new[] { 7, 9 }, item.Suppliers.Select(association => association.SupplierId).OrderBy(id => id));
        Assert.Equal(2, item.UpdatedBy);
    }

    [Fact]
    public void Item_increases_and_decreases_stock_and_stamps_modification()
    {
        var item = InventoryItem.Create(Values() with { CurrentStock = 5.00m }, new UserId(1), Now);

        item.AdjustStock(InventoryStockAdjustmentDirection.Increase, 2.50m, new UserId(2), Now.AddHours(1));
        Assert.Equal(7.50m, item.CurrentStock);

        item.AdjustStock(InventoryStockAdjustmentDirection.Decrease, 3.00m, new UserId(2), Now.AddHours(2));
        Assert.Equal(4.50m, item.CurrentStock);
        Assert.Equal(2, item.UpdatedBy);
        Assert.Equal(Now.AddHours(2), item.UpdatedAt);
    }

    [Fact]
    public void Item_rejects_a_stock_reduction_below_zero()
    {
        var item = InventoryItem.Create(Values() with { CurrentStock = 1.00m }, new UserId(1), Now);

        var error = Assert.Throws<InventoryValidationException>(() =>
            item.AdjustStock(InventoryStockAdjustmentDirection.Decrease, 2.00m, new UserId(1), Now.AddHours(1)));
        Assert.Equal(InventoryValidationReason.NegativeStock, error.Reason);
        Assert.Equal(1.00m, item.CurrentStock);
    }

    [Fact]
    public void Order_creates_lines_and_stamps_audit()
    {
        var order = InventoryOrder.Create(OrderValues(), new UserId(1), Now);

        Assert.Equal(2, order.Lines.Count);
        Assert.Equal(InventoryOrderStatus.Planning, order.Status);
        Assert.Equal(1, order.CreatedBy);
    }

    [Fact]
    public void Order_rejects_an_empty_line_set()
    {
        Assert.Throws<InventoryValidationException>(() =>
            InventoryOrder.Create(OrderValues() with { Lines = [] }, new UserId(1), Now));
    }

    [Fact]
    public void Order_rejects_more_than_the_maximum_lines()
    {
        var lines = Enumerable.Range(0, InventoryValidation.MaximumOrderLines + 1)
            .Select(_ => new InventoryOrderLineValues(1, 1m, 1m))
            .ToArray();

        Assert.Throws<InventoryValidationException>(() =>
            InventoryOrder.Create(OrderValues() with { Lines = lines }, new UserId(1), Now));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1.001, 1)]
    public void Order_line_rejects_a_nonpositive_or_overprecise_quantity(decimal quantity, decimal lineTotal)
    {
        Assert.Throws<InventoryValidationException>(() =>
            InventoryOrder.Create(
                OrderValues() with { Lines = [new InventoryOrderLineValues(1, quantity, lineTotal)] },
                new UserId(1),
                Now));
    }

    [Fact]
    public void Order_line_accepts_a_zero_total_and_rejects_a_negative_total()
    {
        var order = InventoryOrder.Create(
            OrderValues() with { Lines = [new InventoryOrderLineValues(1, 1m, 0m)] }, new UserId(1), Now);
        Assert.Equal(0m, order.Lines[0].LineTotal);

        Assert.Throws<InventoryValidationException>(() =>
            InventoryOrder.Create(
                OrderValues() with { Lines = [new InventoryOrderLineValues(1, 1m, -1m)] },
                new UserId(1),
                Now));
    }

    [Fact]
    public void Order_update_replaces_the_whole_line_set()
    {
        var order = InventoryOrder.Create(OrderValues(), new UserId(1), Now);

        order.Update(
            OrderValues() with { Lines = [new InventoryOrderLineValues(5, 4m, 40m)] },
            new UserId(2),
            Now.AddMinutes(1));

        Assert.Single(order.Lines);
        Assert.Equal(5, order.Lines[0].ItemId);
        Assert.Equal(2, order.UpdatedBy);
    }

    [Fact]
    public void Order_mark_received_requires_active_status_and_stamps_modification()
    {
        var order = InventoryOrder.Create(OrderValues() with { Status = InventoryOrderStatus.Active }, new UserId(1), Now);

        order.MarkReceived(new UserId(2), Now.AddMinutes(1));

        Assert.Equal(InventoryOrderStatus.Received, order.Status);
        Assert.Equal(2, order.UpdatedBy);
        Assert.Equal(Now.AddMinutes(1), order.UpdatedAt);

        Assert.Throws<InventoryValidationException>(() =>
            order.MarkReceived(new UserId(2), Now.AddMinutes(2)));
    }

    [Fact]
    public void ReplaceCategory_and_ReplaceLocation_update_references_and_stamp_modification()
    {
        var item = InventoryItem.Create(Values(), new UserId(1), Now);

        item.ReplaceCategory(20, new UserId(2), Now.AddHours(1));
        item.ReplaceLocation(30, new UserId(2), Now.AddHours(1));

        Assert.Equal(20, item.CategoryId);
        Assert.Equal(30, item.LocationId);
        Assert.Equal(2, item.UpdatedBy);
        Assert.Equal(Now.AddHours(1), item.UpdatedAt);
    }

    [Fact]
    public async Task Seeder_initializes_categories_and_locations_once_in_declaration_order()
    {
        await using var fixture = await InventoryFixture.CreateAsync();
        var seeder = new InventorySeeder(fixture.Database, new CatalogInitializer(fixture.Database, fixture.Clock));
        await seeder.SeedAsync(CancellationToken.None);

        var categories = await fixture.Database.Set<InventoryCategory>().AsNoTracking()
            .OrderBy(category => category.SortOrder).ToListAsync();
        var locations = await fixture.Database.Set<InventoryLocation>().AsNoTracking()
            .OrderBy(location => location.SortOrder).ToListAsync();
        Assert.Equal(InventoryCatalog.Categories.Select(seed => seed.Name), categories.Select(category => category.Name));
        Assert.Equal(InventoryCatalog.Locations.Select(seed => seed.Name), locations.Select(location => location.Name));
        Assert.Equal(Enumerable.Range(0, categories.Count), categories.Select(category => category.SortOrder));
        Assert.Equal("FOOD", categories[0].NormalizedName);

        fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddDays(1);
        await seeder.SeedAsync(CancellationToken.None);
        var reseeded = await fixture.Database.Set<InventoryCategory>().AsNoTracking()
            .ToDictionaryAsync(category => category.Name, category => category.Id);
        Assert.Equal(categories.ToDictionary(category => category.Name, category => category.Id), reseeded);
    }

    [Fact]
    public async Task Sqlite_persists_an_item_and_cascades_supplier_association_deletion()
    {
        await using var fixture = await InventoryFixture.CreateAsync();
        var references = await fixture.SeedReferencesAsync();

        var item = InventoryItem.Create(
            Values() with
            {
                CategoryId = references.CategoryId,
                LocationId = references.LocationId,
                SupplierIds = references.SupplierIds,
            },
            new UserId(1),
            Now);
        fixture.Database.Add(item);
        await fixture.Database.SaveChangesAsync();
        fixture.Database.ChangeTracker.Clear();

        Assert.Equal(references.SupplierIds.Count, await fixture.Database.Set<InventoryItemSupplier>().CountAsync());

        var stored = await fixture.Database.Set<InventoryItem>().SingleAsync();
        fixture.Database.Remove(stored);
        await fixture.Database.SaveChangesAsync();

        Assert.Equal(0, await fixture.Database.Set<InventoryItemSupplier>().CountAsync());
    }

    [Fact]
    public async Task Sqlite_persists_an_order_and_cascades_line_deletion()
    {
        await using var fixture = await InventoryFixture.CreateAsync();
        var references = await fixture.SeedReferencesAsync();
        var itemId = await fixture.SeedItemAsync(references);

        var order = InventoryOrder.Create(
            OrderValues() with
            {
                SupplierId = references.SupplierIds[0],
                CurrencyId = references.CurrencyId,
                Lines = [new InventoryOrderLineValues(itemId, 2m, 20m)],
            },
            new UserId(1),
            Now);
        fixture.Database.Add(order);
        await fixture.Database.SaveChangesAsync();
        fixture.Database.ChangeTracker.Clear();

        Assert.Equal(1, await fixture.Database.Set<InventoryOrderLine>().CountAsync());

        var stored = await fixture.Database.Set<InventoryOrder>().SingleAsync();
        fixture.Database.Remove(stored);
        await fixture.Database.SaveChangesAsync();

        Assert.Equal(0, await fixture.Database.Set<InventoryOrderLine>().CountAsync());
    }

    [Fact]
    public async Task Category_management_creates_renames_and_rejects_duplicate_names()
    {
        await using var fixture = await InventoryFixture.CreateAsync();
        await fixture.SeedReferencesAsync();
        var service = new InventoryCategoryManagementService(fixture.Database, fixture.Clock);

        var created = await service.CreateAsync(new CatalogItemRequest(" Snacks "), new UserId(1), CancellationToken.None);
        Assert.Equal("Snacks", created.Name);
        Assert.Equal(InventoryCatalog.Categories.Count, created.SortOrder);

        await Assert.ThrowsAsync<ApiProblemException>(() =>
            service.CreateAsync(new CatalogItemRequest("food"), new UserId(1), CancellationToken.None));

        var renamed = await service.UpdateAsync(created.Id, new CatalogItemRequest("Treats"), new UserId(1), CancellationToken.None);
        Assert.Equal("Treats", renamed.Name);
    }

    [Fact]
    public async Task Category_management_protects_the_final_row_and_migrates_references_atomically()
    {
        await using var fixture = await InventoryFixture.CreateAsync();
        var references = await fixture.SeedReferencesAsync();
        var service = new InventoryCategoryManagementService(fixture.Database, fixture.Clock);

        var replacementId = await fixture.Database.Set<InventoryCategory>()
            .Where(category => category.Id != references.CategoryId)
            .Select(category => category.Id).FirstAsync();
        await fixture.SeedItemAsync(references);

        await Assert.ThrowsAsync<ApiProblemException>(() =>
            service.DeleteAsync(references.CategoryId, CancellationToken.None));

        fixture.Clock.UtcNow = Now.AddHours(1);
        await service.ReplaceAndDeleteAsync(
            references.CategoryId,
            new CatalogReplacementRequest(replacementId, ClearReferences: false, ExchangeRate: null),
            new UserId(2),
            CancellationToken.None);

        fixture.Database.ChangeTracker.Clear();
        var migrated = await fixture.Database.Set<InventoryItem>().SingleAsync();
        Assert.Equal(replacementId, migrated.CategoryId);
        Assert.Equal(2, migrated.UpdatedBy);
        Assert.False(await fixture.Database.Set<InventoryCategory>().AnyAsync(category => category.Id == references.CategoryId));
    }

    [Fact]
    public async Task Location_management_protects_the_final_row_and_migrates_references_atomically()
    {
        await using var fixture = await InventoryFixture.CreateAsync();
        var references = await fixture.SeedReferencesAsync();
        var service = new InventoryLocationManagementService(fixture.Database, fixture.Clock);

        var replacementId = await fixture.Database.Set<InventoryLocation>()
            .Where(location => location.Id != references.LocationId)
            .Select(location => location.Id).FirstAsync();
        await fixture.SeedItemAsync(references);

        await Assert.ThrowsAsync<ApiProblemException>(() =>
            service.DeleteAsync(references.LocationId, CancellationToken.None));

        fixture.Clock.UtcNow = Now.AddHours(1);
        await service.ReplaceAndDeleteAsync(
            references.LocationId,
            new CatalogReplacementRequest(replacementId, ClearReferences: false, ExchangeRate: null),
            new UserId(2),
            CancellationToken.None);

        fixture.Database.ChangeTracker.Clear();
        var migrated = await fixture.Database.Set<InventoryItem>().SingleAsync();
        Assert.Equal(replacementId, migrated.LocationId);
        Assert.False(await fixture.Database.Set<InventoryLocation>().AnyAsync(location => location.Id == references.LocationId));
    }

    private static InventoryItemValues Values() => new(
        "Example",
        InventoryItemStatus.Candidate,
        Notes: null,
        CategoryId: 1,
        LocationId: 1,
        CurrentStock: 0.00m,
        MinimumStock: 0.00m,
        SupplierIds: [1],
        RecordVisibility.Public);

    private static InventoryOrderValues OrderValues() => new(
        SupplierId: 1,
        InventoryOrderStatus.Planning,
        CurrencyId: 1,
        OrderDate: new DateOnly(2026, 6, 16),
        ExpectedReceiptDate: new DateOnly(2026, 6, 23),
        Notes: null,
        RecordVisibility.Public,
        Lines:
        [
            new InventoryOrderLineValues(1, 2m, 20m),
            new InventoryOrderLineValues(2, 1m, 5m),
        ]);

    private sealed record References(int CategoryId, int LocationId, IReadOnlyList<int> SupplierIds, int CurrencyId);

    private sealed class InventoryFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private InventoryFixture(SqliteConnection connection, SegarisDbContext database, MutableClock clock)
        {
            this.connection = connection;
            Database = database;
            Clock = clock;
        }

        public SegarisDbContext Database { get; }
        public MutableClock Clock { get; }

        public static async Task<InventoryFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<SegarisDbContext>()
                .UseSqlite(connection)
                .EnableServiceProviderCaching(false)
                .Options;
            var database = new SegarisDbContext(options,
                [new IdentityModelContributor(), new ConfigurationModelContributor(), new InventoryModelContributor()]);
            await database.Database.EnsureCreatedAsync();
            database.Set<SegarisUser>().Add(new SegarisUser
            {
                Id = 1,
                UserName = "owner",
                NormalizedUserName = "OWNER",
                DisplayName = "Owner",
                Language = "en-GB",
                CreatedAt = Now,
            });
            database.Set<SegarisUser>().Add(new SegarisUser
            {
                Id = 2,
                UserName = "admin",
                NormalizedUserName = "ADMIN",
                DisplayName = "Admin",
                Language = "en-GB",
                CreatedAt = Now,
            });
            await database.SaveChangesAsync();
            var clock = new MutableClock { UtcNow = Now };
            return new InventoryFixture(connection, database, clock);
        }

        public async Task<References> SeedReferencesAsync()
        {
            await new ConfigurationSeeder(Database, new CatalogInitializer(Database, Clock))
                .SeedAsync(CancellationToken.None);
            await new InventorySeeder(Database, new CatalogInitializer(Database, Clock))
                .SeedAsync(CancellationToken.None);
            var categoryId = await Database.Set<InventoryCategory>()
                .OrderBy(category => category.SortOrder).Select(category => category.Id).FirstAsync();
            var locationId = await Database.Set<InventoryLocation>()
                .OrderBy(location => location.SortOrder).Select(location => location.Id).FirstAsync();
            var supplierIds = await Database.Set<SegarisSupplier>()
                .OrderBy(supplier => supplier.SortOrder).Select(supplier => supplier.Id).Take(2).ToListAsync();
            var currencyId = await Database.Set<SegarisCurrency>()
                .Where(currency => currency.Code == ConfigurationCatalog.CurrencyCodes.Default)
                .Select(currency => currency.Id).SingleAsync();
            return new References(categoryId, locationId, supplierIds, currencyId);
        }

        public async Task<int> SeedItemAsync(References references)
        {
            var item = InventoryItem.Create(
                Values() with
                {
                    CategoryId = references.CategoryId,
                    LocationId = references.LocationId,
                    SupplierIds = references.SupplierIds,
                },
                new UserId(1),
                Now);
            Database.Add(item);
            await Database.SaveChangesAsync();
            Database.ChangeTracker.Clear();
            return item.Id;
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
