using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Persistence;
using Segaris.Api.Modules.Wellness.Attention;
using Segaris.Api.Modules.Wellness.Domain;
using Segaris.Api.Modules.Wellness.Persistence;
using Segaris.Persistence;
using Segaris.Shared.Identity;

namespace Segaris.UnitTests;

public sealed class WellnessDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 13, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Task_creation_trims_name_validates_category_and_audit()
    {
        var task = WellnessTask.Create(
            "  Drink water  ",
            WellnessCategory.HealthAndBody,
            2,
            new UserId(1),
            Now);

        Assert.Equal("Drink water", task.Name);
        Assert.Equal(WellnessCategory.HealthAndBody, task.Category);
        Assert.Equal(2, task.SortOrder);
        Assert.Equal(1, task.CreatedBy);
        Assert.Equal(Now, task.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Task_creation_rejects_blank_names(string name)
    {
        Assert.Throws<WellnessValidationException>(() =>
            WellnessTask.Create(name, WellnessCategory.HealthAndBody, 0, new UserId(1), Now));
    }

    [Fact]
    public void Task_creation_rejects_long_names_unknown_categories_and_negative_sort_order()
    {
        Assert.Throws<WellnessValidationException>(() =>
            WellnessTask.Create(new string('a', WellnessDefaults.TaskNameMaximumLength + 1), WellnessCategory.HealthAndBody, 0, new UserId(1), Now));
        Assert.Throws<WellnessValidationException>(() =>
            WellnessTask.Create("Task", (WellnessCategory)42, 0, new UserId(1), Now));
        Assert.Throws<WellnessValidationException>(() =>
            WellnessTask.Create("Task", WellnessCategory.HealthAndBody, -1, new UserId(1), Now));
    }

    [Fact]
    public void Day_validates_score_range_and_stamps_owner()
    {
        var day = WellnessDay.Create(new DateOnly(2026, 7, 13), new UserId(1), Now, 50);

        Assert.Equal(new DateOnly(2026, 7, 13), day.Date);
        Assert.Equal(50, day.Score);
        Assert.Equal(1, day.CreatedBy);

        day.SetScore(null, new UserId(1), Now.AddMinutes(1));
        Assert.Null(day.Score);

        Assert.Throws<WellnessValidationException>(() =>
            WellnessDay.Create(new DateOnly(2026, 7, 13), new UserId(1), Now, -1));
        Assert.Throws<WellnessValidationException>(() =>
            day.SetScore(101, new UserId(1), Now.AddMinutes(2)));
    }

    [Fact]
    public void Day_task_snapshot_copies_catalogue_values_and_completion_changes_only_flag()
    {
        var snapshot = WellnessDayTask.CreateSnapshot(
            10,
            "  Stretch  ",
            WellnessCategory.HealthAndBody,
            0);

        Assert.Equal("Stretch", snapshot.Name);
        Assert.Equal(WellnessCategory.HealthAndBody, snapshot.Category);
        Assert.False(snapshot.Completed);
        Assert.Equal(0, snapshot.Position);

        snapshot.SetCompletion(true);
        Assert.True(snapshot.Completed);
        Assert.Equal("Stretch", snapshot.Name);
    }

    [Fact]
    public async Task Sqlite_persists_wellness_model_enforces_uniqueness_and_snapshot_independence()
    {
        await using var fixture = await WellnessFixture.CreateAsync();
        var task = WellnessTask.Create("Drink water", WellnessCategory.HealthAndBody, 0, new UserId(1), Now);
        fixture.Database.Add(task);
        await fixture.Database.SaveChangesAsync();

        var day = WellnessDay.Create(new DateOnly(2026, 7, 13), new UserId(1), Now, 0);
        fixture.Database.Add(day);
        await fixture.Database.SaveChangesAsync();

        fixture.Database.Add(WellnessDayTask.CreateSnapshot(day.Id, task.Name, task.Category, 0));
        await fixture.Database.SaveChangesAsync();
        fixture.Database.ChangeTracker.Clear();

        var duplicate = WellnessDay.Create(new DateOnly(2026, 7, 13), new UserId(1), Now, 0);
        fixture.Database.Add(duplicate);
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Database.SaveChangesAsync());
        fixture.Database.ChangeTracker.Clear();

        var otherUserDay = WellnessDay.Create(new DateOnly(2026, 7, 13), new UserId(2), Now, 0);
        fixture.Database.Add(otherUserDay);
        await fixture.Database.SaveChangesAsync();

        var storedTask = await fixture.Database.Set<WellnessTask>().SingleAsync();
        fixture.Database.Remove(storedTask);
        await fixture.Database.SaveChangesAsync();

        var snapshot = await fixture.Database.Set<WellnessDayTask>().SingleAsync();
        Assert.Equal("Drink water", snapshot.Name);
        Assert.Equal(WellnessCategory.HealthAndBody, snapshot.Category);

        var storedDay = await fixture.Database.Set<WellnessDay>()
            .SingleAsync(existing => existing.CreatedBy == 1);
        fixture.Database.Remove(storedDay);
        await fixture.Database.SaveChangesAsync();

        Assert.Equal(0, await fixture.Database.Set<WellnessDayTask>().CountAsync());
        Assert.Equal(1, await fixture.Database.Set<WellnessDay>().CountAsync());
    }

    [Fact]
    public async Task Launcher_attention_is_constant_false()
    {
        var contributor = new WellnessAttentionContributor();

        Assert.Equal("wellness", contributor.Module);
        Assert.False(await contributor.RequiresAttentionAsync(CancellationToken.None));
    }

    private sealed class WellnessFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private WellnessFixture(SqliteConnection connection, SegarisDbContext database)
        {
            this.connection = connection;
            Database = database;
        }

        public SegarisDbContext Database { get; }

        public static async Task<WellnessFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<SegarisDbContext>()
                .UseSqlite(connection)
                .EnableServiceProviderCaching(false)
                .Options;
            var database = new SegarisDbContext(
                options,
                [new IdentityModelContributor(), new WellnessModelContributor()]);
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
                UserName = "other",
                NormalizedUserName = "OTHER",
                DisplayName = "Other",
                Language = "en-GB",
                CreatedAt = Now,
            });
            await database.SaveChangesAsync();
            return new WellnessFixture(connection, database);
        }

        public async ValueTask DisposeAsync()
        {
            await Database.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
