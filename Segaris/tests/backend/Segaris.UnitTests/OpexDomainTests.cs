using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Configuration.Seeding;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Persistence;
using Segaris.Api.Modules.Opex;
using Segaris.Api.Modules.Opex.Domain;
using Segaris.Api.Modules.Opex.Mutations;
using Segaris.Api.Modules.Opex.Persistence;
using Segaris.Api.Modules.Opex.Seeding;
using Segaris.Api.Platform.Api;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.UnitTests;

public sealed class OpexDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Contract_trims_name_normalizes_and_stamps_audit()
    {
        var contract = OpexContract.Create(
            Values() with { Name = " Netflix " }, new UserId(1), Now);

        Assert.Equal("Netflix", contract.Name);
        Assert.Equal("NETFLIX", contract.NormalizedName);
        Assert.Equal(1, contract.CreatedBy);
        Assert.Equal(1, contract.UpdatedBy);
        Assert.Equal(Now, contract.CreatedAt);
        Assert.Equal(Now, contract.UpdatedAt);
    }

    [Fact]
    public void Contract_accepts_a_null_estimate_and_optional_references()
    {
        var contract = OpexContract.Create(
            Values() with { EstimatedAnnualAmount = null, SupplierId = null, CostCenterId = null },
            new UserId(1),
            Now);

        Assert.Null(contract.EstimatedAnnualAmount);
        Assert.Null(contract.SupplierId);
        Assert.Null(contract.CostCenterId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Contract_rejects_a_blank_name(string name)
    {
        Assert.Throws<OpexValidationException>(() =>
            OpexContract.Create(Values() with { Name = name }, new UserId(1), Now));
    }

    [Fact]
    public void Contract_rejects_an_overlong_name()
    {
        var name = new string('a', OpexValidation.ContractNameMaximumLength + 1);
        Assert.Throws<OpexValidationException>(() =>
            OpexContract.Create(Values() with { Name = name }, new UserId(1), Now));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1.001)]
    public void Contract_rejects_a_negative_or_overprecise_estimate(decimal estimate)
    {
        Assert.Throws<OpexValidationException>(() =>
            OpexContract.Create(Values() with { EstimatedAnnualAmount = estimate }, new UserId(1), Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Contract_rejects_nonpositive_required_catalog_identifiers(int value)
    {
        Assert.Throws<OpexValidationException>(() =>
            OpexContract.Create(Values() with { CategoryId = value }, new UserId(1), Now));
        Assert.Throws<OpexValidationException>(() =>
            OpexContract.Create(Values() with { CurrencyId = value }, new UserId(1), Now));
    }

    [Fact]
    public void Only_the_creator_can_change_visibility()
    {
        var contract = OpexContract.Create(Values(), new UserId(1), Now);
        var changed = Values() with { Visibility = RecordVisibility.Private };

        var error = Assert.Throws<OpexValidationException>(() =>
            contract.Update(changed, new UserId(2), Now.AddMinutes(1)));
        Assert.Equal(OpexValidationReason.VisibilityForbidden, error.Reason);

        contract.Update(changed, new UserId(1), Now.AddMinutes(1));
        Assert.Equal(RecordVisibility.Private, contract.Visibility);
    }

    [Fact]
    public void Non_creator_can_edit_other_fields_of_a_public_contract()
    {
        var contract = OpexContract.Create(Values(), new UserId(1), Now);

        contract.Update(Values() with { Name = "Renamed", Status = OpexContractStatus.Active }, new UserId(2), Now.AddMinutes(1));

        Assert.Equal("Renamed", contract.Name);
        Assert.Equal(OpexContractStatus.Active, contract.Status);
        Assert.Equal(2, contract.UpdatedBy);
    }

    [Fact]
    public void Occurrence_trims_description_preserves_notes_and_validates_amount()
    {
        var occurrence = OpexOccurrence.Create(
            5,
            new OpexOccurrenceValues(new DateOnly(2026, 3, 1), 12.34m, " Invoice ", " Notes "),
            new UserId(1),
            Now);

        Assert.Equal(5, occurrence.ContractId);
        Assert.Equal("Invoice", occurrence.Description);
        Assert.Equal(" Notes ", occurrence.Notes);
        Assert.Equal(12.34m, occurrence.ActualAmount);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1.001)]
    public void Occurrence_rejects_a_negative_or_overprecise_amount(decimal amount)
    {
        Assert.Throws<OpexValidationException>(() => OpexOccurrence.Create(
            5,
            new OpexOccurrenceValues(new DateOnly(2026, 3, 1), amount, null, null),
            new UserId(1),
            Now));
    }

    [Fact]
    public void Occurrence_accepts_a_zero_amount_and_any_date()
    {
        var occurrence = OpexOccurrence.Create(
            5,
            new OpexOccurrenceValues(new DateOnly(2030, 12, 31), 0m, null, null),
            new UserId(1),
            Now);

        Assert.Equal(0m, occurrence.ActualAmount);
        Assert.Null(occurrence.Description);
        Assert.Equal(new DateOnly(2030, 12, 31), occurrence.EffectiveDate);
    }

    [Fact]
    public async Task Seeder_initializes_categories_once_in_declaration_order_with_stable_ids()
    {
        await using var fixture = await OpexFixture.CreateAsync();
        var seeder = new OpexSeeder(fixture.Database, new CatalogInitializer(fixture.Database, fixture.Clock));
        await seeder.SeedAsync(CancellationToken.None);

        var first = await fixture.Database.Set<OpexCategory>().AsNoTracking()
            .OrderBy(category => category.SortOrder).ToListAsync();
        Assert.Equal(OpexCategoryCatalog.Categories.Select(seed => seed.Name), first.Select(category => category.Name));
        Assert.Equal(Enumerable.Range(0, first.Count), first.Select(category => category.SortOrder));
        Assert.Equal("HOUSING", first[0].NormalizedName);

        fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddDays(1);
        await seeder.SeedAsync(CancellationToken.None);
        var second = await fixture.Database.Set<OpexCategory>().AsNoTracking()
            .ToDictionaryAsync(category => category.Name, category => category.Id);

        Assert.Equal(first.ToDictionary(category => category.Name, category => category.Id), second);
    }

    [Fact]
    public async Task Sqlite_persists_a_contract_and_cascades_occurrence_deletion()
    {
        await using var fixture = await OpexFixture.CreateAsync();
        var (categoryId, currencyId) = await fixture.SeedReferencesAsync();

        var contract = OpexContract.Create(
            Values() with { CategoryId = categoryId, CurrencyId = currencyId, SupplierId = null, CostCenterId = null },
            new UserId(1),
            Now);
        fixture.Database.Add(contract);
        await fixture.Database.SaveChangesAsync();

        var occurrence = OpexOccurrence.Create(
            contract.Id,
            new OpexOccurrenceValues(new DateOnly(2026, 1, 15), 9.99m, null, null),
            new UserId(1),
            Now);
        fixture.Database.Add(occurrence);
        await fixture.Database.SaveChangesAsync();
        fixture.Database.ChangeTracker.Clear();

        Assert.Equal(1, await fixture.Database.Set<OpexOccurrence>().CountAsync());

        var stored = await fixture.Database.Set<OpexContract>().SingleAsync();
        fixture.Database.Remove(stored);
        await fixture.Database.SaveChangesAsync();

        Assert.Equal(0, await fixture.Database.Set<OpexOccurrence>().CountAsync());
    }

    [Fact]
    public async Task Contract_names_are_globally_unique_after_normalization()
    {
        await using var fixture = await OpexFixture.CreateAsync();
        var (categoryId, currencyId) = await fixture.SeedReferencesAsync();

        fixture.Database.Add(OpexContract.Create(
            Values() with { Name = "Netflix", CategoryId = categoryId, CurrencyId = currencyId, SupplierId = null, CostCenterId = null },
            new UserId(1), Now));
        await fixture.Database.SaveChangesAsync();

        fixture.Database.Add(OpexContract.Create(
            Values() with { Name = " netflix ", CategoryId = categoryId, CurrencyId = currencyId, SupplierId = null, CostCenterId = null },
            new UserId(1), Now));

        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Database.SaveChangesAsync());
    }

    [Fact]
    public async Task Category_management_creates_renames_and_rejects_duplicate_names()
    {
        await using var fixture = await OpexFixture.CreateAsync();
        await fixture.SeedReferencesAsync();
        var service = new OpexCategoryManagementService(fixture.Database, fixture.Clock);

        var created = await service.CreateAsync(new CatalogItemRequest(" Pets "), new UserId(1), CancellationToken.None);
        Assert.Equal("Pets", created.Name);
        Assert.Equal(OpexCategoryCatalog.Categories.Count, created.SortOrder);

        await Assert.ThrowsAsync<ApiProblemException>(() =>
            service.CreateAsync(new CatalogItemRequest("housing"), new UserId(1), CancellationToken.None));

        var renamed = await service.UpdateAsync(created.Id, new CatalogItemRequest("Animals"), new UserId(1), CancellationToken.None);
        Assert.Equal("Animals", renamed.Name);
    }

    [Fact]
    public async Task Category_management_protects_the_final_row_and_migrates_references_atomically()
    {
        await using var fixture = await OpexFixture.CreateAsync();
        var (categoryId, currencyId) = await fixture.SeedReferencesAsync();
        var service = new OpexCategoryManagementService(fixture.Database, fixture.Clock);

        var replacementId = await fixture.Database.Set<OpexCategory>()
            .Where(category => category.Id != categoryId)
            .Select(category => category.Id).FirstAsync();
        var contract = OpexContract.Create(
            Values() with { CategoryId = categoryId, CurrencyId = currencyId, SupplierId = null, CostCenterId = null },
            new UserId(1), Now);
        fixture.Database.Add(contract);
        await fixture.Database.SaveChangesAsync();

        // A referenced category cannot be deleted directly.
        await Assert.ThrowsAsync<ApiProblemException>(() =>
            service.DeleteAsync(categoryId, CancellationToken.None));

        // Replace-and-delete reassigns the contract and removes the source.
        fixture.Clock.UtcNow = Now.AddHours(1);
        await service.ReplaceAndDeleteAsync(
            categoryId,
            new CatalogReplacementRequest(replacementId, ClearReferences: false, ExchangeRate: null),
            new UserId(2),
            CancellationToken.None);

        fixture.Database.ChangeTracker.Clear();
        var migrated = await fixture.Database.Set<OpexContract>().SingleAsync();
        Assert.Equal(replacementId, migrated.CategoryId);
        Assert.Equal(2, migrated.UpdatedBy);
        Assert.Equal(Now.AddHours(1), migrated.UpdatedAt);
        Assert.False(await fixture.Database.Set<OpexCategory>().AnyAsync(category => category.Id == categoryId));
    }

    [Fact]
    public void ReplaceSupplier_updates_the_reference_and_stamps_modification()
    {
        var contract = OpexContract.Create(Values() with { SupplierId = 10 }, new UserId(1), Now);

        contract.ReplaceSupplier(20, new UserId(2), Now.AddHours(1));

        Assert.Equal(20, contract.SupplierId);
        Assert.Equal(2, contract.UpdatedBy);
        Assert.Equal(Now.AddHours(1), contract.UpdatedAt);
    }

    [Fact]
    public void ReplaceSupplier_allows_clearing_to_null_and_stamps_modification()
    {
        var contract = OpexContract.Create(Values() with { SupplierId = 10 }, new UserId(1), Now);

        contract.ReplaceSupplier(null, new UserId(2), Now.AddHours(1));

        Assert.Null(contract.SupplierId);
        Assert.Equal(2, contract.UpdatedBy);
    }

    [Fact]
    public void ReplaceCostCenter_updates_the_reference_and_stamps_modification()
    {
        var contract = OpexContract.Create(Values() with { CostCenterId = 10 }, new UserId(1), Now);

        contract.ReplaceCostCenter(20, new UserId(2), Now.AddHours(1));

        Assert.Equal(20, contract.CostCenterId);
        Assert.Equal(2, contract.UpdatedBy);
        Assert.Equal(Now.AddHours(1), contract.UpdatedAt);
    }

    [Fact]
    public void ReplaceCostCenter_allows_clearing_to_null_and_stamps_modification()
    {
        var contract = OpexContract.Create(Values() with { CostCenterId = 10 }, new UserId(1), Now);

        contract.ReplaceCostCenter(null, new UserId(2), Now.AddHours(1));

        Assert.Null(contract.CostCenterId);
        Assert.Equal(2, contract.UpdatedBy);
    }

    [Fact]
    public void ConvertCurrency_converts_the_estimate_switches_currency_and_stamps_modification()
    {
        var contract = OpexContract.Create(
            Values() with { EstimatedAnnualAmount = 100.00m, CurrencyId = 1 }, new UserId(1), Now);

        contract.ConvertCurrency(2, 1.20m, new UserId(2), Now.AddHours(1));

        Assert.Equal(2, contract.CurrencyId);
        Assert.Equal(120.00m, contract.EstimatedAnnualAmount);
        Assert.Equal(2, contract.UpdatedBy);
        Assert.Equal(Now.AddHours(1), contract.UpdatedAt);
    }

    [Fact]
    public void ConvertCurrency_leaves_a_null_estimate_unchanged()
    {
        var contract = OpexContract.Create(
            Values() with { EstimatedAnnualAmount = null, CurrencyId = 1 }, new UserId(1), Now);

        contract.ConvertCurrency(2, 1.20m, new UserId(2), Now.AddHours(1));

        Assert.Null(contract.EstimatedAnnualAmount);
        Assert.Equal(2, contract.CurrencyId);
    }

    [Theory]
    [InlineData(10.55, 1.20, 12.66)]
    [InlineData(0.00, 1.50, 0.00)]
    [InlineData(9.99, 1.005, 10.04)]
    public void ConvertCurrency_rounds_the_estimate_to_two_decimal_places_away_from_zero(
        decimal estimate, decimal rate, decimal expected)
    {
        var contract = OpexContract.Create(
            Values() with { EstimatedAnnualAmount = estimate, CurrencyId = 1 }, new UserId(1), Now);

        contract.ConvertCurrency(2, rate, new UserId(2), Now.AddHours(1));

        Assert.Equal(expected, contract.EstimatedAnnualAmount);
    }

    [Theory]
    [InlineData(10.00, 1.20, 12.00)]
    [InlineData(0.00, 2.00, 0.00)]
    [InlineData(5.55, 1.20, 6.66)]
    public void ConvertAmount_on_occurrence_rounds_two_decimal_places_away_from_zero(
        decimal amount, decimal rate, decimal expected)
    {
        var occurrence = OpexOccurrence.Create(
            5,
            new OpexOccurrenceValues(new DateOnly(2026, 1, 1), amount, null, null),
            new UserId(1),
            Now);

        occurrence.ConvertAmount(rate, new UserId(2), Now.AddHours(1));

        Assert.Equal(expected, occurrence.ActualAmount);
        Assert.Equal(2, occurrence.UpdatedBy);
        Assert.Equal(Now.AddHours(1), occurrence.UpdatedAt);
    }

    private static OpexContractValues Values() => new(
        "Example",
        OpexMovementType.Expense,
        OpexContractStatus.Planning,
        StartDate: null,
        ClosedDate: null,
        EstimatedAnnualAmount: 120.00m,
        OpexExpectedFrequency.Monthly,
        CategoryId: 1,
        SupplierId: 1,
        CostCenterId: 1,
        CurrencyId: 1,
        Notes: " Notes ",
        RecordVisibility.Public);

    private sealed class OpexFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private OpexFixture(SqliteConnection connection, SegarisDbContext database, MutableClock clock)
        {
            this.connection = connection;
            Database = database;
            Clock = clock;
        }

        public SegarisDbContext Database { get; }
        public MutableClock Clock { get; }

        public static async Task<OpexFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<SegarisDbContext>()
                .UseSqlite(connection)
                .EnableServiceProviderCaching(false)
                .Options;
            var database = new SegarisDbContext(options,
                [new IdentityModelContributor(), new ConfigurationModelContributor(), new OpexModelContributor()]);
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
            return new OpexFixture(connection, database, clock);
        }

        public async Task<(int CategoryId, int CurrencyId)> SeedReferencesAsync()
        {
            await new ConfigurationSeeder(Database, new CatalogInitializer(Database, Clock))
                .SeedAsync(CancellationToken.None);
            await new OpexSeeder(Database, new CatalogInitializer(Database, Clock))
                .SeedAsync(CancellationToken.None);
            var categoryId = await Database.Set<OpexCategory>()
                .OrderBy(category => category.SortOrder).Select(category => category.Id).FirstAsync();
            var currencyId = await Database.Set<SegarisCurrency>()
                .Where(currency => currency.Code == ConfigurationCatalog.CurrencyCodes.Default)
                .Select(currency => currency.Id).SingleAsync();
            return (categoryId, currencyId);
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
