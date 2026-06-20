using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Persistence;
using Segaris.Api.Modules.Projects.Domain;
using Segaris.Api.Modules.Projects.Mutations;
using Segaris.Api.Modules.Projects.Persistence;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.UnitTests;

public sealed class ProjectsDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 20, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Program_trims_name_validates_code_and_stamps_audit()
    {
        var program = ProjectProgram.Create("  Infrastructure  ", "INFR", new UserId(1), Now);

        Assert.Equal("Infrastructure", program.Name);
        Assert.Equal("INFR", program.Code);
        Assert.Equal(1, program.CreatedBy);
        Assert.Equal(1, program.UpdatedBy);
        Assert.Equal(Now, program.CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Program_rejects_a_blank_name(string name)
    {
        Assert.Throws<ProjectsValidationException>(() =>
            ProjectProgram.Create(name, "INFR", new UserId(1), Now));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ABC")]
    [InlineData("ABCDE")]
    [InlineData("abcd")]
    [InlineData("AB12")]
    public void Program_and_axis_reject_invalid_codes(string? code)
    {
        Assert.Throws<ProjectsValidationException>(() =>
            ProjectProgram.Create("Infrastructure", code, new UserId(1), Now));
        Assert.Throws<ProjectsValidationException>(() =>
            ProjectAxis.Create(1, "Websites", code, new UserId(1), Now));
    }

    [Fact]
    public void Axis_requires_a_positive_parent_and_can_be_reassigned()
    {
        Assert.Throws<ProjectsValidationException>(() =>
            ProjectAxis.Create(0, "Websites", "WEBS", new UserId(1), Now));

        var axis = ProjectAxis.Create(1, "Websites", "WEBS", new UserId(1), Now);
        axis.ReplaceProgram(2, new UserId(2), Now.AddMinutes(1));

        Assert.Equal(2, axis.ProgramId);
        Assert.Equal(2, axis.UpdatedBy);
        Assert.Equal(Now.AddMinutes(1), axis.UpdatedAt);
    }

    [Fact]
    public void Project_and_activity_validate_values_and_keep_the_allocated_number()
    {
        var project = Project.Create(Values(), 1, new UserId(1), Now);
        var activity = Activity.Create(Values() with { Name = "Documentation" }, 2, new UserId(1), Now);

        project.Update(
            Values() with { AxisId = 2, Name = "Renamed", Status = ProjectStatus.Active },
            new UserId(2),
            Now.AddMinutes(1));

        Assert.Equal(1, project.Number);
        Assert.Equal(2, activity.Number);
        Assert.Equal(2, project.AxisId);
        Assert.Equal("Renamed", project.Name);
        Assert.Equal(ProjectStatus.Active, project.Status);
        Assert.Equal("INFRWEBS-000001 Renamed", project.Identifier("INFR", "WEBS"));
        Assert.Equal("HOMEOPSX-000001 Renamed", project.Identifier("HOME", "OPSX"));
    }

    [Fact]
    public void Project_rejects_unknown_status_or_visibility()
    {
        Assert.Throws<ProjectsValidationException>(() =>
            Project.Create(Values() with { Status = (ProjectStatus)42 }, 1, new UserId(1), Now));
        Assert.Throws<ProjectsValidationException>(() =>
            Project.Create(Values() with { Visibility = (RecordVisibility)42 }, 1, new UserId(1), Now));
    }

    [Fact]
    public void Only_the_creator_may_change_project_visibility()
    {
        var project = Project.Create(Values(), 1, new UserId(1), Now);

        var forbidden = Assert.Throws<ProjectsValidationException>(() =>
            project.Update(Values() with { Visibility = RecordVisibility.Private }, new UserId(2), Now));

        Assert.Equal(ProjectsValidationReason.VisibilityForbidden, forbidden.Reason);

        project.Update(Values() with { Visibility = RecordVisibility.Private }, new UserId(1), Now);
        Assert.Equal(RecordVisibility.Private, project.Visibility);
    }

    [Fact]
    public async Task Sqlite_persists_the_hierarchy_and_enforces_code_uniqueness()
    {
        await using var fixture = await ProjectsFixture.CreateAsync();
        var program = ProjectProgram.Create("Infrastructure", "INFR", new UserId(1), Now);
        fixture.Database.Add(program);
        await fixture.Database.SaveChangesAsync();
        var axis = ProjectAxis.Create(program.Id, "Websites", "WEBS", new UserId(1), Now);
        fixture.Database.Add(axis);
        await fixture.Database.SaveChangesAsync();
        var allocator = new ProjectNumberAllocator(fixture.Database, fixture.Clock);
        var projectNumber = await allocator.AllocateAsync(CancellationToken.None);
        var activityNumber = await allocator.AllocateAsync(CancellationToken.None);
        fixture.Database.Add(Project.Create(Values() with { AxisId = axis.Id }, projectNumber, new UserId(1), Now));
        fixture.Database.Add(Activity.Create(
            Values() with { AxisId = axis.Id, Name = "Paint room" },
            activityNumber,
            new UserId(1),
            Now));
        await fixture.Database.SaveChangesAsync();
        fixture.Database.ChangeTracker.Clear();

        var storedProject = await fixture.Database.Set<Project>().SingleAsync();
        var storedActivity = await fixture.Database.Set<Activity>().SingleAsync();
        Assert.Equal(projectNumber, storedProject.Number);
        Assert.Equal(activityNumber, storedActivity.Number);
        Assert.NotEqual(storedProject.Number, storedActivity.Number);

        fixture.Database.Add(ProjectProgram.Create("Duplicate", "INFR", new UserId(1), Now));
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Database.SaveChangesAsync());
    }

    [Fact]
    public async Task Number_allocator_assigns_unique_monotonic_numbers_under_concurrent_requests()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"segaris-projects-{Guid.NewGuid():N}.db");
        try
        {
            await using (var fixture = await ProjectsFixture.CreateAsync(databasePath))
            {
                var tasks = Enumerable.Range(0, 8)
                    .Select(_ => AllocateWithIndependentContextAsync(databasePath))
                    .ToArray();

                var numbers = await Task.WhenAll(tasks);

                Assert.Equal(Enumerable.Range(1, 8), numbers.Order());
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }

    private static async Task<int> AllocateWithIndependentContextAsync(string databasePath)
    {
        await using var fixture = await ProjectsFixture.CreateAsync(databasePath, ensureCreated: false);
        return await new ProjectNumberAllocator(fixture.Database, fixture.Clock)
            .AllocateAsync(CancellationToken.None);
    }

    private static ProjectItemValues Values() => new(
        AxisId: 1,
        Name: "Cellar renovation",
        Status: ProjectsDefaults.Status,
        Visibility: ProjectsDefaults.Visibility);

    private sealed class ProjectsFixture : IAsyncDisposable
    {
        private readonly SqliteConnection? connection;

        private ProjectsFixture(SqliteConnection? connection, SegarisDbContext database, MutableClock clock)
        {
            this.connection = connection;
            Database = database;
            Clock = clock;
        }

        public SegarisDbContext Database { get; }
        public MutableClock Clock { get; }

        public static async Task<ProjectsFixture> CreateAsync() =>
            await CreateCoreAsync(databasePath: null, ensureCreated: true);

        public static async Task<ProjectsFixture> CreateAsync(string databasePath, bool ensureCreated = true) =>
            await CreateCoreAsync(databasePath, ensureCreated);

        private static async Task<ProjectsFixture> CreateCoreAsync(string? databasePath, bool ensureCreated)
        {
            SqliteConnection? connection = null;
            DbContextOptionsBuilder<SegarisDbContext> optionsBuilder = new();
            if (databasePath is null)
            {
                connection = new SqliteConnection("Data Source=:memory:");
                await connection.OpenAsync();
                optionsBuilder.UseSqlite(connection);
            }
            else
            {
                optionsBuilder.UseSqlite($"Data Source={databasePath}");
            }

            var database = new SegarisDbContext(
                optionsBuilder.EnableServiceProviderCaching(false).Options,
                [new IdentityModelContributor(), new ConfigurationModelContributor(), new ProjectsModelContributor()]);
            if (ensureCreated)
            {
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
            }

            return new ProjectsFixture(connection, database, new MutableClock { UtcNow = Now });
        }

        public async ValueTask DisposeAsync()
        {
            await Database.DisposeAsync();
            if (connection is not null)
            {
                await connection.DisposeAsync();
            }
        }
    }

    private sealed class MutableClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}
