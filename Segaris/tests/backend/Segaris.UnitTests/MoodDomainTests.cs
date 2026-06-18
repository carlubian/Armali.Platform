using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Persistence;
using Segaris.Api.Modules.Mood.Contracts;
using Segaris.Api.Modules.Mood.Domain;
using Segaris.Api.Modules.Mood.Persistence;
using Segaris.Persistence;
using Segaris.Shared.Identity;

namespace Segaris.UnitTests;

public sealed class MoodDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Entry_trims_notes_stamps_owner_and_leaves_updated_unset()
    {
        var entry = MoodEntry.Create(Values() with { Notes = "  steady morning  " }, new UserId(1), Now);

        Assert.Equal(new DateOnly(2026, 6, 18), entry.EntryDate);
        Assert.Equal(3, entry.Score);
        Assert.Equal("steady morning", entry.Notes);
        Assert.Equal(1, entry.CreatedBy);
        Assert.Equal(Now, entry.CreatedAt);
        Assert.Null(entry.UpdatedAt);
        Assert.Null(entry.UpdatedBy);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Entry_rejects_scores_outside_the_frozen_range(int score)
    {
        var error = Assert.Throws<MoodValidationException>(() =>
            MoodEntry.Create(Values() with { Score = score }, new UserId(1), Now));

        Assert.Equal(MoodValidationReason.Score, error.Reason);
    }

    [Fact]
    public void Entry_rejects_overlong_notes_and_normalizes_blank_notes_to_null()
    {
        var blank = MoodEntry.Create(Values() with { Notes = "   " }, new UserId(1), Now);
        Assert.Null(blank.Notes);

        var notes = new string('a', MoodDefaults.NotesMaxLength + 1);
        var error = Assert.Throws<MoodValidationException>(() =>
            MoodEntry.Create(Values() with { Notes = notes }, new UserId(1), Now));
        Assert.Equal(MoodValidationReason.Notes, error.Reason);
    }

    [Fact]
    public void Entry_accepts_any_civil_date_without_time_of_day_boundary()
    {
        var min = MoodEntry.Create(Values() with { EntryDate = DateOnly.MinValue }, new UserId(1), Now);
        var max = MoodEntry.Create(Values() with { EntryDate = DateOnly.MaxValue }, new UserId(1), Now);

        Assert.Equal(DateOnly.MinValue, min.EntryDate);
        Assert.Equal(DateOnly.MaxValue, max.EntryDate);
    }

    [Fact]
    public void Entry_rejects_unknown_criteria_values()
    {
        var error = Assert.Throws<MoodValidationException>(() =>
            MoodEntry.Create(Values() with { Energy = (MoodEnergy)99 }, new UserId(1), Now));

        Assert.Equal(MoodValidationReason.Criteria, error.Reason);
    }

    [Fact]
    public void Entry_update_revalidates_and_stamps_modification()
    {
        var entry = MoodEntry.Create(Values(), new UserId(1), Now);

        entry.Update(
            Values() with
            {
                EntryDate = new DateOnly(2026, 6, 19),
                Score = 5,
                Energy = MoodEnergy.High,
                Notes = "updated",
            },
            new UserId(2),
            Now.AddHours(1));

        Assert.Equal(new DateOnly(2026, 6, 19), entry.EntryDate);
        Assert.Equal(5, entry.Score);
        Assert.Equal(MoodEnergy.High, entry.Energy);
        Assert.Equal("updated", entry.Notes);
        Assert.Equal(2, entry.UpdatedBy);
        Assert.Equal(Now.AddHours(1), entry.UpdatedAt);
    }

    [Fact]
    public void Derived_emotion_matrix_covers_every_combination_once()
    {
        var allCombinations =
            from energy in Enum.GetValues<MoodEnergy>()
            from alignment in Enum.GetValues<MoodAlignment>()
            from direction in Enum.GetValues<MoodDirection>()
            from source in Enum.GetValues<MoodSource>()
            select new MoodCriteriaCombination(energy, alignment, direction, source);

        Assert.Equal(MoodCriteriaCatalog.DerivedEmotionCombinationCount, MoodDerivedEmotionMatrix.All.Count);
        Assert.Equal(MoodCriteriaCatalog.DerivedEmotionCombinationCount, MoodDerivedEmotionMatrix.All.Keys.Distinct().Count());
        Assert.Empty(allCombinations.Except(MoodDerivedEmotionMatrix.All.Keys));
    }

    [Theory]
    [InlineData((int)MoodEnergy.High, (int)MoodAlignment.Positive, (int)MoodDirection.Harmony, (int)MoodSource.Internal, "Happy")]
    [InlineData((int)MoodEnergy.High, (int)MoodAlignment.Negative, (int)MoodDirection.Offensive, (int)MoodSource.External, "Angry")]
    [InlineData((int)MoodEnergy.Low, (int)MoodAlignment.Medium, (int)MoodDirection.Harmony, (int)MoodSource.Internal, "Self-Care")]
    [InlineData((int)MoodEnergy.Low, (int)MoodAlignment.Negative, (int)MoodDirection.Stability, (int)MoodSource.External, "Burnout")]
    public void Derived_emotion_matrix_returns_stable_codes(
        int energy,
        int alignment,
        int direction,
        int source,
        string expected)
    {
        Assert.Equal(
            expected,
            MoodDerivedEmotionMatrix.Resolve(
                (MoodEnergy)energy,
                (MoodAlignment)alignment,
                (MoodDirection)direction,
                (MoodSource)source));
    }

    [Fact]
    public void Derived_emotion_matrix_rejects_impossible_combinations()
    {
        var error = Assert.Throws<MoodValidationException>(() =>
            MoodDerivedEmotionMatrix.Resolve((MoodEnergy)99, MoodAlignment.Positive, MoodDirection.Harmony, MoodSource.Internal));

        Assert.Equal(MoodValidationReason.Criteria, error.Reason);
    }

    [Fact]
    public async Task Sqlite_persists_entries_and_orders_same_day_by_insertion_identifier()
    {
        await using var fixture = await MoodFixture.CreateAsync();
        fixture.Database.Add(MoodEntry.Create(
            Values() with { EntryDate = new DateOnly(2026, 6, 18), Notes = "first" },
            new UserId(1),
            Now));
        fixture.Database.Add(MoodEntry.Create(
            Values() with { EntryDate = new DateOnly(2026, 6, 18), Notes = "second" },
            new UserId(1),
            Now.AddMinutes(1)));
        await fixture.Database.SaveChangesAsync();
        fixture.Database.ChangeTracker.Clear();

        var stored = await fixture.Database.Set<MoodEntry>()
            .OrderBy(entry => entry.EntryDate)
            .ThenBy(entry => entry.Id)
            .ToListAsync();

        Assert.Equal(["first", "second"], stored.Select(entry => entry.Notes));
        Assert.All(stored, entry => Assert.Equal(1, entry.CreatedBy));
    }

    private static MoodEntryValues Values() => new(
        EntryDate: new DateOnly(2026, 6, 18),
        Score: 3,
        Energy: MoodEnergy.Medium,
        Alignment: MoodAlignment.Positive,
        Direction: MoodDirection.Harmony,
        Source: MoodSource.Internal,
        Notes: null);

    private sealed class MoodFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private MoodFixture(SqliteConnection connection, SegarisDbContext database)
        {
            this.connection = connection;
            Database = database;
        }

        public SegarisDbContext Database { get; }

        public static async Task<MoodFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<SegarisDbContext>()
                .UseSqlite(connection)
                .EnableServiceProviderCaching(false)
                .Options;
            var database = new SegarisDbContext(options, [new IdentityModelContributor(), new MoodModelContributor()]);
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
            return new MoodFixture(connection, database);
        }

        public async ValueTask DisposeAsync()
        {
            await Database.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
