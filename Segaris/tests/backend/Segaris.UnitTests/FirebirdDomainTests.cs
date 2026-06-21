using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Configuration.Seeding;
using Segaris.Api.Modules.Firebird.Domain;
using Segaris.Api.Modules.Firebird.Persistence;
using Segaris.Api.Modules.Firebird.Seeding;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Persistence;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.UnitTests;

public sealed class FirebirdDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 21, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Person_trims_values_validates_birthday_and_stamps_audit()
    {
        var person = Person.Create(
            new PersonValues("  Ada Lovelace  ", 4, PersonStatus.Active, 12, 10, "  Mathematician  ", RecordVisibility.Public),
            new UserId(1),
            Now);

        Assert.Equal("Ada Lovelace", person.Name);
        Assert.Equal(4, person.CategoryId);
        Assert.Equal(PersonStatus.Active, person.Status);
        Assert.Equal(12, person.BirthdayMonth);
        Assert.Equal(10, person.BirthdayDay);
        Assert.Equal("Mathematician", person.Notes);
        Assert.Equal(RecordVisibility.Public, person.Visibility);
        Assert.Equal(1, person.CreatedBy);
        Assert.Equal(Now, person.CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Person_rejects_a_blank_name(string name)
    {
        Assert.Throws<FirebirdValidationException>(() =>
            Person.Create(PersonValues() with { Name = name }, new UserId(1), Now));
    }

    [Fact]
    public void Person_rejects_invalid_catalog_status_visibility_and_partial_birthday()
    {
        Assert.Throws<FirebirdValidationException>(() =>
            Person.Create(PersonValues() with { CategoryId = 0 }, new UserId(1), Now));
        Assert.Throws<FirebirdValidationException>(() =>
            Person.Create(PersonValues() with { Status = (PersonStatus)42 }, new UserId(1), Now));
        Assert.Throws<FirebirdValidationException>(() =>
            Person.Create(PersonValues() with { Visibility = (RecordVisibility)42 }, new UserId(1), Now));
        Assert.Throws<ArgumentException>(() =>
            Person.Create(PersonValues() with { BirthdayMonth = 2, BirthdayDay = null }, new UserId(1), Now));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Person.Create(PersonValues() with { BirthdayMonth = 2, BirthdayDay = 30 }, new UserId(1), Now));
    }

    [Fact]
    public void Only_the_creator_may_change_person_visibility()
    {
        var person = Person.Create(PersonValues(), new UserId(1), Now);

        var forbidden = Assert.Throws<FirebirdValidationException>(() =>
            person.Update(PersonValues() with { Visibility = RecordVisibility.Private }, new UserId(2), Now));

        Assert.Equal(FirebirdValidationReason.VisibilityForbidden, forbidden.Reason);
    }

    [Fact]
    public void Username_and_interaction_validate_required_fields_and_future_dates()
    {
        var username = Username.Create(
            7,
            new UsernameValues(3, "  @ada  ", "  handle  "),
            new UserId(1),
            Now);
        Assert.Equal("@ada", username.Handle);
        Assert.Equal("handle", username.Notes);

        Assert.Throws<FirebirdValidationException>(() =>
            Username.Create(7, new UsernameValues(0, "@ada", null), new UserId(1), Now));
        Assert.Throws<FirebirdValidationException>(() =>
            Username.Create(7, new UsernameValues(3, "   ", null), new UserId(1), Now));

        var interaction = Interaction.Create(
            7,
            new InteractionValues(new DateOnly(2026, 6, 20), "  Met at dinner  "),
            new UserId(1),
            Now,
            new DateOnly(2026, 6, 21));
        Assert.Equal(new DateOnly(2026, 6, 20), interaction.Date);
        Assert.Equal("Met at dinner", interaction.Description);

        Assert.Throws<FirebirdValidationException>(() =>
            Interaction.Create(7, new InteractionValues(new DateOnly(2026, 6, 22), "Future"), new UserId(1), Now, new DateOnly(2026, 6, 21)));
    }

    [Fact]
    public async Task Sqlite_persists_people_with_usernames_and_interactions_and_cascades_on_deletion()
    {
        await using var fixture = await FirebirdFixture.CreateAsync();
        var category = new PersonCategory { Name = "Friend", NormalizedName = "FRIEND", SortOrder = 0, CreatedAt = Now, UpdatedAt = Now };
        var platform = new UsernamePlatform { Name = "Email", NormalizedName = "EMAIL", SortOrder = 0, CreatedAt = Now, UpdatedAt = Now };
        fixture.Database.AddRange(category, platform);
        await fixture.Database.SaveChangesAsync();

        var person = Person.Create(PersonValues() with { CategoryId = category.Id }, new UserId(1), Now);
        fixture.Database.Add(person);
        await fixture.Database.SaveChangesAsync();

        fixture.Database.Add(Username.Create(person.Id, new UsernameValues(platform.Id, "ada@example.test", null), new UserId(1), Now));
        fixture.Database.Add(Interaction.Create(person.Id, new InteractionValues(new DateOnly(2026, 6, 20), "Dinner"), new UserId(1), Now, new DateOnly(2026, 6, 21)));
        await fixture.Database.SaveChangesAsync();
        fixture.Database.ChangeTracker.Clear();

        Assert.Equal(1, await fixture.Database.Set<Username>().CountAsync(username => username.PersonId == person.Id));
        Assert.Equal(1, await fixture.Database.Set<Interaction>().CountAsync(interaction => interaction.PersonId == person.Id));

        var stored = await fixture.Database.Set<Person>().SingleAsync();
        fixture.Database.Remove(stored);
        await fixture.Database.SaveChangesAsync();
        fixture.Database.ChangeTracker.Clear();

        Assert.Equal(0, await fixture.Database.Set<Username>().CountAsync());
        Assert.Equal(0, await fixture.Database.Set<Interaction>().CountAsync());
        Assert.Equal(1, await fixture.Database.Set<PersonCategory>().CountAsync());
        Assert.Equal(1, await fixture.Database.Set<UsernamePlatform>().CountAsync());
    }

    [Fact]
    public async Task Sqlite_enforces_catalog_uniqueness_references_and_birthday_check()
    {
        await using var fixture = await FirebirdFixture.CreateAsync();
        var category = new PersonCategory { Name = "Friend", NormalizedName = "FRIEND", SortOrder = 0, CreatedAt = Now, UpdatedAt = Now };
        fixture.Database.Add(category);
        await fixture.Database.SaveChangesAsync();

        var person = Person.Create(PersonValues() with { CategoryId = category.Id }, new UserId(1), Now);
        fixture.Database.Add(person);
        await fixture.Database.SaveChangesAsync();
        fixture.Database.ChangeTracker.Clear();

        fixture.Database.Add(new PersonCategory { Name = "friend", NormalizedName = "FRIEND", SortOrder = 1, CreatedAt = Now, UpdatedAt = Now });
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Database.SaveChangesAsync());
        fixture.Database.ChangeTracker.Clear();

        var referenced = await fixture.Database.Set<PersonCategory>().SingleAsync(value => value.Id == category.Id);
        fixture.Database.Remove(referenced);
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Database.SaveChangesAsync());
    }

    [Fact]
    public async Task Firebird_seeder_initializes_catalogues_once()
    {
        await using var fixture = await FirebirdFixture.CreateAsync();
        var seeder = new FirebirdSeeder(fixture.Database, new CatalogInitializer(fixture.Database, fixture.Clock));

        await seeder.SeedAsync(CancellationToken.None);
        Assert.Equal(FirebirdDefaults.InitialCategories, await fixture.Database.Set<PersonCategory>().OrderBy(value => value.SortOrder).Select(value => value.Name).ToArrayAsync());
        Assert.Equal(FirebirdDefaults.InitialUsernamePlatforms, await fixture.Database.Set<UsernamePlatform>().OrderBy(value => value.SortOrder).Select(value => value.Name).ToArrayAsync());

        var category = await fixture.Database.Set<PersonCategory>().SingleAsync(value => value.Name == "Friend");
        fixture.Database.Remove(category);
        await fixture.Database.SaveChangesAsync();

        fixture.Clock.UtcNow = Now.AddDays(1);
        await seeder.SeedAsync(CancellationToken.None);

        Assert.DoesNotContain("Friend", await fixture.Database.Set<PersonCategory>().Select(value => value.Name).ToArrayAsync());
        Assert.Equal(FirebirdDefaults.InitialCategories.Count - 1, await fixture.Database.Set<PersonCategory>().CountAsync());
        Assert.Equal(FirebirdDefaults.InitialUsernamePlatforms.Count, await fixture.Database.Set<UsernamePlatform>().CountAsync());
    }

    private static PersonValues PersonValues() => new(
        Name: "Ada Lovelace",
        CategoryId: 1,
        Status: PersonStatus.Unknown,
        BirthdayMonth: null,
        BirthdayDay: null,
        Notes: null,
        Visibility: RecordVisibility.Public);

    private sealed class FirebirdFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private FirebirdFixture(SqliteConnection connection, SegarisDbContext database, MutableClock clock)
        {
            this.connection = connection;
            Database = database;
            Clock = clock;
        }

        public SegarisDbContext Database { get; }

        public MutableClock Clock { get; }

        public static async Task<FirebirdFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<SegarisDbContext>()
                .UseSqlite(connection)
                .EnableServiceProviderCaching(false)
                .Options;
            var database = new SegarisDbContext(
                options,
                [new IdentityModelContributor(), new ConfigurationModelContributor(), new FirebirdModelContributor()]);
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
            return new FirebirdFixture(connection, database, new MutableClock { UtcNow = Now });
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
