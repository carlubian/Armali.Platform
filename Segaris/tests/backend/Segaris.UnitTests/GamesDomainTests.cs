using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Games.Domain;
using Segaris.Api.Modules.Games.Persistence;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Persistence;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.UnitTests;

public sealed class GamesDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 7, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Playthrough_validates_start_date_status_visibility_and_audit()
    {
        var playthrough = Playthrough.Create(
            new PlaythroughValues("  Honour mode  ", 4, 2026, 7, "Active", [], "Private"),
            new UserId(1),
            Now);

        Assert.Equal("Honour mode", playthrough.Name);
        Assert.Equal("HONOUR MODE", playthrough.NormalizedName);
        Assert.Equal(4, playthrough.GameId);
        Assert.Equal((2026, 7), (playthrough.StartYear, playthrough.StartMonth));
        Assert.Equal(PlaythroughStatus.Active, playthrough.Status);
        Assert.Equal(RecordVisibility.Private, playthrough.Visibility);
        Assert.Equal(1, playthrough.CreatedBy);
    }

    [Fact]
    public void Playthrough_rejects_invalid_start_month_year_and_unknown_status()
    {
        Assert.Throws<GamesValidationException>(() =>
            Playthrough.Create(Values() with { StartMonth = 13 }, new UserId(1), Now));
        Assert.Throws<GamesValidationException>(() =>
            Playthrough.Create(Values() with { StartYear = 0 }, new UserId(1), Now));
        Assert.Throws<GamesValidationException>(() =>
            Playthrough.Create(Values() with { Status = "Paused" }, new UserId(1), Now));
    }

    [Fact]
    public void Only_creator_may_change_playthrough_visibility()
    {
        var playthrough = Playthrough.Create(Values(), new UserId(1), Now);

        var exception = Assert.Throws<GamesValidationException>(() =>
            playthrough.Update(Values() with { Visibility = "Private" }, new UserId(2), Now));
        Assert.Equal(GamesValidationReason.VisibilityForbidden, exception.Reason);

        playthrough.Update(Values() with { Visibility = "Private" }, new UserId(1), Now);
        Assert.Equal(RecordVisibility.Private, playthrough.Visibility);
    }

    [Fact]
    public void Section_trims_name_validates_colour_and_repositions()
    {
        var section = Section.Create(1, "  Act I  ", "Purple", 0, new UserId(1), Now);

        Assert.Equal("Act I", section.Name);
        Assert.Equal("ACT I", section.NormalizedName);
        Assert.Equal(SectionColor.Purple, section.Color);
        Assert.Equal(0, section.SortOrder);

        section.Reposition(2, new UserId(2), Now.AddMinutes(1));
        Assert.Equal(2, section.SortOrder);
        Assert.Equal(2, section.UpdatedBy);
    }

    [Fact]
    public void Goal_starts_in_creation_order_and_completion_does_not_move_it()
    {
        var goal = Goal.Create(3, "  Reach the grove  ", 4, new UserId(1), Now);

        Assert.Equal("Reach the grove", goal.Text);
        Assert.False(goal.Completed);
        Assert.Equal(4, goal.Position);

        goal.SetCompletion(true, new UserId(2), Now.AddMinutes(1));
        Assert.True(goal.Completed);
        Assert.Equal(4, goal.Position);
    }

    [Fact]
    public async Task Sqlite_persists_games_model_enforces_uniqueness_and_cascades_owned_children()
    {
        await using var fixture = await GamesFixture.CreateAsync();
        var game = new Game
        {
            Name = "Baldur's Gate 3",
            NormalizedName = "BALDUR'S GATE 3",
            Platform = GamePlatform.PC,
            SortOrder = 0,
            CreatedAt = Now,
            UpdatedAt = Now,
        };
        fixture.Database.Add(game);
        await fixture.Database.SaveChangesAsync();

        var playthrough = Playthrough.Create(Values() with { GameId = game.Id }, new UserId(1), Now);
        fixture.Database.Add(playthrough);
        await fixture.Database.SaveChangesAsync();

        fixture.Database.Add(PlaythroughTag.Create(playthrough.Id, "Story", 0));
        var section = Section.Create(playthrough.Id, "Act I", "Blue", 0, new UserId(1), Now);
        fixture.Database.Add(section);
        await fixture.Database.SaveChangesAsync();
        fixture.Database.Add(Goal.Create(section.Id, "First goal", 0, new UserId(1), Now));
        fixture.Database.Add(Goal.Create(section.Id, "Second goal", 1, new UserId(1), Now));
        await fixture.Database.SaveChangesAsync();
        fixture.Database.ChangeTracker.Clear();

        fixture.Database.Add(PlaythroughTag.Create(playthrough.Id, "story", 1));
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Database.SaveChangesAsync());
        fixture.Database.ChangeTracker.Clear();

        fixture.Database.Add(Section.Create(playthrough.Id, "ACT I", "Green", 1, new UserId(1), Now));
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Database.SaveChangesAsync());
        fixture.Database.ChangeTracker.Clear();

        var stored = await fixture.Database.Set<Playthrough>().SingleAsync();
        fixture.Database.Remove(stored);
        await fixture.Database.SaveChangesAsync();

        Assert.Equal(0, await fixture.Database.Set<PlaythroughTag>().CountAsync());
        Assert.Equal(0, await fixture.Database.Set<Section>().CountAsync());
        Assert.Equal(0, await fixture.Database.Set<Goal>().CountAsync());
        Assert.Equal(1, await fixture.Database.Set<Game>().CountAsync());
    }

    private static PlaythroughValues Values() => new(
        Name: "Honour mode",
        GameId: 1,
        StartYear: 2026,
        StartMonth: 7,
        Status: "Planning",
        Tags: [],
        Visibility: "Public");

    private sealed class GamesFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private GamesFixture(SqliteConnection connection, SegarisDbContext database)
        {
            this.connection = connection;
            Database = database;
        }

        public SegarisDbContext Database { get; }

        public static async Task<GamesFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<SegarisDbContext>()
                .UseSqlite(connection)
                .EnableServiceProviderCaching(false)
                .Options;
            var database = new SegarisDbContext(
                options,
                [new IdentityModelContributor(), new ConfigurationModelContributor(), new GamesModelContributor()]);
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
            return new GamesFixture(connection, database);
        }

        public async ValueTask DisposeAsync()
        {
            await Database.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
