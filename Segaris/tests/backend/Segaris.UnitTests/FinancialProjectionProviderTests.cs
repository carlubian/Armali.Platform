using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Capex;
using Segaris.Api.Modules.Capex.Contracts;
using Segaris.Api.Modules.Capex.Domain;
using Segaris.Api.Modules.Capex.Persistence;
using Segaris.Api.Modules.Capex.Queries;
using Segaris.Api.Modules.Capex.Seeding;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Configuration.Seeding;
using Segaris.Api.Modules.Destinations.Contracts;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Persistence;
using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Api.Modules.Inventory.Persistence;
using Segaris.Api.Modules.Inventory.Queries;
using Segaris.Api.Modules.Inventory.Seeding;
using Segaris.Api.Modules.Opex;
using Segaris.Api.Modules.Opex.Contracts;
using Segaris.Api.Modules.Opex.Domain;
using Segaris.Api.Modules.Opex.Persistence;
using Segaris.Api.Modules.Opex.Queries;
using Segaris.Api.Modules.Opex.Seeding;
using Segaris.Api.Modules.Travel.Domain;
using Segaris.Api.Modules.Travel.Persistence;
using Segaris.Api.Modules.Travel.Queries;
using Segaris.Api.Modules.Travel.Seeding;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.UnitTests;

