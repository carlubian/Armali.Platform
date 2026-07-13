using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Wellness.Contracts;
using Segaris.Api.Modules.Wellness.Domain;
using Segaris.Persistence;
using Segaris.Shared.Time;

namespace Segaris.Api.IntegrationTests.Wellness;

public sealed class WellnessDayApiTests
{
    [Fact]
    public async Task Today_requires_authentication_and_empty_catalogue_generates_empty_day()
    {
        using var server = CreateServer(new DateOnly(2026, 7, 13));
        using var anonymous = server.CreateClient();

        using var unauthorized = await anonymous.GetAsync("/api/wellness/today", CancellationToken.None);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        using var client = await server.CreateAuthenticatedClientAsync();
        var today = await client.GetFromJsonAsync<WellnessTodayResponse>("/api/wellness/today", CancellationToken.None);

        Assert.NotNull(today);
        Assert.Equal(new DateOnly(2026, 7, 13), today.Date);
        Assert.Null(today.Score);
        Assert.Empty(today.Tasks);
    }

    [Fact]
    public async Task First_read_generates_stable_same_day_snapshot()
    {
        using var server = CreateServer(new DateOnly(2026, 7, 13));
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        await SeedTaskPoolAsync(client, csrf);

        var first = await client.GetFromJsonAsync<WellnessTodayResponse>("/api/wellness/today", CancellationToken.None);
        var second = await client.GetFromJsonAsync<WellnessTodayResponse>("/api/wellness/today", CancellationToken.None);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(0, first.Score);
        Assert.Equal(WellnessDefaults.DailyTaskCount, first.Tasks.Count);
        Assert.Equal(first.Tasks.Select(task => task.Id), second.Tasks.Select(task => task.Id));
        Assert.Equal(first.Tasks.Select(task => task.Name), second.Tasks.Select(task => task.Name));
        Assert.Contains(first.Tasks, task => task.Category == nameof(WellnessCategory.HealthAndBody));
        Assert.Contains(first.Tasks, task => task.Category == nameof(WellnessCategory.MindAndSleep));
        Assert.Contains(first.Tasks, task => task.Category == nameof(WellnessCategory.PeopleAndWork));
    }

