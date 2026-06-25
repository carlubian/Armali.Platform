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
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Persistence;
using Segaris.Api.Modules.Opex;
using Segaris.Api.Modules.Opex.Contracts;
using Segaris.Api.Modules.Opex.Domain;
using Segaris.Api.Modules.Opex.Persistence;
using Segaris.Api.Modules.Opex.Queries;
using Segaris.Api.Modules.Opex.Seeding;
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
                [new IdentityModelContributor(), new ConfigurationModelContributor(), new CapexModelContributor(), new OpexModelContributor()]);
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

    private sealed class MutableClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}
