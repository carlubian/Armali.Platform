using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Wellness.Contracts;
using Segaris.Api.Modules.Wellness.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;

namespace Segaris.Api.IntegrationTests.Wellness;

/// <summary>
/// Wave 2 coverage for the administrator-managed Wellness task catalogue surfaced
/// through the Configuration presentation boundary: creation-order reads, admin
/// create and delete, name and category validation, impact-free (snapshot-preserving)
/// deletion, and administrator authorization on writes.
/// </summary>
public sealed class WellnessTaskCatalogueTests
{
    [Fact]
    public async Task Tasks_require_authentication_and_start_empty()
    {
        using var server = new CapexTestServer();
        using var anonymous = server.CreateClient();

        using var unauthorized = await anonymous.GetAsync("/api/wellness/tasks", CancellationToken.None);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        using var client = await server.CreateAuthenticatedClientAsync();
        var tasks = await client.GetFromJsonAsync<WellnessTaskResponse[]>("/api/wellness/tasks", CancellationToken.None);
        Assert.NotNull(tasks);
        Assert.Empty(tasks);
    }

    [Fact]
    public async Task Write_routes_reject_normal_users()
    {
        using var server = new CapexTestServer();
        await server.CreateUserAsync("member", "MemberPass123!");
        using var client = await server.CreateAuthenticatedClientAsync("member", "MemberPass123!");
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var create = await CapexApi.PostJsonAsync(
            client,
            "/api/wellness/tasks",
            new CreateWellnessTaskRequest("Drink water", "HealthAndBody"),
            csrf);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);

        using var delete = await CapexApi.DeleteAsync(client, "/api/wellness/tasks/1", csrf);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);

        // Reading the catalogue is available to any authenticated user.
        var tasks = await client.GetFromJsonAsync<WellnessTaskResponse[]>("/api/wellness/tasks", CancellationToken.None);
        Assert.NotNull(tasks);
    }

    [Fact]
    public async Task Admin_can_create_tasks_that_list_in_creation_order()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        var first = await CreateAsync(client, csrf, "Drink water", "HealthAndBody");
        var second = await CreateAsync(client, csrf, "Meditate", "MindAndSleep");
        var third = await CreateAsync(client, csrf, "Call a friend", "PeopleAndWork");

        Assert.Equal((0, 1, 2), (first.SortOrder, second.SortOrder, third.SortOrder));

        var tasks = await client.GetFromJsonAsync<WellnessTaskResponse[]>("/api/wellness/tasks", CancellationToken.None);
        Assert.NotNull(tasks);
        Assert.Equal([first.Id, second.Id, third.Id], tasks.Select(task => task.Id).ToArray());
        Assert.Equal(["Drink water", "Meditate", "Call a friend"], tasks.Select(task => task.Name).ToArray());
        Assert.Equal(
            ["HealthAndBody", "MindAndSleep", "PeopleAndWork"],
            tasks.Select(task => task.Category).ToArray());
    }

    [Fact]
    public async Task Blank_name_and_unknown_category_fail_validation()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var blankName = await CapexApi.PostJsonAsync(
            client,
            "/api/wellness/tasks",
            new CreateWellnessTaskRequest("   ", "HealthAndBody"),
            csrf);
        Assert.Equal(HttpStatusCode.BadRequest, blankName.StatusCode);
        var nameProblem = await blankName.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("wellness.task.validation", nameProblem!.Code);
        Assert.True(nameProblem.Errors!.ContainsKey("name"));

        using var badCategory = await CapexApi.PostJsonAsync(
            client,
            "/api/wellness/tasks",
            new CreateWellnessTaskRequest("Drink water", "NotACategory"),
            csrf);
        Assert.Equal(HttpStatusCode.BadRequest, badCategory.StatusCode);
        var categoryProblem = await badCategory.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("wellness.task.validation", categoryProblem!.Code);
        Assert.True(categoryProblem.Errors!.ContainsKey("category"));

        using var missingCategory = await CapexApi.PostJsonAsync(
            client,
            "/api/wellness/tasks",
            new CreateWellnessTaskRequest("Drink water", null),
            csrf);
        Assert.Equal(HttpStatusCode.BadRequest, missingCategory.StatusCode);
    }

    [Fact]
    public async Task Deleting_a_missing_task_returns_not_found()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.DeleteAsync(client, "/api/wellness/tasks/999", csrf);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("wellness.task.not_found", problem!.Code);
    }

    [Fact]
    public async Task Deleting_a_task_leaves_persisted_day_snapshots_intact()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        var task = await CreateAsync(client, csrf, "Drink water", "HealthAndBody");
        var ownerId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);

        // Persist a day that snapshotted the catalogue task, mirroring a generated day.
        await using (var scope = server.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
            var now = new DateTimeOffset(2026, 7, 13, 8, 0, 0, TimeSpan.Zero);
            var day = WellnessDay.Create(new DateOnly(2026, 7, 13), new UserId(ownerId), now, score: 0);
            database.Add(day);
            await database.SaveChangesAsync();

            database.Add(WellnessDayTask.CreateSnapshot(
                day.Id,
                task.Name,
                WellnessCategory.HealthAndBody,
                position: 0));
            await database.SaveChangesAsync();
        }

        using var delete = await CapexApi.DeleteAsync(client, $"/api/wellness/tasks/{task.Id}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var tasks = await client.GetFromJsonAsync<WellnessTaskResponse[]>("/api/wellness/tasks", CancellationToken.None);
        Assert.Empty(tasks!);

        await using (var scope = server.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
            Assert.False(await database.Set<WellnessTask>().AnyAsync(existing => existing.Id == task.Id));

            var snapshot = await database.Set<WellnessDayTask>().SingleAsync();
            Assert.Equal("Drink water", snapshot.Name);
            Assert.Equal(WellnessCategory.HealthAndBody, snapshot.Category);
        }
    }

    private static async Task<WellnessTaskResponse> CreateAsync(
        HttpClient client,
        string csrf,
        string name,
        string category)
    {
        using var response = await CapexApi.PostJsonAsync(
            client,
            "/api/wellness/tasks",
            new CreateWellnessTaskRequest(name, category),
            csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<WellnessTaskResponse>(CancellationToken.None))!;
    }

    private sealed record ProblemPayload(string? Code, Dictionary<string, string[]>? Errors);
}
