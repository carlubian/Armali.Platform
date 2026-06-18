using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Clothes;
using Segaris.Api.Modules.Clothes.Domain;
using Segaris.Api.Modules.Clothes.Mutations;
using Segaris.Api.Modules.Clothes.Persistence;
using Segaris.Api.Modules.Clothes.Seeding;
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

public sealed class ClothesDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Garment_trims_name_applies_defaults_and_stamps_audit()
    {
        var garment = ClothesGarment.Create(
            Values() with { Name = " Linen shirt " }, new UserId(1), Now);

        Assert.Equal("Linen shirt", garment.Name);
        Assert.Equal(ClothesGarmentStatus.Active, garment.Status);
        Assert.Equal(RecordVisibility.Public, garment.Visibility);
        Assert.Null(garment.Size);
        Assert.Empty(garment.Colors);
        Assert.Null(garment.WashingCare);
        Assert.Equal(1, garment.CreatedBy);
        Assert.Equal(1, garment.UpdatedBy);
        Assert.Equal(Now, garment.CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Garment_rejects_a_blank_name(string name)
    {
        Assert.Throws<ClothesValidationException>(() =>
            ClothesGarment.Create(Values() with { Name = name }, new UserId(1), Now));
    }

    [Fact]
    public void Garment_rejects_an_overlong_name()
    {
        var name = new string('a', ClothesValidation.GarmentNameMaximumLength + 1);
        Assert.Throws<ClothesValidationException>(() =>
            ClothesGarment.Create(Values() with { Name = name }, new UserId(1), Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Garment_rejects_a_nonpositive_category_identifier(int value)
    {
        Assert.Throws<ClothesValidationException>(() =>
            ClothesGarment.Create(Values() with { CategoryId = value }, new UserId(1), Now));
    }

    [Fact]
    public void Garment_trims_size_and_treats_blank_as_unset()
    {
        Assert.Equal("M", ClothesGarment.Create(Values() with { Size = " M " }, new UserId(1), Now).Size);
        Assert.Null(ClothesGarment.Create(Values() with { Size = "   " }, new UserId(1), Now).Size);
    }

    [Fact]
    public void Garment_deduplicates_repeated_colour_references()
    {
        var garment = ClothesGarment.Create(Values() with { ColorIds = [2, 2, 3, 3] }, new UserId(1), Now);

        Assert.Equal([2, 3], garment.Colors.Select(association => association.ColorId).OrderBy(id => id));
    }

    [Fact]
    public void Garment_rejects_a_nonpositive_colour_identifier()
    {
        Assert.Throws<ClothesValidationException>(() =>
            ClothesGarment.Create(Values() with { ColorIds = [1, 0] }, new UserId(1), Now));
    }

    [Fact]
    public void Garment_accepts_known_care_values_on_every_axis()
    {
        var garment = ClothesGarment.Create(
            Values() with
            {
                WashingCare = WashingCare.Wash30Delicate,
                DryingCare = DryingCare.VeryDelicate,
                IroningCare = IroningCare.DoNotIron,
                DryCleaningCare = DryCleaningCare.DoNotDryClean,
            },
            new UserId(1),
            Now);

        Assert.Equal(WashingCare.Wash30Delicate, garment.WashingCare);
        Assert.Equal(DryingCare.VeryDelicate, garment.DryingCare);
        Assert.Equal(IroningCare.DoNotIron, garment.IroningCare);
        Assert.Equal(DryCleaningCare.DoNotDryClean, garment.DryCleaningCare);
    }

    [Fact]
    public void Garment_rejects_an_unknown_care_value()
    {
        Assert.Throws<ClothesValidationException>(() =>
            ClothesGarment.Create(Values() with { WashingCare = (WashingCare)999 }, new UserId(1), Now));
        Assert.Throws<ClothesValidationException>(() =>
            ClothesGarment.Create(Values() with { DryingCare = (DryingCare)42 }, new UserId(1), Now));
    }

    [Fact]
    public void Garment_update_reconciles_the_colour_set_and_stamps_modification()
    {
        var garment = ClothesGarment.Create(Values() with { ColorIds = [2, 3] }, new UserId(1), Now);

        garment.Update(Values() with { ColorIds = [3, 5] }, new UserId(2), Now.AddMinutes(1));

        Assert.Equal([3, 5], garment.Colors.Select(association => association.ColorId).OrderBy(id => id));
        Assert.Equal(2, garment.UpdatedBy);
        Assert.Equal(Now.AddMinutes(1), garment.UpdatedAt);
    }

    [Fact]
    public void Garment_replace_category_repoints_and_stamps_modification()
    {
        var garment = ClothesGarment.Create(Values(), new UserId(1), Now);

        garment.ReplaceCategory(20, new UserId(2), Now.AddHours(1));

        Assert.Equal(20, garment.CategoryId);
        Assert.Equal(2, garment.UpdatedBy);
        Assert.Equal(Now.AddHours(1), garment.UpdatedAt);
    }

    [Fact]
    public void Garment_replace_colour_deduplicates_when_target_already_referenced()
    {
        var garment = ClothesGarment.Create(Values() with { ColorIds = [2, 5] }, new UserId(1), Now);

        garment.ReplaceColor(2, 5, new UserId(2), Now.AddHours(1));

        Assert.Equal([5], garment.Colors.Select(association => association.ColorId));
    }

    [Fact]
    public void Garment_clear_colour_removes_the_association_and_is_a_no_op_when_absent()
    {
        var garment = ClothesGarment.Create(Values() with { ColorIds = [2, 5] }, new UserId(1), Now);

        garment.ClearColor(2, new UserId(2), Now.AddHours(1));
        Assert.Equal([5], garment.Colors.Select(association => association.ColorId));
        Assert.Equal(2, garment.UpdatedBy);

        garment.ClearColor(9, new UserId(3), Now.AddHours(2));
        // The unaffected garment keeps its previous modification metadata.
        Assert.Equal(2, garment.UpdatedBy);
        Assert.Equal(Now.AddHours(1), garment.UpdatedAt);
    }

    [Theory]
    [InlineData("#000000", "#000000")]
    [InlineData("#abcdef", "#ABCDEF")]
    [InlineData(" #1b2A4a ", "#1B2A4A")]
    public void Color_value_validation_canonicalizes_a_six_digit_hex(string input, string expected)
    {
        Assert.Equal(expected, ClothesValidation.ValidateColorValue(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("000000")]
    [InlineData("#FFF")]
    [InlineData("#GGGGGG")]
    [InlineData("#12345678")]
    [InlineData("red")]
    public void Color_value_validation_rejects_non_six_digit_hex(string? input)
    {
        Assert.Throws<ClothesValidationException>(() => ClothesValidation.ValidateColorValue(input));
    }

    [Fact]
    public async Task Seeder_initializes_categories_and_colours_once_with_colour_values()
    {
        await using var fixture = await ClothesFixture.CreateAsync();
        var seeder = new ClothesSeeder(fixture.Database, new CatalogInitializer(fixture.Database, fixture.Clock));
        await seeder.SeedAsync(CancellationToken.None);

        var categories = await fixture.Database.Set<ClothingCategory>().AsNoTracking()
            .OrderBy(category => category.SortOrder).ToListAsync();
        var colors = await fixture.Database.Set<ClothingColor>().AsNoTracking()
            .OrderBy(color => color.SortOrder).ToListAsync();

        Assert.Equal(ClothesCatalog.Categories.Select(seed => seed.Name), categories.Select(category => category.Name));
        Assert.Equal(ClothesCatalog.Colors.Select(seed => seed.Name), colors.Select(color => color.Name));
        Assert.Equal(Enumerable.Range(0, colors.Count), colors.Select(color => color.SortOrder));
        Assert.Equal("TOPS", categories[0].NormalizedName);
        Assert.Equal("#000000", colors[0].ColorValue);
        Assert.All(colors, color => Assert.Matches("^#[0-9A-F]{6}$", color.ColorValue));

        fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddDays(1);
        await seeder.SeedAsync(CancellationToken.None);
        var reseeded = await fixture.Database.Set<ClothingColor>().AsNoTracking()
            .ToDictionaryAsync(color => color.Name, color => color.Id);
        Assert.Equal(colors.ToDictionary(color => color.Name, color => color.Id), reseeded);
    }

    [Fact]
    public async Task Sqlite_persists_a_garment_and_cascades_colour_association_deletion()
    {
        await using var fixture = await ClothesFixture.CreateAsync();
        var references = await fixture.SeedCatalogsAsync();

        var garment = ClothesGarment.Create(
            Values() with { CategoryId = references.CategoryId, ColorIds = references.ColorIds },
            new UserId(1),
            Now);
        fixture.Database.Add(garment);
        await fixture.Database.SaveChangesAsync();
        fixture.Database.ChangeTracker.Clear();

        Assert.Equal(references.ColorIds.Count, await fixture.Database.Set<ClothesGarmentColor>().CountAsync());

        var stored = await fixture.Database.Set<ClothesGarment>().SingleAsync();
        fixture.Database.Remove(stored);
        await fixture.Database.SaveChangesAsync();

        Assert.Equal(0, await fixture.Database.Set<ClothesGarmentColor>().CountAsync());
        // The colour catalog rows survive the garment deletion.
        Assert.Equal(ClothesCatalog.Colors.Count, await fixture.Database.Set<ClothingColor>().CountAsync());
    }

    [Fact]
    public async Task Sqlite_restricts_deleting_a_colour_that_a_garment_references()
    {
        await using var fixture = await ClothesFixture.CreateAsync();
        var references = await fixture.SeedCatalogsAsync();

        var garment = ClothesGarment.Create(
            Values() with { CategoryId = references.CategoryId, ColorIds = [references.ColorIds[0]] },
            new UserId(1),
            Now);
        fixture.Database.Add(garment);
        await fixture.Database.SaveChangesAsync();
        fixture.Database.ChangeTracker.Clear();

        var referenced = await fixture.Database.Set<ClothingColor>().SingleAsync(color => color.Id == references.ColorIds[0]);
        fixture.Database.Remove(referenced);

        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Database.SaveChangesAsync());
    }

    [Fact]
    public async Task Category_management_protects_the_final_row_and_rejects_a_referenced_category()
    {
        await using var fixture = await ClothesFixture.CreateAsync();
        var references = await fixture.SeedCatalogsAsync();
        var service = new ClothingCategoryManagementService(fixture.Database, fixture.Clock);

        var garment = ClothesGarment.Create(Values() with { CategoryId = references.CategoryId }, new UserId(1), Now);
        fixture.Database.Add(garment);
        await fixture.Database.SaveChangesAsync();
        fixture.Database.ChangeTracker.Clear();

        var referencedError = await Assert.ThrowsAsync<ApiProblemException>(() =>
            service.DeleteAsync(references.CategoryId, CancellationToken.None));
        Assert.Equal("clothes.category.referenced", referencedError.Code.Value);

        // Remove the garment, then delete every category but the last to reach the guard.
        fixture.Database.Remove(await fixture.Database.Set<ClothesGarment>().SingleAsync());
        await fixture.Database.SaveChangesAsync();
        var ids = await fixture.Database.Set<ClothingCategory>().OrderBy(category => category.SortOrder).Select(category => category.Id).ToListAsync();
        for (var index = 0; index < ids.Count - 1; index++)
        {
            await service.DeleteAsync(ids[index], CancellationToken.None);
        }

        var finalError = await Assert.ThrowsAsync<ApiProblemException>(() =>
            service.DeleteAsync(ids[^1], CancellationToken.None));
        Assert.Equal("clothes.category.required_not_empty", finalError.Code.Value);
    }

    [Fact]
    public async Task Colour_management_creates_with_a_value_and_rejects_a_referenced_colour()
    {
        await using var fixture = await ClothesFixture.CreateAsync();
        var references = await fixture.SeedCatalogsAsync();
        var service = new ClothingColorManagementService(fixture.Database, fixture.Clock);

        var created = await service.CreateAsync(new CatalogItemRequest(" Olive ", " #708238 "), new UserId(1), CancellationToken.None);
        Assert.Equal("Olive", created.Name);
        Assert.Equal("#708238", created.ColorValue);
        Assert.Equal(ClothesCatalog.Colors.Count, created.SortOrder);

        await Assert.ThrowsAsync<ApiProblemException>(() =>
            service.CreateAsync(new CatalogItemRequest("black", "#000000"), new UserId(1), CancellationToken.None));

        var garment = ClothesGarment.Create(
            Values() with { CategoryId = references.CategoryId, ColorIds = [references.ColorIds[0]] }, new UserId(1), Now);
        fixture.Database.Add(garment);
        await fixture.Database.SaveChangesAsync();
        fixture.Database.ChangeTracker.Clear();

        var referencedError = await Assert.ThrowsAsync<ApiProblemException>(() =>
            service.DeleteAsync(references.ColorIds[0], CancellationToken.None));
        Assert.Equal("clothes.color.referenced", referencedError.Code.Value);
    }

    private static ClothesGarmentValues Values() => new(
        "Example",
        CategoryId: 1,
        ClothesGarmentStatus.Active,
        Size: null,
        ColorIds: [],
        WashingCare: null,
        DryingCare: null,
        IroningCare: null,
        DryCleaningCare: null,
        Notes: null,
        RecordVisibility.Public);

    private sealed record Catalogs(int CategoryId, IReadOnlyList<int> ColorIds);

    private sealed class ClothesFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private ClothesFixture(SqliteConnection connection, SegarisDbContext database, MutableClock clock)
        {
            this.connection = connection;
            Database = database;
            Clock = clock;
        }

        public SegarisDbContext Database { get; }
        public MutableClock Clock { get; }

        public static async Task<ClothesFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<SegarisDbContext>()
                .UseSqlite(connection)
                .EnableServiceProviderCaching(false)
                .Options;
            var database = new SegarisDbContext(options,
                [new IdentityModelContributor(), new ConfigurationModelContributor(), new ClothesModelContributor()]);
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
            return new ClothesFixture(connection, database, new MutableClock { UtcNow = Now });
        }

        public async Task<Catalogs> SeedCatalogsAsync()
        {
            await new ClothesSeeder(Database, new CatalogInitializer(Database, Clock)).SeedAsync(CancellationToken.None);
            var categoryId = await Database.Set<ClothingCategory>()
                .OrderBy(category => category.SortOrder).Select(category => category.Id).FirstAsync();
            var colorIds = await Database.Set<ClothingColor>()
                .OrderBy(color => color.SortOrder).Select(color => color.Id).Take(2).ToListAsync();
            return new Catalogs(categoryId, colorIds);
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
