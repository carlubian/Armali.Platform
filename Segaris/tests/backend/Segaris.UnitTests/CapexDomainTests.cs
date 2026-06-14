using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Capex;
using Segaris.Api.Modules.Capex.Domain;
using Segaris.Api.Modules.Capex.Persistence;
using Segaris.Api.Modules.Capex.Seeding;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Configuration.Seeding;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Persistence;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.UnitTests;

public sealed class CapexDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 14, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Entry_trims_values_preserves_order_and_sums_rounded_lines()
    {
        var entry = CreateEntry(
            [new(" First ", 0.01m, 0.50m), new("Second", 2m, 1m)]);

        Assert.Equal("Example", entry.Title);
        Assert.Equal(["First", "Second"], entry.Items.Select(item => item.Description));
        Assert.Equal([0, 1], entry.Items.Select(item => item.Position));
        Assert.Equal([0.01m, 2m], entry.Items.Select(item => item.LineAmount));
        Assert.Equal(2.01m, entry.TotalAmount);
    }

    [Fact]
    public void Entry_allows_a_zero_total_and_exactly_one_hundred_items()
    {
        var items = Enumerable.Range(1, 100)
            .Select(index => new CapexItemValues($"Item {index}", 1m, 0m))
            .ToArray();

        var entry = CreateEntry(items);

        Assert.Equal(100, entry.Items.Count);
        Assert.Equal(0m, entry.TotalAmount);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(101, 1)]
    public void Entry_rejects_item_counts_outside_the_bounds(int count, decimal unitAmount)
    {
        var items = Enumerable.Range(1, count)
            .Select(index => new CapexItemValues($"Item {index}", 1m, unitAmount))
            .ToArray();

        Assert.Throws<CapexValidationException>(() => CreateEntry(items));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, -1)]
    [InlineData(1.001, 1)]
    [InlineData(1, 1.001)]
    public void Entry_rejects_invalid_item_numbers(decimal quantity, decimal unitAmount)
    {
        Assert.Throws<CapexValidationException>(() =>
            CreateEntry([new("Item", quantity, unitAmount)]));
    }

    [Fact]
    public void Only_the_creator_can_change_visibility()
    {
        var entry = CreateEntry([new("Item", 1m, 1m)]);
        var changed = Values() with { Visibility = RecordVisibility.Private };

        Assert.Throws<CapexValidationException>(() =>
            entry.Update(changed, [new("Item", 1m, 1m)], new UserId(2), Now.AddMinutes(1)));

        entry.Update(changed, [new("Item", 1m, 1m)], new UserId(1), Now.AddMinutes(1));
        Assert.Equal(RecordVisibility.Private, entry.Visibility);
    }

    [Fact]
    public void Visibility_predicates_allow_public_collaboration_and_isolate_private_entries()
    {
        var publicEntry = CreateEntry([new("Public", 1m, 1m)]);
        var privateEntry = CapexEntry.Create(
            Values() with { Visibility = RecordVisibility.Private },
            [new("Private", 1m, 1m)], new UserId(1), Now);
        var otherUserPredicate = CapexEntryPolicies.AccessibleTo(new UserId(2)).Compile();

        Assert.True(otherUserPredicate(publicEntry));
        Assert.False(otherUserPredicate(privateEntry));
        Assert.True(CapexEntryPolicies.AccessibleTo(new UserId(1)).Compile()(privateEntry));
    }

    [Fact]
    public async Task Catalog_validator_accepts_seeded_references_and_rejects_unknown_ones()
    {
        await using var fixture = await CapexFixture.CreateAsync();
        await new CapexSeeder(fixture.Database, fixture.Clock).SeedAsync(CancellationToken.None);
        var categoryId = await fixture.Database.Set<CapexCategory>().Select(category => category.Id).FirstAsync();
        var currencyId = await fixture.Database.Set<SegarisCurrency>().Select(currency => currency.Id).FirstAsync();
        var validator = new CapexCatalogValidator(
            new ConfigurationCatalogService(fixture.Database), fixture.Database);

        await validator.ValidateAsync(
            Values() with { CategoryId = categoryId, CurrencyId = currencyId }, CancellationToken.None);

        await Assert.ThrowsAsync<CapexValidationException>(() => validator.ValidateAsync(
            Values() with { CategoryId = int.MaxValue, CurrencyId = currencyId }, CancellationToken.None));
    }

    [Fact]
    public async Task Sqlite_persists_ordered_decimal_values_and_category_seed_ids_stably()
    {
        await using var fixture = await CapexFixture.CreateAsync();
        var seeder = new CapexSeeder(fixture.Database, fixture.Clock);
        await seeder.SeedAsync(CancellationToken.None);
        var originalIds = await fixture.Database.Set<CapexCategory>()
            .AsNoTracking().ToDictionaryAsync(category => category.Code, category => category.Id);
        fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddDays(1);
        await seeder.SeedAsync(CancellationToken.None);
        var repeatedIds = await fixture.Database.Set<CapexCategory>()
            .AsNoTracking().ToDictionaryAsync(category => category.Code, category => category.Id);

        var categoryId = originalIds[CapexCategoryCatalog.Codes.Other];
        var currencyId = await fixture.Database.Set<SegarisCurrency>()
            .Where(currency => currency.Code == ConfigurationCatalog.CurrencyCodes.Default)
            .Select(currency => currency.Id).SingleAsync();
        var entry = CapexEntry.Create(
            Values() with { CategoryId = categoryId, CurrencyId = currencyId },
            [new("First", 0.01m, 0.50m), new("Second", 3m, 2.25m)],
            new UserId(1), Now);
        fixture.Database.Add(entry);
        await fixture.Database.SaveChangesAsync();
        fixture.Database.ChangeTracker.Clear();

        var stored = await fixture.Database.Set<CapexEntry>().Include(value => value.Items).SingleAsync();

        Assert.Equal(originalIds, repeatedIds);
        Assert.Equal(6.76m, stored.TotalAmount);
        Assert.Equal([0.01m, 6.75m], stored.Items.OrderBy(item => item.Position).Select(item => item.LineAmount));
    }

    private static CapexEntry CreateEntry(IReadOnlyList<CapexItemValues> items) =>
        CapexEntry.Create(Values(), items, new UserId(1), Now);

    private static CapexEntryValues Values() => new(
        " Example ", CapexMovementType.Expense, CapexEntryStatus.Planning,
        new DateOnly(2026, 6, 14), 1, null, null, 1, " Notes ", RecordVisibility.Public);

    private sealed class CapexFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private CapexFixture(SqliteConnection connection, SegarisDbContext database, MutableClock clock)
        {
            this.connection = connection;
            Database = database;
            Clock = clock;
        }

        public SegarisDbContext Database { get; }
        public MutableClock Clock { get; }

        public static async Task<CapexFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<SegarisDbContext>()
                .UseSqlite(connection)
                .EnableServiceProviderCaching(false)
                .Options;
            var database = new SegarisDbContext(options,
                [new IdentityModelContributor(), new ConfigurationModelContributor(), new CapexModelContributor()]);
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
            await database.SaveChangesAsync();
            var clock = new MutableClock { UtcNow = Now };
            await new ConfigurationSeeder(database, clock).SeedAsync(CancellationToken.None);
            return new CapexFixture(connection, database, clock);
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
