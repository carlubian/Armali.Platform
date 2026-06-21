using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Persistence;
using Segaris.Api.Modules.Processes.Domain;
using Segaris.Api.Modules.Processes.Persistence;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.UnitTests;

public sealed class ProcessesDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 21, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Process_trims_name_and_notes_validates_and_stamps_audit()
    {
        var process = Process.Create(
            new ProcessValues("  Renew passport  ", CategoryId: 4, DueDate: new DateOnly(2026, 7, 1), Notes: "  Soon  ", RecordVisibility.Public),
            new UserId(1),
            Now);

        Assert.Equal("Renew passport", process.Name);
        Assert.Equal(4, process.CategoryId);
        Assert.Equal(new DateOnly(2026, 7, 1), process.DueDate);
        Assert.Equal("Soon", process.Notes);
        Assert.False(process.IsCancelled);
        Assert.Equal(RecordVisibility.Public, process.Visibility);
        Assert.Equal(1, process.CreatedBy);
        Assert.Equal(Now, process.CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Process_rejects_a_blank_name(string name)
    {
        Assert.Throws<ProcessesValidationException>(() =>
            Process.Create(Values() with { Name = name }, new UserId(1), Now));
    }

    [Fact]
    public void Process_rejects_a_non_positive_category_and_unknown_visibility()
    {
        Assert.Throws<ProcessesValidationException>(() =>
            Process.Create(Values() with { CategoryId = 0 }, new UserId(1), Now));
        Assert.Throws<ProcessesValidationException>(() =>
            Process.Create(Values() with { Visibility = (RecordVisibility)42 }, new UserId(1), Now));
    }

    [Fact]
    public void Only_the_creator_may_change_process_visibility()
    {
        var process = Process.Create(Values(), new UserId(1), Now);

        var forbidden = Assert.Throws<ProcessesValidationException>(() =>
            process.Update(Values() with { Visibility = RecordVisibility.Private }, new UserId(2), Now));
        Assert.Equal(ProcessesValidationReason.VisibilityForbidden, forbidden.Reason);

        process.Update(Values() with { Visibility = RecordVisibility.Private }, new UserId(1), Now);
        Assert.Equal(RecordVisibility.Private, process.Visibility);
    }

    [Fact]
    public void Cancel_and_reopen_toggle_the_terminal_override()
    {
        var process = Process.Create(Values(), new UserId(1), Now);

        process.Cancel(new UserId(2), Now.AddMinutes(1));
        Assert.True(process.IsCancelled);
        Assert.Equal(2, process.UpdatedBy);

        process.Reopen(new UserId(1), Now.AddMinutes(2));
        Assert.False(process.IsCancelled);
    }

    [Fact]
    public void Step_starts_pending_trims_description_and_preserves_state_on_update()
    {
        var step = Step.Create(
            7,
            new StepValues("  Gather documents  ", new DateOnly(2026, 6, 1), "  note  ", IsOptional: false),
            sortOrder: 0,
            new UserId(1),
            Now);

        Assert.Equal("Gather documents", step.Description);
        Assert.Equal("note", step.Notes);
        Assert.Equal(StepExecutionState.Pending, step.State);
        Assert.Equal(0, step.SortOrder);

        step.Complete(new UserId(1), Now.AddMinutes(1));
        step.Update(new StepValues("Gather all documents", null, null, IsOptional: true), sortOrder: 1, new UserId(1), Now.AddMinutes(2));

        // The execution state survives a restructure of the editable fields.
        Assert.Equal(StepExecutionState.Completed, step.State);
        Assert.Equal("Gather all documents", step.Description);
        Assert.Equal(1, step.SortOrder);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Step_rejects_a_blank_description(string description)
    {
        Assert.Throws<ProcessesValidationException>(() =>
            Step.Create(1, new StepValues(description, null, null, false), 0, new UserId(1), Now));
    }

    [Fact]
    public void Only_an_optional_step_may_be_skipped()
    {
        var required = Step.Create(1, new StepValues("Required", null, null, IsOptional: false), 0, new UserId(1), Now);
        var notOptional = Assert.Throws<ProcessesValidationException>(() => required.Skip(new UserId(1), Now));
        Assert.Equal(ProcessesValidationReason.StepNotOptional, notOptional.Reason);

        var optional = Step.Create(1, new StepValues("Optional", null, null, IsOptional: true), 1, new UserId(1), Now);
        optional.Skip(new UserId(1), Now);
        Assert.Equal(StepExecutionState.Skipped, optional.State);
    }

    [Fact]
    public void Derived_status_reflects_the_step_list()
    {
        Assert.Equal(ProcessDerivedStatus.NotStarted, ProcessExecution.DeriveStatus([]));
        Assert.Equal(ProcessDerivedStatus.NotStarted, ProcessExecution.DeriveStatus([Pending()]));
        Assert.Equal(
            ProcessDerivedStatus.InProgress,
            ProcessExecution.DeriveStatus([Completed(), Pending()]));
        Assert.Equal(
            ProcessDerivedStatus.Completed,
            ProcessExecution.DeriveStatus([Completed()]));
        Assert.Equal(
            ProcessDerivedStatus.Completed,
            ProcessExecution.DeriveStatus([Completed(), SkippedOptional()]));
    }

    [Fact]
    public void Frontier_skips_resolved_steps_including_skipped_optional_ones()
    {
        IReadOnlyList<StepSnapshot> steps = [SkippedOptional(), Completed(), Pending(), Pending()];

        Assert.Equal(2, ProcessExecution.FrontierIndex(steps));
        Assert.Equal(2, ProcessExecution.ResolvedCount(steps));
        Assert.Null(ProcessExecution.FrontierIndex([Completed(), SkippedOptional()]));
    }

    [Fact]
    public void Contiguity_invariant_accepts_a_resolved_prefix_and_rejects_gaps()
    {
        Assert.True(ProcessExecution.ResolvedFormContiguousPrefix([Completed(), SkippedOptional(), Pending(), Pending()]));
        Assert.True(ProcessExecution.ResolvedFormContiguousPrefix([Pending(), Pending()]));
        Assert.True(ProcessExecution.ResolvedFormContiguousPrefix([]));

        // A resolved step after a pending step breaks the contiguous prefix.
        Assert.False(ProcessExecution.ResolvedFormContiguousPrefix([Completed(), Pending(), Completed()]));
        // A skipped step that is not optional is an invalid state.
        Assert.False(ProcessExecution.ResolvedFormContiguousPrefix([new StepSnapshot(StepExecutionState.Skipped, IsOptional: false)]));
    }

    [Fact]
    public async Task Sqlite_persists_processes_with_steps_and_cascades_on_deletion()
    {
        await using var fixture = await ProcessesFixture.CreateAsync();
        var category = new ProcessCategory { Name = "Administrative", NormalizedName = "ADMINISTRATIVE", SortOrder = 0, CreatedAt = Now, UpdatedAt = Now };
        fixture.Database.Add(category);
        await fixture.Database.SaveChangesAsync();

        var process = Process.Create(Values() with { CategoryId = category.Id }, new UserId(1), Now);
        fixture.Database.Add(process);
        await fixture.Database.SaveChangesAsync();

        fixture.Database.Add(Step.Create(process.Id, new StepValues("First", null, null, false), 0, new UserId(1), Now));
        fixture.Database.Add(Step.Create(process.Id, new StepValues("Second", null, null, true), 1, new UserId(1), Now));
        await fixture.Database.SaveChangesAsync();
        fixture.Database.ChangeTracker.Clear();

        Assert.Equal(2, await fixture.Database.Set<Step>().CountAsync(step => step.ProcessId == process.Id));

        // Deleting the process removes its owned steps.
        var stored = await fixture.Database.Set<Process>().SingleAsync();
        fixture.Database.Remove(stored);
        await fixture.Database.SaveChangesAsync();
        fixture.Database.ChangeTracker.Clear();

        Assert.Equal(0, await fixture.Database.Set<Step>().CountAsync());
        // The category survives because the process referenced it with a restrict delete.
        Assert.Equal(1, await fixture.Database.Set<ProcessCategory>().CountAsync());
    }

    [Fact]
    public async Task Sqlite_enforces_category_uniqueness_and_reference_restriction()
    {
        await using var fixture = await ProcessesFixture.CreateAsync();
        var category = new ProcessCategory { Name = "Administrative", NormalizedName = "ADMINISTRATIVE", SortOrder = 0, CreatedAt = Now, UpdatedAt = Now };
        fixture.Database.Add(category);
        await fixture.Database.SaveChangesAsync();

        var process = Process.Create(Values() with { CategoryId = category.Id }, new UserId(1), Now);
        fixture.Database.Add(process);
        await fixture.Database.SaveChangesAsync();
        fixture.Database.ChangeTracker.Clear();

        // The normalized name is unique case-insensitively.
        fixture.Database.Add(new ProcessCategory { Name = "administrative", NormalizedName = "ADMINISTRATIVE", SortOrder = 1, CreatedAt = Now, UpdatedAt = Now });
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Database.SaveChangesAsync());
        fixture.Database.ChangeTracker.Clear();

        // A referenced category cannot be deleted (restrict).
        var referenced = await fixture.Database.Set<ProcessCategory>().SingleAsync(value => value.Id == category.Id);
        fixture.Database.Remove(referenced);
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Database.SaveChangesAsync());
    }

    private static ProcessValues Values() => new(
        Name: "Renew passport",
        CategoryId: 1,
        DueDate: null,
        Notes: null,
        Visibility: RecordVisibility.Public);

    private static StepSnapshot Pending() => new(StepExecutionState.Pending, IsOptional: false);
    private static StepSnapshot Completed() => new(StepExecutionState.Completed, IsOptional: false);
    private static StepSnapshot SkippedOptional() => new(StepExecutionState.Skipped, IsOptional: true);

    private sealed class ProcessesFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private ProcessesFixture(SqliteConnection connection, SegarisDbContext database)
        {
            this.connection = connection;
            Database = database;
        }

        public SegarisDbContext Database { get; }

        public static async Task<ProcessesFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<SegarisDbContext>()
                .UseSqlite(connection)
                .EnableServiceProviderCaching(false)
                .Options;
            var database = new SegarisDbContext(
                options,
                [new IdentityModelContributor(), new ConfigurationModelContributor(), new ProcessesModelContributor()]);
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
            return new ProcessesFixture(connection, database);
        }

        public async ValueTask DisposeAsync()
        {
            await Database.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
