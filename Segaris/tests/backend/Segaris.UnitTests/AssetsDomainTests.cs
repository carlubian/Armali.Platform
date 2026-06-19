using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Assets;
using Segaris.Api.Modules.Assets.Contracts;
using Segaris.Api.Modules.Assets.Domain;
using Segaris.Api.Modules.Assets.Mutations;
using Segaris.Api.Modules.Assets.Persistence;
using Segaris.Api.Modules.Assets.Seeding;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Configuration.Seeding;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Persistence;
using Segaris.Api.Platform.Api;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.UnitTests;

public sealed class AssetsDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 19, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Asset_trims_name_normalizes_code_and_stamps_audit()
    {
        var asset = Asset.Create(
            Values() with { Name = " Drill ", Code = " tool-1 " }, new UserId(1), Now);

        Assert.Equal("Drill", asset.Name);
        Assert.Equal("tool-1", asset.Code);
        Assert.Equal("TOOL-1", asset.NormalizedCode);
        Assert.Equal(AssetStatus.Active, asset.Status);
        Assert.Equal(RecordVisibility.Public, asset.Visibility);
        Assert.Equal(1, asset.CreatedBy);
        Assert.Equal(1, asset.UpdatedBy);
        Assert.Equal(Now, asset.CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Asset_rejects_a_blank_name(string name)
    {
        Assert.Throws<AssetValidationException>(() =>
            Asset.Create(Values() with { Name = name }, new UserId(1), Now));
    }

    [Fact]
    public void Asset_rejects_an_overlong_name()
    {
        var name = new string('a', AssetValidation.NameMaximumLength + 1);
        Assert.Throws<AssetValidationException>(() =>
            Asset.Create(Values() with { Name = name }, new UserId(1), Now));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Asset_treats_a_blank_code_as_absent(string? code)
    {
        var asset = Asset.Create(Values() with { Code = code }, new UserId(1), Now);

        Assert.Null(asset.Code);
        Assert.Null(asset.NormalizedCode);
    }

    [Fact]
    public void Asset_rejects_an_overlong_code()
    {
        var code = new string('a', AssetValidation.CodeMaximumLength + 1);
        Assert.Throws<AssetValidationException>(() =>
            Asset.Create(Values() with { Code = code }, new UserId(1), Now));
    }

    [Fact]
    public void Asset_normalizes_codes_case_insensitively()
    {
        Assert.Equal(
            Asset.Create(Values() with { Code = "abc-123" }, new UserId(1), Now).NormalizedCode,
            Asset.Create(Values() with { Code = "ABC-123" }, new UserId(1), Now).NormalizedCode);
    }

    [Fact]
    public void Asset_trims_optional_identification_and_drops_empty_values()
    {
        var asset = Asset.Create(
            Values() with { BrandModel = " Bosch ", SerialNumber = "  " }, new UserId(1), Now);

        Assert.Equal("Bosch", asset.BrandModel);
        Assert.Null(asset.SerialNumber);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Asset_rejects_nonpositive_catalog_identifiers(int value)
    {
        Assert.Throws<AssetValidationException>(() =>
            Asset.Create(Values() with { CategoryId = value }, new UserId(1), Now));
        Assert.Throws<AssetValidationException>(() =>
            Asset.Create(Values() with { LocationId = value }, new UserId(1), Now));
    }

    [Fact]
    public void Asset_rejects_unknown_status_or_visibility()
    {
        Assert.Throws<AssetValidationException>(() =>
            Asset.Create(Values() with { Status = (AssetStatus)42 }, new UserId(1), Now));
        Assert.Throws<AssetValidationException>(() =>
            Asset.Create(Values() with { Visibility = (RecordVisibility)42 }, new UserId(1), Now));
    }

    [Fact]
    public void Asset_keeps_optional_dates_without_an_artificial_boundary()
    {
        var asset = Asset.Create(
            Values() with
            {
                AcquisitionDate = new DateOnly(1990, 1, 1),
                ExpectedEndOfLifeDate = new DateOnly(2099, 12, 31),
            },
            new UserId(1),
            Now);

        Assert.Equal(new DateOnly(1990, 1, 1), asset.AcquisitionDate);
        Assert.Equal(new DateOnly(2099, 12, 31), asset.ExpectedEndOfLifeDate);
    }

    [Fact]
    public void ReplaceCategory_and_ReplaceLocation_update_references_and_stamp_modification()
    {
        var asset = Asset.Create(Values(), new UserId(1), Now);

        asset.ReplaceCategory(20, new UserId(2), Now.AddHours(1));
        asset.ReplaceLocation(30, new UserId(2), Now.AddHours(1));

        Assert.Equal(20, asset.CategoryId);
        Assert.Equal(30, asset.LocationId);
        Assert.Equal(2, asset.UpdatedBy);
        Assert.Equal(Now.AddHours(1), asset.UpdatedAt);
    }

    [Fact]
    public async Task Seeder_initializes_categories_and_locations_once_in_declaration_order()
    {
        await using var fixture = await AssetsFixture.CreateAsync();
        var seeder = new AssetsSeeder(fixture.Database, new CatalogInitializer(fixture.Database, fixture.Clock));
        await seeder.SeedAsync(CancellationToken.None);

        var categories = await fixture.Database.Set<AssetCategory>().AsNoTracking()
            .OrderBy(category => category.SortOrder).ToListAsync();
        var locations = await fixture.Database.Set<AssetLocation>().AsNoTracking()
            .OrderBy(location => location.SortOrder).ToListAsync();
        Assert.Equal(AssetCatalog.Categories.Select(seed => seed.Name), categories.Select(category => category.Name));
        Assert.Equal(AssetCatalog.Locations.Select(seed => seed.Name), locations.Select(location => location.Name));
        Assert.Equal(Enumerable.Range(0, categories.Count), categories.Select(category => category.SortOrder));
        Assert.Equal("FURNITURE", categories[0].NormalizedName);

        fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddDays(1);
        await seeder.SeedAsync(CancellationToken.None);
        var reseeded = await fixture.Database.Set<AssetCategory>().AsNoTracking()
            .ToDictionaryAsync(category => category.Name, category => category.Id);
        Assert.Equal(categories.ToDictionary(category => category.Name, category => category.Id), reseeded);
    }

    [Fact]
    public async Task Sqlite_persists_an_asset_with_its_references()
    {
        await using var fixture = await AssetsFixture.CreateAsync();
        var references = await fixture.SeedReferencesAsync();

        var asset = Asset.Create(
            Values() with { CategoryId = references.CategoryId, LocationId = references.LocationId, Code = "SN-1" },
            new UserId(1),
            Now);
        fixture.Database.Add(asset);
        await fixture.Database.SaveChangesAsync();
        fixture.Database.ChangeTracker.Clear();

        var stored = await fixture.Database.Set<Asset>().SingleAsync();
        Assert.Equal("SN-1", stored.Code);
        Assert.Equal("SN-1", stored.NormalizedCode);
        Assert.Equal(references.CategoryId, stored.CategoryId);
    }

    [Fact]
    public async Task Sqlite_rejects_a_case_insensitively_duplicate_code()
    {
        await using var fixture = await AssetsFixture.CreateAsync();
        var references = await fixture.SeedReferencesAsync();

        fixture.Database.Add(Asset.Create(
            Values() with { CategoryId = references.CategoryId, LocationId = references.LocationId, Code = "dup-1" },
            new UserId(1),
            Now));
        await fixture.Database.SaveChangesAsync();

        fixture.Database.Add(Asset.Create(
            Values() with { CategoryId = references.CategoryId, LocationId = references.LocationId, Code = "DUP-1" },
            new UserId(1),
            Now));

        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Database.SaveChangesAsync());
    }

    [Fact]
    public async Task Sqlite_allows_multiple_assets_without_a_code()
    {
        await using var fixture = await AssetsFixture.CreateAsync();
        var references = await fixture.SeedReferencesAsync();

        fixture.Database.Add(Asset.Create(
            Values() with { CategoryId = references.CategoryId, LocationId = references.LocationId, Code = null },
            new UserId(1),
            Now));
        fixture.Database.Add(Asset.Create(
            Values() with { CategoryId = references.CategoryId, LocationId = references.LocationId, Code = null },
            new UserId(1),
            Now));

        await fixture.Database.SaveChangesAsync();
        Assert.Equal(2, await fixture.Database.Set<Asset>().CountAsync());
    }

    [Fact]
    public async Task Category_management_creates_renames_and_rejects_duplicate_names()
    {
        await using var fixture = await AssetsFixture.CreateAsync();
        await fixture.SeedReferencesAsync();
        var service = new AssetCategoryManagementService(fixture.Database, fixture.Clock);

        var created = await service.CreateAsync(new CatalogItemRequest(" Gadgets "), new UserId(1), CancellationToken.None);
        Assert.Equal("Gadgets", created.Name);
        Assert.Equal(AssetCatalog.Categories.Count, created.SortOrder);

        await Assert.ThrowsAsync<ApiProblemException>(() =>
            service.CreateAsync(new CatalogItemRequest("furniture"), new UserId(1), CancellationToken.None));

        var renamed = await service.UpdateAsync(created.Id, new CatalogItemRequest("Devices"), new UserId(1), CancellationToken.None);
        Assert.Equal("Devices", renamed.Name);
    }

    [Fact]
    public async Task Category_management_protects_the_final_row_and_migrates_references_atomically()
    {
        await using var fixture = await AssetsFixture.CreateAsync();
        var references = await fixture.SeedReferencesAsync();
        var service = new AssetCategoryManagementService(fixture.Database, fixture.Clock);

        var replacementId = await fixture.Database.Set<AssetCategory>()
            .Where(category => category.Id != references.CategoryId)
            .Select(category => category.Id).FirstAsync();
        await fixture.SeedAssetAsync(references);

        await Assert.ThrowsAsync<ApiProblemException>(() =>
            service.DeleteAsync(references.CategoryId, CancellationToken.None));

        fixture.Clock.UtcNow = Now.AddHours(1);
        await service.ReplaceAndDeleteAsync(
            references.CategoryId,
            new CatalogReplacementRequest(replacementId, ClearReferences: false, ExchangeRate: null),
            new UserId(2),
            CancellationToken.None);

        fixture.Database.ChangeTracker.Clear();
        var migrated = await fixture.Database.Set<Asset>().SingleAsync();
        Assert.Equal(replacementId, migrated.CategoryId);
        Assert.Equal(2, migrated.UpdatedBy);
        Assert.False(await fixture.Database.Set<AssetCategory>().AnyAsync(category => category.Id == references.CategoryId));
    }

    [Fact]
    public async Task Location_management_protects_the_final_row_and_migrates_references_atomically()
    {
        await using var fixture = await AssetsFixture.CreateAsync();
        var references = await fixture.SeedReferencesAsync();
        var service = new AssetLocationManagementService(fixture.Database, fixture.Clock);

        var replacementId = await fixture.Database.Set<AssetLocation>()
            .Where(location => location.Id != references.LocationId)
            .Select(location => location.Id).FirstAsync();
        await fixture.SeedAssetAsync(references);

        await Assert.ThrowsAsync<ApiProblemException>(() =>
            service.DeleteAsync(references.LocationId, CancellationToken.None));

        fixture.Clock.UtcNow = Now.AddHours(1);
        await service.ReplaceAndDeleteAsync(
            references.LocationId,
            new CatalogReplacementRequest(replacementId, ClearReferences: false, ExchangeRate: null),
            new UserId(2),
            CancellationToken.None);

        fixture.Database.ChangeTracker.Clear();
        var migrated = await fixture.Database.Set<Asset>().SingleAsync();
        Assert.Equal(replacementId, migrated.LocationId);
        Assert.False(await fixture.Database.Set<AssetLocation>().AnyAsync(location => location.Id == references.LocationId));
    }

    [Fact]
    public async Task Category_replacement_rejects_clearing_references()
    {
        await using var fixture = await AssetsFixture.CreateAsync();
        var references = await fixture.SeedReferencesAsync();
        var service = new AssetCategoryManagementService(fixture.Database, fixture.Clock);

        await Assert.ThrowsAsync<ApiProblemException>(() =>
            service.ReplaceAndDeleteAsync(
                references.CategoryId,
                new CatalogReplacementRequest(ReplacementId: null, ClearReferences: true, ExchangeRate: null),
                new UserId(2),
                CancellationToken.None));
    }

    private static AssetValues Values() => new(
        Name: "Example",
        CategoryId: 1,
        LocationId: 1,
        Status: AssetStatus.Active,
        Code: null,
        BrandModel: null,
        SerialNumber: null,
        AcquisitionDate: null,
        ExpectedEndOfLifeDate: null,
        Notes: null,
        Visibility: RecordVisibility.Public);

    private sealed record References(int CategoryId, int LocationId);

    private sealed class AssetsFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private AssetsFixture(SqliteConnection connection, SegarisDbContext database, MutableClock clock)
        {
            this.connection = connection;
            Database = database;
            Clock = clock;
        }

        public SegarisDbContext Database { get; }
        public MutableClock Clock { get; }

        public static async Task<AssetsFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<SegarisDbContext>()
                .UseSqlite(connection)
                .EnableServiceProviderCaching(false)
                .Options;
            var database = new SegarisDbContext(options,
                [new IdentityModelContributor(), new ConfigurationModelContributor(), new AssetsModelContributor()]);
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
            return new AssetsFixture(connection, database, clock);
        }

        public async Task<References> SeedReferencesAsync()
        {
            await new ConfigurationSeeder(Database, new CatalogInitializer(Database, Clock))
                .SeedAsync(CancellationToken.None);
            await new AssetsSeeder(Database, new CatalogInitializer(Database, Clock))
                .SeedAsync(CancellationToken.None);
            var categoryId = await Database.Set<AssetCategory>()
                .OrderBy(category => category.SortOrder).Select(category => category.Id).FirstAsync();
            var locationId = await Database.Set<AssetLocation>()
                .OrderBy(location => location.SortOrder).Select(location => location.Id).FirstAsync();
            return new References(categoryId, locationId);
        }

        public async Task<int> SeedAssetAsync(References references)
        {
            var asset = Asset.Create(
                Values() with { CategoryId = references.CategoryId, LocationId = references.LocationId },
                new UserId(1),
                Now);
            Database.Add(asset);
            await Database.SaveChangesAsync();
            Database.ChangeTracker.Clear();
            return asset.Id;
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