    [Fact]
    public async Task Day_rollover_generates_a_new_day_for_the_new_household_date()
    {
        var clock = new MutableClock(new DateOnly(2026, 7, 13));
        using var server = CreateServer(clock);
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        await SeedTaskPoolAsync(client, csrf);

        var first = await client.GetFromJsonAsync<WellnessTodayResponse>("/api/wellness/today", CancellationToken.None);
        clock.SetMadridDate(new DateOnly(2026, 7, 14));
        var second = await client.GetFromJsonAsync<WellnessTodayResponse>("/api/wellness/today", CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 7, 13), first!.Date);
        Assert.Equal(new DateOnly(2026, 7, 14), second!.Date);

        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var ownerId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        Assert.Equal(2, await database.Set<WellnessDay>().CountAsync(day => day.CreatedBy == ownerId));
    }

    [Fact]
    public async Task Concurrent_first_reads_create_one_day()
    {
        using var server = CreateServer(new DateOnly(2026, 7, 13));
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        await SeedTaskPoolAsync(client, csrf);

        var reads = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ =>
            client.GetFromJsonAsync<WellnessTodayResponse>("/api/wellness/today", CancellationToken.None)));

        Assert.All(reads, response => Assert.NotNull(response));
        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var ownerId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        Assert.Equal(1, await database.Set<WellnessDay>().CountAsync(day => day.CreatedBy == ownerId));
    }

    [Fact]
    public async Task Toggle_updates_score_and_rejects_other_users_day_tasks()
    {
        using var server = CreateServer(new DateOnly(2026, 7, 13));
        await server.CreateUserAsync("member", "MemberPass123!");
        using var admin = await server.CreateAuthenticatedClientAsync();
        var adminCsrf = await CapexTestServer.GetCsrfTokenAsync(admin);
        await SeedTaskPoolAsync(admin, adminCsrf);

        using var member = await server.CreateAuthenticatedClientAsync("member", "MemberPass123!");
        var memberToday = await member.GetFromJsonAsync<WellnessTodayResponse>("/api/wellness/today", CancellationToken.None);
        var memberTaskId = memberToday!.Tasks[0].Id;

        var adminToday = await admin.GetFromJsonAsync<WellnessTodayResponse>("/api/wellness/today", CancellationToken.None);
        var adminTaskId = adminToday!.Tasks[0].Id;

        using var toggled = await PostAsync(admin, $"/api/wellness/today/tasks/{adminTaskId}/toggle", adminCsrf);
        Assert.Equal(HttpStatusCode.OK, toggled.StatusCode);
        var updated = await toggled.Content.ReadFromJsonAsync<WellnessTodayResponse>(CancellationToken.None);
        Assert.Equal(17, updated!.Score);
        Assert.True(updated.Tasks.Single(task => task.Id == adminTaskId).Completed);

        using var forbiddenByScope = await PostAsync(admin, $"/api/wellness/today/tasks/{memberTaskId}/toggle", adminCsrf);
        Assert.Equal(HttpStatusCode.NotFound, forbiddenByScope.StatusCode);
        var problem = await forbiddenByScope.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("wellness.day_task.not_found", problem!.Code);
    }

    [Fact]
    public async Task Days_range_returns_existing_scores_for_current_user_only()
    {
        using var server = CreateServer(new DateOnly(2026, 7, 13));
        await server.CreateUserAsync("member", "MemberPass123!");
        using var admin = await server.CreateAuthenticatedClientAsync();
        var adminCsrf = await CapexTestServer.GetCsrfTokenAsync(admin);
        await SeedTaskPoolAsync(admin, adminCsrf);

        var adminToday = await admin.GetFromJsonAsync<WellnessTodayResponse>("/api/wellness/today", CancellationToken.None);
        await PostAsync(admin, $"/api/wellness/today/tasks/{adminToday!.Tasks[0].Id}/toggle", adminCsrf);

        using var member = await server.CreateAuthenticatedClientAsync("member", "MemberPass123!");
        _ = await member.GetFromJsonAsync<WellnessTodayResponse>("/api/wellness/today", CancellationToken.None);

        var range = await admin.GetFromJsonAsync<WellnessDayListResponse>(
            "/api/wellness/days?from=2026-07-12&to=2026-07-14",
            CancellationToken.None);

        Assert.NotNull(range);
        Assert.Equal(new DateOnly(2026, 7, 12), range.From);
        Assert.Equal(new DateOnly(2026, 7, 14), range.To);
        var day = Assert.Single(range.Days);
        Assert.Equal(new DateOnly(2026, 7, 13), day.Date);
        Assert.Equal(17, day.Score);
    }

    [Fact]
    public async Task Days_range_validates_bounds()
    {
        using var server = CreateServer(new DateOnly(2026, 7, 13));
        using var client = await server.CreateAuthenticatedClientAsync();

        using var missing = await client.GetAsync("/api/wellness/days?from=2026-07-13", CancellationToken.None);
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);

        using var reversed = await client.GetAsync(
            "/api/wellness/days?from=2026-07-14&to=2026-07-13",
            CancellationToken.None);
        Assert.Equal(HttpStatusCode.BadRequest, reversed.StatusCode);
        var problem = await reversed.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("wellness.day.range_validation", problem!.Code);
    }

    private static async Task SeedTaskPoolAsync(HttpClient client, string csrf)
    {
        await CreateTaskAsync(client, csrf, "Drink water", "HealthAndBody");
        await CreateTaskAsync(client, csrf, "Move", "HealthAndBody");
        await CreateTaskAsync(client, csrf, "Stretch", "HealthAndBody");
        await CreateTaskAsync(client, csrf, "Meditate", "MindAndSleep");
        await CreateTaskAsync(client, csrf, "Read", "MindAndSleep");
        await CreateTaskAsync(client, csrf, "Wind down", "MindAndSleep");
        await CreateTaskAsync(client, csrf, "Call someone", "PeopleAndWork");
        await CreateTaskAsync(client, csrf, "Tidy desk", "PeopleAndWork");
        await CreateTaskAsync(client, csrf, "Close a loop", "PeopleAndWork");
    }

    private static async Task CreateTaskAsync(HttpClient client, string csrf, string name, string category)
    {
        using var response = await CapexApi.PostJsonAsync(
            client,
            "/api/wellness/tasks",
            new CreateWellnessTaskRequest(name, category),
            csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string route, string csrf)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, route);
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        return await client.SendAsync(request, CancellationToken.None);
    }

    private static CapexTestServer CreateServer(DateOnly madridToday) =>
        CreateServer(new MutableClock(madridToday));

    private static CapexTestServer CreateServer(MutableClock clock) =>
        new(configureServices: services =>
        {
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(clock);
        });

    private sealed record ProblemPayload(string? Code);

    private sealed class MutableClock(DateOnly madridToday) : IClock
    {
        public DateTimeOffset UtcNow { get; private set; } = ToUtc(madridToday);

        public void SetMadridDate(DateOnly date)
        {
            UtcNow = ToUtc(date);
        }

        private static DateTimeOffset ToUtc(DateOnly date) =>
            new DateTimeOffset(date.Year, date.Month, date.Day, 10, 0, 0, TimeSpan.FromHours(1)).ToUniversalTime();
    }
}
