using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Calendar.Domain;
using Segaris.Api.Modules.Calendar.Persistence;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Persistence;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.UnitTests;

public sealed class CalendarDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 24, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Daily_note_trims_title_and_body_and_stamps_audit()
    {
        var note = CalendarDailyNote.Create(
            Values() with { Title = "  Shopping  ", Body = "  Buy milk  " },
            new UserId(1),
            Now);

        Assert.Equal(new DateOnly(2026, 6, 24), note.Date);
        Assert.Equal("Shopping", note.Title);
        Assert.Equal("Buy milk", note.Body);
        Assert.Equal(RecordVisibility.Private, note.Visibility);
        Assert.Equal(1, note.CreatedBy);
        Assert.Equal(1, note.UpdatedBy);
        Assert.Equal(Now, note.CreatedAt);
        Assert.Equal(Now, note.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Daily_note_requires_body(string? body)
    {
        var error = Assert.Throws<CalendarValidationException>(() =>
            CalendarDailyNote.Create(Values() with { Body = body }, new UserId(1), Now));

        Assert.Equal(CalendarValidationReason.Body, error.Reason);
    }

    [Fact]
    public void Daily_note_rejects_overlong_title_and_body()
    {
        var title = new string('a', CalendarDefaults.TitleMaximumLength + 1);
        var body = new string('a', CalendarDefaults.BodyMaximumLength + 1);

        var titleError = Assert.Throws<CalendarValidationException>(() =>
            CalendarDailyNote.Create(Values() with { Title = title }, new UserId(1), Now));
        var bodyError = Assert.Throws<CalendarValidationException>(() =>
            CalendarDailyNote.Create(Values() with { Body = body }, new UserId(1), Now));

        Assert.Equal(CalendarValidationReason.Title, titleError.Reason);
        Assert.Equal(CalendarValidationReason.Body, bodyError.Reason);
    }

    [Fact]
    public void Daily_note_normalizes_blank_title_to_absent()
    {
        var note = CalendarDailyNote.Create(Values() with { Title = "   " }, new UserId(1), Now);

        Assert.Null(note.Title);
    }

    [Fact]
    public void Only_creator_may_change_visibility()
    {
        var note = CalendarDailyNote.Create(Values(), new UserId(1), Now);

        var forbidden = Assert.Throws<CalendarValidationException>(() =>
            note.Update(Values() with { Visibility = RecordVisibility.Public }, new UserId(2), Now.AddMinutes(1)));
        Assert.Equal(CalendarValidationReason.VisibilityForbidden, forbidden.Reason);

        note.Update(Values() with { Visibility = RecordVisibility.Public }, new UserId(1), Now.AddMinutes(2));
        Assert.Equal(RecordVisibility.Public, note.Visibility);
    }

    [Fact]
    public void Collaborator_may_edit_public_note_without_changing_visibility()
    {
        var note = CalendarDailyNote.Create(Values() with { Visibility = RecordVisibility.Public }, new UserId(1), Now);

        note.Update(Values() with { Body = "Edited", Visibility = RecordVisibility.Public }, new UserId(2), Now.AddMinutes(1));

        Assert.Equal("Edited", note.Body);
        Assert.Equal(2, note.UpdatedBy);
        Assert.Equal(RecordVisibility.Public, note.Visibility);
    }

    [Fact]
    public async Task Sqlite_persists_notes_and_orders_same_day_by_identifier()
    {
        await using var fixture = await CalendarFixture.CreateAsync();
        fixture.Database.Add(CalendarDailyNote.Create(
            Values() with { Body = "first" },
            new UserId(1),
            Now));
        fixture.Database.Add(CalendarDailyNote.Create(
            Values() with { Body = "second" },
            new UserId(1),
            Now.AddMinutes(1)));
        await fixture.Database.SaveChangesAsync();
        fixture.Database.ChangeTracker.Clear();

        var stored = await fixture.Database.Set<CalendarDailyNote>()
            .OrderBy(note => note.Date)
            .ThenBy(note => note.Id)
            .ToListAsync();

        Assert.Equal(["first", "second"], stored.Select(note => note.Body));
        Assert.All(stored, note => Assert.Equal(RecordVisibility.Private, note.Visibility));
    }

    private static CalendarDailyNoteValues Values() => new(
        Date: new DateOnly(2026, 6, 24),
        Title: null,
        Body: "Body",
        Visibility: RecordVisibility.Private);

    private sealed class CalendarFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private CalendarFixture(SqliteConnection connection, SegarisDbContext database)
        {
            this.connection = connection;
            Database = database;
        }

        public SegarisDbContext Database { get; }

        public static async Task<CalendarFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<SegarisDbContext>()
                .UseSqlite(connection)
                .EnableServiceProviderCaching(false)
                .Options;
            var database = new SegarisDbContext(options, [new IdentityModelContributor(), new CalendarModelContributor()]);
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
            return new CalendarFixture(connection, database);
        }

        public async ValueTask DisposeAsync()
        {
            await Database.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