public sealed class FinancialProjectionProviderTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly UserId Owner = new(1);
    private static readonly UserId Collaborator = new(2);

    [Fact]
    public async Task Capex_provider_projects_accessible_completed_entries_with_labels_and_inclusive_dates()
    {
        await using var fixture = await ProjectionFixture.CreateAsync();
        var references = await fixture.SeedCapexAsync();
        var provider = new CapexFinancialProjectionProvider(fixture.Database);

        var lowerBound = await fixture.AddCapexEntryAsync("Lower", CapexMovementType.Expense, CapexEntryStatus.Completed, new DateOnly(2026, 1, 1), 10m, references, RecordVisibility.Public, Owner);
        var upperBound = await fixture.AddCapexEntryAsync("Upper", CapexMovementType.Income, CapexEntryStatus.Completed, new DateOnly(2026, 12, 31), 25m, references, RecordVisibility.Public, Owner);
        await fixture.AddCapexEntryAsync("Planning", CapexMovementType.Expense, CapexEntryStatus.Planning, new DateOnly(2026, 6, 1), 99m, references, RecordVisibility.Public, Owner);
        await fixture.AddCapexEntryAsync("Before", CapexMovementType.Expense, CapexEntryStatus.Completed, new DateOnly(2025, 12, 31), 99m, references, RecordVisibility.Public, Owner);
        await fixture.AddCapexEntryAsync("After", CapexMovementType.Expense, CapexEntryStatus.Completed, new DateOnly(2027, 1, 1), 99m, references, RecordVisibility.Public, Owner);

        var projections = await provider.ListFinancialProjectionsAsync(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            Collaborator,
            CancellationToken.None);

        Assert.Equal([$"capex:{lowerBound}", $"capex:{upperBound}"], projections.Select(projection => projection.SourceId));
        Assert.All(projections, projection =>
        {
            Assert.Equal("capex", projection.SourceModule);
            Assert.Equal("entry", projection.SourceType);
            Assert.Equal("EUR", projection.CurrencyCode);
            Assert.Equal("Home", projection.CategoryLabel);
            Assert.Equal("Amazon", projection.SupplierLabel);
            Assert.Equal("Household", projection.CostCenterLabel);
            Assert.Null(projection.ItemCategoryLabel);
            Assert.Null(projection.ItemLabel);
            Assert.Null(projection.DestinationLabel);
        });
        Assert.Equal("Expense", projections[0].MovementDirection);
        Assert.Equal(10m, projections[0].Amount);
        Assert.Equal("Income", projections[1].MovementDirection);
        Assert.Equal(25m, projections[1].Amount);
    }

    [Fact]
    public async Task Capex_provider_keeps_private_entries_creator_only()
    {
        await using var fixture = await ProjectionFixture.CreateAsync();
        var references = await fixture.SeedCapexAsync();
        var privateEntryId = await fixture.AddCapexEntryAsync("Private", CapexMovementType.Expense, CapexEntryStatus.Completed, new DateOnly(2026, 6, 1), 12m, references, RecordVisibility.Private, Owner);
        var provider = new CapexFinancialProjectionProvider(fixture.Database);

        var collaborator = await provider.ListFinancialProjectionsAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), Collaborator, CancellationToken.None);
        var owner = await provider.ListFinancialProjectionsAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), Owner, CancellationToken.None);

        Assert.Empty(collaborator);
        Assert.Equal($"capex:{privateEntryId}", Assert.Single(owner).SourceId);
    }

    [Fact]
    public async Task Opex_provider_projects_accessible_occurrences_with_parent_labels_and_inclusive_dates()
    {
        await using var fixture = await ProjectionFixture.CreateAsync();
        var references = await fixture.SeedOpexAsync();
        var provider = new OpexFinancialProjectionProvider(fixture.Database);

        var expense = await fixture.AddOpexContractAsync("Rent", OpexMovementType.Expense, references, RecordVisibility.Public, Owner);
        var income = await fixture.AddOpexContractAsync("Allowance", OpexMovementType.Income, references, RecordVisibility.Public, Owner);
        var lowerBound = await fixture.AddOpexOccurrenceAsync(expense, new DateOnly(2026, 1, 1), 100m);
        var upperBound = await fixture.AddOpexOccurrenceAsync(income, new DateOnly(2026, 12, 31), 250m);
        await fixture.AddOpexOccurrenceAsync(expense, new DateOnly(2025, 12, 31), 999m);
        await fixture.AddOpexOccurrenceAsync(income, new DateOnly(2027, 1, 1), 999m);

        var projections = await provider.ListFinancialProjectionsAsync(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            Collaborator,
            CancellationToken.None);

        Assert.Equal([$"opex:{expense}:{lowerBound}", $"opex:{income}:{upperBound}"], projections.Select(projection => projection.SourceId));
        Assert.All(projections, projection =>
        {
            Assert.Equal("opex", projection.SourceModule);
            Assert.Equal("occurrence", projection.SourceType);
            Assert.Equal("EUR", projection.CurrencyCode);
            Assert.Equal("Housing", projection.CategoryLabel);
            Assert.Equal("Amazon", projection.SupplierLabel);
            Assert.Equal("Household", projection.CostCenterLabel);
            Assert.Null(projection.ItemCategoryLabel);
            Assert.Null(projection.ItemLabel);
            Assert.Null(projection.DestinationLabel);
        });
        Assert.Equal("Expense", projections[0].MovementDirection);
        Assert.Equal(100m, projections[0].Amount);
        Assert.Equal("Income", projections[1].MovementDirection);
        Assert.Equal(250m, projections[1].Amount);
    }

    [Fact]
    public async Task Opex_provider_keeps_private_parent_contracts_creator_only()
    {
        await using var fixture = await ProjectionFixture.CreateAsync();
        var references = await fixture.SeedOpexAsync();
        var contractId = await fixture.AddOpexContractAsync("Private", OpexMovementType.Expense, references, RecordVisibility.Private, Owner);
        var occurrenceId = await fixture.AddOpexOccurrenceAsync(contractId, new DateOnly(2026, 6, 1), 75m);
        var provider = new OpexFinancialProjectionProvider(fixture.Database);

        var collaborator = await provider.ListFinancialProjectionsAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), Collaborator, CancellationToken.None);
        var owner = await provider.ListFinancialProjectionsAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), Owner, CancellationToken.None);

        Assert.Empty(collaborator);
        Assert.Equal($"opex:{contractId}:{occurrenceId}", Assert.Single(owner).SourceId);
    }

    [Fact]
    public async Task Inventory_provider_projects_active_and_received_order_lines_excluding_planning_cancelled_and_out_of_range()
    {
        await using var fixture = await ProjectionFixture.CreateAsync();
        var references = await fixture.SeedInventoryAsync();
        var itemA = await fixture.AddInventoryItemAsync("Detergent", references, RecordVisibility.Public, Owner);
        var itemB = await fixture.AddInventoryItemAsync("Sponge", references, RecordVisibility.Public, Owner);
        var provider = new InventoryFinancialProjectionProvider(fixture.Database);

        var active = await fixture.AddInventoryOrderAsync(InventoryOrderStatus.Active, new DateOnly(2026, 1, 1), null, references, RecordVisibility.Public, Owner, (itemA, 10m));
        var received = await fixture.AddInventoryOrderAsync(InventoryOrderStatus.Received, null, new DateOnly(2026, 12, 31), references, RecordVisibility.Public, Owner, (itemB, 25m));
        await fixture.AddInventoryOrderAsync(InventoryOrderStatus.Planning, new DateOnly(2026, 6, 1), null, references, RecordVisibility.Public, Owner, (itemA, 99m));
        await fixture.AddInventoryOrderAsync(InventoryOrderStatus.Cancelled, new DateOnly(2026, 6, 1), null, references, RecordVisibility.Public, Owner, (itemA, 99m));
        await fixture.AddInventoryOrderAsync(InventoryOrderStatus.Active, new DateOnly(2025, 12, 31), null, references, RecordVisibility.Public, Owner, (itemA, 99m));
        await fixture.AddInventoryOrderAsync(InventoryOrderStatus.Active, new DateOnly(2027, 1, 1), null, references, RecordVisibility.Public, Owner, (itemA, 99m));

        var projections = await provider.ListFinancialProjectionsAsync(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            Collaborator,
            CancellationToken.None);

        var activeLine = await fixture.LineIdAsync(active);
        var receivedLine = await fixture.LineIdAsync(received);
        Assert.Equal([$"inventory:{active}:{activeLine}", $"inventory:{received}:{receivedLine}"], projections.Select(projection => projection.SourceId));
        Assert.All(projections, projection =>
        {
            Assert.Equal("inventory", projection.SourceModule);
            Assert.Equal("orderLine", projection.SourceType);
            Assert.Equal("Expense", projection.MovementDirection);
            Assert.Equal("EUR", projection.CurrencyCode);
            Assert.Equal("Amazon", projection.SupplierLabel);
            Assert.Equal(references.CategoryName, projection.ItemCategoryLabel);
            Assert.Null(projection.CategoryLabel);
            Assert.Null(projection.CostCenterLabel);
            Assert.Null(projection.DestinationLabel);
        });
        Assert.Equal(new DateOnly(2026, 1, 1), projections[0].AccountingDate);
        Assert.Equal(10m, projections[0].Amount);
        Assert.Equal("Detergent", projections[0].ItemLabel);
        Assert.Equal(new DateOnly(2026, 12, 31), projections[1].AccountingDate);
        Assert.Equal(25m, projections[1].Amount);
        Assert.Equal("Sponge", projections[1].ItemLabel);
    }

    [Fact]
    public async Task Inventory_provider_emits_one_projection_per_order_line_sharing_the_order_identifier()
    {
        await using var fixture = await ProjectionFixture.CreateAsync();
        var references = await fixture.SeedInventoryAsync();
        var itemA = await fixture.AddInventoryItemAsync("Detergent", references, RecordVisibility.Public, Owner);
        var itemB = await fixture.AddInventoryItemAsync("Sponge", references, RecordVisibility.Public, Owner);
        var provider = new InventoryFinancialProjectionProvider(fixture.Database);

        var order = await fixture.AddInventoryOrderAsync(
            InventoryOrderStatus.Active, new DateOnly(2026, 5, 1), null, references, RecordVisibility.Public, Owner, (itemA, 10m), (itemB, 15m));

        var projections = await provider.ListFinancialProjectionsAsync(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            Owner,
            CancellationToken.None);

        Assert.Equal(2, projections.Count);
        Assert.All(projections, projection => Assert.StartsWith($"inventory:{order}:", projection.SourceId));
        Assert.Equal([10m, 15m], projections.Select(projection => projection.Amount).OrderBy(amount => amount));
        Assert.Equal(["Detergent", "Sponge"], projections.Select(projection => projection.ItemLabel).OrderBy(label => label));
    }

    [Fact]
    public async Task Inventory_provider_keeps_private_orders_creator_only()
    {
        await using var fixture = await ProjectionFixture.CreateAsync();
        var references = await fixture.SeedInventoryAsync();
        var item = await fixture.AddInventoryItemAsync("Detergent", references, RecordVisibility.Private, Owner);
        var orderId = await fixture.AddInventoryOrderAsync(InventoryOrderStatus.Active, new DateOnly(2026, 6, 1), null, references, RecordVisibility.Private, Owner, (item, 12m));
        var provider = new InventoryFinancialProjectionProvider(fixture.Database);

        var collaborator = await provider.ListFinancialProjectionsAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), Collaborator, CancellationToken.None);
        var owner = await provider.ListFinancialProjectionsAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), Owner, CancellationToken.None);

        Assert.Empty(collaborator);
        Assert.StartsWith($"inventory:{orderId}:", Assert.Single(owner).SourceId);
    }

    [Fact]
    public async Task Travel_provider_projects_non_cancelled_trip_expenses_with_labels_and_inclusive_dates()
    {
        await using var fixture = await ProjectionFixture.CreateAsync();
        var references = await fixture.SeedTravelAsync();
        var provider = new TravelFinancialProjectionProvider(
            fixture.Database,
            new FakeDestinationReferenceReader(new Dictionary<int, string> { [7] = "Rome" }));

        var plannedTrip = await fixture.AddTravelTripAsync(TravelTripStatus.Planned, 7, references, RecordVisibility.Public, Owner);
        var completedTrip = await fixture.AddTravelTripAsync(TravelTripStatus.Completed, null, references, RecordVisibility.Public, Owner);
        var cancelledTrip = await fixture.AddTravelTripAsync(TravelTripStatus.Cancelled, 7, references, RecordVisibility.Public, Owner);

        var lowerBound = await fixture.AddTravelExpenseAsync(plannedTrip, new DateOnly(2026, 1, 1), 10m, references, withOptionalLabels: true, Owner);
        var upperBound = await fixture.AddTravelExpenseAsync(completedTrip, new DateOnly(2026, 12, 31), 25m, references, withOptionalLabels: false, Owner);
        await fixture.AddTravelExpenseAsync(cancelledTrip, new DateOnly(2026, 6, 1), 99m, references, withOptionalLabels: true, Owner);
        await fixture.AddTravelExpenseAsync(plannedTrip, new DateOnly(2025, 12, 31), 99m, references, withOptionalLabels: true, Owner);
        await fixture.AddTravelExpenseAsync(plannedTrip, new DateOnly(2027, 1, 1), 99m, references, withOptionalLabels: true, Owner);

        var projections = await provider.ListFinancialProjectionsAsync(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            Collaborator,
            CancellationToken.None);

        Assert.Equal([$"travel:{lowerBound}", $"travel:{upperBound}"], projections.Select(projection => projection.SourceId));
        Assert.All(projections, projection =>
        {
            Assert.Equal("travel", projection.SourceModule);
            Assert.Equal("expense", projection.SourceType);
            Assert.Equal("Expense", projection.MovementDirection);
            Assert.Equal("EUR", projection.CurrencyCode);
            Assert.Equal(references.ExpenseCategoryName, projection.CategoryLabel);
            Assert.Null(projection.ItemCategoryLabel);
            Assert.Null(projection.ItemLabel);
        });
        Assert.Equal(10m, projections[0].Amount);
        Assert.Equal("Amazon", projections[0].SupplierLabel);
        Assert.Equal("Household", projections[0].CostCenterLabel);
        Assert.Equal("Rome", projections[0].DestinationLabel);
        Assert.Equal(25m, projections[1].Amount);
        Assert.Null(projections[1].SupplierLabel);
        Assert.Null(projections[1].CostCenterLabel);
        Assert.Null(projections[1].DestinationLabel);
    }

    [Fact]
    public async Task Travel_provider_keeps_private_trip_expenses_creator_only()
    {
        await using var fixture = await ProjectionFixture.CreateAsync();
        var references = await fixture.SeedTravelAsync();
        var trip = await fixture.AddTravelTripAsync(TravelTripStatus.Planned, null, references, RecordVisibility.Private, Owner);
        var expenseId = await fixture.AddTravelExpenseAsync(trip, new DateOnly(2026, 6, 1), 30m, references, withOptionalLabels: false, Owner);
        var provider = new TravelFinancialProjectionProvider(
            fixture.Database,
            new FakeDestinationReferenceReader(new Dictionary<int, string>()));

        var collaborator = await provider.ListFinancialProjectionsAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), Collaborator, CancellationToken.None);
        var owner = await provider.ListFinancialProjectionsAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), Owner, CancellationToken.None);

        Assert.Empty(collaborator);
        Assert.Equal($"travel:{expenseId}", Assert.Single(owner).SourceId);
    }

    private sealed class ProjectionFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private ProjectionFixture(SqliteConnection connection, SegarisDbContext database, MutableClock clock)
        {
            this.connection = connection;
            Database = database;
            Clock = clock;
        }

        public SegarisDbContext Database { get; }
        public MutableClock Clock { get; }

        public static async Task<ProjectionFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<SegarisDbContext>()
                .UseSqlite(connection)
                .EnableServiceProviderCaching(false)
                .Options;
            var database = new SegarisDbContext(options,
                [
                    new IdentityModelContributor(),
                    new ConfigurationModelContributor(),
                    new CapexModelContributor(),
                    new OpexModelContributor(),
                    new InventoryModelContributor(),
                    new TravelModelContributor(),
                ]);
            await database.Database.EnsureCreatedAsync();
            database.Set<SegarisUser>().Add(new SegarisUser
            {
                Id = Owner.Value,
                UserName = "owner",
                NormalizedUserName = "OWNER",
                DisplayName = "Owner",
                Language = "en-GB",
                CreatedAt = Now,
            });
            database.Set<SegarisUser>().Add(new SegarisUser
            {
                Id = Collaborator.Value,
                UserName = "collaborator",
                NormalizedUserName = "COLLABORATOR",
                DisplayName = "Collaborator",
                Language = "en-GB",
                CreatedAt = Now,
            });
            await database.SaveChangesAsync();
            var clock = new MutableClock { UtcNow = Now };
            await new ConfigurationSeeder(database, new CatalogInitializer(database, clock)).SeedAsync(CancellationToken.None);
            return new ProjectionFixture(connection, database, clock);
        }

        public async Task<ProjectionReferences> SeedCapexAsync()
        {
            await new CapexSeeder(Database, new CatalogInitializer(Database, Clock)).SeedAsync(CancellationToken.None);
            return await ReferencesAsync<CapexCategory>("Home");
        }

        public async Task<ProjectionReferences> SeedOpexAsync()
        {
            await new OpexSeeder(Database, new CatalogInitializer(Database, Clock)).SeedAsync(CancellationToken.None);
            return await ReferencesAsync<OpexCategory>("Housing");
        }

        public async Task<int> AddCapexEntryAsync(
            string title,
            CapexMovementType movementType,
            CapexEntryStatus status,
            DateOnly dueDate,
            decimal amount,
            ProjectionReferences references,
            RecordVisibility visibility,
            UserId creator)
        {
            var entry = CapexEntry.Create(
                new CapexEntryValues(title, movementType, status, dueDate, references.CategoryId, references.SupplierId, references.CostCenterId, references.CurrencyId, null, visibility),
                [new CapexItemValues(title, 1m, amount)],
                creator,
                Now);
            Database.Add(entry);
            await Database.SaveChangesAsync();
            return entry.Id;
        }

        public async Task<int> AddOpexContractAsync(
            string name,
            OpexMovementType movementType,
            ProjectionReferences references,
            RecordVisibility visibility,
            UserId creator)
        {
            var contract = OpexContract.Create(
                new OpexContractValues(name, movementType, OpexContractStatus.Active, null, null, null, OpexExpectedFrequency.Monthly, references.CategoryId, references.SupplierId, references.CostCenterId, references.CurrencyId, null, visibility),
                creator,
                Now);
            Database.Add(contract);
            await Database.SaveChangesAsync();
            return contract.Id;
        }

        public async Task<int> AddOpexOccurrenceAsync(int contractId, DateOnly effectiveDate, decimal amount)
        {
            var occurrence = OpexOccurrence.Create(
                contractId,
                new OpexOccurrenceValues(effectiveDate, amount, null, null),
                Owner,
                Now);
            Database.Add(occurrence);
            await Database.SaveChangesAsync();
            return occurrence.Id;
        }

        public async Task<InventoryReferences> SeedInventoryAsync()
        {
            await new InventorySeeder(Database, new CatalogInitializer(Database, Clock)).SeedAsync(CancellationToken.None);
            var category = await Database.Set<InventoryCategory>()
                .OrderBy(value => value.SortOrder)
                .Select(value => new { value.Id, value.Name })
                .FirstAsync();
            var locationId = await Database.Set<InventoryLocation>()
                .OrderBy(value => value.SortOrder).Select(value => value.Id).FirstAsync();
            var supplierId = await Database.Set<SegarisSupplier>()
                .Where(supplier => supplier.Name == "Amazon").Select(supplier => supplier.Id).SingleAsync();
            var currencyId = await Database.Set<SegarisCurrency>()
                .Where(currency => currency.Code == ConfigurationCatalog.CurrencyCodes.Default).Select(currency => currency.Id).SingleAsync();
            return new InventoryReferences(category.Id, category.Name, locationId, supplierId, currencyId);
        }

        public async Task<int> AddInventoryItemAsync(string name, InventoryReferences references, RecordVisibility visibility, UserId creator)
        {
            var item = InventoryItem.Create(
                new InventoryItemValues(name, InventoryItemStatus.Active, null, references.CategoryId, references.LocationId, 0m, 0m, [references.SupplierId], visibility),
                creator,
                Now);
            Database.Add(item);
            await Database.SaveChangesAsync();
            return item.Id;
        }

        public async Task<int> AddInventoryOrderAsync(
            InventoryOrderStatus status,
            DateOnly? expectedReceiptDate,
            DateOnly? orderDate,
            InventoryReferences references,
            RecordVisibility visibility,
            UserId creator,
            params (int ItemId, decimal LineTotal)[] lines)
        {
            var order = InventoryOrder.Create(
                new InventoryOrderValues(
                    references.SupplierId,
                    status,
                    references.CurrencyId,
                    orderDate,
                    expectedReceiptDate,
                    null,
                    visibility,
                    lines.Select(line => new InventoryOrderLineValues(line.ItemId, 1m, line.LineTotal)).ToArray()),
                creator,
                Now);
            Database.Add(order);
            await Database.SaveChangesAsync();
            return order.Id;
        }

        public Task<int> LineIdAsync(int orderId) =>
            Database.Set<InventoryOrderLine>()
                .Where(line => line.OrderId == orderId)
                .OrderBy(line => line.Id)
                .Select(line => line.Id)
                .FirstAsync();

        public async Task<TravelReferences> SeedTravelAsync()
        {
            await new TravelSeeder(Database, new CatalogInitializer(Database, Clock)).SeedAsync(CancellationToken.None);
            var tripTypeId = await Database.Set<TravelTripType>()
                .OrderBy(value => value.SortOrder).Select(value => value.Id).FirstAsync();
            var category = await Database.Set<TravelExpenseCategory>()
                .OrderBy(value => value.SortOrder)
                .Select(value => new { value.Id, value.Name })
                .FirstAsync();
            var supplierId = await Database.Set<SegarisSupplier>()
                .Where(supplier => supplier.Name == "Amazon").Select(supplier => supplier.Id).SingleAsync();
            var costCenterId = await Database.Set<SegarisCostCenter>()
                .Where(costCenter => costCenter.Name == "Household").Select(costCenter => costCenter.Id).SingleAsync();
            var currencyId = await Database.Set<SegarisCurrency>()
                .Where(currency => currency.Code == ConfigurationCatalog.CurrencyCodes.Default).Select(currency => currency.Id).SingleAsync();
            return new TravelReferences(tripTypeId, category.Id, category.Name, supplierId, costCenterId, currencyId);
        }

        public async Task<int> AddTravelTripAsync(
            TravelTripStatus status,
            int? destinationId,
            TravelReferences references,
            RecordVisibility visibility,
            UserId creator)
        {
            var trip = TravelTrip.Create(
                new TravelTripValues("Trip", references.TripTypeId, destinationId, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 5), status, null, visibility, []),
                creator,
                Now);
            Database.Add(trip);
            await Database.SaveChangesAsync();
            return trip.Id;
        }

        public async Task<int> AddTravelExpenseAsync(
            int tripId,
            DateOnly date,
            decimal amount,
            TravelReferences references,
            bool withOptionalLabels,
            UserId creator)
        {
            var expense = TravelExpense.Create(
                tripId,
                new TravelExpenseValues(
                    references.ExpenseCategoryId,
                    "Expense",
                    date,
                    amount,
                    references.CurrencyId,
                    withOptionalLabels ? references.SupplierId : null,
                    withOptionalLabels ? references.CostCenterId : null,
                    null),
                creator,
                Now);
            Database.Add(expense);
            await Database.SaveChangesAsync();
            return expense.Id;
        }

        public async ValueTask DisposeAsync()
        {
            await Database.DisposeAsync();
            await connection.DisposeAsync();
        }

        private async Task<ProjectionReferences> ReferencesAsync<TCategory>(string categoryName)
            where TCategory : class
        {
            var categoryId = await Database.Set<TCategory>()
                .Where(category => EF.Property<string>(category, nameof(CapexCategory.Name)) == categoryName)
                .Select(category => EF.Property<int>(category, nameof(CapexCategory.Id)))
                .SingleAsync();
            var supplierId = await Database.Set<SegarisSupplier>()
                .Where(supplier => supplier.Name == "Amazon")
                .Select(supplier => supplier.Id)
                .SingleAsync();
            var costCenterId = await Database.Set<SegarisCostCenter>()
                .Where(costCenter => costCenter.Name == "Household")
                .Select(costCenter => costCenter.Id)
                .SingleAsync();
            var currencyId = await Database.Set<SegarisCurrency>()
                .Where(currency => currency.Code == ConfigurationCatalog.CurrencyCodes.Default)
                .Select(currency => currency.Id)
                .SingleAsync();
            return new ProjectionReferences(categoryId, supplierId, costCenterId, currencyId);
        }
    }

    private sealed record ProjectionReferences(int CategoryId, int SupplierId, int CostCenterId, int CurrencyId);

    private sealed record InventoryReferences(int CategoryId, string CategoryName, int LocationId, int SupplierId, int CurrencyId);

    private sealed record TravelReferences(int TripTypeId, int ExpenseCategoryId, string ExpenseCategoryName, int SupplierId, int CostCenterId, int CurrencyId);

    private sealed class FakeDestinationReferenceReader(IReadOnlyDictionary<int, string> names) : IDestinationReferenceReader
    {
        public Task<DestinationReference?> FindAccessibleAsync(int destinationId, UserId viewer, CancellationToken cancellationToken) =>
            Task.FromResult(names.TryGetValue(destinationId, out var name)
                ? new DestinationReference(destinationId, name, null, RecordVisibility.Public)
                : null);

        public Task<IReadOnlyDictionary<int, DestinationReference>> ResolveAccessibleAsync(
            IReadOnlyCollection<int> destinationIds,
            UserId viewer,
            CancellationToken cancellationToken)
        {
            var resolved = destinationIds
                .Where(names.ContainsKey)
                .Distinct()
                .ToDictionary(id => id, id => new DestinationReference(id, names[id], null, RecordVisibility.Public));
            return Task.FromResult<IReadOnlyDictionary<int, DestinationReference>>(resolved);
        }
    }

    private sealed class MutableClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}
