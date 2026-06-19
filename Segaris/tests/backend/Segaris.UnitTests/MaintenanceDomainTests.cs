using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Configuration.Seeding;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Persistence;
using Segaris.Api.Modules.Maintenance.Domain;
using Segaris.Api.Modules.Maintenance.Mutations;
using Segaris.Api.Modules.Maintenance.Persistence;
using Segaris.Api.Modules.Maintenance.Seeding;
using Segaris.Api.Platform.Api;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.UnitTests;

public sealed class MaintenanceDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 19, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 6, 19);

    [Fact]
    public void Task_trims_title_and_notes_and_stamps_audit()
    {
        var task = MaintenanceTask.Create(
            Values() with { Title = "  Replace filter  ", Notes = "  worn  " }, new UserId(1), Now, Today);

        Assert.Equal("Replace filter", task.Title);
        Assert.Equal("worn", task.Notes);
        Assert.Equal(MaintenanceStatus.Pending, task.Status);
        Assert.Equal(MaintenancePriority.Medium, task.Priority);
        Assert.Equal(RecordVisibility.Public, task.Visibility);
        Assert.Equal(1, task.CreatedBy);
        Assert.Equal(1, task.UpdatedBy);
        Assert.Equal(Now, task.CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Task_rejects_a_blank_title(string title)
    {
        Assert.Throws<MaintenanceValidationException>(() =>
            MaintenanceTask.Create(Values() with { Title = title }, new UserId(1), Now, Today));
    }

    [Fact]
    public void Task_rejects_an_overlong_title()
    {
        var title = new string('a', MaintenanceValidation.TitleMaximumLength + 1);
        Assert.Throws<MaintenanceValidationException>(() =>
            MaintenanceTask.Create(Values() with { Title = title }, new UserId(1), Now, Today));
    }

    [Fact]
    public void Task_treats_blank_notes_as_absent()
    {
        var task = MaintenanceTask.Create(Values() with { Notes = "   " }, new UserId(1), Now, Today);
        Assert.Null(task.Notes);
    }

    [Fact]
    public void Task_rejects_overlong_notes()
    {
        var notes = new string('a', MaintenanceValidation.NotesMaximumLength + 1);
        Assert.Throws<MaintenanceValidationException>(() =>
            MaintenanceTask.Create(Values() with { Notes = notes }, new UserId(1), Now, Today));
    }

    [Fact]
    public void Task_rejects_unknown_status_priority_or_visibility()
    {
        Assert.Throws<MaintenanceValidationException>(() =>
            MaintenanceTask.Create(Values() with { Status = (MaintenanceStatus)42 }, new UserId(1), Now, Today));
        Assert.Throws<MaintenanceValidationException>(() =>
            MaintenanceTask.Create(Values() with { Priority = (MaintenancePriority)42 }, new UserId(1), Now, Today));
        Assert.Throws<MaintenanceValidationException>(() =>
            MaintenanceTask.Create(Values() with { Visibility = (RecordVisibility)42 }, new UserId(1), Now, Today));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Task_rejects_nonpositive_type_identifier(int value)
    {
        Assert.Throws<MaintenanceValidationException>(() =>
            MaintenanceTask.Create(Values() with { MaintenanceTypeId = value }, new UserId(1), Now, Today));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Task_rejects_a_nonpositive_asset_identifier(int value)
    {
        Assert.Throws<MaintenanceValidationException>(() =>
            MaintenanceTask.Create(Values() with { AssetId = value }, new UserId(1), Now, Today));
    }

    [Fact]
    public void Task_keeps_an_optional_due_date_without_an_artificial_boundary()
    {
        var task = MaintenanceTask.Create(
            Values() with { DueDate = new DateOnly(1990, 1, 1) }, new UserId(1), Now, Today);
        Assert.Equal(new DateOnly(1990, 1, 1), task.DueDate);
    }

    [Fact]
    public void Pending_task_has_no_completion_date()
    {
        var task = MaintenanceTask.Create(Values(), new UserId(1), Now, Today);
        Assert.Null(task.CompletedDate);
    }

    [Fact]
    public void Creating_a_completed_task_stamps_the_completion_date()
    {
        var task = MaintenanceTask.Create(
            Values() with { Status = MaintenanceStatus.Completed }, new UserId(1), Now, Today);
        Assert.Equal(Today, task.CompletedDate);
    }

    [Fact]
    public void Entering_completed_sets_the_completion_date()
    {
        var task = MaintenanceTask.Create(Values(), new UserId(1), Now, Today);
        var completionDay = new DateOnly(2026, 7, 1);

        task.Update(Values() with { Status = MaintenanceStatus.Completed }, new UserId(1), Now, completionDay);

        Assert.Equal(completionDay, task.CompletedDate);
    }

    [Fact]
    public void Staying_completed_preserves_the_original_completion_date()
    {
        var task = MaintenanceTask.Create(
            Values() with { Status = MaintenanceStatus.Completed }, new UserId(1), Now, Today);

        task.Update(
            Values() with { Status = MaintenanceStatus.Completed, Title = "Renamed" },
            new UserId(1),
            Now,
            new DateOnly(2026, 8, 15));

        Assert.Equal(Today, task.CompletedDate);
        Assert.Equal("Renamed", task.Title);
    }

    [Fact]
    public void Leaving_completed_for_pending_clears_the_completion_date() =>
        AssertLeavingCompletedClearsDate(MaintenanceStatus.Pending);

    [Fact]
    public void Leaving_completed_for_in_progress_clears_the_completion_date() =>
        AssertLeavingCompletedClearsDate(MaintenanceStatus.InProgress);

    [Fact]
    public void Leaving_completed_for_cancelled_clears_the_completion_date() =>
        AssertLeavingCompletedClearsDate(MaintenanceStatus.Cancelled);

    private static void AssertLeavingCompletedClearsDate(MaintenanceStatus next)
    {
        var task = MaintenanceTask.Create(
            Values() with { Status = MaintenanceStatus.Completed }, new UserId(1), Now, Today);

        task.Update(Values() with { Status = next }, new UserId(1), Now, new DateOnly(2026, 9, 1));

        Assert.Null(task.CompletedDate);
    }

    [Fact]
    public void Only_the_creator_may_change_visibility()
    {
        var task = MaintenanceTask.Create(Values(), new UserId(1), Now, Today);

        var forbidden = Assert.Throws<MaintenanceValidationException>(() =>
            task.Update(Values() with { Visibility = RecordVisibility.Private }, new UserId(2), Now, Today));
        Assert.Equal(MaintenanceValidationReason.VisibilityForbidden, forbidden.Reason);

        task.Update(Values() with { Visibility = RecordVisibility.Private }, new UserId(1), Now, Today);
        Assert.Equal(RecordVisibility.Private, task.Visibility);
    }

    [Fact]
    public void A_collaborator_may_edit_a_public_task_without_changing_visibility()
    {
        var task = MaintenanceTask.Create(Values(), new UserId(1), Now, Today);

        task.Update(Values() with { Title = "Edited by collaborator" }, new UserId(2), Now, Today);

        Assert.Equal("Edited by collaborator", task.Title);
        Assert.Equal(2, task.UpdatedBy);
    }

    [Fact]
    public void ReplaceType_updates_the_reference_and_stamps_modification()
    {
        var task = MaintenanceTask.Create(Values(), new UserId(1), Now, Today);

        task.ReplaceType(20, new UserId(2), Now.AddHours(1));

        Assert.Equal(20, task.MaintenanceTypeId);
        Assert.Equal(2, task.UpdatedBy);
        Assert.Equal(Now.AddHours(1), task.UpdatedAt);
    }

    [Fact]
    public async Task Seeder_initializes_types_once_in_declaration_order()
    {
        await using var fixture = await MaintenanceFixture.CreateAsync();
        var seeder = new MaintenanceSeeder(fixture.Database, new CatalogInitializer(fixture.Database, fixture.Clock));
        await seeder.SeedAsync(CancellationToken.None);

        var types = await fixture.Database.Set<MaintenanceType>().AsNoTracking()
            .OrderBy(type => type.SortOrder).ToListAsync();
        Assert.Equal(MaintenanceDefaults.InitialTypes, types.Select(type => type.Name));
        Assert.Equal(Enumerable.Range(0, types.Count), types.Select(type => type.SortOrder));
        Assert.Equal("REPAIR", types[0].NormalizedName);

        fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddDays(1);
        await seeder.SeedAsync(CancellationToken.None);
        var reseeded = await fixture.Database.Set<MaintenanceType>().AsNoTracking()
            .ToDictionaryAsync(type => type.Name, type => type.Id);
        Assert.Equal(types.ToDictionary(type => type.Name, type => type.Id), reseeded);
    }

    [Fact]
    public async Task Sqlite_persists_a_task_with_its_type_reference()
    {
        await using var fixture = await MaintenanceFixture.CreateAsync();
        var typeId = await fixture.SeedTypesAsync();

        var task = MaintenanceTask.Create(
            Values() with { MaintenanceTypeId = typeId, DueDate = new DateOnly(2026, 7, 1) },
            new UserId(1),
            Now,
            Today);
        fixture.Database.Add(task);
        await fixture.Database.SaveChangesAsync();
        fixture.Database.ChangeTracker.Clear();

        var stored = await fixture.Database.Set<MaintenanceTask>().SingleAsync();
        Assert.Equal(typeId, stored.MaintenanceTypeId);
        Assert.Equal(new DateOnly(2026, 7, 1), stored.DueDate);
    }

    [Fact]
    public async Task Type_management_creates_renames_and_rejects_duplicate_names()
    {
        await using var fixture = await MaintenanceFixture.CreateAsync();
        await fixture.SeedTypesAsync();
        var service = new MaintenanceTypeManagementService(fixture.Database, fixture.Clock);

        var created = await service.CreateAsync(new CatalogItemRequest("  Calibration  "), new UserId(1), CancellationToken.None);
        Assert.Equal("Calibration", created.Name);
        Assert.Equal(MaintenanceDefaults.InitialTypes.Count, created.SortOrder);

        await Assert.ThrowsAsync<ApiProblemException>(() =>
            service.CreateAsync(new CatalogItemRequest("repair"), new UserId(1), CancellationToken.None));

        var renamed = await service.UpdateAsync(created.Id, new CatalogItemRequest("Adjustment"), new UserId(1), CancellationToken.None);
        Assert.Equal("Adjustment", renamed.Name);
    }

    [Fact]
    public async Task Type_management_protects_the_final_row_and_migrates_references_atomically()
    {
        await using var fixture = await MaintenanceFixture.CreateAsync();
        var typeId = await fixture.SeedTypesAsync();
        var service = new MaintenanceTypeManagementService(fixture.Database, fixture.Clock);

        var replacementId = await fixture.Database.Set<MaintenanceType>()
            .Where(type => type.Id != typeId)
            .Select(type => type.Id).FirstAsync();
        await fixture.SeedTaskAsync(typeId);

        await Assert.ThrowsAsync<ApiProblemException>(() =>
            service.DeleteAsync(typeId, CancellationToken.None));

        fixture.Clock.UtcNow = Now.AddHours(1);
        await service.ReplaceAndDeleteAsync(
            typeId,
            new CatalogReplacementRequest(replacementId, ClearReferences: false, ExchangeRate: null),
            new UserId(2),
            CancellationToken.None);

        fixture.Database.ChangeTracker.Clear();
        var migrated = await fixture.Database.Set<MaintenanceTask>().SingleAsync();
        Assert.Equal(replacementId, migrated.MaintenanceTypeId);
        Assert.Equal(2, migrated.UpdatedBy);
        Assert.False(await fixture.Database.Set<MaintenanceType>().AnyAsync(type => type.Id == typeId));
    }

    [Fact]
    public async Task Type_replacement_rejects_clearing_references()
    {
        await using var fixture = await MaintenanceFixture.CreateAsync();
        var typeId = await fixture.SeedTypesAsync();
        var service = new MaintenanceTypeManagementService(fixture.Database, fixture.Clock);

        await Assert.ThrowsAsync<ApiProblemException>(() =>
            service.ReplaceAndDeleteAsync(
                typeId,
                new CatalogReplacementRequest(ReplacementId: null, ClearReferences: true, ExchangeRate: null),
                new UserId(2),
                CancellationToken.None));
    }

    private static MaintenanceTaskValues Values() => new(
        Title: "Example task",
        MaintenanceTypeId: 1,
        Status: MaintenanceStatus.Pending,
        Priority: MaintenancePriority.Medium,
        DueDate: null,
        Notes: null,
        AssetId: null,
        Visibility: RecordVisibility.Public);

    private sealed class MaintenanceFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private MaintenanceFixture(SqliteConnection connection, SegarisDbContext database, MutableClock clock)
        {
            this.connection = connection;
            Database = database;
            Clock = clock;
        }

        public SegarisDbContext Database { get; }
        public MutableClock Clock { get; }

        public static async Task<MaintenanceFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<SegarisDbContext>()
                .UseSqlite(connection)
                .EnableServiceProviderCaching(false)
                .Options;
            var database = new SegarisDbContext(options,
                [new IdentityModelContributor(), new ConfigurationModelContributor(), new MaintenanceModelContributor()]);
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
            return new MaintenanceFixture(connection, database, clock);
        }

        public async Task<int> SeedTypesAsync()
        {
            await new MaintenanceSeeder(Database, new CatalogInitializer(Database, Clock))
                .SeedAsync(CancellationToken.None);
            return await Database.Set<MaintenanceType>()
                .OrderBy(type => type.SortOrder).Select(type => type.Id).FirstAsync();
        }

        public async Task<int> SeedTaskAsync(int typeId)
        {
            var task = MaintenanceTask.Create(
                Values() with { MaintenanceTypeId = typeId }, new UserId(1), Now, Today);
            Database.Add(task);
            await Database.SaveChangesAsync();
            Database.ChangeTracker.Clear();
            return task.Id;
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
