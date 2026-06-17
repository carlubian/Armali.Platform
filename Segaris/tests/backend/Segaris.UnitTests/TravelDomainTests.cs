using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Configuration.Seeding;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Persistence;
using Segaris.Api.Modules.Travel;
using Segaris.Api.Modules.Travel.Contracts;
using Segaris.Api.Modules.Travel.Domain;
using Segaris.Api.Modules.Travel.Mutations;
using Segaris.Api.Modules.Travel.Persistence;
using Segaris.Api.Modules.Travel.Seeding;
using Segaris.Api.Platform.Api;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.UnitTests;

public sealed class TravelDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 17, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Trip_trims_name_stamps_created_audit_and_leaves_updated_unset()
    {
        var trip = TravelTrip.Create(TripValues() with { Name = "  Lisbon getaway  " }, new UserId(1), Now);

        Assert.Equal("Lisbon getaway", trip.Name);
        Assert.Equal(TravelTripStatus.Planned, trip.Status);
        Assert.Equal(RecordVisibility.Public, trip.Visibility);
        Assert.Equal(1, trip.CreatedBy);
        Assert.Equal(Now, trip.CreatedAt);
        // The frozen detail contract exposes nullable update metadata: a brand-new
        // trip has never been modified.
        Assert.Null(trip.UpdatedAt);
        Assert.Null(trip.UpdatedBy);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Trip_rejects_a_blank_name(string name)
    {
        Assert.Throws<TravelValidationException>(() =>
            TravelTrip.Create(TripValues() with { Name = name }, new UserId(1), Now));
    }

    [Fact]
    public void Trip_rejects_an_overlong_name()
    {
        var name = new string('a', TravelValidation.NameMaxLength + 1);
        Assert.Throws<TravelValidationException>(() =>
            TravelTrip.Create(TripValues() with { Name = name }, new UserId(1), Now));
    }

    [Fact]
    public void Trip_accepts_a_one_day_trip_and_rejects_an_end_before_the_start()
    {
        var sameDay = TravelTrip.Create(
            TripValues() with { StartDate = new DateOnly(2026, 7, 1), EndDate = new DateOnly(2026, 7, 1) },
            new UserId(1),
            Now);
        Assert.Equal(sameDay.StartDate, sameDay.EndDate);

        var error = Assert.Throws<TravelValidationException>(() =>
            TravelTrip.Create(
                TripValues() with { StartDate = new DateOnly(2026, 7, 2), EndDate = new DateOnly(2026, 7, 1) },
                new UserId(1),
                Now));
        Assert.Equal(TravelValidationReason.DateRange, error.Reason);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Trip_rejects_a_nonpositive_trip_type(int tripTypeId)
    {
        Assert.Throws<TravelValidationException>(() =>
            TravelTrip.Create(TripValues() with { TripTypeId = tripTypeId }, new UserId(1), Now));
    }

    [Fact]
    public void Trip_orders_itinerary_entries_by_stable_insertion_sequence()
    {
        var trip = TravelTrip.Create(
            TripValues() with
            {
                Itinerary =
                [
                    Entry("Departure", new DateOnly(2026, 7, 2), new TimeOnly(8, 0)),
                    Entry("Check-in", new DateOnly(2026, 7, 1), null),
                    Entry("Dinner", new DateOnly(2026, 7, 1), new TimeOnly(21, 0)),
                ],
            },
            new UserId(1),
            Now);

        Assert.Equal(["Departure", "Check-in", "Dinner"], trip.Itinerary.Select(entry => entry.Title));
        Assert.Equal([0, 1, 2], trip.Itinerary.Select(entry => entry.SortOrder));
    }

    [Fact]
    public void Trip_rejects_more_than_the_maximum_itinerary_entries()
    {
        var entries = Enumerable.Range(0, TravelValidation.MaximumItineraryEntries + 1)
            .Select(_ => Entry("Stop", new DateOnly(2026, 7, 1), null))
            .ToArray();

        var error = Assert.Throws<TravelValidationException>(() =>
            TravelTrip.Create(TripValues() with { Itinerary = entries }, new UserId(1), Now));
        Assert.Equal(TravelValidationReason.Itinerary, error.Reason);
    }

    [Fact]
    public void Trip_rejects_a_blank_itinerary_title()
    {
        var error = Assert.Throws<TravelValidationException>(() =>
            TravelTrip.Create(
                TripValues() with { Itinerary = [Entry("   ", new DateOnly(2026, 7, 1), null)] },
                new UserId(1),
                Now));
        Assert.Equal(TravelValidationReason.Itinerary, error.Reason);
    }

    [Fact]
    public void Trip_replace_trip_type_repoints_the_required_reference_and_stamps_modification()
    {
        var trip = TravelTrip.Create(TripValues() with { TripTypeId = 4 }, new UserId(1), Now);

        trip.ReplaceTripType(11, new UserId(2), Now.AddHours(1));

        Assert.Equal(11, trip.TripTypeId);
        Assert.Equal(2, trip.UpdatedBy);
        Assert.Equal(Now.AddHours(1), trip.UpdatedAt);
    }

    [Fact]
    public void Expense_requires_a_trip_and_stamps_created_audit()
    {
        var expense = TravelExpense.Create(7, ExpenseValues() with { Description = "  Taxi  " }, new UserId(1), Now);

        Assert.Equal(7, expense.TripId);
        Assert.Equal("Taxi", expense.Description);
        Assert.Equal(1, expense.CreatedBy);
        Assert.Null(expense.UpdatedAt);
        Assert.Null(expense.UpdatedBy);

        Assert.Throws<TravelValidationException>(() =>
            TravelExpense.Create(0, ExpenseValues(), new UserId(1), Now));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1.001)]
    public void Expense_rejects_negative_or_overprecise_amount(decimal amount)
    {
        Assert.Throws<TravelValidationException>(() =>
            TravelExpense.Create(1, ExpenseValues() with { Amount = amount }, new UserId(1), Now));
    }

    [Fact]
    public void Expense_accepts_a_zero_amount_and_optional_references_left_unset()
    {
        var expense = TravelExpense.Create(
            1,
            ExpenseValues() with { Amount = 0.00m, SupplierId = null, CostCenterId = null },
            new UserId(1),
            Now);

        Assert.Equal(0.00m, expense.Amount);
        Assert.Null(expense.SupplierId);
        Assert.Null(expense.CostCenterId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Expense_rejects_nonpositive_required_references(int value)
    {
        Assert.Throws<TravelValidationException>(() =>
            TravelExpense.Create(1, ExpenseValues() with { ExpenseCategoryId = value }, new UserId(1), Now));
        Assert.Throws<TravelValidationException>(() =>
            TravelExpense.Create(1, ExpenseValues() with { CurrencyId = value }, new UserId(1), Now));
    }

    [Fact]
    public void Expense_replace_category_repoints_the_required_reference_and_stamps_modification()
    {
        var expense = TravelExpense.Create(1, ExpenseValues() with { ExpenseCategoryId = 3 }, new UserId(1), Now);

        expense.ReplaceExpenseCategory(9, new UserId(2), Now.AddHours(1));

        Assert.Equal(9, expense.ExpenseCategoryId);
        Assert.Equal(2, expense.UpdatedBy);
        Assert.Equal(Now.AddHours(1), expense.UpdatedAt);
    }

    [Fact]
    public void Expense_migration_helpers_update_optional_references_and_currency_amounts()
    {
        var expense = TravelExpense.Create(
            1,
            ExpenseValues() with { Amount = 10.01m, CurrencyId = 3, SupplierId = 4, CostCenterId = 5 },
            new UserId(1),
            Now);

        expense.ReplaceSupplier(null, new UserId(2), Now.AddHours(1));
        expense.ReplaceCostCenter(8, new UserId(2), Now.AddHours(1));
        expense.ConvertCurrency(9, 1.25m, new UserId(2), Now.AddHours(1));

        Assert.Null(expense.SupplierId);
        Assert.Equal(8, expense.CostCenterId);
        Assert.Equal(9, expense.CurrencyId);
        Assert.Equal(12.51m, expense.Amount);
        Assert.Equal(2, expense.UpdatedBy);
        Assert.Equal(Now.AddHours(1), expense.UpdatedAt);
    }

    [Fact]
    public async Task Seeder_initializes_trip_types_and_expense_categories_once_in_declaration_order()
    {
        await using var fixture = await TravelFixture.CreateAsync();
        var seeder = new TravelSeeder(fixture.Database, new CatalogInitializer(fixture.Database, fixture.Clock));
        await seeder.SeedAsync(CancellationToken.None);

        var tripTypes = await fixture.Database.Set<TravelTripType>().AsNoTracking()
            .OrderBy(value => value.SortOrder).ToListAsync();
        var categories = await fixture.Database.Set<TravelExpenseCategory>().AsNoTracking()
            .OrderBy(value => value.SortOrder).ToListAsync();
        Assert.Equal(TravelCatalog.TripTypes.Select(seed => seed.Name), tripTypes.Select(value => value.Name));
        Assert.Equal(TravelCatalog.ExpenseCategories.Select(seed => seed.Name), categories.Select(value => value.Name));
        Assert.Equal(Enumerable.Range(0, tripTypes.Count), tripTypes.Select(value => value.SortOrder));
        Assert.Equal("REGIONAL", tripTypes[0].NormalizedName);

        fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddDays(1);
        await seeder.SeedAsync(CancellationToken.None);
        var reseeded = await fixture.Database.Set<TravelTripType>().AsNoTracking()
            .ToDictionaryAsync(value => value.Name, value => value.Id);
        Assert.Equal(tripTypes.ToDictionary(value => value.Name, value => value.Id), reseeded);
    }

    [Fact]
    public async Task Sqlite_persists_a_trip_and_cascades_itinerary_and_expense_deletion()
    {
        await using var fixture = await TravelFixture.CreateAsync();
        var references = await fixture.SeedReferencesAsync();

        var trip = TravelTrip.Create(
            TripValues() with
            {
                TripTypeId = references.TripTypeId,
                Itinerary =
                [
                    Entry("Check-in", new DateOnly(2026, 7, 1), null),
                    Entry("Tour", new DateOnly(2026, 7, 2), new TimeOnly(10, 0)),
                ],
            },
            new UserId(1),
            Now);
        fixture.Database.Add(trip);
        await fixture.Database.SaveChangesAsync();

        var expense = TravelExpense.Create(
            trip.Id,
            ExpenseValues() with
            {
                ExpenseCategoryId = references.ExpenseCategoryId,
                CurrencyId = references.CurrencyId,
            },
            new UserId(1),
            Now);
        fixture.Database.Add(expense);
        await fixture.Database.SaveChangesAsync();
        fixture.Database.ChangeTracker.Clear();

        Assert.Equal(2, await fixture.Database.Set<TravelItineraryEntry>().CountAsync());
        Assert.Equal(1, await fixture.Database.Set<TravelExpense>().CountAsync());

        var stored = await fixture.Database.Set<TravelTrip>().SingleAsync();
        fixture.Database.Remove(stored);
        await fixture.Database.SaveChangesAsync();

        Assert.Equal(0, await fixture.Database.Set<TravelItineraryEntry>().CountAsync());
        Assert.Equal(0, await fixture.Database.Set<TravelExpense>().CountAsync());
    }

    [Fact]
    public async Task Trip_type_management_creates_renames_and_rejects_duplicate_names()
    {
        await using var fixture = await TravelFixture.CreateAsync();
        await fixture.SeedReferencesAsync();
        var service = new TravelTripTypeManagementService(fixture.Database, fixture.Clock);

        var created = await service.CreateAsync(new TravelTripTypeRequest(" Intercontinental "), new UserId(1), CancellationToken.None);
        Assert.Equal("Intercontinental", created.Name);
        Assert.Equal(TravelCatalog.TripTypes.Count, created.SortOrder);

        await Assert.ThrowsAsync<ApiProblemException>(() =>
            service.CreateAsync(new TravelTripTypeRequest("regional"), new UserId(1), CancellationToken.None));

        var renamed = await service.UpdateAsync(created.Id, new TravelTripTypeRequest("Long-haul"), new UserId(1), CancellationToken.None);
        Assert.Equal("Long-haul", renamed.Name);
    }

    [Fact]
    public async Task Trip_type_management_protects_the_final_row_and_migrates_references_atomically()
    {
        await using var fixture = await TravelFixture.CreateAsync();
        var references = await fixture.SeedReferencesAsync();
        var service = new TravelTripTypeManagementService(fixture.Database, fixture.Clock);

        var replacementId = await fixture.Database.Set<TravelTripType>()
            .Where(value => value.Id != references.TripTypeId)
            .Select(value => value.Id).FirstAsync();
        await fixture.SeedTripAsync(references);

        await Assert.ThrowsAsync<ApiProblemException>(() =>
            service.DeleteAsync(references.TripTypeId, CancellationToken.None));

        fixture.Clock.UtcNow = Now.AddHours(1);
        await service.ReplaceAndDeleteAsync(
            references.TripTypeId,
            new CatalogReplacementRequest(replacementId, ClearReferences: false, ExchangeRate: null),
            new UserId(2),
            CancellationToken.None);

        fixture.Database.ChangeTracker.Clear();
        var migrated = await fixture.Database.Set<TravelTrip>().SingleAsync();
        Assert.Equal(replacementId, migrated.TripTypeId);
        Assert.Equal(2, migrated.UpdatedBy);
        Assert.False(await fixture.Database.Set<TravelTripType>().AnyAsync(value => value.Id == references.TripTypeId));
    }

    [Fact]
    public async Task Expense_category_management_protects_the_final_row_and_migrates_references_atomically()
    {
        await using var fixture = await TravelFixture.CreateAsync();
        var references = await fixture.SeedReferencesAsync();
        var service = new TravelExpenseCategoryManagementService(fixture.Database, fixture.Clock);

        var replacementId = await fixture.Database.Set<TravelExpenseCategory>()
            .Where(value => value.Id != references.ExpenseCategoryId)
            .Select(value => value.Id).FirstAsync();
        var tripId = await fixture.SeedTripAsync(references);
        await fixture.SeedExpenseAsync(tripId, references);

        await Assert.ThrowsAsync<ApiProblemException>(() =>
            service.DeleteAsync(references.ExpenseCategoryId, CancellationToken.None));

        fixture.Clock.UtcNow = Now.AddHours(1);
        await service.ReplaceAndDeleteAsync(
            references.ExpenseCategoryId,
            new CatalogReplacementRequest(replacementId, ClearReferences: false, ExchangeRate: null),
            new UserId(2),
            CancellationToken.None);

        fixture.Database.ChangeTracker.Clear();
        var migrated = await fixture.Database.Set<TravelExpense>().SingleAsync();
        Assert.Equal(replacementId, migrated.ExpenseCategoryId);
        Assert.Equal(2, migrated.UpdatedBy);
        Assert.False(await fixture.Database.Set<TravelExpenseCategory>().AnyAsync(value => value.Id == references.ExpenseCategoryId));
    }

    [Fact]
    public async Task Configuration_supplier_migration_replaces_or_clears_travel_expense_references()
    {
        await using var fixture = await TravelFixture.CreateAsync();
        var references = await fixture.SeedReferencesAsync();
        var tripId = await fixture.SeedTripAsync(references);
        await fixture.SeedExpenseAsync(
            tripId,
            references with { SupplierId = references.SourceSupplierId });

        var service = fixture.ConfigurationService([
            new TravelCatalogReferenceHandler(fixture.Database, ConfigurationCatalogKind.Suppliers),
        ]);

        await service.ReplaceAndDeleteSupplierAsync(
            references.SourceSupplierId,
            new CatalogReplacementRequest(references.ReplacementSupplierId, ClearReferences: false, ExchangeRate: null),
            new UserId(2),
            CancellationToken.None);

        fixture.Database.ChangeTracker.Clear();
        var replaced = await fixture.Database.Set<TravelExpense>().SingleAsync();
        Assert.Equal(references.ReplacementSupplierId, replaced.SupplierId);
        Assert.Equal(2, replaced.UpdatedBy);
        Assert.False(await fixture.Database.Set<SegarisSupplier>().AnyAsync(value => value.Id == references.SourceSupplierId));

        var nextSourceId = references.ReplacementSupplierId;
        await service.ReplaceAndDeleteSupplierAsync(
            nextSourceId,
            new CatalogReplacementRequest(ReplacementId: null, ClearReferences: true, ExchangeRate: null),
            new UserId(2),
            CancellationToken.None);

        fixture.Database.ChangeTracker.Clear();
        var cleared = await fixture.Database.Set<TravelExpense>().SingleAsync();
        Assert.Null(cleared.SupplierId);
        Assert.False(await fixture.Database.Set<SegarisSupplier>().AnyAsync(value => value.Id == nextSourceId));
    }

    [Fact]
    public async Task Configuration_cost_center_migration_replaces_or_clears_travel_expense_references()
    {
        await using var fixture = await TravelFixture.CreateAsync();
        var references = await fixture.SeedReferencesAsync();
        var tripId = await fixture.SeedTripAsync(references);
        await fixture.SeedExpenseAsync(
            tripId,
            references with { CostCenterId = references.SourceCostCenterId });

        var service = fixture.ConfigurationService([
            new TravelCatalogReferenceHandler(fixture.Database, ConfigurationCatalogKind.CostCenters),
        ]);

        await service.ReplaceAndDeleteCostCenterAsync(
            references.SourceCostCenterId,
            new CatalogReplacementRequest(references.ReplacementCostCenterId, ClearReferences: false, ExchangeRate: null),
            new UserId(2),
            CancellationToken.None);

        fixture.Database.ChangeTracker.Clear();
        var replaced = await fixture.Database.Set<TravelExpense>().SingleAsync();
        Assert.Equal(references.ReplacementCostCenterId, replaced.CostCenterId);
        Assert.False(await fixture.Database.Set<SegarisCostCenter>().AnyAsync(value => value.Id == references.SourceCostCenterId));

        var nextSourceId = references.ReplacementCostCenterId;
        await service.ReplaceAndDeleteCostCenterAsync(
            nextSourceId,
            new CatalogReplacementRequest(ReplacementId: null, ClearReferences: true, ExchangeRate: null),
            new UserId(2),
            CancellationToken.None);

        fixture.Database.ChangeTracker.Clear();
        var cleared = await fixture.Database.Set<TravelExpense>().SingleAsync();
        Assert.Null(cleared.CostCenterId);
        Assert.False(await fixture.Database.Set<SegarisCostCenter>().AnyAsync(value => value.Id == nextSourceId));
    }

    [Fact]
    public async Task Configuration_currency_migration_converts_travel_expense_amounts()
    {
        await using var fixture = await TravelFixture.CreateAsync();
        var references = await fixture.SeedReferencesAsync();
        var tripId = await fixture.SeedTripAsync(references);
        await fixture.SeedExpenseAsync(
            tripId,
            references with { CurrencyId = references.SourceCurrencyId, Amount = 10.01m });

        var service = fixture.ConfigurationService([
            new TravelCatalogReferenceHandler(fixture.Database, ConfigurationCatalogKind.Currencies),
        ]);

        await service.ReplaceAndDeleteCurrencyAsync(
            references.SourceCurrencyId,
            new CatalogReplacementRequest(references.ReplacementCurrencyId, ClearReferences: false, ExchangeRate: 1.25m),
            new UserId(2),
            CancellationToken.None);

        fixture.Database.ChangeTracker.Clear();
        var converted = await fixture.Database.Set<TravelExpense>().SingleAsync();
        Assert.Equal(references.ReplacementCurrencyId, converted.CurrencyId);
        Assert.Equal(12.51m, converted.Amount);
        Assert.Equal(2, converted.UpdatedBy);
        Assert.False(await fixture.Database.Set<SegarisCurrency>().AnyAsync(value => value.Id == references.SourceCurrencyId));
    }

    [Fact]
    public async Task Configuration_reference_migration_rolls_back_travel_changes_when_a_later_handler_fails()
    {
        await using var fixture = await TravelFixture.CreateAsync();
        var references = await fixture.SeedReferencesAsync();
        var tripId = await fixture.SeedTripAsync(references);
        await fixture.SeedExpenseAsync(
            tripId,
            references with { SupplierId = references.SourceSupplierId });

        var service = fixture.ConfigurationService([
            new TravelCatalogReferenceHandler(fixture.Database, ConfigurationCatalogKind.Suppliers),
            new FailingReferenceHandler(ConfigurationCatalogKind.Suppliers),
        ]);

        var exception = await Assert.ThrowsAsync<ApiProblemException>(() => service.ReplaceAndDeleteSupplierAsync(
            references.SourceSupplierId,
            new CatalogReplacementRequest(references.ReplacementSupplierId, ClearReferences: false, ExchangeRate: null),
            new UserId(2),
            CancellationToken.None));

        Assert.Equal(ConfigurationErrorCodes.CatalogMigrationFailed, exception.Code);
        fixture.Database.ChangeTracker.Clear();
        var stored = await fixture.Database.Set<TravelExpense>().SingleAsync();
        Assert.Equal(references.SourceSupplierId, stored.SupplierId);
        Assert.True(await fixture.Database.Set<SegarisSupplier>().AnyAsync(value => value.Id == references.SourceSupplierId));
    }

    private static TravelTripValues TripValues() => new(
        "Example trip",
        TripTypeId: 1,
        Destination: null,
        StartDate: new DateOnly(2026, 7, 1),
        EndDate: new DateOnly(2026, 7, 5),
        TravelTripStatus.Planned,
        Notes: null,
        RecordVisibility.Public,
        Itinerary: []);

    private static TravelExpenseValues ExpenseValues() => new(
        ExpenseCategoryId: 1,
        Description: "Example expense",
        Date: new DateOnly(2026, 7, 2),
        Amount: 12.50m,
        CurrencyId: 1,
        SupplierId: null,
        CostCenterId: null,
        Notes: null);

    private static TravelItineraryEntryValues Entry(string title, DateOnly date, TimeOnly? time) =>
        new(date, time, title, Place: null, ReservationLocator: null, Note: null);

    private sealed record References(
        int TripTypeId,
        int ExpenseCategoryId,
        int CurrencyId,
        int SourceSupplierId,
        int ReplacementSupplierId,
        int SourceCostCenterId,
        int ReplacementCostCenterId,
        int SourceCurrencyId,
        int ReplacementCurrencyId,
        decimal Amount = 12.50m,
        int? SupplierId = null,
        int? CostCenterId = null);

    private sealed class TravelFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private TravelFixture(SqliteConnection connection, SegarisDbContext database, MutableClock clock)
        {
            this.connection = connection;
            Database = database;
            Clock = clock;
        }

        public SegarisDbContext Database { get; }
        public MutableClock Clock { get; }

        public static async Task<TravelFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<SegarisDbContext>()
                .UseSqlite(connection)
                .EnableServiceProviderCaching(false)
                .Options;
            var database = new SegarisDbContext(options,
                [new IdentityModelContributor(), new ConfigurationModelContributor(), new TravelModelContributor()]);
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
            return new TravelFixture(connection, database, clock);
        }

        public async Task<References> SeedReferencesAsync()
        {
            await new ConfigurationSeeder(Database, new CatalogInitializer(Database, Clock))
                .SeedAsync(CancellationToken.None);
            await new TravelSeeder(Database, new CatalogInitializer(Database, Clock))
                .SeedAsync(CancellationToken.None);
            var tripTypeId = await Database.Set<TravelTripType>()
                .OrderBy(value => value.SortOrder).Select(value => value.Id).FirstAsync();
            var expenseCategoryId = await Database.Set<TravelExpenseCategory>()
                .OrderBy(value => value.SortOrder).Select(value => value.Id).FirstAsync();
            var currencyId = await Database.Set<SegarisCurrency>()
                .Where(currency => currency.Code == ConfigurationCatalog.CurrencyCodes.Default)
                .Select(currency => currency.Id).SingleAsync();
            var sourceSupplierId = await Database.Set<SegarisSupplier>()
                .Where(value => value.Name == "Amazon")
                .Select(value => value.Id)
                .SingleAsync();
            var replacementSupplierId = await Database.Set<SegarisSupplier>()
                .Where(value => value.Name == "IKEA")
                .Select(value => value.Id)
                .SingleAsync();
            var sourceCostCenterId = await Database.Set<SegarisCostCenter>()
                .Where(value => value.Name == "Household")
                .Select(value => value.Id)
                .SingleAsync();
            var replacementCostCenterId = await Database.Set<SegarisCostCenter>()
                .Where(value => value.Name == "Personal")
                .Select(value => value.Id)
                .SingleAsync();
            var sourceCurrencyId = currencyId;
            var replacementCurrencyId = await Database.Set<SegarisCurrency>()
                .Where(currency => currency.Code == ConfigurationCatalog.CurrencyCodes.UsDollar)
                .Select(currency => currency.Id)
                .SingleAsync();
            return new References(
                tripTypeId,
                expenseCategoryId,
                currencyId,
                sourceSupplierId,
                replacementSupplierId,
                sourceCostCenterId,
                replacementCostCenterId,
                sourceCurrencyId,
                replacementCurrencyId);
        }

        public async Task<int> SeedTripAsync(References references)
        {
            var trip = TravelTrip.Create(TripValues() with { TripTypeId = references.TripTypeId }, new UserId(1), Now);
            Database.Add(trip);
            await Database.SaveChangesAsync();
            Database.ChangeTracker.Clear();
            return trip.Id;
        }

        public async Task SeedExpenseAsync(int tripId, References references)
        {
            var expense = TravelExpense.Create(
                tripId,
                ExpenseValues() with
                {
                    ExpenseCategoryId = references.ExpenseCategoryId,
                    CurrencyId = references.CurrencyId,
                    Amount = references.Amount,
                    SupplierId = references.SupplierId,
                    CostCenterId = references.CostCenterId,
                },
                new UserId(1),
                Now);
            Database.Add(expense);
            await Database.SaveChangesAsync();
            Database.ChangeTracker.Clear();
        }

        public ConfigurationCatalogManagementService ConfigurationService(ICatalogReferenceHandler[] handlers) =>
            new(Database, handlers, Clock);

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

    private sealed class FailingReferenceHandler(ConfigurationCatalogKind kind) : ICatalogReferenceHandler
    {
        public ConfigurationCatalogKind Kind => kind;

        public Task<bool> HasReferencesAsync(int catalogId, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task MigrateReferencesAsync(CatalogReferenceMigration migration, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Injected consumer failure.");
    }
}
